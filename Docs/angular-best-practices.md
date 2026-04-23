# Angular Best Practices Reference

Reference checklist used while implementing the Angular app at `/FrontendAngular`.

Source material: the Pluralsight course *Angular Foundations: Modern Patterns
and Best Practices* (course table of contents captured in `Docs/Screenshots/`),
expanded with commonly recommended modern Angular practices not covered by the
course TOC.

This is **reference** material, not enforcement. Non-negotiable rules live in
`.claude/rules/frontend-angular.md`. If a rule here contradicts that file, the
rule file wins.

---

## 1. Modern Angular Defaults and Style Guide Essentials

- Enable strict modes for TypeScript and Angular (`strict`, `strictTemplates`,
  `noImplicitOverride`, `noPropertyAccessFromIndexSignature`,
  `noFallthroughCasesInSwitch`).
- Use modern build tooling (Vite / esbuild builder).
- Use Signals for reactivity.
- Build zoneless applications.
- Use standalone components; no `NgModule`.
- Use modern control flow syntax (`@if`, `@for`, `@switch`).
- Follow the Angular Style Guide for naming and project structure.
- Follow the Angular Style Guide for components, directives, and services.
- Use `inject()` over constructor injection.
- Prefer `providedIn: 'root'` for tree-shakeable services.
- Configure `angular-eslint` + Prettier from day one.

## 2. Modern Angular Architectural Patterns

- Embrace emergent architecture — let structure evolve with real needs.
- Use the feature shell pattern for feature-scoped composition.
- Enforce module boundaries with Sheriff.
- Evaluate monorepo patterns when running multiple applications.
- Consider micro-frontends only for genuinely independent product lines.
- Separate smart (container) and presentational (dumb) components.
- Lazy-load routes via `loadComponent` / `loadChildren`.
- Use functional route guards (`CanActivateFn`) and resolvers.
- Bind route params to component inputs via `withComponentInputBinding()`.

## 3. Modern Angular State Management

- Start with signal-based local state; reach for a store only when coordination
  across components demands it.
- Use signal stores for feature-local state.
- Use NgRx Signal Store for cross-feature / cross-route state.
- Traditional RxJS-based stores remain valid but are no longer the default.
- Use `computed()` for derived state.
- Use `effect()` sparingly — side-effects only, never to chain state.
- Use signal inputs / outputs: `input()`, `input.required()`, `output()`,
  `model()` — not the `@Input` / `@Output` decorators.
- Prefer `rxResource()` / `httpResource()` for async data sources.
- Use `takeUntilDestroyed()` instead of manual subscribe/unsubscribe.

## 4. Load and Runtime Performance

- Diagnose with Chrome DevTools Network and Performance tabs.
- Use Angular DevTools to profile change detection and hydration.
- Set `ChangeDetectionStrategy.OnPush` on every component.
- Monitor application bundle sizes; enforce budgets in `angular.json`.
- Use `@defer` blocks for below-the-fold or interaction-triggered views.
- Provide `track` expressions on every `@for` loop.
- Enable SSR + hydration (including incremental hydration) when meaningful.
- Configure preloading strategies for lazy routes.
- Use `NgOptimizedImage` for every `<img>` with known dimensions.

## 5. General Coding Best Practices

- Apply the Single Responsibility Principle.
- Use small functions.
- Avoid complex logic in components; move it to services, `computed()`
  signals, or pure pipes.
- Avoid `any`; rely on strict TypeScript and `readonly` where applicable.
- Keep templates free of complex expressions — no method calls, no inline
  arithmetic beyond trivial cases.
- Prefer pure pipes over method calls in templates.

## 6. Forms

- Use Reactive Forms over template-driven for non-trivial forms.
- Use strictly typed form groups.
- Write custom validators as pure functions.
- Use async validators for server-side checks.

## 7. HTTP and Data Access

- Use `HttpClient` with functional interceptors (`HttpInterceptorFn`) for
  auth, logging, and error mapping.
- Centralize error handling via a global `ErrorHandler`.
- Type DTOs explicitly; map at the boundary, not in components.

## 8. Testing

- Use Jest or Vitest; don't adopt Karma/Jasmine defaults.
- Unit-test standalone components via `TestBed`.
- Use `@angular/cdk/testing` harnesses for component interaction.
- Test signals and `computed()` values directly.
- Use Playwright for E2E.

## 9. Accessibility and Internationalization

- Prefer semantic HTML; add ARIA only when it adds meaning.
- Use `@angular/cdk/a11y` for focus management and keyboard navigation.
- Use `@angular/localize` for i18n when multilingual support becomes a
  requirement.
