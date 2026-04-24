import { expect, test } from '@playwright/test';

const ID = '44444444-4444-4444-4444-444444444444';
const DETAIL = `http://localhost:5117/api/recipes/${ID}`;

test.describe('recipe details', () => {
  test('renders name, ingredients, and steps on success', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Pancakes',
          ingredients: [
            { name: 'Flour', quantity: 200, unit: 'g' },
            { name: 'Milk', quantity: 300, unit: 'ml' },
          ],
          steps: [
            { order: 1, instruction: 'Mix dry ingredients.' },
            { order: 2, instruction: 'Whisk in the milk.' },
          ],
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);

    await expect(page.getByRole('heading', { name: 'Pancakes' })).toBeVisible();
    await expect(page.getByText('Flour', { exact: true })).toBeVisible();
    await expect(page.getByText('200 g')).toBeVisible();
    await expect(page.getByText('Milk', { exact: true })).toBeVisible();
    await expect(page.getByText('300 ml')).toBeVisible();
    await expect(page.getByText('Mix dry ingredients.')).toBeVisible();
    await expect(page.getByText('Whisk in the milk.')).toBeVisible();
  });

  test('shows the not-found state on 404 (distinct from generic error)', async ({
    page,
  }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'Not Found',
          detail: `Recipe '${ID}' was not found.`,
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);

    await expect(page.getByRole('heading', { name: 'Recipe not found' })).toBeVisible();
    await expect(page.getByText('Failed to load recipe')).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Back to recipes/ })).toBeVisible();
  });

  test('shows the generic error state on 500', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto(`/recipes/${ID}`);

    await expect(page.getByText('Failed to load recipe')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Recipe not found' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Back to recipes/ })).toBeVisible();
  });

  test('renders empty hints when ingredients and steps are empty', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Blank recipe',
          ingredients: [],
          steps: [],
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);

    await expect(page.getByRole('heading', { name: 'Blank recipe' })).toBeVisible();
    await expect(page.getByText('No ingredients')).toBeVisible();
    await expect(page.getByText('No steps')).toBeVisible();
  });

  test('back link returns to /recipes', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Pancakes',
          ingredients: [],
          steps: [],
        }),
      });
    });
    await page.route('http://localhost:5117/api/recipes', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '[]',
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('link', { name: /Back to recipes/ }).click();

    await expect(page).toHaveURL(/\/recipes$/);
  });
});
