import { expect, test } from '@playwright/test';

const HOUSEHOLD_ID = 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa';
const HOUSEHOLD_URL = `http://localhost:5117/api/households/${HOUSEHOLD_ID}`;
const PERSONS_URL = 'http://localhost:5117/api/persons';
const ADD_MEMBER_URL = `http://localhost:5117/api/households/${HOUSEHOLD_ID}/members/bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb`;

const householdWithMembers = {
  id: HOUSEHOLD_ID,
  name: 'Smith Family',
  members: [
    {
      personId: 'pppppppp-pppp-pppp-pppp-pppppppppppp',
      personName: 'Alice',
      dietaryPreferences: [1, 3],
      healthConcerns: [1],
      notes: null,
    },
  ],
};

const personsList = [
  { id: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', name: 'Bob', dietaryPreferences: [], healthConcerns: [] },
];

test.describe('households details', () => {
  test('renders household name and members with dietary and health badges', async ({ page }) => {
    await page.route(HOUSEHOLD_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(householdWithMembers),
      });
    });
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(personsList) });
    });

    await page.goto(`/households/${HOUSEHOLD_ID}`);

    await expect(page.getByText('Smith Family')).toBeVisible();
    await expect(page.getByText('Alice')).toBeVisible();
    await expect(page.getByText('Vegetarian')).toBeVisible();
    await expect(page.getByText('Vegan')).toBeVisible();
    await expect(page.getByText('Diabetes')).toBeVisible();
  });

  test('shows 404 not-found state when household does not exist', async ({ page }) => {
    await page.route(HOUSEHOLD_URL, async (route) => {
      await route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Not Found' }),
      });
    });
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    await page.goto(`/households/${HOUSEHOLD_ID}`);

    await expect(page.getByText('Household not found.')).toBeVisible();
  });

  test('add-member select is populated from GET /api/persons', async ({ page }) => {
    await page.route(HOUSEHOLD_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: HOUSEHOLD_ID, name: 'Smith Family', members: [] }),
      });
    });
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(personsList),
      });
    });

    await page.goto(`/households/${HOUSEHOLD_ID}`);

    await expect(page.getByRole('option', { name: 'Bob' })).toBeAttached();
  });

  test('on 204 household reloads and select resets', async ({ page }) => {
    let householdCallCount = 0;
    const updatedHousehold = {
      id: HOUSEHOLD_ID,
      name: 'Smith Family',
      members: [
        {
          personId: 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
          personName: 'Bob',
          dietaryPreferences: [],
          healthConcerns: [],
        },
      ],
    };

    await page.route(HOUSEHOLD_URL, async (route) => {
      householdCallCount++;
      const body = householdCallCount === 1
        ? { id: HOUSEHOLD_ID, name: 'Smith Family', members: [] }
        : updatedHousehold;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(body),
      });
    });
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(personsList),
      });
    });
    await page.route(ADD_MEMBER_URL, async (route) => {
      await route.fulfill({ status: 204 });
    });

    await page.goto(`/households/${HOUSEHOLD_ID}`);

    const select = page.locator('#person-select');
    await select.selectOption('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    await page.getByRole('button', { name: 'Add' }).click();

    await expect(page.locator('li').filter({ hasText: 'Bob' })).toBeVisible();
    await expect(select).toHaveValue('');
  });

  test('on add-member error shows inline error message', async ({ page }) => {
    await page.route(HOUSEHOLD_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: HOUSEHOLD_ID, name: 'Smith Family', members: [] }),
      });
    });
    await page.route(PERSONS_URL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(personsList),
      });
    });
    await page.route(ADD_MEMBER_URL, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Bad request', detail: 'Person is already a member.' }),
      });
    });

    await page.goto(`/households/${HOUSEHOLD_ID}`);

    const select = page.locator('#person-select');
    await select.selectOption('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb');
    await page.getByRole('button', { name: 'Add' }).click();

    await expect(page.getByRole('alert')).toContainText('Person is already a member.');
  });
});
