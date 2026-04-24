import { expect, test } from '@playwright/test';

const PERSONS_URL = 'http://localhost:5117/api/persons';

test.describe('persons list', () => {
  test('renders a list of persons with dietary and health labels', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
            name: 'Alice',
            dietaryPreferences: [1, 3],
            healthConcerns: [1],
            notes: null,
          },
          {
            id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
            name: 'Bob',
            dietaryPreferences: [],
            healthConcerns: [],
            notes: 'Prefers spicy food.',
          },
        ]),
      });
    });

    await page.goto('/persons');

    await expect(page.getByText('Alice')).toBeVisible();
    await expect(page.getByText('Vegetarian')).toBeVisible();
    await expect(page.getByText('Vegan')).toBeVisible();
    await expect(page.getByText('Diabetes')).toBeVisible();
    await expect(page.getByText('Bob')).toBeVisible();
    await expect(page.getByText('Prefers spicy food.')).toBeVisible();
  });

  test('renders empty state when the list is empty', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/persons');

    await expect(page.getByText('No persons yet')).toBeVisible();
  });

  test('renders error state on 500', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto('/persons');

    await expect(page.getByRole('alert')).toBeVisible();
    await expect(page.getByText('Failed to load persons')).toBeVisible();
  });

  test('New person link navigates to /persons/new', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto('/persons');
    await page.getByRole('link', { name: 'New person' }).click();

    await expect(page).toHaveURL('/persons/new');
  });
});
