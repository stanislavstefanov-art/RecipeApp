# L1 — Frontend Localization: Implementation Plan

Reference spec: `Docs/specs/L1-frontend-localization.md`

Build order: bootstrap React (L1-1) → bootstrap Angular (L1-4) →
React full sweep (L1-2) → Angular full sweep (L1-5) →
React validation + server-error mapping (L1-3) → docs (L1-6).

Each bundle is one commit.

---

## Bundle L1-1 — React i18n bootstrap

1. From `/Frontend`: `npm i i18next react-i18next i18next-browser-languagedetector`.
2. Create `Frontend/src/i18n/index.ts`:
   ```ts
   import i18n from 'i18next';
   import { initReactI18next } from 'react-i18next';
   import LanguageDetector from 'i18next-browser-languagedetector';
   import bg from '../locales/bg.json';
   import en from '../locales/en.json';

   i18n
     .use(LanguageDetector)
     .use(initReactI18next)
     .init({
       resources: { bg: { translation: bg }, en: { translation: en } },
       fallbackLng: 'bg',
       lng: 'bg',
       detection: { order: ['localStorage', 'htmlTag'], caches: ['localStorage'] },
       interpolation: { escapeValue: false },
     });

   export default i18n;
   ```
3. Edit `Frontend/src/main.tsx`: `import './i18n';` before the `<App />` mount.
4. Create `Frontend/src/locales/bg.json` and `en.json` with `nav.*` and `common.*` keys only.
5. Create `Frontend/src/components/ui/LanguageSwitcher.tsx` — two pill buttons (`БГ` / `EN`); the active one is highlighted; clicking calls `i18n.changeLanguage('bg' | 'en')`.
6. Edit `Frontend/src/components/layout/AppLayout.tsx` to render `<LanguageSwitcher />` in the header. Translate the nav labels with `t('nav.recipes')` etc.

**Verify:** `npm run build`. Open the app — nav is in BG by default; `EN` toggle switches it; reload preserves the choice.

---

## Bundle L1-4 — Angular i18n bootstrap

1. From `/FrontendAngular`: `npm i @ngx-translate/core @ngx-translate/http-loader --legacy-peer-deps`.
2. Edit `FrontendAngular/src/app/app.config.ts`. Add the translate service, an HTTP loader pointing at `assets/i18n/{lang}.json`, default `bg`, fallback `bg`. Bootstrap the saved-language read here.
3. Verify `angular.json` includes `src/assets` in the build's asset globs.
4. Create `FrontendAngular/src/assets/i18n/bg.json` and `en.json` mirroring the React shape (nav + common only).
5. Create `FrontendAngular/src/app/shared/ui/language-switcher.ts` (+ `.html`) — standalone component with two buttons; click calls `translate.use(lang)` and writes `localStorage['lang']`.
6. Edit `FrontendAngular/src/app/app.html` to render `<app-language-switcher />`. Replace hardcoded nav labels with `{{ 'nav.recipes' | translate }}`.

**Verify:** `ng build`. Same UX as L1-1.

---

## Bundle L1-2 — React full-app translation sweep

1. Add full key set to `bg.json` / `en.json` — `recipes.*`, `mealPlans.*`, `shoppingLists.*`, `expenses.*`, `households.*`, `persons.*`, `validation.*`, `enums.*`. Source of truth for English values is the existing literals in code.
2. Sweep the codebase, replacing hardcoded JSX text with `t(...)`. Order: `PageState.tsx` and `ToastViewport.tsx` first (used everywhere), then page-by-page (recipes → households → persons → meal plans → shopping lists → expenses), then components inside each feature.
3. Add a `getCurrentIntlLocale()` helper in `Frontend/src/i18n/index.ts` (or a new `lib/locale.ts`) that returns `'bg-BG'` / `'en-GB'` from `i18n.language`. Replace hardcoded `"en-GB"` in `features/expenses/utils.ts:35,51` and `features/mealPlans/utils.ts:34`.
4. Replace enum-label switch cases (`getExpenseCategoryLabel`, `getMealTypeLabel`, `getMealScopeLabel`, `getExpenseSourceTypeLabel`) with `t('enums.<group>.' + value)` lookups (or thin wrappers that take `t` as an argument).

**Verify:** `npm run build`. Click through every page in BG, then EN — every label switches; dates and currency reflect locale.

---

## Bundle L1-5 — Angular template translation sweep

1. Add full key set to `bg.json` / `en.json` (same structure as React).
2. Sweep the 25 feature template files, replacing literal text with `{{ 'key' | translate }}`. Use `[attr.aria-label]="'key' | translate"` for ARIA. Preserve interpolation: `{{ 'key' | translate:{count: …} }}`.
3. Replace inline validator-error messages (`is required`, `must be N characters or fewer`) with translated keys. Use `@if (form.controls.x.errors?.required) { {{ 'validation.required' | translate }} }`.
4. Replace component-level constants like `CATEGORY_LABELS` (in `expenses-list.ts`) with `translate.instant('enums.expenseCategory.' + value)` calls or a pure pipe.
5. Replace `recipes-details.ts:68` `window.confirm('Delete this recipe?')` with `window.confirm(this.translate.instant('recipes.confirmDelete'))`.
6. Add a small locale provider in `app.config.ts` that registers `bg-BG` and `en-GB` locale data and binds `LOCALE_ID` (via a factory) to the active language so `DatePipe`/`DecimalPipe` follow the user's choice.

