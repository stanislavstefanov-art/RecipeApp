# M3-4 — Architecture Guard Workflow: Implementation Plan

Reference spec: `Docs/specs/M3-4-architecture-guard.md`

Depends on: M3-1, M3-2.

Build order: extract slash command → workflow → smoke test against
each rule → wire as required check → CLAUDE.md update → CCAF doc.

---

## Step 1 — Extract the rules into a slash command

Create `.claude/commands/architecture-check.md`:

```markdown
Check the diff `git diff origin/$BASE...HEAD` against the four
RecipesApp architecture invariants. Report only violations of these
exact rules — ignore style.

## Rules

1. **No IRecipesDbContext in Application layer.** Any file under
   `Backend/src/Recipes.Application/**` that imports or references
   `IRecipesDbContext` (or `RecipesDbContext`) is a violation. Handlers
   must depend on repository interfaces (`IRecipeRepository`,
   `IMealPlanRepository`, etc.).

2. **No cross-aggregate manipulation in Application.** `Ingredient` and
   `RecipeStep` are entities owned by the `Recipe` aggregate root.
   Application-layer code may read them via a `Recipe` it loaded from
   `IRecipeRepository`, but must not query, project, or mutate them
   without going through the aggregate.

3. **Commands have validators (when they accept user input).** Every
   new `Backend/src/Recipes.Application/**/*Command.cs` whose record
   has at least one parameter likely supplied by an HTTP caller (Guid,
   string, int, complex DTO) must have a sibling `*Validator.cs` in
   the same folder. Queries that take only an id may skip the
   validator.

4. **AI-using slices have a CCAF doc.** Any new file under
   `Backend/src/Recipes.Application/Recipes/**` that injects a Claude
   service interface (`IRecipeCritiqueService`, `IRecipeScalingService`,
   `IRecipeBatchAnalysisService`, `IRecipeDraftReviewService`,
   `IClaudeRecipeImportClient`, etc.) must have an accompanying
   `Backend/Docs/CCAF/<id>-*.md` entry added in the same PR.

## Output

For each finding, post a GitHub review comment using the action's
review-comment tool, citing file:line and which rule was broken.

After all comments are posted, emit a single line on stdout:

    GUARD_RESULT: {"violations": <N>}

where <N> is the integer count of findings.
```

This file is reusable from a local Claude Code session (`/architecture-check`).

---

## Step 2 — architecture-guard.yml

Create `.github/workflows/architecture-guard.yml`:

```yaml
name: architecture-guard

on:
  pull_request:
    types: [opened, synchronize, reopened]
    paths:
      - 'Backend/src/Recipes.Application/**'
      - 'Backend/src/Recipes.Domain/**'
      - 'Backend/src/Recipes.Infrastructure/**'
      - '.claude/commands/architecture-check.md'
      - '.github/workflows/architecture-guard.yml'

permissions:
  contents: read
  pull-requests: write

jobs:
  guard:
    runs-on: ubuntu-latest
    timeout-minutes: 6
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Run architecture check
        id: claude
        uses: anthropics/claude-code-action@v1
        with:
          anthropic_api_key: ${{ secrets.ANTHROPIC_API_KEY }}
          model: claude-haiku-4-5
          max_turns: 6
          allowed_tools: |
            Read
            Grep
            Glob
            Bash(git diff:*)
            Bash(git log:*)
          prompt: |
            BASE=${{ github.base_ref }}
            Run the /architecture-check slash command (defined in
            .claude/commands/architecture-check.md) against the diff
            between origin/$BASE and HEAD. Follow the slash command's
            instructions exactly, including the final
            `GUARD_RESULT: {"violations": <N>}` line.

      - name: Fail if violations found
        if: always()
        uses: actions/github-script@v7
        with:
          script: |
            const output = `${{ steps.claude.outputs.result || '' }}`;
            const m = output.match(/GUARD_RESULT:\s*(\{[^}]+\})/);
            if (!m) {
              core.setFailed('Architecture guard did not emit a GUARD_RESULT line.');
              return;
            }
            const { violations } = JSON.parse(m[1]);
            if (violations > 0) {
              core.setFailed(`Architecture guard found ${violations} violation(s).`);
            } else {
              core.info('Architecture guard: no violations.');
            }
```

The `actions/github-script` step parses the GUARD_RESULT line, fails
the job if violations > 0, and surfaces the count in the run summary.

---

## Step 3 — Smoke-test each rule

Open four throwaway PRs (one per rule) to confirm the guard catches
each violation. Each is a single-file diff:

**Rule 1:** `Backend/src/Recipes.Application/Recipes/GetRecipe/GetRecipeQuery.cs`
add `using Recipes.Infrastructure.Persistence;` near the top. Expect
failed check with rule-1 finding.

**Rule 2:** `Backend/src/Recipes.Application/Recipes/GetRecipe/GetRecipeHandler.cs`
add a method that does `dbContext.Set<Ingredient>().ToList()`. Expect
failed check with rule-2 finding.

**Rule 3:** add `Backend/src/Recipes.Application/Recipes/Foo/FooCommand.cs`
with `record FooCommand(Guid RecipeId, string Note) : IRequest<...>`
and **no** `FooValidator.cs`. Expect failed check with rule-3 finding.

**Rule 4:** add a handler that injects `IRecipeCritiqueService`
without a corresponding `Backend/Docs/CCAF/<id>-*.md`. Expect failed
check with rule-4 finding.

Each smoke-test PR should be closed after confirming the failure.

---

## Step 4 — Configure as required check

In repo `Settings → Branches → Branch protection rules` for `main`:

1. Enable "Require status checks to pass before merging".
2. Add `architecture-guard` to the list.
3. Save.

(This is a manual step — no IaC needed for repo settings unless you
later adopt `terraform-provider-github`.)

---

## Step 5 — CLAUDE.md update

Append a new subsection after the "Key conventions" block:

```markdown
## Architecture invariants (CI-enforced)

The `architecture-guard` workflow (`.github/workflows/architecture-guard.yml`)
fails the PR check on any of these violations:

1. `IRecipesDbContext` referenced in `Recipes.Application/**`.
2. Cross-aggregate access (`Ingredient`, `RecipeStep` outside a
   `Recipe` traversal) in `Recipes.Application/**`.
3. New `*Command.cs` accepting user input without a matching
   `*Validator.cs`.
4. New AI-using slice without a `Backend/Docs/CCAF/<id>-*.md` entry.

The full rule definitions live in `.claude/commands/architecture-check.md`,
which is invokable locally as `/architecture-check`.
```

---

## Step 6 — CCAF doc

Create `Backend/Docs/CCAF/M3-4-architecture-guard.md` covering:
- What this implements
- CCAF subtopics table (3.6 gating, 3.3 workflow-level paths, 3.2 reused command)
- Why blocking rather than advisory (M3-2 vs M3-4 split)
- The four rules and the rationale for each
- How the GUARD_RESULT JSON-on-stdout pattern works
- How to add a fifth rule (one-place edit in `.claude/commands/architecture-check.md`)
