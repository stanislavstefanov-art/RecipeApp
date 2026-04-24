import { expect, test } from '@playwright/test';

const ID = '55555555-5555-5555-5555-555555555555';
const DETAIL = `http://localhost:5117/api/recipes/${ID}`;

function stubDetail(body: unknown) {
  return async (route: import('@playwright/test').Route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(body),
    });
  };
}

test.describe('update recipe name', () => {
  test('form is pre-filled with the current name', async ({ page }) => {
    await page.route(
      DETAIL,
      stubDetail({
        id: ID,
        name: 'Old name',
        ingredients: [],
        steps: [],
      }),
    );

    await page.goto(`/recipes/${ID}`);

    await expect(page.getByLabel('Recipe name')).toHaveValue('Old name');
  });

  test('clearing the name shows inline error and issues no request', async ({
    page,
  }) => {
    await page.route(
      DETAIL,
      stubDetail({
        id: ID,
        name: 'Old name',
        ingredients: [],
        steps: [],
      }),
    );
    let putRequested = false;
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'PUT') {
        putRequested = true;
        await route.fulfill({ status: 204, body: '' });
      } else {
        await route.fallback();
      }
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Recipe name').fill('');
    await page.getByRole('button', { name: 'Save name' }).click();

    await expect(page.getByText('Name is required.')).toBeVisible();
    expect(putRequested).toBe(false);
  });

  test('201-character name shows inline error and issues no request', async ({
    page,
  }) => {
    await page.route(
      DETAIL,
      stubDetail({
        id: ID,
        name: 'Old name',
        ingredients: [],
        steps: [],
      }),
    );
    let putRequested = false;
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'PUT') {
        putRequested = true;
        await route.fulfill({ status: 204, body: '' });
      } else {
        await route.fallback();
      }
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Recipe name').fill('a'.repeat(201));
    await page.getByRole('button', { name: 'Save name' }).click();

    await expect(page.getByText('Name must be 200 characters or fewer.')).toBeVisible();
    expect(putRequested).toBe(false);
  });

  test('on 204 the details view shows the new name', async ({ page }) => {
    let getCount = 0;
    await page.route(DETAIL, async (route) => {
      const method = route.request().method();
      if (method === 'PUT') {
        expect(route.request().postDataJSON()).toEqual({ name: 'New name' });
        await route.fulfill({ status: 204, body: '' });
        return;
      }
      getCount += 1;
      const name = getCount === 1 ? 'Old name' : 'New name';
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ id: ID, name, ingredients: [], steps: [] }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await expect(page.getByRole('heading', { name: 'Old name' })).toBeVisible();

    await page.getByLabel('Recipe name').fill('New name');
    await page.getByRole('button', { name: 'Save name' }).click();

    await expect(page.getByRole('heading', { name: 'New name' })).toBeVisible();
    await expect(page.getByLabel('Recipe name')).toHaveValue('New name');
  });

  test('on 400 shows a server error and preserves the name', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({
          status: 400,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            title: 'Validation failed',
            detail: 'Name cannot be blank.',
          }),
        });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Old name',
          ingredients: [],
          steps: [],
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Recipe name').fill('Bad name');
    await page.getByRole('button', { name: 'Save name' }).click();

    const alert = page.locator('form[novalidate] [role="alert"]');
    await expect(alert).toContainText('Name cannot be blank.');
    await expect(page.getByLabel('Recipe name')).toHaveValue('Bad name');
  });

  test('on 500 shows a server error and keeps form interactive', async ({ page }) => {
    await page.route(DETAIL, async (route) => {
      if (route.request().method() === 'PUT') {
        await route.fulfill({
          status: 500,
          contentType: 'application/problem+json',
          body: JSON.stringify({ title: 'Server error' }),
        });
        return;
      }
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: ID,
          name: 'Old name',
          ingredients: [],
          steps: [],
        }),
      });
    });

    await page.goto(`/recipes/${ID}`);
    await page.getByLabel('Recipe name').fill('New name');
    await page.getByRole('button', { name: 'Save name' }).click();

    const alert = page.locator('form[novalidate] [role="alert"]');
    await expect(alert).toBeVisible();
    await expect(page.getByLabel('Recipe name')).toBeEditable();
    await expect(page.getByRole('button', { name: 'Save name' })).toBeEnabled();
  });
});
