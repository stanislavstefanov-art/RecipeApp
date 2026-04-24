import { expect, test } from '@playwright/test';

const PERSON_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const PERSON_URL = `http://localhost:5117/api/persons/${PERSON_ID}`;
const PERSONS_URL = 'http://localhost:5117/api/persons';

test.describe('persons details', () => {
  test('renders name, dietary preferences, health concerns, and notes', async ({ page }) => {
    await page.route(PERSON_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: PERSON_ID,
          name: 'Alice',
          dietaryPreferences: [1, 3],
          healthConcerns: [1],
          notes: 'Prefers spicy food.',
        }),
      });
    });

    await page.goto(`/persons/${PERSON_ID}`);

    await expect(page.getByRole('heading', { name: 'Alice' })).toBeVisible();
    await expect(page.getByText('Vegetarian')).toBeVisible();
    await expect(page.getByText('Vegan')).toBeVisible();
    await expect(page.getByText('Diabetes')).toBeVisible();
    await expect(page.getByText('Prefers spicy food.')).toBeVisible();
  });

  test('shows None for empty dietary preferences and health concerns', async ({ page }) => {
    await page.route(PERSON_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: PERSON_ID,
          name: 'Bob',
          dietaryPreferences: [],
          healthConcerns: [],
        }),
      });
    });

    await page.goto(`/persons/${PERSON_ID}`);

    await expect(page.getByText('None').first()).toBeVisible();
  });

  test('shows No notes when notes is absent', async ({ page }) => {
    await page.route(PERSON_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: PERSON_ID, name: 'Bob', dietaryPreferences: [], healthConcerns: [] }),
      });
    });

    await page.goto(`/persons/${PERSON_ID}`);

    await expect(page.getByText('No notes')).toBeVisible();
  });

  test('shows 404 not-found state', async ({ page }) => {
    await page.route(PERSON_URL, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Not Found' }),
      });
    });

    await page.goto(`/persons/${PERSON_ID}`);

    await expect(page.getByText('Person not found.')).toBeVisible();
  });

  test('person name on list page links to detail page', async ({ page }) => {
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: PERSON_ID, name: 'Alice', dietaryPreferences: [], healthConcerns: [] },
        ]),
      });
    });
    await page.route(PERSON_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: PERSON_ID, name: 'Alice', dietaryPreferences: [], healthConcerns: [] }),
      });
    });

    await page.goto('/persons');
    await page.getByRole('link', { name: 'Alice' }).click();

    await expect(page).toHaveURL(`/persons/${PERSON_ID}`);
  });
});
