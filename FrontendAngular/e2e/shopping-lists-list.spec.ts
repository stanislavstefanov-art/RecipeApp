import { expect, test } from '@playwright/test';

const SHOPPING_LISTS_URL = 'http://localhost:5117/api/shopping-lists';

const LIST_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';

test.describe('shopping lists list', () => {
  test('renders list of shopping lists with item counts', async ({ page }) => {
    await page.route(SHOPPING_LISTS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: LIST_ID,
            name: 'Weekly Groceries',
            items: [
              { id: 'i1', name: 'Milk', sourceType: 1, isPending: false, isPurchased: false },
              { id: 'i2', name: 'Eggs', sourceType: 1, isPending: false, isPurchased: false },
            ],
          },
        ]),
      });
    });

    await page.goto('/shopping-lists');

    await expect(page.getByRole('link', { name: 'Weekly Groceries' })).toBeVisible();
    await expect(page.getByText('2 items')).toBeVisible();
  });

  test('shows empty state when no shopping lists', async ({ page }) => {
    await page.route(SHOPPING_LISTS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/shopping-lists');

    await expect(page.getByText('No shopping lists yet')).toBeVisible();
  });

  test('creates a new shopping list and shows it', async ({ page }) => {
    let callCount = 0;
    await page.route(SHOPPING_LISTS_URL, async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: LIST_ID, name: 'Party Shopping' }),
        });
      } else {
        callCount++;
        const lists =
          callCount === 1
            ? []
            : [{ id: LIST_ID, name: 'Party Shopping', items: [] }];
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(lists),
        });
      }
    });

    await page.goto('/shopping-lists');
    await page.getByPlaceholder('New list name…').fill('Party Shopping');
    await page.getByRole('button', { name: 'Create' }).click();

    await expect(page.getByRole('link', { name: 'Party Shopping' })).toBeVisible();
  });

  test('list name links to detail page', async ({ page }) => {
    await page.route(SHOPPING_LISTS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: LIST_ID, name: 'Weekly Groceries', items: [] },
        ]),
      });
    });
    await page.route(`${SHOPPING_LISTS_URL}/${LIST_ID}`, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: LIST_ID, name: 'Weekly Groceries', items: [] }),
      });
    });
    await page.route('http://localhost:5117/api/meal-plans', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/shopping-lists');
    await page.getByRole('link', { name: 'Weekly Groceries' }).click();

    await expect(page).toHaveURL(`/shopping-lists/${LIST_ID}`);
  });
});
