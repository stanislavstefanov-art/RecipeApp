import { expect, test } from '@playwright/test';

const ID = '77777777-7777-7777-7777-777777777777';
const DETAIL = `http://localhost:5117/api/recipes/${ID}`;
const STEPS = `http://localhost:5117/api/recipes/${ID}/steps`;

test.describe('add step', () => {
  test('empty instruction shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(STEPS, async (route) => {
      requested = true;
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Instruction').fill('');
    await page.getByRole('button', { name: 'Add step' }).click();

    await expect(page.getByText('Instruction is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('1001-character instruction shows inline error and issues no request', async ({
    page,
  }) => {
    let requested = false;
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(STEPS, async (route) => {
      requested = true;
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Instruction').fill('a'.repeat(1001));
    await page.getByRole('button', { name: 'Add step' }).click();

    await expect(
      page.getByText('Instruction must be 1000 characters or fewer.'),
    ).toBeVisible();
    expect(requested).toBe(false);
  });

  test('on 204 the new step appears and the form resets', async ({ page }) => {
    let getCount = 0;
    await page.route(DETAIL, async (route) => {
      getCount += 1;
      const steps = getCount === 1 ? [] : [{ order: 1, instruction: 'Mix it.' }];
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Cake',
          ingredients: [],
          steps,
        }),
      });
    });
    await page.route(STEPS, async (route) => {
      expect(route.request().method()).toBe('POST');
      expect(route.request().postDataJSON()).toEqual({ instruction: 'Mix it.' });
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await expect(page.getByText('No steps')).toBeVisible();

    await page.getByLabel('Instruction').fill('Mix it.');
    await page.getByRole('button', { name: 'Add step' }).click();

    await expect(page.getByText('Mix it.')).toBeVisible();
    await expect(page.getByLabel('Instruction')).toHaveValue('');
  });

  test('on 400 shows a server error and preserves the instruction', async ({
    page,
  }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(STEPS, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'Validation failed',
          detail: 'Instruction cannot be blank.',
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Instruction').fill('Some instruction');
    await page.getByRole('button', { name: 'Add step' }).click();

    const alert = page.locator('app-add-step-form [role="alert"]');
    await expect(alert).toContainText('Instruction cannot be blank.');
    await expect(page.getByLabel('Instruction')).toHaveValue('Some instruction');
  });

  test('on 500 shows a server error and keeps form interactive', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(STEPS, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Instruction').fill('Mix it.');
    await page.getByRole('button', { name: 'Add step' }).click();

    const alert = page.locator('app-add-step-form [role="alert"]');
    await expect(alert).toBeVisible();
    await expect(page.getByLabel('Instruction')).toBeEditable();
    await expect(page.getByRole('button', { name: 'Add step' })).toBeEnabled();
  });
});
