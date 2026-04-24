import { expect, test } from '@playwright/test';

const HOUSEHOLDS_URL = 'http://localhost:5117/api/households';
const NEW_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

test.describe('households create', () => {
  test('blank name shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(HOUSEHOLDS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        requested = true;
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: NEW_ID, name: '' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/households/new');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('Name is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('on 201 navigates to /households/:id', async ({ page }) => {
    await page.route(HOUSEHOLDS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as { name: string };
        expect(body.name).toBe('Smith Family');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: NEW_ID, name: 'Smith Family' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.route(`http://localhost:5117/api/households/${NEW_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: NEW_ID, name: 'Smith Family', members: [] }),
      });
    });

    await page.route('http://localhost:5117/api/persons', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/households/new');
    await page.getByLabel('Name').fill('Smith Family');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page).toHaveURL(`/households/${NEW_ID}`);
  });

  test('on 400 shows error and keeps form interactive', async ({ page }) => {
    await page.route(HOUSEHOLDS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 400,
          contentType: 'application/problem+json',
          body: JSON.stringify({ title: 'Validation failed', detail: 'Name is required.' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/households/new');
    await page.getByLabel('Name').fill('Test');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toContainText('Name is required.');
    await expect(page.getByLabel('Name')).toBeEditable();
  });
});
