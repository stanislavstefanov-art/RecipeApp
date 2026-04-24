import { expect, test } from '@playwright/test';

const HOUSEHOLDS_URL = 'http://localhost:5117/api/households';

test.describe('households list', () => {
  test('renders households with name and member count', async ({ page }) => {
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', name: 'Smith Family', memberCount: 3 },
          { id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Jones Household', memberCount: 1 },
        ]),
      });
    });

    await page.goto('/households');

    await expect(page.getByText('Smith Family')).toBeVisible();
    await expect(page.getByText('3 members')).toBeVisible();
    await expect(page.getByText('Jones Household')).toBeVisible();
    await expect(page.getByText('1 member')).toBeVisible();
  });

  test('renders empty state when the list is empty', async ({ page }) => {
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/households');

    await expect(page.getByText('No households yet.')).toBeVisible();
  });

  test('renders error state on 500', async ({ page }) => {
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto('/households');

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByText('Failed to load households')).toBeVisible();
  });

  test('New household link navigates to /households/new', async ({ page }) => {
    await page.route(HOUSEHOLDS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/households');
    await page.getByRole('link', { name: 'New household' }).click();

    await expect(page).toHaveURL('/households/new');
  });
});
