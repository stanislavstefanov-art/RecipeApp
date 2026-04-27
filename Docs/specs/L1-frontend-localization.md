# L1 — Frontend Localization (Bulgarian + English)

## Summary

End users of RecipesApp are Bulgarian. Today the UI on both frontends
(`/Frontend` React 19 and `/FrontendAngular` Angular 21) is entirely English
with no i18n infrastructure. This feature adds runtime language switching with
**Bulgarian as the default** and **English as the switchable secondary**, the
choice persisted to `localStorage`, on both apps.

Library choices:
- React: `react-i18next` + `i18next-browser-languagedetector`
- Angular: `@ngx-translate/core` + `@ngx-translate/http-loader`

Both libraries support runtime switching, JSON resource files, and a
`localStorage`-backed detector. ngx-translate is symmetric with react-i18next
and works cleanly with Angular 21 zoneless / signals / standalone setup.

Backend errors stay English. The frontend maps known server error codes
(`AI.timeout`, `Recipe.NotFound`, FluentValidation property names, etc.) to
translated strings via an `errors.*` block in the translation files; unknown
messages pass through verbatim. A future plan will tackle backend `IStringLocalizer`.

---

## Goals

| Goal | Where |
|---|---|
| Default UI language is Bulgarian | i18n init in both apps |
| User can toggle to English from a header switcher | `LanguageSwitcher` (React) + `language-switcher` (Angular) |
| Choice persists across reloads | `localStorage['lang']` |
| Date and currency formatting reflect the chosen locale | `Intl.*` helpers (React), `LOCALE_ID` provider (Angular) |
| Form validation messages are localized | Zod schema builders (React), template-level validation messages (Angular) |
| Known backend error codes are localized | `errors.*` map in translation files |

---

## Out of scope

- Backend i18n (`IStringLocalizer`, `Accept-Language`). Future plan.
- Translating recipe / household / person / expense **content** (user data, not UI).
- Plural forms beyond `i18next`'s built-in count interpolation. Current UI doesn't need them.
- A third locale.

---

## Translation file shape (both apps)

Single namespace per language with feature-grouped keys:

```
Frontend/src/locales/bg.json
Frontend/src/locales/en.json

FrontendAngular/src/assets/i18n/bg.json
FrontendAngular/src/assets/i18n/en.json
```

```jsonc
{
  "nav":     { "recipes": "...", "households": "...", ... },
  "common":  { "loading": "...", "error": "...", "back": "...", "save": "...", "delete": "..." },
  "recipes": { "title": "...", "newRecipe": "...", "addIngredient": "...", "noIngredients": "...", ... },
  "mealPlans":     { ... },
  "shoppingLists": { ... },
  "expenses":      { ... },
  "households":    { ... },
  "persons":       { ... },
  "validation": { "required": "...", "minLength": "...", "greaterThan": "...", ... },
  "enums": {
    "mealType":        { "1": "Закуска", "2": "Обяд", "3": "Вечеря", "4": "Снакс" },
    "expenseCategory": { "1": "Храна", ... },
    "mealScope":       { "1": "Споделено", ... }
  },
  "errors": {
    "AI.timeout":            "AI услугата не отговори навреме.",
    "AI.api_error":          "AI услугата отговори с грешка.",
    "AI.configuration_error":"AI услугата не е конфигурирана.",
    "AI.output_validation":  "Невалиден отговор от AI услугата.",
    "Recipe.NotFound":       "Рецептата не е намерена.",
    "Household.NotFound":    "Домакинството не е намерено."
  }
}
```

---

## Architecture

### React

- `Frontend/src/i18n/index.ts` calls `i18next.use(LanguageDetector).use(initReactI18next).init({ resources, fallbackLng: 'bg' })` once.
- `main.tsx` imports `./i18n` before mounting `<App />`.
- `LanguageSwitcher.tsx` renders two pill buttons (`БГ` / `EN`) bound to `i18n.changeLanguage()`.
- Components use `const { t } = useTranslation();` and reference keys: `t('recipes.title')`.
- A `getCurrentIntlLocale()` helper maps `i18n.language` → `bg-BG` / `en-GB` for `Intl.DateTimeFormat` / `Intl.NumberFormat`.

### Angular

- `app.config.ts` calls `provideTranslateService({ loader, fallbackLang: 'bg', defaultLanguage: 'bg' })`.
- `provideAppLocale()` factory provides `LOCALE_ID` from a function that reads `TranslateService.currentLang` and registers `bg-BG` / `en-GB` locale data.
- `language-switcher` standalone component injects `TranslateService` and writes `localStorage['lang']` on change.
- Templates use `{{ 'recipes.title' | translate }}`. Components inject `TranslateService` for imperative calls (e.g. inside `window.confirm`).

### Backend error mapping

`Frontend/src/lib/getErrorMessage.ts` (and the equivalent Angular HTTP error helper) inspects the JSON response. If the body has a `code` (or `type` field for problem-details) matching a key in `errors.*`, the translated string is returned; otherwise the existing `detail`/`title`/`message` fallback runs. New backend error codes degrade gracefully to the raw English text until added to the map.

---

## Files to create

| Path | Purpose |
|---|---|
| `Frontend/src/i18n/index.ts` | i18next bootstrap |
| `Frontend/src/components/ui/LanguageSwitcher.tsx` | Header switcher |
| `Frontend/src/locales/bg.json`, `en.json` | Resource files |
| `FrontendAngular/src/assets/i18n/bg.json`, `en.json` | Resource files |
| `FrontendAngular/src/app/shared/ui/language-switcher.ts/.html` | Header switcher |
| `Docs/Plans/L1-frontend-localization.md` | Implementation plan |

## Files to modify

Roughly 70 frontend files total — every page and most components have hardcoded
English strings. Full per-bundle list lives in the plan document.

---

## Acceptance criteria

1. `npm run build` passes on both `/Frontend` and `/FrontendAngular`.
2. Default language on both apps is Bulgarian; nav and page headers render in BG on a fresh load.
3. Clicking the `EN` switcher in the header instantly switches every visible string to English.
4. Refreshing the page preserves the chosen language (via `localStorage['lang']`).
5. Submitting a form with empty required fields shows validation messages in the active language.
6. Date and currency formatting on the expense report page differs between BG (e.g. `42,30 €`) and EN (e.g. `€42.30`).
7. Triggering a known backend error (`AI.timeout` simulated) renders the localized error string instead of the raw server message.
8. The set of keys in `bg.json` matches the set in `en.json` for both apps (`jq 'paths'` diff is empty).
9. No hardcoded English literal remains in any `.tsx` / `.html` template surveyed during the review pass.
