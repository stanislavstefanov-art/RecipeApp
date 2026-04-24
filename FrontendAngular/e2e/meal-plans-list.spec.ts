import { expect, test } from '@playwright/test';

const MEAL_PLANS_URL = 'http://localhost:5117/api/meal-plans';

test.describe('meal plans list', () => {
  test('renders meal plans with name, household, and entry count', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            name: 'Week of April 28',
            householdId: 'hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh',
            householdName: 'Smith Family',
            entryCount: 7,
          },
          {
            id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            name: 'Weekend meals',
            householdId: 'hhhhhhhh-hhhh-hhhh-hhhh-hhhhhhhhhhhh',
            householdName: 'Smith Family',
            entryCount: 1,
          },
        ]),
      });
    });

    await page.goto('/meal-plans');

    await expect(page.getByText('Week of April 28')).toBeVisible();
    await expect(page.getByText('Smith Family · 7 entries')).toBeVisible();
    await expect(page.getByText('Weekend meals')).toBeVisible();
    await expect(page.getByText('Smith Family · 1 entry')).toBeVisible();
  });

  test('renders empty state when the list is empty', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/meal-plans');

    await expect(page.getByText('No meal plans yet.')).toBeVisible();
  });

  test('renders error state on 500', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto('/meal-plans');

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByText('Failed to load meal plans')).toBeVisible();
  });

  test('New meal plan link navigates to /meal-plans/new', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/meal-plans');
    await page.getByRole('link', { name: 'New meal plan' }).click();

    await expect(page).toHaveURL('/meal-plans/new');
  });

  test('Suggest with AI link navigates to /meal-plans/suggest', async ({ page }) => {
    await page.route(MEAL_PLANS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/meal-plans');
    await page.getByRole('link', { name: 'Suggest with AI' }).click();

    await expect(page).toHaveURL('/meal-plans/suggest');
  });
});
