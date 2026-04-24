import { expect, test } from '@playwright/test';

const EXPENSES_URL = 'http://localhost:5117/api/expenses';

const EXPENSE_ID = 'dddddddd-dddd-dddd-dddd-dddddddddddd';

test.describe('expenses list', () => {
  test('renders list of expenses', async ({ page }) => {
    await page.route(EXPENSES_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: EXPENSE_ID,
            amount: 12.5,
            currency: 'USD',
            expenseDate: '2025-01-15',
            category: 1,
            description: 'Groceries run',
            sourceType: 1,
          },
        ]),
      });
    });

    await page.goto('/expenses');

    await expect(page.getByText('12.50 USD')).toBeVisible();
    await expect(page.getByText('Groceries run')).toBeVisible();
    await expect(page.locator('ul').getByText('Food', { exact: true })).toBeVisible();
  });

  test('shows empty state when no expenses', async ({ page }) => {
    await page.route(EXPENSES_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/expenses');

    await expect(page.getByText('No expenses yet')).toBeVisible();
  });

  test('creates a new expense and shows it', async ({ page }) => {
    let callCount = 0;
    await page.route(EXPENSES_URL, async (route) => {
      if (route.request().method() === 'POST') {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({ id: EXPENSE_ID }),
        });
      } else {
        callCount++;
        const expenses =
          callCount === 1
            ? []
            : [
                {
                  id: EXPENSE_ID,
                  amount: 5.0,
                  currency: 'EUR',
                  expenseDate: '2025-02-01',
                  category: 2,
                  sourceType: 1,
                },
              ];
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(expenses),
        });
      }
    });

    await page.goto('/expenses');
    await page.getByPlaceholder('0.00').fill('5');
    await page.getByPlaceholder('USD').fill('EUR');
    await page.locator('input[type="date"]').fill('2025-02-01');
    await page.locator('select').selectOption('2');
    await page.getByRole('button', { name: 'Add expense' }).click();

    await expect(page.locator('ul').getByText('Transport', { exact: true })).toBeVisible();
  });

  test('monthly report link is visible', async ({ page }) => {
    await page.route(EXPENSES_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([]),
      });
    });

    await page.goto('/expenses');

    await expect(page.getByRole('link', { name: 'Monthly report' })).toBeVisible();
  });
});
