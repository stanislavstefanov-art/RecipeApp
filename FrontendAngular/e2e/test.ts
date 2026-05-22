import { test as base, expect } from '@playwright/test';

/** Build a complete RecipeDto for test mocks — supply only the fields your test cares about. */
export function recipeDto(overrides: Record<string, unknown> = {}): Record<string, unknown> {
  return {
    averageStars: null,
    ratingCount: 0,
    ratings: [],
    myRating: null,
    ...overrides,
  };
}

const test = base.extend<object>({
  page: async ({ page }, use) => {
    // Seed auth session + English locale before Angular bootstraps.
    await page.addInitScript(() => {
      const session = {
        token: 'test-token',
        expiresAt: new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString(),
        user: {
          id: '00000000-0000-0000-0000-000000000001',
          email: 'test@example.com',
          displayName: 'Test User',
          provider: 'local',
        },
      };
      localStorage.setItem('auth.session', JSON.stringify(session));
      // Tests assert English strings; force 'en' so the language-switcher doesn't default to 'bg'.
      localStorage.setItem('lang', 'en');
    });

    // Auto-stub the cooking-log endpoint — it's always fetched by the recipe details
    // page but never relevant to individual test assertions.
    await page.route('**/api/cooking-log/**', async (route) => {
      await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
    });

    // Auto-stub households — loaded on init by several list pages; individual tests
    // can override with a more specific route if they need different data.
    await page.route('**/api/households', async (route) => {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          { id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', name: 'Test Household', memberCount: 2 },
        ]),
      });
    });

    await use(page);
  },
});

export { test, expect };
