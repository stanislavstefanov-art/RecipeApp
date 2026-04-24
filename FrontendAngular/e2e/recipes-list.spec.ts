import { expect, test } from '@playwright/test';

const API = 'http://localhost:5117/api/recipes';

test.describe('recipes list', () => {
  test('renders a list of recipes on success', async ({ page }) => {
    await page.route(API, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: '11111111-1111-1111-1111-111111111111', name: 'Pancakes' },
          { id: '22222222-2222-2222-2222-222222222222', name: 'Omelette' },
        ]),
      });
    });

    await page.goto('/recipes');

    await expect(page.getByRole('heading', { name: 'Recipes' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Pancakes' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Omelette' })).toBeVisible();
  });

  test('renders empty state when the list is empty', async ({ page }) => {
    await page.route(API, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '[]',
      });
    });

    await page.goto('/recipes');

    await expect(page.getByText('No recipes yet')).toBeVisible();
  });

  test('renders error state on 500', async ({ page }) => {
    await page.route(API, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto('/recipes');

    await expect(page.getByText('Failed to load recipes')).toBeVisible();
  });

  test('New recipe button links to /recipes/new', async ({ page }) => {
    await page.route(API, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: '[]',
      });
    });

    await page.goto('/recipes');
    await page.getByRole('link', { name: 'New recipe' }).click();

    await expect(page).toHaveURL(/\/recipes\/new$/);
  });
});
