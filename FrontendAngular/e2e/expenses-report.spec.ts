import { expect, test } from './test';

const REPORT_URL = 'http://localhost:5106/api/expenses/monthly-report?year=2025&month=3';
const INSIGHTS_URL = 'http://localhost:5106/api/expenses/insights?year=2025&month=3';

test.describe('expenses report', () => {
  test('renders monthly totals and category breakdown', async ({ page }) => {
    await page.route(/\/api\/expenses\/monthly-report/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          year: 2025,
          month: 3,
          totalAmount: 250.0,
          currency: 'USD',
          byCategory: [
            { category: 1, totalAmount: 150.0, count: 3 },
            { category: 5, totalAmount: 100.0, count: 1 },
          ],
        }),
      });
    });
    await page.route(/\/api\/expenses\/insights/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ summary: 'Monthly analysis.', keyFindings: ['Food spending up 20%'], recommendations: [], confidence: 0.8, needsReview: false, notes: null }),
      });
    });

    await page.goto('/expenses/report');
    await page.locator('#report-year').fill('2025');
    await page.locator('#report-month').selectOption('3');
    await page.getByRole('button', { name: 'Load' }).click();

    await expect(page.getByText('250.00 USD')).toBeVisible();
    await expect(page.getByText('Food', { exact: true })).toBeVisible();
    await expect(page.getByText('Health', { exact: true })).toBeVisible();
  });

  test('renders insights when present', async ({ page }) => {
    await page.route(/\/api\/expenses\/monthly-report/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          year: 2025,
          month: 3,
          totalAmount: 100.0,
          currency: 'USD',
          byCategory: [],
        }),
      });
    });
    await page.route(/\/api\/expenses\/insights/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ summary: 'Monthly analysis.', keyFindings: ['You spent more on food this month.'], recommendations: [], confidence: 0.8, needsReview: false, notes: null }),
      });
    });

    await page.goto('/expenses/report');
    await page.locator('#report-year').fill('2025');
    await page.locator('#report-month').selectOption('3');
    await page.getByRole('button', { name: 'Load' }).click();

    await expect(page.getByText('You spent more on food this month.')).toBeVisible();
  });

  test('back to expenses link is visible', async ({ page }) => {
    await page.route(/\/api\/expenses\/monthly-report/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ year: 2025, month: 1, totalAmount: 0, currency: 'USD', byCategory: [] }),
      });
    });
    await page.route(/\/api\/expenses\/insights/, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ summary: '', keyFindings: [], recommendations: [], confidence: 0, needsReview: false, notes: null }),
      });
    });

    await page.goto('/expenses/report');

    await expect(page.getByRole('link', { name: /← Expenses/ })).toBeVisible();
  });
});
