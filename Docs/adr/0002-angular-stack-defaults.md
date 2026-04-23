# ADR 0002: Angular stack defaults for `/FrontendAngular`

- **Status:** Accepted
- **Date:** 2026-04-23
- **Deciders:** Stanislav Stefanov
- **Depends on:** [ADR 0001](./0001-frontend-dual-react-angular.md)

## Context

ADR 0001 commits to adding an Angular frontend at `/FrontendAngular`
alongside the existing React app. Before running `ng new`, the non-default
choices need to be pinned so the scaffold is deterministic and the rules in
`.claude/rules/frontend-angular.md` are defensible.

The Pluralsight course *Angular Foundations: Modern Patterns and Best Practices*
and the broader Angular community consensus both point at the same modern
defaults. Rather than re-derive each decision per feature, lock them here.

## Decision

### Framework and runtime

| Concern | Decision |
|---|---|
| Angular version | Latest stable at scaffold time |
| Component style | **Standalone only** — no `NgModule` |
| Change detection | Zoneless, `OnPush` on every component |
| Reactivity | Signals first (`signal`, `computed`, `effect`) |
| Template control flow | `@if`, `@for`, `@switch` only |
| Dependency injection | `inject()` function, no constructor injection |
| Component I/O | `input()`, `input.required()`, `output()`, `model()` |

### Tooling

| Concern | Decision | Rationale |
|---|---|---|
| Build | Vite / esbuild builder | Faster dev loop; Angular's modern default |
| Lint | `angular-eslint` + Prettier | Standard modern stack |
| Boundaries | `@softarc/sheriff` | Enforce feature/shared/core layering |
| Unit test | Jest or Vitest | Avoid Karma/Jasmine; align with ecosystem direction |
| E2E test | Playwright | Modern, cross-browser, maintained |
| Styling | Tailwind CSS | Mirrors `/Frontend` choice for visual consistency |

### Data and state

| Concern | Decision |
|---|---|
| HTTP | `HttpClient` with functional `HttpInterceptorFn` |
| Async reads | `httpResource()` / `rxResource()` |
| Feature state | Signal store per feature |
| Cross-feature state | NgRx Signal Store — only when genuinely needed |
| Forms | Typed Reactive Forms |
| Error handling | Global `ErrorHandler` + HTTP interceptor that maps errors |

### Routing

- Lazy loading via `loadComponent` / `loadChildren`.
- Functional guards (`CanActivateFn`).
- Route params bound to component inputs via `withComponentInputBinding()`.

### TypeScript strictness

Enable all of: `strict`, `strictTemplates`, `noImplicitOverride`,
`noPropertyAccessFromIndexSignature`, `noFallthroughCasesInSwitch`.

## Scaffold command

These decisions translate into the following `ng new` invocation (also in
`FrontendAngular/README.md`):

```bash
npx @angular/cli@latest new frontend-angular \
  --directory FrontendAngular \
  --style=css \
  --ssr=false \
  --routing \
  --strict \
  --standalone \
  --zoneless \
  --package-manager=npm
```

Post-scaffold steps — Karma → Jest/Vitest, add Tailwind, add
`angular-eslint`/Prettier, add Sheriff with an initial `sheriff.config.ts` —
are tracked separately after the scaffold lands.

## Consequences

**Positive**

- One place to point at when deciding per-feature: "is the answer already in
  ADR 0002?"
- The `ng new` flags are traceable back to a decision, not a guess.

**Negative**

- Some decisions (e.g., "Jest or Vitest") defer the final pick. When the
  scaffold replaces Karma, that choice must be made and this ADR updated.
- NgRx Signal Store usage requires a judgment call per feature — the ADR
  intentionally doesn't prescribe when it's "needed". Expect the first few
  features to recalibrate this rule.

## Revisiting

Revisit this ADR if:

- The Angular team changes defaults (e.g., required-signals policy, SSR
  defaults) in a way that conflicts with these choices.
- Sheriff becomes unmaintained or the boundary model no longer fits.
- Performance profiling reveals that a stack choice here is the bottleneck.
