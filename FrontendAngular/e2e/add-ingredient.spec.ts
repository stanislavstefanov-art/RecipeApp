import { expect, test } from '@playwright/test';

const ID = '66666666-6666-6666-6666-666666666666';
const DETAIL = `http://localhost:5117/api/recipes/${ID}`;
const INGREDIENTS = `http://localhost:5117/api/recipes/${ID}/ingredients`;

test.describe('add ingredient', () => {
  test('empty name shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(INGREDIENTS, async (route) => {
      requested = true;
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Quantity').fill('100');
    await page.getByLabel('Unit').fill('g');
    await page.getByRole('button', { name: 'Add ingredient' }).click();

    await expect(page.getByText('Ingredient name is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('empty unit shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(INGREDIENTS, async (route) => {
      requested = true;
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Ingredient name').fill('Flour');
    await page.getByLabel('Quantity').fill('100');
    await page.getByLabel('Unit').fill('');
    await page.getByRole('button', { name: 'Add ingredient' }).click();

    await expect(page.getByText('Unit is required.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('quantity of 0 shows inline error and issues no request', async ({ page }) => {
    let requested = false;
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(INGREDIENTS, async (route) => {
      requested = true;
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Ingredient name').fill('Flour');
    await page.getByLabel('Quantity').fill('0');
    await page.getByLabel('Unit').fill('g');
    await page.getByRole('button', { name: 'Add ingredient' }).click();

    await expect(page.getByText('Quantity must be greater than 0.')).toBeVisible();
    expect(requested).toBe(false);
  });

  test('on 204 the new ingredient appears and the form resets', async ({ page }) => {
    let getCount = 0;
    await page.route(DETAIL, async (route) => {
      getCount += 1;
      const ingredients =
        getCount === 1 ? [] : [{ name: 'Flour', quantity: 200, unit: 'g' }];
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Cake',
          ingredients,
          steps: [],
        }),
      });
    });
    await page.route(INGREDIENTS, async (route) => {
      expect(route.request().method()).toBe('POST');
      expect(route.request().postDataJSON()).toEqual({
        name: 'Flour',
        quantity: 200,
        unit: 'g',
      });
      await route.fulfill({ status: 204, body: '' });
    });

    await page.goto(`/recipes/${ID}`);
    await expect(page.getByText('No ingredients')).toBeVisible();

    await page.getByLabel('Ingredient name').fill('Flour');
    await page.getByLabel('Quantity').fill('200');
    await page.getByLabel('Unit').fill('g');
    await page.getByRole('button', { name: 'Add ingredient' }).click();

    await expect(page.getByText('Flour', { exact: true })).toBeVisible();
    await expect(page.getByText('200 g')).toBeVisible();
    await expect(page.getByLabel('Ingredient name')).toHaveValue('');
    await expect(page.getByLabel('Quantity')).toHaveValue('1');
    await expect(page.getByLabel('Unit')).toHaveValue('');
  });

  test('on 400 shows a server error and preserves entered values', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(INGREDIENTS, async (route) => {
      await route.fulfill({
        status: 400,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'Validation failed',
          detail: 'Ingredient name already exists.',
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Ingredient name').fill('Flour');
    await page.getByLabel('Quantity').fill('200');
    await page.getByLabel('Unit').fill('g');
    await page.getByRole('button', { name: 'Add ingredient' }).click();

    const alert = page.locator('app-add-ingredient-form [role="alert"]');
    await expect(alert).toContainText('Ingredient name already exists.');
    await expect(page.getByLabel('Ingredient name')).toHaveValue('Flour');
    await expect(page.getByLabel('Quantity')).toHaveValue('200');
    await expect(page.getByLabel('Unit')).toHaveValue('g');
  });

  test('on 500 shows a server error and keeps form interactive', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name: 'Cake', ingredients: [], steps: [] }),
      });
    });
    await page.route(INGREDIENTS, async (route) => {
      await route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title: 'Server error' }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Ingredient name').fill('Flour');
    await page.getByLabel('Quantity').fill('200');
    await page.getByLabel('Unit').fill('g');
    await page.getByRole('button', { name: 'Add ingredient' }).click();

    const alert = page.locator('app-add-ingredient-form [role="alert"]');
    await expect(alert).toBeVisible();
    await expect(page.getByRole('button', { name: 'Add ingredient' })).toBeEnabled();
    await expect(page.getByLabel('Ingredient name')).toBeEditable();
  });
});
