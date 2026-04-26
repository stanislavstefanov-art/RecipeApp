# M3-4 — Architecture Guard Workflow

## Summary

A focused, **blocking** check that runs only when a PR touches files
that matter to the project's architecture invariants. Unlike M3-2
(broad advisory review), M3-4 enforces a small, hard-coded list of
rules and exits non-zero on violation, failing the PR check.

Triggered on `pull_request: [opened, synchronize]` with a path filter
that fires only on changes to `Backend/src/Recipes.Application/**`,
`Backend/src/Recipes.Domain/**`, or `Backend/src/Recipes.Infrastructure/**`.

The workflow runs Claude with a short, opinionated prompt that checks
exactly four things:

1. No `IRecipesDbContext` usage in `Recipes.Application/**`.
2. No cross-aggregate access (e.g. `Ingredient` accessed outside a
   `Recipe` traversal in `Recipes.Application/**`).
3. Every new `*Command.cs` has a matching `*Validator.cs` in the same
   folder (or no `Validator` at all if the command takes no user input
   — Claude judges this).
4. Every new file under `Backend/src/Recipes.Application/Recipes/**`
   that touches an AI service (`IRecipeCritiqueService`,
   `IRecipeScalingService`, etc.) has a corresponding
   `Backend/Docs/CCAF/<id>-*.md` entry.

Findings are posted as PR review comments **and** the workflow exits
non-zero, blocking merge if marked required.

Depends on M3-1 and M3-2.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (gating, not advisory) | `.github/workflows/architecture-guard.yml` exits non-zero on violations and is configured as a required check |
| **3.3 — Path-Specific Rules** (workflow-level conditional loading) | The workflow's `paths:` filter scopes the entire job to architecture-relevant directories; CLAUDE.md and `.claude/rules/backend.md` are the source of truth that the workflow's prompt references |
| **3.2 — Custom Slash Commands** (focused variant) | The prompt is a tightly-scoped derivative of `.claude/commands/review.md`; it could later be extracted into `.claude/commands/architecture-check.md` and shared between local and CI usage |

## Architecture

```
PR opened / synchronize
        │
        ├─ paths: Backend/src/Recipes.Application/**
        │  paths: Backend/src/Recipes.Domain/**
        │  paths: Backend/src/Recipes.Infrastructure/**
        │
        └─ architecture-guard.yml (ubuntu-latest)
              ├─ checkout (fetch-depth: 0)
              ├─ anthropics/claude-code-action@v1
              │     ├─ model: claude-haiku-4-5
              │     ├─ max_turns: 6
              │     ├─ allowed-tools: Read, Grep, Glob, Bash(git diff:*)
              │     └─ prompt: focused 4-rule check; exit code via JSON output
              └─ if findings:
                    ├─ post review comments
                    └─ exit 1 (workflow fails, required check fails)
```

## The four rules in detail

| # | Rule | Why |
|---|---|---|
| 1 | No `IRecipesDbContext` import in any `Recipes.Application/**` file | Application layer must talk to repositories only. CLAUDE.md, `.claude/rules/backend.md`. |
| 2 | No direct manipulation of `Ingredient` or `RecipeStep` entities outside a `Recipe` traversal in Application | Recipe is the only aggregate root. Cross-aggregate access bypasses domain invariants. |
| 3 | New `*Command.cs` files have matching `*Validator.cs` (or take zero user input) | FluentValidation convention from CLAUDE.md. |
| 4 | New AI-service-using slices have a `Backend/Docs/CCAF/<id>-*.md` entry | Enforces the convention used for C1–C6. |

The prompt enumerates these explicitly so Claude doesn't drift into
broad style commentary — that's M3-2's job.

## Surfacing failure

The action's `prompt` ends with a directive: emit a final JSON line
like `{"violations": N}` (where N=0 for clean). A post-step
`actions/github-script@v7` reads the action's output, fails the job if
N>0, and ensures GitHub renders the failed status check.

## Files to create

| Path | Purpose |
|---|---|
| `.github/workflows/architecture-guard.yml` | The guard workflow |
| `.claude/commands/architecture-check.md` | Extracted slash command (optional, but keeps the rules editable in one place) |
| `Backend/Docs/CCAF/M3-4-architecture-guard.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `CLAUDE.md` | Add an "Architecture invariants (CI-enforced)" subsection listing the four rules, with a pointer to the workflow file |

## Acceptance criteria

1. PR introducing `using Recipes.Infrastructure.Persistence;` in any
   `Recipes.Application/**` file fails the `architecture-guard` check
   with a finding citing rule #1.
2. PR adding `Recipes.Application/Recipes/Foo/FooCommand.cs` without a
   `FooValidator.cs` (and the command has properties suggesting user
   input) fails with a finding citing rule #3.
3. PR introducing a new AI-using handler without a matching
   `Backend/Docs/CCAF/<id>-*.md` fails with a finding citing rule #4.
4. PR touching only `Frontend/**` does **not** trigger the workflow.
5. PR touching only `Backend/Docs/**` does **not** trigger the
   workflow.
6. The check appears as a required status check named
   `architecture-guard` and blocks merge when failing.
