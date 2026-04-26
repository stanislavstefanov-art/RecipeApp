# M3-1 — Baseline CI Gates

## Summary

The repository has zero CI today (no `.github/` directory). Before any
Claude-in-CI feature can land (M3-2, M3-3, M3-4) there must be baseline
GitHub Actions workflows that build and test each part of the stack on
every pull request. M3-1 is plain CI scaffolding — no Claude calls yet —
so M3-2/3/4 have a `workflow_run` or status-check surface to plug into.

Three workflows ship under `.github/workflows/`:

- `backend-ci.yml` — `dotnet restore` → `dotnet build` → `dotnet test`
  on the `Recipes.sln`. Splits into two jobs: a **fast** job that runs
  Domain + Application unit tests on every PR, and a **integration**
  job (Linux + Docker) that runs the Testcontainers-backed
  `Recipes.Api.Tests`. Path filter: any change under `Backend/**`.
- `frontend-react-ci.yml` — `npm ci` → `npm run lint` → `npm run build`
  in `/Frontend`. Path filter: `Frontend/**`.
- `frontend-angular-ci.yml` — same shape for `/FrontendAngular`. Path
  filter: `FrontendAngular/**`. Skipped gracefully if the project is
  still scaffold-pending.

Existing repo conventions (CLAUDE.md commands, paths-scoped rules)
inform the workflow design: each workflow's path filter mirrors the
`paths:` frontmatter of the matching `.claude/rules/*.md` file. A
fourth workflow (`bicep-validate.yml`) is **deferred** until `/infra`
exists.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (foundation) | `.github/workflows/backend-ci.yml`, `.github/workflows/frontend-react-ci.yml`, `.github/workflows/frontend-angular-ci.yml` provide the build/test gate that M3-2/3/4 layer Claude steps on top of. Without these, Claude-in-CI features have nothing to gate against. |
| **3.3 — Path-Specific Rules** (workflow analogue) | Each workflow uses `on.pull_request.paths` to fire only when files in its scoped folder change — the same conditional-loading pattern the `.claude/rules/` files use, applied at the CI layer. |

## Architecture

```
PR opened / synchronize
        │
        ├─ paths: Backend/**            → backend-ci.yml
        │     ├─ unit job (ubuntu-latest, no Docker): build + Domain/Application tests
        │     └─ integration job (ubuntu-latest, Docker available): Recipes.Api.Tests
        │
        ├─ paths: Frontend/**           → frontend-react-ci.yml
        │     └─ npm ci → lint → build
        │
        └─ paths: FrontendAngular/**    → frontend-angular-ci.yml
              └─ npm ci → lint → build (skip if package.json absent)
```

All three workflows publish standard GitHub status checks named
`backend-ci / unit`, `backend-ci / integration`, `frontend-react-ci`,
`frontend-angular-ci`, so branch-protection rules can require them.

## Backend test split

`Recipes.Api.Tests` uses Testcontainers (SQL Server image) and needs
Docker on the runner. Two strategies, choose one:

**Option A — Trait filter (preferred):** mark Testcontainers tests with
`[Trait("Category", "Docker")]` and run:
- unit job: `dotnet test --filter "Category!=Docker"`
- integration job: `dotnet test --filter "Category=Docker"` (Docker is
  available out-of-the-box on `ubuntu-latest` GitHub-hosted runners).

**Option B — Project filter:** unit job runs
`Recipes.Domain.Tests`/`Recipes.Application.Tests` only, integration job
runs `Recipes.Api.Tests`. Faster to ship, but couples CI to project
layout.

Spec assumes **Option A** for cleanliness. Implementation plan covers
the trait-tagging step.

## Files to create

| Path | Purpose |
|---|---|
| `.github/workflows/backend-ci.yml` | Backend build + unit + integration test gate |
| `.github/workflows/frontend-react-ci.yml` | React lint + build gate |
| `.github/workflows/frontend-angular-ci.yml` | Angular lint + build gate |

## Files to modify

| Path | Change |
|---|---|
| `Backend/tests/Recipes.Api.Tests/**/*.cs` (Testcontainers-using fixtures) | Add `[Trait("Category", "Docker")]` to test classes that use the `RecipesApiFactory` Testcontainers fixture, OR add the trait to the fixture itself via `[CollectionDefinition]` |

No application code changes. No `CLAUDE.md` edits required (workflows
are self-documenting).

## Acceptance criteria

1. Opening a PR that changes only `Backend/src/**` triggers
   `backend-ci.yml` and **does not** trigger either frontend workflow.
2. Opening a PR that changes only `Frontend/**` triggers
   `frontend-react-ci.yml` and not the other two.
3. The backend `unit` job passes without Docker.
4. The backend `integration` job passes on `ubuntu-latest`
   (Testcontainers spins up SQL Server inside the runner's Docker).
5. All three workflows are required status checks on `main` (configured
   manually in repo settings — not part of this feature, but documented
   in the CCAF doc).
6. Total wall-clock time for a backend-only PR is under 4 minutes.
