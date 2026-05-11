# RecipesApp — Angular Frontend

Angular (zoneless, standalone, signals) + Tailwind frontend for RecipesApp.

## Prerequisites

- Node.js 22+
- Backend API running at `http://localhost:5106` (see [`/Backend`](../Backend/README.md))

## Development

```bash
npm install --legacy-peer-deps
npm run start   # http://localhost:4200
```

## Build & lint

```bash
npm run build
npm run lint
```

## Unit tests

```bash
npm test
```

## E2E tests (Playwright)

```bash
npx playwright test           # all specs (headless)
npx playwright test --ui      # interactive UI mode
```

Tests mock the API via `page.route()` — no running backend required.

## Environment / API URL

The API base URL is set in `src/environments/`:

| File | URL | Used when |
|---|---|---|
| `environment.ts` | `http://localhost:5106` | `ng serve` (development) |
| `environment.prod.ts` | *(empty — relative URLs)* | `ng build` (production) |

For production deployments where the API is on a separate domain, set `apiBaseUrl` in `environment.prod.ts` before building, or use file replacement in `angular.json`.

## Project structure

```
src/app/
  core/          — auth store, interceptors, global error handler
  api/           — typed HTTP clients, DTO types
  features/      — feature components (recipes, households, mealPlans, …)
  shared/ui/     — reusable presentational components
  app.routes.ts
public/
  i18n/          — translation files (bg.json, en.json)
```

## Localization

Default language: **Bulgarian**. Click the language switcher in the header to switch to English. The choice is persisted in `localStorage`.

All new template strings must use the `translate` pipe (`{{ 'key' | translate }}`). For imperative strings use `TranslateService.instant('key')`. Add every new key to both `public/i18n/bg.json` and `public/i18n/en.json`.
