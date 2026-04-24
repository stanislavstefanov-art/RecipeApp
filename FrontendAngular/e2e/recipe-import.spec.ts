import { expect, test } from '@playwright/test';

const IMPORT_URL = 'http://localhost:5117/api/recipes/import';
const LIST_URL = 'http://localhost:5117/api/recipes';

function makeResult(overrides: object = {}) {
  return {
    title: 'Classic Pancakes',
    servings: 4,
    ingredients: [
      { name: 'flour', quantity: '200', unit: 'g' },
      { name: 'milk', quantity: '300', unit: 'ml', notes: 'whole milk preferred' },
    ],
    steps: ['Mix dry ingredients.', 'Add milk and stir.', 'Cook on medium heat.'],
    notes: null,
    confidence: 0.95,
    needsReview: false,
    ...overrides,
  };
}

test.describe('recipe import', () => {
  test('Import recipe link on list page navigates to /recipes/import', async ({ page }) => {
    await page.route(LIST_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/recipes');
    await page.getByRole('link', { name: 'Import recipe' }).click();

    await expect(page).toHaveURL('/recipes/import');
  });

  test('short text (< 10 chars) shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(IMPORT_URL, async (route) => {
      requested = true;
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });

    await page.goto('/recipes/import');
    await page.getByLabel('Recipe text').fill('short');
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByText('Recipe text must be at least 10 characters.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('blank text shows required error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(IMPORT_URL, async (route) => {
      requested = true;
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });

    await page.goto('/recipes/import');
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByText('Recipe text is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('sends correct body and renders extraction preview', async ({ page }) => {
    const inputText = 'Mix flour, milk and eggs. Cook on medium heat for 2 minutes each side.';
    await page.route(IMPORT_URL, async (route) => {
      expect(route.request().postDataJSON()).toEqual({ text: inputText });
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult()),
      });
    });

    await page.goto('/recipes/import');
    await page.getByLabel('Recipe text').fill(inputText);
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByText('Classic Pancakes')).toBeVisible();
    await expect(page.getByText('Serves 4')).toBeVisible();
    await expect(page.getByText('flour')).toBeVisible();
    await expect(page.getByText('Mix dry ingredients.')).toBeVisible();
  });

  test('needsReview:true renders advisory note', async ({ page }) => {
    await page.route(IMPORT_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult({ needsReview: true })),
      });
    });

    await page.goto('/recipes/import');
    await page.getByLabel('Recipe text').fill('Some recipe text here to pass validation check.');
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByText(/may need review/)).toBeVisible();
  });

  test('notes are rendered in the preview', async ({ page }) => {
    await page.route(IMPORT_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult({ notes: 'Best served warm with maple syrup.' })),
      });
    });

    await page.goto('/recipes/import');
    await page.getByLabel('Recipe text').fill('Some recipe text here to pass validation check.');
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByText('Best served warm with maple syrup.')).toBeVisible();
  });

  test('on 400 shows error message and textarea retains content', async ({ page }) => {
    await page.route(IMPORT_URL, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Validation failed', detail: 'Text is too short.' }),
      });
    });

    await page.goto('/recipes/import');
    await page.getByLabel('Recipe text').fill('Some recipe text here to pass validation check.');
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByRole('alert')).toContainText('Text is too short.');
    await expect(page.getByLabel('Recipe text')).toHaveValue('Some recipe text here to pass validation check.');
  });

  test('on 500 shows error message and keeps form interactive', async ({ page }) => {
    await page.route(IMPORT_URL, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto('/recipes/import');
    await page.getByLabel('Recipe text').fill('Some recipe text here to pass validation check.');
    await page.getByRole('button', { name: 'Extract' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByLabel('Recipe text')).toBeEditable();
    await expect(page.getByRole('button', { name: 'Extract' })).toBeEnabled();
  });
});
