# M3-2 — Claude-Powered PR Reviewer

## What this implements

A GitHub Actions workflow (`claude-review.yml`) that runs a Claude Code
session on every non-draft pull request. Claude reads `CLAUDE.md` and
the matching `.claude/rules/*.md` files for the paths changed, then
runs the `/review` slash command logic against the PR diff and outputs
findings in a structured pipe-delimited format. The workflow is advisory
— it posts findings but never blocks merge. Draft PRs and dependabot
PRs are excluded to avoid wasting credits on WIP and dependency bumps.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (headline) | `.github/workflows/claude-review.yml` — Claude Code invoked on every PR; the canonical "Claude in pipeline" pattern |
| **3.2 — Custom Slash Commands** (CI usage) | The workflow prompt runs the logic of `.claude/commands/review.md` in a headless context, showing a slash command works in both interactive and CI modes |
| **3.3 — Path-Specific Rules** (auto-loaded) | The action loads `CLAUDE.md` and the matching `.claude/rules/*.md` based on which paths changed — same conditional loading as a local session |

## Key decisions

- **`claude-haiku-4-5-20251001` model** — CLAUDE.md mandates haiku for all
  runtime app calls; the same constraint applies to CI automation. Haiku
  handles a review-quality analysis at a fraction of Sonnet cost.
- **`max_turns: 5`** — review is read-only. Five turns is more than enough
  for diff inspection plus comment drafting; capping prevents runaway loops
  on large PRs.
- **Advisory, not blocking** — false positives from a broad review hurt dev
  velocity more than a missed convention. The architecture-guard workflow
  (M3-4) blocks on a smaller, high-confidence rule set; this workflow
  provides broad advisory coverage on top.
- **Draft and dependabot exclusions** — draft PRs are in-progress by
  definition; reviewing them wastes credits. Dependabot bumps have no
  application code to review against CLAUDE.md rules.
- **Structured FINDING output** — pipe-delimited lines make the output
  programmatically parseable by downstream steps without needing Claude to
  call the GitHub API directly.
