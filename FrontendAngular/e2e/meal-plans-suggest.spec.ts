import { expect, test } from '@playwright/test';

const SUGGEST_URL = 'http://localhost:5117/api/meal-plans/suggest';
const ACCEPT_URL = 'http://localhost:5117/api/meal-plans/accept-suggestion';
const HOUSEHOLDS_URL = 'http://localhost:5117/api/households';
const RECIPES_URL = 'http://localhost:5117/api/recipes';
const MEAL_PLAN_ID = 'mmmmmmmm-mmmm-mmmm-mmmm-mmmmmmmmmmmm';
const HOUSEHOLD_ID = 'hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh';
const RECIPE_ID = 'rrrrrrrr-rrrr-rrrr-rrrr-rrrrrrrrrrrr';

const householdsList = [{ id: HOUSEHOLD_ID, name: 'Smith Family', memberCount: 2 }];
const recipesList = [{ id: RECIPE_ID, name: 'Pasta Carbonara' }];

const suggestion = {
  name: 'Week of April 28',
  entries: [
    {
      baseRecipeId: RECIPE_ID,
      plannedDate: '2026-04-28',
      mealType: 3,
      scope: 1,
      assignments: [],
    },
  ],
  confidence: 0.9,
  needsReview: false,
  notes: null,
};

test.describe('meal plans suggest', () => {
  test('blank name shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(SUGGEST_URL, async (route) => {
      requested = true;
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByText('Name is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('no meal type selected shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(SUGGEST_URL, async (route) => {
      requested = true;
      await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByText('Select at least one meal type.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('sends correct request body and renders preview', async ({ page }) => {
    await page.route(SUGGEST_URL, async (route) => {
      const body = route.request().postDataJSON() as {
        name: string;
        householdId: string;
        startDate: string;
        numberOfDays: number;
        mealTypes: number[];
      };
      expect(body.name).toBe('Week of April 28');
      expect(body.householdId).toBe(HOUSEHOLD_ID);
      expect(body.startDate).toBe('2026-04-28');
      expect(body.numberOfDays).toBe(7);
      expect(body.mealTypes).toContain(3);
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(suggestion),
      });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByLabel('Dinner').check();
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByText('2026-04-28')).toBeVisible();
    await expect(page.getByRole('listitem').getByText('Dinner')).toBeVisible();
    await expect(page.getByText('Pasta Carbonara')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Accept' })).toBeVisible();
  });

  test('needsReview:true renders advisory', async ({ page }) => {
    await page.route(SUGGEST_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...suggestion, needsReview: true }),
      });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByLabel('Dinner').check();
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByText('This suggestion needs review before use.')).toBeVisible();
  });

  test('notes are rendered in the preview', async ({ page }) => {
    await page.route(SUGGEST_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ ...suggestion, notes: 'Optimised for low-carb diet.' }),
      });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByLabel('Dinner').check();
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByText('Optimised for low-carb diet.')).toBeVisible();
  });

  test('on Accept 201 navigates to /meal-plans/:id', async ({ page }) => {
    await page.route(SUGGEST_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(suggestion),
      });
    });
    await page.route(ACCEPT_URL, async (route) => {
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ mealPlanId: MEAL_PLAN_ID, name: 'Week of April 28' }),
      });
    });
    await page.route(`http://localhost:5117/api/meal-plans/${MEAL_PLAN_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: MEAL_PLAN_ID, name: 'Week of April 28', householdId: HOUSEHOLD_ID, householdName: 'Smith Family', entries: [] }),
      });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByLabel('Dinner').check();
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByRole('button', { name: 'Accept' })).toBeVisible();
    await page.getByRole('button', { name: 'Accept' }).click();

    await expect(page).toHaveURL(`/meal-plans/${MEAL_PLAN_ID}`);
  });

  test('on Accept error shows inline error', async ({ page }) => {
    await page.route(SUGGEST_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(suggestion),
      });
    });
    await page.route(ACCEPT_URL, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Bad request', detail: 'Household not found.' }),
      });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByLabel('Dinner').check();
    await page.getByRole('button', { name: 'Suggest' }).click();
    await page.getByRole('button', { name: 'Accept' }).click();

    await expect(page.getByRole('alert')).toContainText('Household not found.');
  });

  test('on suggest 400 shows error message', async ({ page }) => {
    await page.route(SUGGEST_URL, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Validation failed', detail: 'Household not found.' }),
      });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(householdsList) });
    });
    await page.route(RECIPES_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(recipesList) });
    });

    await page.goto('/meal-plans/suggest');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByLabel('Start date').fill('2026-04-28');
    await page.getByLabel('Number of days').fill('7');
    await page.getByLabel('Dinner').check();
    await page.getByRole('button', { name: 'Suggest' }).click();

    await expect(page.getByRole('alert')).toContainText('Household not found.');
    await expect(page.getByRole('button', { name: 'Suggest' })).toBeEnabled();
  });
});
