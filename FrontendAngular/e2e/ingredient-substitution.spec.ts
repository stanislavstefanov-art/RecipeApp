import { expect, test } from '@playwright/test';

const ID = '99999999-9999-9999-9999-999999999999';
const DETAIL = `http://localhost:5117/api/recipes/${ID}`;
const SUGGEST = 'http://localhost:5117/api/recipes/suggest-substitutions';

const RECIPE_BODY = JSON.stringify({
  id: ID,
  name: 'Chocolate Cake',
  ingredients: [],
  steps: [],
});

function makeResult(overrides: object = {}) {
  return {
    originalIngredient: 'butter',
    substitutes: [
      {
        name: 'coconut oil',
        reason: 'Similar fat content, works well in baking.',
        quantityAdjustment: 'Use ¾ the amount.',
        isDirectReplacement: false,
      },
      {
        name: 'margarine',
        reason: 'Direct 1:1 replacement for butter.',
        isDirectReplacement: true,
      },
    ],
    confidence: 0.9,
    needsReview: false,
    notes: null,
    ...overrides,
  };
}

test.describe('ingredient substitution', () => {
  test('empty ingredient name shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      requested = true;
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });

    await page.goto(`/recipes/${ID}`);
    await page.locator('app-suggest-substitutions-form').getByRole('button', { name: 'Find substitutes' }).click();

    await expect(page.getByText('Ingredient name is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('sends correct request body and renders substitutes list', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      expect(route.request().postDataJSON()).toEqual({
        ingredientName: 'butter',
        recipeContext: 'chocolate cake',
        dietaryGoal: 'vegan',
      });
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult()),
      });
    });

    const form = page.locator('app-suggest-substitutions-form');
    await page.goto(`/recipes/${ID}`);
    await form.getByLabel('Ingredient name').fill('butter');
    await form.getByLabel('Recipe context').fill('chocolate cake');
    await form.getByLabel('Dietary goal').fill('vegan');
    await form.getByRole('button', { name: 'Find substitutes' }).click();

    await expect(page.getByText('coconut oil')).toBeVisible();
    await expect(page.getByText('margarine')).toBeVisible();
  });

  test('isDirectReplacement:true shows "Direct replacement" badge', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult()),
      });
    });

    const form = page.locator('app-suggest-substitutions-form');
    await page.goto(`/recipes/${ID}`);
    await form.getByLabel('Ingredient name').fill('butter');
    await form.getByRole('button', { name: 'Find substitutes' }).click();

    await expect(form.getByText('Direct replacement')).toBeVisible();
    await expect(form.getByText('Partial substitution')).toBeVisible();
  });

  test('needsReview:true renders advisory note', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult({ needsReview: true })),
      });
    });

    const form = page.locator('app-suggest-substitutions-form');
    await page.goto(`/recipes/${ID}`);
    await form.getByLabel('Ingredient name').fill('butter');
    await form.getByRole('button', { name: 'Find substitutes' }).click();

    await expect(form.getByText(/may need review/)).toBeVisible();
  });

  test('notes are rendered below the substitutes list', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(makeResult({ notes: 'Best results with unsalted varieties.' })),
      });
    });

    const form = page.locator('app-suggest-substitutions-form');
    await page.goto(`/recipes/${ID}`);
    await form.getByLabel('Ingredient name').fill('butter');
    await form.getByRole('button', { name: 'Find substitutes' }).click();

    await expect(form.getByText('Best results with unsalted varieties.')).toBeVisible();
  });

  test('on 400 shows error message and keeps form interactive', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Validation failed', detail: 'Ingredient name is required.' }),
      });
    });

    const form = page.locator('app-suggest-substitutions-form');
    await page.goto(`/recipes/${ID}`);
    await form.getByLabel('Ingredient name').fill('butter');
    await form.getByRole('button', { name: 'Find substitutes' }).click();

    await expect(form.locator('[role="alert"]')).toContainText('Ingredient name is required.');
    await expect(form.getByLabel('Ingredient name')).toBeEditable();
    await expect(form.getByRole('button', { name: 'Find substitutes' })).toBeEnabled();
  });

  test('on 500 shows error message and keeps form interactive', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: RECIPE_BODY });
    });
    await page.route(SUGGEST, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    const form = page.locator('app-suggest-substitutions-form');
    await page.goto(`/recipes/${ID}`);
    await form.getByLabel('Ingredient name').fill('butter');
    await form.getByRole('button', { name: 'Find substitutes' }).click();

    await expect(form.locator('[role="alert"]')).toBeVisible();
    await expect(form.getByRole('button', { name: 'Find substitutes' })).toBeEnabled();
  });
});
