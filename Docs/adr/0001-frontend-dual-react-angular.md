# ADR 0001: Maintain parallel React and Angular frontends

- **Status:** Accepted
- **Date:** 2026-04-23
- **Deciders:** Stanislav Stefanov

## Context

The backend API at `/Backend` was originally paired with a single React 19
frontend at `/Frontend`. The repo owner is also learning Angular and wants
hands-on practice building the same domain in both frameworks to compare
ergonomics, performance characteristics, and architectural fit.

The options considered were:

1. **Replace React with Angular.** Lose an already-working implementation and
   the React learning value.
2. **Spin up Angular in a separate repository.** Fragments the domain model,
   forces duplicated backend local-dev setup, and loses the side-by-side
   comparison benefit — the core reason for doing this at all.
3. **Keep both frontends in the same repo, sharing the backend.** Higher
   maintenance cost but directly serves the learning goal.

## Decision

Keep both frontends in the same repository, each in a top-level folder:

- `/Frontend`        — React 19 + Vite + TanStack Query + Zustand + Tailwind
- `/FrontendAngular` — Angular (zoneless, standalone, signals) + Tailwind

Both consume the same `/Backend` API. **Source code is not shared between
the two frontends.** Each slice is reimplemented idiomatically per framework.

Claude Code rules are split per framework and `paths:`-scoped:

- `.claude/rules/frontend-react.md`    → `/Frontend/**/*`
- `.claude/rules/frontend-angular.md`  → `/FrontendAngular/**/*`

## Consequences

**Positive**

- Direct, same-domain comparison between React and Angular.
- Backend changes are exercised by two independent clients, which surfaces
  coupling and API ergonomics issues faster.
- Learning Angular is done against a real, non-trivial domain instead of a toy
  tutorial app.

**Negative**

- Two toolchains to maintain (two `package.json`, two lint configs, two CI
  jobs, two deploy targets).
- Feature parity is a moving target; one side will usually lag.
- Shared concerns (DTO types, validation rules) are duplicated by design. To
  keep drift manageable, generating API types from OpenAPI into both apps
  becomes worthwhile once the API stabilizes.

**Neutral**

- Backend CORS in development must allow both `http://localhost:5173` (React
  dev server) and `http://localhost:4200` (Angular dev server).

## Non-goals

- This is **not** a migration from React to Angular. Both apps are first-class.
- Sharing UI code, hooks/services, or business logic between the two frontends
  is explicitly out of scope.

## Revisiting

Revisit this ADR if:

- The learning goal is met and maintaining both apps becomes pure overhead.
- API-type drift between the two apps causes recurring bugs despite any
  generation strategy.
- One framework is chosen for production deployment and the other can be
  archived.
