# M3-2 — Claude-Powered PR Reviewer

## Summary

The repository has a `.claude/commands/review.md` slash command that
audits staged changes against `CLAUDE.md` architecture rules. Today it
only runs in a local Claude Code session. This feature wires the same
command into GitHub Actions so every pull request gets an automated
architecture review posted as inline review comments.

Triggered on `pull_request: [opened, synchronize, reopened]`. Uses the
official `anthropics/claude-code-action@v1` action, which spins up a
Claude Code session inside the runner with the repo checked out, runs
the `/review` slash command against the PR diff, and posts findings as
GitHub review comments. Uses `claude-haiku-4-5` to keep PR-traffic cost
predictable (~$0.005 per average PR).

The `/review` command itself is **not modified** — same prompt, same
output format. The novelty is the CI surface.

Depends on M3-1 (the workflow runs alongside `backend-ci` and the
frontend workflows; review failure does not block merge by default —
findings are advisory).

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (headline) | `.github/workflows/claude-review.yml` invokes Claude Code on every PR — the canonical "Claude in pipeline" pattern the cert tests for |
| **3.2 — Custom Slash Commands** (CI usage) | The workflow runs the existing `/review` command via the action's `prompt` input, demonstrating that a slash command authored once works in both interactive and headless contexts |
| **3.3 — Path-Specific Rules** (auto-loaded by Claude in CI) | The action reads `CLAUDE.md` and the matching `.claude/rules/*.md` file based on which paths changed in the PR — same conditional convention loading as a local session |

## Architecture

```
PR opened / synchronize
        │
        └─ claude-review.yml (ubuntu-latest)
              ├─ checkout (fetch-depth: 0, so diff is available)
              ├─ anthropics/claude-code-action@v1
              │     ├─ ANTHROPIC_API_KEY: ${{ secrets.ANTHROPIC_API_KEY }}
              │     ├─ model: claude-haiku-4-5
              │     ├─ allowed-tools: Read, Grep, Glob, Bash(git diff:*), Bash(git log:*)
              │     └─ prompt: "Run the /review slash command against this PR's diff.
              │                 Post findings as GitHub review comments grouped by file.
              │                 Use severity levels: high, medium, low."
              └─ writes review comments via the action's GH_TOKEN
```

The action handles the GitHub API plumbing — there is no custom
TypeScript/JS in this feature.

## Authentication

- **Required secret:** `ANTHROPIC_API_KEY` in repo secrets. Provisioned
  manually; out of scope for this feature.
- **Required permission:** `pull-requests: write` and `contents: read`
  in the workflow YAML (default `GITHUB_TOKEN` covers both when granted
  explicitly).
- **No other tokens needed** — the action uses the workflow's
  `GITHUB_TOKEN` for posting comments.

## Cost / scope guardrails

- `model: claude-haiku-4-5` — never escalate to Sonnet from CI.
- `max_turns: 5` — hard cap on the agentic loop. Review is a
  read-only task; five turns is plenty for diff inspection + comment
  drafting.
- `allowed-tools` whitelist — no `Edit`, no `Write`, no
  `Bash(rm:*)`, no network calls beyond the action itself.
- Skip on draft PRs (`if: github.event.pull_request.draft == false`)
  to avoid burning credits on WIP branches.
- Skip on dependabot PRs (architecture review of dep bumps is noise).

## Files to create

| Path | Purpose |
|---|---|
| `.github/workflows/claude-review.yml` | The reviewer workflow |
| `Backend/Docs/CCAF/M3-2-claude-pr-reviewer.md` | CCAF documentation |

## Files to modify

None. The existing `.claude/commands/review.md` is reused unchanged.

(Optional minor edit: add a one-paragraph "Automated PR review" section
to `CLAUDE.md` pointing at the workflow so contributors know to expect
a Claude bot comment.)

## Acceptance criteria

1. A PR that touches `Backend/src/Recipes.Application/**` results in a
   Claude review-comment thread within ~90 seconds of the PR opening.
2. The review references at least one rule from `CLAUDE.md` or
   `.claude/rules/backend.md` (proves the action loaded the correct
   path-scoped rule file).
3. A draft PR does **not** trigger the workflow.
4. A dependabot PR does **not** trigger the workflow.
5. Cost per PR (verified via Anthropic console after a week of use)
   averages under $0.02.
6. The action's run logs show `model=claude-haiku-4-5` and never
   `claude-sonnet-*`.
