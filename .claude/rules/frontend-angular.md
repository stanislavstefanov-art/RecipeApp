---
paths: ["FrontendAngular/**/*"]
---

# Claude Code Rules: Angular Modern Patterns

> Applies to the **Angular** app at `/FrontendAngular`. The sibling React app
> lives at `/Frontend` — do not share source code between the two; reimplement
> each slice idiomatically in each framework.

## Core Tech Stack
- **Framework:** Angular (latest) + TypeScript strict mode + Vite/esbuild builder
- **Runtime:** Zoneless, **standalone components only** (no NgModule)
- **Reactivity:** Signals first — `signal`, `computed`, `effect` (use `effect` sparingly, side-effects only)
- **Styling:** Tailwind CSS
- **HTTP:** `HttpClient` with **functional** interceptors (`HttpInterceptorFn`)
- **Forms:** **Typed Reactive Forms** (never template-driven for non-trivial forms)
- **State:** Signal store per feature; NgRx Signal Store only for cross-feature / cross-route state
- **Routing:** Functional guards (`CanActivateFn`), lazy loading via `loadComponent` / `loadChildren`, `withComponentInputBinding()` for route params
- **Testing:** Jest or Vitest (not Karma), Playwright for E2E
- **Architecture boundaries:** Sheriff (`@softarc/sheriff`)
- **Lint:** `angular-eslint` + Prettier

## Project Structure (Feature-First)
- `src/app/features/[feature-name]/` — feature components, services, signal stores, feature-local types
- `src/app/shared/ui/` — reusable presentational (dumb) components; no service injection
- `src/app/core/` — app-wide singletons (interceptors, global error handler, config, auth)
- `src/app/api/` — typed API clients, DTO types
- `src/app/app.routes.ts` — top-level lazy-loaded route config

## Implementation Rules (non-negotiable)
- **Standalone only.** Never create `NgModule`.
- **Signal I/O.** Use `input()`, `input.required()`, `output()`, `model()` — never `@Input`/`@Output` decorators.
- **DI via `inject()`.** No constructor injection.
- **Modern control flow.** `@if`, `@for`, `@switch` — never `*ngIf` / `*ngFor` / `*ngSwitch`.
- **`@for` must declare `track`.**
- **No manual `.subscribe()` in components.** Use `AsyncPipe` or `toSignal`. If you must subscribe, pair with `takeUntilDestroyed()`.
- **`OnPush` change detection** on every component.
- **Smart vs dumb split.** Feature components inject services; `shared/ui/` components take `input()` / `output()` only.
- **No complex template expressions.** Move logic into `computed()` signals or pure pipes.
- **Lazy-load routes.** Use `loadComponent` for leaf routes, `loadChildren` for sub-trees.
- **`NgOptimizedImage`** for every `<img>` with known dimensions.
- **`@defer`** blocks for below-the-fold or interaction-triggered views.

## Data & State
- Prefer `httpResource()` / `rxResource()` for async reads; signal store for multi-read coordinated state.
- **Never fetch inside a component.** Go through `app/api/` typed clients.
- DTOs typed, kept close to the feature that owns them.
- Error handling centralized via global `ErrorHandler` + a functional `HttpInterceptorFn` that maps HTTP errors to app-level shapes.

## Forms
- **Typed Reactive Forms only.** Declare the form group with generic types.
- Sync validators: pure functions. Async validators for server-side checks.
- Keep form state in the component or a feature signal store — never in a parent via two-way binding gymnastics.

## Testing
- Unit: Jest or Vitest (configure at project creation — don't adopt Karma defaults).
- Integration: `TestBed` with standalone components; use `@angular/cdk/testing` harnesses for DOM interaction.
- Test signals and `computed()` values directly, not via indirect DOM assertions.
- E2E: Playwright.

## Purpose of this app
Learning-focused Angular implementation of the same Recipes domain as the React
app at `/Frontend`. Both consume the same backend API at `/Backend`. Idiomatic
per-framework implementations — the learning value comes from the contrast.

## Recipes slice order (build first)
1. recipes list
2. create recipe
3. recipe details
4. update recipe name
5. add ingredient
6. add step
7. delete recipe

Defer persons / households / meal plans until the recipes slice is end-to-end.

## Feature implementation preference
For each feature:
1. define typed API client in `app/api/`
2. define the signal store / `rxResource()` in the feature folder
3. implement the smart (container) component
4. extract reusable UI into `shared/ui/` only after the Rule of Three
