import { expect, test } from './test';

const LIST_ID = 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb';
const ITEM_ID = 'cccccccc-cccc-cccc-cccc-cccccccccccc';
const SHOPPING_LIST_URL = `http://localhost:5106/api/shopping-lists/${LIST_ID}`;
const MEAL_PLANS_URL = 'http://localhost:5106/api/meal-plans';

function makeItem(overrides: Record<string, unknown> = {}) {
  return {
    id: ITEM_ID,
    productId: 'product-1',
    productName: 'Milk',
    quantity: 2,
    unit: 'L',
    sourceType: 1,
    isPurchased: false,
    notes: null,
    sourceReferenceId: null,
    ...overrides,
  };
}

function stubShoppingList(page: import('@playwright/test').Page, items: object[]) {
  return page.route(SHOPPING_LIST_URL, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ id: LIST_ID, name: 'Weekly Groceries', items }),
    });
  });
}

function stubMealPlans(page: import('@playwright/test').Page) {
  return page.route(MEAL_PLANS_URL, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([]),
    });
  });
}

test.describe('shopping list details', () => {
  test('renders list name and items', async ({ page }) => {
    await stubShoppingList(page, [makeItem()]);
    await stubMealPlans(page);

    await page.goto(`/shopping-lists/${LIST_ID}`);

    await expect(page.getByRole('heading', { name: 'Weekly Groceries' })).toBeVisible();
    await expect(page.getByText('Milk')).toBeVisible();
    await expect(page.getByText('2 L')).toBeVisible();
    await expect(page.getByText('Manual')).toBeVisible();
  });

  test('shows empty items message when no items', async ({ page }) => {
    await stubShoppingList(page, []);
    await stubMealPlans(page);

    await page.goto(`/shopping-lists/${LIST_ID}`);

    await expect(page.getByText('No items yet.')).toBeVisible();
  });

  test('shows 404 not-found state', async ({ page }) => {
    await page.route(SHOPPING_LIST_URL, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Not Found' }),
      });
    });
    await stubMealPlans(page);

    await page.goto(`/shopping-lists/${LIST_ID}`);

    await expect(page.getByText('Shopping list not found.')).toBeVisible();
  });

  test('mark pending calls the API and keeps item visible', async ({ page }) => {
    await stubShoppingList(page, [makeItem()]);
    await page.route(`${SHOPPING_LIST_URL}/items/${ITEM_ID}/pending`, async (route) => {
      await route.fulfill({ status: 204 });
    });
    await stubMealPlans(page);

    await page.goto(`/shopping-lists/${LIST_ID}`);

    const pendingRequest = page.waitForRequest(
      (req) => req.url().includes(`/items/${ITEM_ID}/pending`) && req.method() === 'POST',
    );
    await page.getByRole('button', { name: 'Mark as pending' }).click();
    await pendingRequest;

    await expect(page.getByText('Milk')).toBeVisible();
  });

  test('purchase panel opens and submits', async ({ page }) => {
    let reloaded = false;
    await page.route(SHOPPING_LIST_URL, async (route) => {
      if (route.request().method() === 'GET') {
        const items = reloaded
          ? [makeItem({ isPurchased: true })]
          : [makeItem()];
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify({ id: LIST_ID, name: 'Weekly Groceries', items }),
        });
        reloaded = true;
      }
    });
    await page.route(`${SHOPPING_LIST_URL}/items/${ITEM_ID}/purchase-with-expense`, async (route) => {
      await route.fulfill({ status: 204 });
    });
    await stubMealPlans(page);

    await page.goto(`/shopping-lists/${LIST_ID}`);
    await page.getByRole('button', { name: 'Purchase' }).click();

    await expect(page.getByRole('heading', { name: 'Purchase: Milk' })).toBeVisible();

    await page.getByPlaceholder('0.00').fill('3.50');
    await page.getByPlaceholder('USD').fill('EUR');
    await page.locator('input[type="date"]').fill('2025-01-15');
    await page.getByRole('button', { name: 'Save purchase' }).click();

    await expect(page.getByText('Purchased')).toBeVisible();
  });
});
