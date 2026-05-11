# RecipesApp — React Frontend

React 19 + TypeScript + Vite + TanStack Query + Tailwind frontend for RecipesApp.

## Prerequisites

- Node.js 22+
- Backend API running at `http://localhost:5106` (see [`/Backend`](../Backend/README.md))

## Development

```bash
npm install
npm run dev     # http://localhost:5173
```

## Build & lint

```bash
npm run build
npm run lint
```

## Environment variables

Copy `.env.example` to `.env.local` and adjust as needed:

```bash
cp .env.example .env.local
```

| Variable | Default | Description |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:5106` | Backend API URL |
| `VITE_ENTRA_ENABLED` | `false` | Enable Microsoft Entra SSO |
| `VITE_ENTRA_CLIENT_ID` | — | Entra app client ID |
| `VITE_ENTRA_TENANT_ID` | — | Entra tenant ID |
| `VITE_ENTRA_REDIRECT_URI` | `http://localhost:5173` | OAuth redirect URI |

## Project structure

```
src/
  api/             — typed API clients
  components/
    layout/        — AppLayout, navigation
    ui/            — shared UI primitives (PageState, LanguageSwitcher, …)
  features/        — feature slices
    recipes/
    households/
    persons/
    mealPlans/
    shoppingLists/
    expenses/
    auth/
  pages/           — route entry points
  locales/         — translation files (bg.json, en.json)
  i18n/            — i18next configuration
```

## Localization

Default language: **Bulgarian**. Click the language switcher in the header to switch to English. The choice is persisted in `localStorage`.

All new UI strings must use `t('key')` from `useTranslation()` — add the key to both `locales/bg.json` and `locales/en.json`.
