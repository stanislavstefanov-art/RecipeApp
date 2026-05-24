import { expect, test } from './test';

const ID = '44444444-4444-4444-4444-444444444444';
const DETAIL = `http://localhost:5106/api/recipes/${ID}`;

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
            { id: '11111111-1111-1111-1111-111111111111', name: 'Flour', quantity: 200, unit: 'g' },
            { id: '22222222-2222-2222-2222-222222222222', name: 'Milk', quantity: 300, unit: 'ml' },
          ],
          steps: [
            { id: '33333333-3333-3333-3333-333333333333', order: 1, instruction: 'Mix dry ingredients.' },
            { id: '44444444-4444-4444-4444-444444444444', order: 2, instruction: 'Whisk in the milk.' },
          ],
          averageStars: null,
          ratingCount: 0,
          ratings: [],
          myRating: null,
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);

    await expect(page.getByRole('heading', { name: 'Pancakes' })).toBeVisible();
    await expect(page.getByText('Flour', { exact: true })).toBeVisible();
    await expect(page.getByText('200 gram')).toBeVisible();
    await expect(page.getByText('Milk', { exact: true })).toBeVisible();
    await expect(page.getByText('300 milliliter')).toBeVisible();
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
    await expect(page.getByRole('link', { name: /← Recipes/ })).toBeVisible();
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
    await expect(page.getByRole('link', { name: /← Recipes/ })).toBeVisible();
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
          averageStars: null,
          ratingCount: 0,
          ratings: [],
          myRating: null,
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
          averageStars: null,
          ratingCount: 0,
          ratings: [],
          myRating: null,
        }),
      });
    });
    await page.route('http://localhost:5106/api/recipes', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '[]',
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByRole('link', { name: /← Recipes/ }).click();

    await expect(page).toHaveURL(/\/recipes$/);
  });
});
