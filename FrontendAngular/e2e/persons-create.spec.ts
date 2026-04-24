import { expect, test } from '@playwright/test';

const PERSONS_URL = 'http://localhost:5117/api/persons';
const NEW_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc';

test.describe('persons create', () => {
  test('blank name shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(PERSONS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        requested = true;
        await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify({ id: NEW_ID, name: '' }) });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/persons/new');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('Name is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('sends correct body with dietary and health selections then navigates to /persons', async ({
    page,
  }) => {
    await page.route(PERSONS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as {
          name: string;
          dietaryPreferences: number[];
          healthConcerns: number[];
          notes?: string;
        };
        expect(body.name).toBe('Alice');
        expect(body.dietaryPreferences).toEqual([1, 3]);
        expect(body.healthConcerns).toEqual([1]);
        expect(body.notes).toBeUndefined();
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: NEW_ID, name: 'Alice' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/persons/new');
    await page.getByLabel('Name').fill('Alice');
    await page.getByLabel('Vegetarian').check();
    await page.getByLabel('Vegan').check();
    await page.getByLabel('Diabetes').check();
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page).toHaveURL('/persons');
  });

  test('notes field is included in request when filled', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as { notes?: string };
        expect(body.notes).toBe('Prefers spicy food.');
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: NEW_ID, name: 'Bob' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/persons/new');
    await page.getByLabel('Name').fill('Bob');
    await page.getByLabel('Notes').fill('Prefers spicy food.');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page).toHaveURL('/persons');
  });

  test('on 400 shows error and keeps form interactive', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
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

    await page.goto('/persons/new');
    await page.getByLabel('Name').fill('Alice');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toContainText('Name is required.');
    await expect(page.getByLabel('Name')).toBeEditable();
  });

  test('on 500 shows error and keeps form interactive', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({ title: 'Server error' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });

    await page.goto('/persons/new');
    await page.getByLabel('Name').fill('Alice');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create' })).toBeEnabled();
  });
});
