import { expect, test } from '@playwright/test';

const MEAL_PLAN_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const MEAL_PLAN_URL = `http://localhost:5117/api/meal-plans/${MEAL_PLAN_ID}`;

const mealPlanWithEntries = {
  id: MEAL_PLAN_ID,
  name: 'Week of April 28',
  householdId: 'hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh',
  householdName: 'Smith Family',
  entries: [
    {
      id: 'eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee',
      baseRecipeId: 'rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr',
      baseRecipeName: 'Pasta Carbonara',
      plannedDate: '2026-04-28',
      mealType: 3,
      scope: 1,
      assignments: [
        {
          personId: 'pppppppp-pppp-pppp-pppp-pppppppppppp',
          personName: 'Alice',
          assignedRecipeId: 'rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr',
          assignedRecipeName: 'Pasta Carbonara',
          recipeVariationId: null,
          recipeVariationName: null,
          portionMultiplier: 1,
          notes: null,
        },
      ],
    },
  ],
};

test.describe('meal plans details', () => {
  test('renders name, household, and entries with labels', async ({ page }) => {
    await page.route(MEAL_PLAN_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(mealPlanWithEntries),
      });
    });

    await page.goto(`/meal-plans/${MEAL_PLAN_ID}`);

    await expect(page.getByText('Week of April 28')).toBeVisible();
    await expect(page.getByText('Smith Family')).toBeVisible();
    await expect(page.getByText('2026-04-28')).toBeVisible();
    await expect(page.getByText('Dinner')).toBeVisible();
    await expect(page.getByText('Shared')).toBeVisible();
    await expect(page.getByRole('paragraph').filter({ hasText: 'Pasta Carbonara' })).toBeVisible();
    await expect(page.getByText('Alice — Pasta Carbonara')).toBeVisible();
  });

  test('shows 404 not-found state when meal plan does not exist', async ({ page }) => {
    await page.route(MEAL_PLAN_URL, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Not Found' }),
      });
    });

    await page.goto(`/meal-plans/${MEAL_PLAN_ID}`);

    await expect(page.getByText('Meal plan not found.')).toBeVisible();
  });

  test('renders empty entries state', async ({ page }) => {
    await page.route(MEAL_PLAN_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...mealPlanWithEntries, entries: [] }),
      });
    });

    await page.goto(`/meal-plans/${MEAL_PLAN_ID}`);

    await expect(page.getByText('No entries yet.')).toBeVisible();
  });
});
