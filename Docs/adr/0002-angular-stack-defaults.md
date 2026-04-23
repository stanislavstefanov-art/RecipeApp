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
| Angular version | 21.2 at scaffold (latest stable at the time) |
| Component style | **Standalone only** — no `NgModule` |
| Change detection | Zoneless (no `zone.js` in deps), `OnPush` on every component |
| Reactivity | Signals first (`signal`, `computed`, `effect`) |
| Template control flow | `@if`, `@for`, `@switch` only |
| Dependency injection | `inject()` function, no constructor injection |
| Component I/O | `input()`, `input.required()`, `output()`, `model()` |

### Tooling

| Concern | Decision | Rationale |
|---|---|---|
| Build | `@angular/build:application` (esbuild) | Angular 21 default; fast dev loop |
| Lint | `angular-eslint` + Prettier | Standard modern stack (Prettier pre-wired by scaffold) |
| Boundaries | `@softarc/sheriff` | Enforce feature/shared/core layering |
| Unit test | **Vitest** (pre-wired by scaffold) | Replaces Karma/Jasmine; aligns with ecosystem direction |
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

The project was scaffolded with:

```bash
npx @angular/cli@latest new frontend-angular \
  --directory FrontendAngular \
  --style=css --ssr=false --routing \
  --strict --standalone --zoneless \
  --package-manager=npm
```

Notes from the actual run:
- The CLI prompts interactively for "Which AI tools do you want to configure
  with Angular best practices?" — answered **None**. No flag found to suppress
  this prompt; expect it if re-running.
- `--strict` and `--standalone` are defaults in Angular 21 and are effectively
  no-ops; kept in the command for documentation value.

Post-scaffold follow-ups (tracked in separate commits after the scaffold):
add Tailwind, add `angular-eslint`, add Sheriff + initial `sheriff.config.ts`,
enforce `OnPush` via lint rule.

## Consequences

**Positive**

- One place to point at when deciding per-feature: "is the answer already in
  ADR 0002?"
- The `ng new` flags are traceable back to a decision, not a guess.

**Negative**

- NgRx Signal Store usage requires a judgment call per feature — the ADR
  intentionally doesn't prescribe when it's "needed". Expect the first few
  features to recalibrate this rule.

## Revisiting

Revisit this ADR if:

- The Angular team changes defaults (e.g., required-signals policy, SSR
  defaults) in a way that conflicts with these choices.
- Sheriff becomes unmaintained or the boundary model no longer fits.
- Performance profiling reveals that a stack choice here is the bottleneck.