**Verify:** `ng build`. Same click-through as L1-2 on the Angular app.

---

## Bundle L1-3 — React validation + server-error mapping

1. Convert each Zod schema in `Frontend/src/features/**/schemas.ts` from a top-level `z.object(...)` to a builder function that takes a `t` argument:
   ```ts
   export const createRecipeSchema = (t: TFunction) =>
     z.object({ name: z.string().min(1, t('validation.required')) });
   ```
   Update form components to call `useTranslation()` and pass `t` into the schema builder.
2. Edit `Frontend/src/lib/getErrorMessage.ts` to inspect the response body for a `code` (or problem-details `type` suffix). If a known key exists under `errors.*`, return `t('errors.' + code)`. Otherwise fall back to the existing path.
3. Populate `errors.*` in both translation files with the AI envelope codes (`AI.api_error`, `AI.timeout`, `AI.configuration_error`, `AI.output_validation`) and a few common app codes (`Recipe.NotFound`, `Household.NotFound`, `MealPlanSuggestion.NoHouseholdMembers`, etc.).

**Verify:** Empty-form submit shows BG validation messages. Force a server `AI.configuration_error` (set `RecipeImport:Provider=Claude` with empty key) — UI shows BG translated text.

---

## Bundle L1-6 — Documentation

Edit `CLAUDE.md`. Add a "Localization" subsection just below "Frontend implementation guidance" covering:
- Two languages (`bg` default, `en` secondary). All new UI strings must be added to both `*.json` files.
- React: `useTranslation()` hook from `react-i18next`.
- Angular: `translate` pipe / `TranslateService`.
- Backend errors: known codes mapped under `errors.*`; unknown ones pass through.
- Date/currency: derive locale from i18n; never hardcode `"en-GB"` again.

Optionally add a one-line rule to `.claude/rules/frontend-react.md` and `.claude/rules/frontend-angular.md` requiring all new user-facing strings to use `t()` / `translate`.

---

## Files to modify (cross-bundle)

| Path | Bundle |
|---|---|
| `Frontend/package.json` | L1-1 |
| `Frontend/src/i18n/index.ts` (new) | L1-1 |
| `Frontend/src/main.tsx` | L1-1 |
| `Frontend/src/components/ui/LanguageSwitcher.tsx` (new) | L1-1 |
| `Frontend/src/components/layout/AppLayout.tsx` | L1-1 |
| `Frontend/src/locales/{bg,en}.json` (new) | L1-1 → L1-3 |
| `Frontend/src/components/ui/{PageState,ToastViewport}.tsx` | L1-2 |
| `Frontend/src/pages/**/*.tsx` (~20 files) | L1-2 |
| `Frontend/src/features/**/components/*.tsx` (~20 files) | L1-2 |
| `Frontend/src/features/{expenses,mealPlans}/utils.ts` | L1-2 |
| `Frontend/src/features/**/schemas.ts` (~6 files) | L1-3 |
| `Frontend/src/lib/getErrorMessage.ts` | L1-3 |
| `FrontendAngular/package.json` | L1-4 |
| `FrontendAngular/src/app/app.config.ts` | L1-4 / L1-5 |
| `FrontendAngular/src/assets/i18n/{bg,en}.json` (new) | L1-4 → L1-5 |
| `FrontendAngular/src/app/shared/ui/language-switcher.ts/.html` (new) | L1-4 |
| `FrontendAngular/src/app/app.html` | L1-4 |
| `FrontendAngular/src/app/features/**/*.html` (~25 files) | L1-5 |
| `FrontendAngular/src/app/features/**/*.ts` | L1-5 |
| `CLAUDE.md` | L1-6 |

---

## Verification

```
# React
cd Frontend && npm install && npm run lint && npm run build

# Angular
cd FrontendAngular && npm install --legacy-peer-deps && npm run build

# Backend (mock mode, no DB / API key)
dotnet run --project Backend/src/Recipes.Api
```

Manual click-through:

1. Default language is BG on both apps.
2. Toggle to EN — every visible string switches.
3. Reload — language sticks.
4. Open the seeded `Banana Bread` recipe — action buttons translated.
5. Empty `Create recipe` form submit — validation messages in active language.
6. Open expenses report — currency `42,30 €` (BG) / `€42.30` (EN); dates likewise.
7. Force a backend error — UI shows localized message.

Acceptance: see spec.

---

## Review pass

- Grep for `"Loading"`, `"Failed"`, `"Save"`, `"Cancel"`, `"Delete"`, `"Add"`, `"Create"` in `.tsx` / `.html` files. Any hits other than translation-key content are misses.
- Diff `bg.json` paths against `en.json` paths — must match exactly (run on both apps).
- Manual click-through on at least 5 pages per language to spot leaks.
