import { expect, test } from '@playwright/test';

const API = 'http://localhost:5117/api/recipes';
const NEW_ID = '33333333-3333-3333-3333-333333333333';

test.describe('recipes create', () => {
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
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('Name is required.')).toBeVisible();
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
    await page.getByLabel('Name').fill('a'.repeat(201));
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByText('Name must be 200 characters or fewer.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('navigates to /recipes/{id} on 201', async ({ page }) => {
    await page.route(API, async (route, request) => {
      expect(request.method()).toBe('POST');
      expect(request.postDataJSON()).toEqual({ name: 'Pancakes' });
      await route.fulfill({
        status: 201,
        contentType: 'application/json',
        headers: { Location: `/api/recipes/${NEW_ID}` },
        body: JSON.stringify({ id: NEW_ID }),
      });
    });

    await page.goto('/recipes/new');
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
    await page.getByLabel('Name').fill('Pancakes');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByLabel('Name')).toBeEditable();
    await expect(page.getByRole('button', { name: 'Create' })).toBeEnabled();
  });
});
