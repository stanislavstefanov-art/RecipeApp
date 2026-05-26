import { expect, test } from './test';

const API = 'http://localhost:5106/api/recipes';
const HOUSEHOLDS_API = 'http://localhost:5106/api/households';
const NEW_ID = '33333333-3333-3333-3333-333333333333';
const HOUSEHOLD_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';

const ONE_HOUSEHOLD = [{ id: HOUSEHOLD_ID, name: 'Test Household', memberCount: 1 }];

test.describe('recipes create', () => {
  test.beforeEach(async ({ page }) => {
    // Auto-select: single household → householdId is set without a dropdown
    await page.route(HOUSEHOLDS_API, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(ONE_HOUSEHOLD),
      });
    });
  });

  test('shows no-household message and disables button when user has no households', async ({
    page,
  }) => {
    await page.unroute(HOUSEHOLDS_API);
    await page.route(HOUSEHOLDS_API, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/recipes/new');
    await page.waitForResponse(HOUSEHOLDS_API);

    await expect(page.getByText('You need a household to create recipes.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Create' })).toBeDisabled();
  });

  test('client-side validation blocks empty submit and makes no request', async ({
    page,
  }) => {
    let requested = false;
    await page.route(API, async (route) => {
      requested = true;
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        body: JSON.stringify({ id: NEW_ID }),
      });
    });

    await page.goto('/recipes/new');
    await page.waitForResponse(HOUSEHOLDS_API);
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('This field is required.')).toBeVisible();
    expect(requested).toBe(false);
    await expect(page).toHaveURL(/\/recipes\/new$/);
  });

  test('client-side validation blocks over-long name and makes no request', async ({
    page,
  }) => {
    let requested = false;
    await page.route(API, async (route) => {
      requested = true;
      await route.fulfill({ status: 201, body: '' });
    });

    await page.goto('/recipes/new');
    await page.waitForResponse(HOUSEHOLDS_API);
    await page.getByLabel('Name').fill('a'.repeat(201));
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('Maximum length is 200 characters.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('navigates to /recipes/{id} on 201', async ({ page }) => {
    await page.route(API, async (route, request) => {
      expect(request.method()).toBe('POST');
      expect(request.postDataJSON()).toEqual({ name: 'Pancakes', householdId: HOUSEHOLD_ID, recipeType: 1, difficultyLevel: null });
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        headers: { Location: `/api/recipes/${NEW_ID}` },
        body: JSON.stringify({ id: NEW_ID }),
      });
    });

    await page.goto('/recipes/new');
    await page.waitForResponse(HOUSEHOLDS_API);
    await page.getByLabel('Name').fill('Pancakes');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page).toHaveURL(new RegExp(`/recipes/${NEW_ID}$`));
  });

  test('shows a server error on 400 and preserves the name', async ({ page }) => {
    await page.route(API, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'Validation failed',
          detail: 'Name cannot be blank.',
        }),
      });
    });

    await page.goto('/recipes/new');
    await page.waitForResponse(HOUSEHOLDS_API);
    await page.getByLabel('Name').fill('Pancakes');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toContainText('Name cannot be blank.');
    await expect(page.getByLabel('Name')).toHaveValue('Pancakes');
    await expect(page).toHaveURL(/\/recipes\/new$/);
  });

  test('shows a server error on 500 and keeps form interactive', async ({ page }) => {
    await page.route(API, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto('/recipes/new');
    await page.waitForResponse(HOUSEHOLDS_API);
    await page.getByLabel('Name').fill('Pancakes');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByLabel('Name')).toBeEditable();
    await expect(page.getByRole('button', { name: 'Create' })).toBeEnabled();
  });
});
