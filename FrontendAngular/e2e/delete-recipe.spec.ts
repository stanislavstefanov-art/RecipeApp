import { expect, test } from '@playwright/test';

const ID = '88888888-8888-8888-8888-888888888888';
const DETAIL = `http://localhost:5117/api/recipes/${ID}`;
const DELETE = `http://localhost:5117/api/recipes/${ID}`;

const RECIPE_BODY = JSON.stringify({
  id: ID,
  name: 'Pancakes',
  ingredients: [],
  steps: [],
});

test.describe('delete recipe', () => {
  test('delete button is visible on the details page', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
      } else {
        await route.continue();
      }
    });

    await page.goto(`/recipes/${ID}`);
    await expect(page.getByRole('button', { name: 'Delete recipe' })).toBeVisible();
  });

  test('dismissing confirm dialog issues no request and keeps user on page', async ({
    page,
  }) => {
    let deleteRequested = false;
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
      } else {
        deleteRequested = true;
        await route.fulfill({ status: 204, body: '' });
      }
    });

    page.on('dialog', (dialog) => dialog.dismiss());

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('button', { name: 'Delete recipe' }).click();

    expect(deleteRequested).toBe(false);
    await expect(page).toHaveURL(`/recipes/${ID}`);
  });

  test('on 204 navigates to /recipes', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
      } else {
        await route.fulfill({ status: 204, body: '' });
      }
    });
    await page.route('http://localhost:5117/api/recipes', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    page.on('dialog', (dialog) => dialog.accept());

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('button', { name: 'Delete recipe' }).click();

    await expect(page).toHaveURL('/recipes');
  });

  test('button is disabled while deleting', async ({ page }) => {
    let resolveDelete!: () => void;
    const deleteStarted = new Promise<void>((res) => {
      resolveDelete = res;
    });

    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
      } else {
        resolveDelete();
        await new Promise((r) => setTimeout(r, 2000));
        await route.fulfill({ status: 204, body: '' });
      }
    });
    await page.route('http://localhost:5117/api/recipes', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    page.on('dialog', (dialog) => dialog.accept());

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('button', { name: 'Delete recipe' }).click();

    await deleteStarted;
    await expect(page.getByRole('button', { name: 'Deleting…' })).toBeDisabled();
  });

  test('on 404 shows error message and keeps user on page', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
      } else {
        await route.fulfill({
          status: 404,
          contentType: 'application/problem+json',
          body: JSON.stringify({ title: 'Not found', detail: 'Recipe not found.' }),
        });
      }
    });

    page.on('dialog', (dialog) => dialog.accept());

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('button', { name: 'Delete recipe' }).click();

    await expect(page.locator('[role="alert"]')).toContainText('Recipe not found.');
    await expect(page).toHaveURL(`/recipes/${ID}`);
  });

  test('on 500 shows error message and keeps button interactive', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
      } else {
        await route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({ title: 'Server error' }),
        });
      }
    });

    page.on('dialog', (dialog) => dialog.accept());

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('button', { name: 'Delete recipe' }).click();

    await expect(page.locator('[role="alert"]')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Delete recipe' })).toBeEnabled();
  });
});
