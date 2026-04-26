# M3-1 — Baseline CI Gates: Implementation Plan

Reference spec: `Docs/specs/M3-1-baseline-ci-gates.md`

Build order: tag Testcontainers tests → backend workflow → react
workflow → angular workflow → smoke-test against a throwaway PR →
CCAF doc.

---

## Step 1 — Tag Testcontainers-using tests

Find the xUnit collection or fixture class in
`Backend/tests/Recipes.Api.Tests/` that owns the
Testcontainers-managed SQL Server. Add the trait at the collection
level so every test that joins the collection inherits it:

```csharp
[CollectionDefinition("Docker")]
[Trait("Category", "Docker")]
public sealed class DockerCollection : ICollectionFixture<RecipesApiFactory> { }
```

If individual classes use the fixture directly (no collection),
add `[Trait("Category", "Docker")]` to each. Verify by running
`dotnet test --filter "Category!=Docker"` locally and confirming the
Testcontainers-backed tests are skipped.

---

## Step 2 — backend-ci.yml

Create `.github/workflows/backend-ci.yml`:

```yaml
name: backend-ci

on:
  pull_request:
    paths:
      - 'Backend/**'
      - '.github/workflows/backend-ci.yml'
  push:
    branches: [main]
    paths:
      - 'Backend/**'

jobs:
  unit:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore Backend/Recipes.sln
      - run: dotnet build Backend/Recipes.sln --no-restore --configuration Release
      - run: dotnet test Backend/Recipes.sln --no-build --configuration Release --filter "Category!=Docker"

  integration:
    runs-on: ubuntu-latest
    needs: unit
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet restore Backend/Recipes.sln
      - run: dotnet build Backend/Recipes.sln --no-restore --configuration Release
      - run: dotnet test Backend/Recipes.sln --no-build --configuration Release --filter "Category=Docker"
```

Notes:
- `needs: unit` keeps the slow Docker job out of the critical path; if
  unit tests fail, integration is skipped.
- `actions/setup-dotnet@v4` supports `.NET 10` once stable; if not yet
  available, pin to a `global.json` checked into the repo root.

---

## Step 3 — frontend-react-ci.yml

Create `.github/workflows/frontend-react-ci.yml`:

```yaml
name: frontend-react-ci

on:
  pull_request:
    paths:
      - 'Frontend/**'
      - '.github/workflows/frontend-react-ci.yml'
  push:
    branches: [main]
    paths:
      - 'Frontend/**'

jobs:
  build:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: Frontend
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: Frontend/package-lock.json
      - run: npm ci
      - run: npm run lint
      - run: npm run build
```

---

## Step 4 — frontend-angular-ci.yml

Create `.github/workflows/frontend-angular-ci.yml`. Angular folder may
be empty/scaffold-pending — guard with a presence check:

```yaml
name: frontend-angular-ci

on:
  pull_request:
    paths:
      - 'FrontendAngular/**'
      - '.github/workflows/frontend-angular-ci.yml'
  push:
    branches: [main]
    paths:
      - 'FrontendAngular/**'

jobs:
  build:
    runs-on: ubuntu-latest
    defaults:
      run:
        working-directory: FrontendAngular
    steps:
      - uses: actions/checkout@v4
      - id: check
        run: |
          if [ -f FrontendAngular/package.json ]; then
            echo "ready=true" >> "$GITHUB_OUTPUT"
          else
            echo "ready=false" >> "$GITHUB_OUTPUT"
          fi
        working-directory: .
      - if: steps.check.outputs.ready == 'true'
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: FrontendAngular/package-lock.json
      - if: steps.check.outputs.ready == 'true'
        run: npm ci
      - if: steps.check.outputs.ready == 'true'
        run: npm run lint
      - if: steps.check.outputs.ready == 'true'
        run: npm run build
```

---

## Step 5 — Smoke-test against a throwaway PR

1. Push a branch that touches only `Backend/Docs/README-test.md` (no
   path-filter match for any workflow). Open a PR. Confirm **none** of
   the three workflows run.
2. Push a commit that touches `Backend/src/Recipes.Domain/Dummy.cs`.
   Confirm **only** `backend-ci` runs, both jobs pass.
3. Push a commit that touches `Frontend/README.md`. Confirm **only**
   `frontend-react-ci` runs.
4. Delete the branch.

---

## Step 6 — CCAF doc

Create `Backend/Docs/CCAF/M3-1-baseline-ci-gates.md` covering:
- What this implements
- CCAF subtopics table (3.6 foundation, 3.3 path-scoping at workflow layer)
- The unit/integration split rationale
- Why path filters mirror the `.claude/rules/*.md` `paths:` frontmatter
- How to add the workflows as required status checks
