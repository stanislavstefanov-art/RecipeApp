# M3-1 — Baseline CI Gates

## What this implements

Three GitHub Actions workflows that build and test each part of the
stack on every pull request. `backend-ci` splits into a fast **unit**
job (no Docker required) and an **integration** job that runs the
Testcontainers-backed `Recipes.Api.Tests` on an Ubuntu runner with
Docker available. The two frontend workflows mirror the same structure
for React and Angular. Each workflow's `paths:` filter matches the
`paths:` frontmatter of the corresponding `.claude/rules/*.md` file,
making path-scoped convention loading consistent across Claude Code
sessions and CI alike.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (foundation) | `.github/workflows/backend-ci.yml`, `frontend-react-ci.yml`, `frontend-angular-ci.yml` — the build/test gate that M3-2/3/4 layer Claude steps on top of |
| **3.3 — Path-Specific Rules** (workflow analogue) | Each workflow's `on.pull_request.paths` filter scopes the job to one directory, mirroring the `.claude/rules/*.md` `paths:` frontmatter pattern at the CI layer |

## Key decisions

- **Unit/integration job split** — Testcontainers needs Docker. GitHub-hosted
  `ubuntu-latest` runners have Docker pre-installed; `needs: unit` ensures the
  slow integration job only runs after the fast unit job passes, keeping PR
  feedback fast.
- **`[Trait("Category", "Docker")]` on `RecipesEndpointsTests`** — tag at the
  test class level rather than per test so a new test added to the class
  automatically inherits the category and won't silently run in the wrong job.
- **`frontend-angular-ci` presence check** — Angular folder is scaffold-pending.
  A shell `if` block inside the workflow checks for `package.json` and skips
  all Node steps if absent, so the workflow doesn't fail on an empty directory.
- **Node 24** — matches the local Node version (`node --version` = 24.12.0).
  Pinning ensures CI doesn't silently use a different Node than local dev.
- **Path filter includes the workflow file itself** — changes to a workflow file
  trigger that workflow, so CI isn't stale when a workflow is modified.
