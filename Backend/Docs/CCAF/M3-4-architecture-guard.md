# M3-4 — Architecture Guard Workflow

## What this implements

A **blocking** GitHub Actions workflow (`architecture-guard.yml`) that
enforces four hard architecture invariants on every PR touching the
backend source tree. Unlike M3-2 (broad advisory review), this check
exits non-zero when violations are found, failing the required PR status
check. The four rules are defined in `.claude/commands/architecture-check.md`,
which is also invokable locally as `/architecture-check`, making the
rules a single source of truth for both CI and interactive sessions.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (blocking gate) | `.github/workflows/architecture-guard.yml` — required status check; exits non-zero on violation |
| **3.3 — Path-Specific Rules** (workflow-level scoping) | The workflow's `paths:` filter fires only on `Recipes.Application/**`, `Recipes.Domain/**`, `Recipes.Infrastructure/**` — the same scoping logic as the `.claude/rules/backend.md` file |
| **3.2 — Custom Slash Commands** (CI reuse) | `.claude/commands/architecture-check.md` is authored once and consumed by both the CI workflow and local `/architecture-check` sessions |

## Key decisions

- **Blocking vs advisory split** — M3-2 is advisory (broad, noisy)
  and M3-4 is blocking (narrow, high-confidence). Coupling a blocking
  check to a noisy rule set would hurt velocity; the two-layer approach
  lets each operate at its natural confidence level.
- **Rules in a slash command file, not in the workflow YAML** — the
  `architecture-check.md` file is version-controlled alongside the code
  it governs. Adding a fifth rule is a one-line edit to the command,
  automatically picked up by both CI and interactive use.
- **GUARD_RESULT JSON on stdout** — a structured single-line marker
  (`GUARD_RESULT: {"violations": N}`) lets the downstream
  `actions/github-script` step parse the count without fragile line
  scanning. If Claude doesn't emit the line, the step fails as a
  precaution.
- **Rule 4 (CCAF doc enforcement)** — encodes the convention used
  throughout C1–C6: every new AI-service-using slice must have a
  corresponding `Backend/Docs/CCAF/<id>-*.md` entry. CI now prevents
  this from being forgotten on future features.
