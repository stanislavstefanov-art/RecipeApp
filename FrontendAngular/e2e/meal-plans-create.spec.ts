import { expect, test } from '@playwright/test';

const MEAL_PLANS_URL = 'http://localhost:5117/api/meal-plans';
const HOUSEHOLDS_URL = 'http://localhost:5117/api/households';
const NEW_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const HOUSEHOLD_ID = 'hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh';

const householdsList = [{ id: HOUSEHOLD_ID, name: 'Smith Family', memberCount: 2 }];

test.describe('meal plans create', () => {
  test('blank name shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(MEAL_PLANS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        requested = true;
        await route.fulfill({ status: 201, contentType: 'application/json', body: '{}' });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(householdsList),
      });
    });

    await page.goto('/meal-plans/new');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('Name is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('household select is populated from GET /api/households', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(householdsList),
      });
    });

    await page.goto('/meal-plans/new');

    await expect(page.getByRole('option', { name: 'Smith Family' })).toBeAttached();
  });

  test('on 201 navigates to /meal-plans/:id', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as { name: string; householdId: string };
        expect(body.name).toBe('Week of April 28');
        expect(body.householdId).toBe(HOUSEHOLD_ID);
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: NEW_ID, name: 'Week of April 28' }),
        });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
      }
    });
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(householdsList),
      });
    });
    await page.route(`http://localhost:5117/api/meal-plans/${NEW_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: NEW_ID, name: 'Week of April 28', householdId: HOUSEHOLD_ID, householdName: 'Smith Family', entries: [] }),
      });
    });

    await page.goto('/meal-plans/new');
    await page.getByLabel('Name').fill('Week of April 28');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page).toHaveURL(`/meal-plans/${NEW_ID}`);
  });

  test('on 400 shows error and keeps form interactive', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
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
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(householdsList),
      });
    });

    await page.goto('/meal-plans/new');
    await page.getByLabel('Name').fill('Test');
    await page.getByLabel('Household').selectOption(HOUSEHOLD_ID);
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toContainText('Name is required.');
    await expect(page.getByLabel('Name')).toBeEditable();
  });
});
