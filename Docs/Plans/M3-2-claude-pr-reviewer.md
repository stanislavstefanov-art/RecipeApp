# M3-2 — Claude-Powered PR Reviewer: Implementation Plan

Reference spec: `Docs/specs/M3-2-claude-pr-reviewer.md`

Depends on: M3-1 (baseline CI). Open a PR after M3-1 has merged so
this workflow can coexist with `backend-ci` / frontend workflows.

Build order: provision secret → workflow → first PR smoke test → tune
prompt → CCAF doc.

---

## Step 1 — Provision ANTHROPIC_API_KEY

Out of band, before merging this feature:

1. Create a billing-capped Anthropic API key (recommend $10/month cap
   while the workflow stabilises).
2. Add it as a GitHub Actions secret at repo level:
   `Settings → Secrets and variables → Actions → New repository secret`,
   name `ANTHROPIC_API_KEY`.
3. Verify with `gh secret list` that the secret exists.

---

## Step 2 — claude-review.yml

Create `.github/workflows/claude-review.yml`:

```yaml
name: claude-review

on:
  pull_request:
    types: [opened, synchronize, reopened]

permissions:
  contents: read
  pull-requests: write
  issues: write

jobs:
  review:
    if: >-
      github.event.pull_request.draft == false &&
      github.event.pull_request.user.login != 'dependabot[bot]'
    runs-on: ubuntu-latest
    timeout-minutes: 8
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: Claude review
        uses: anthropics/claude-code-action@v1
        with:
          anthropic_api_key: ${{ secrets.ANTHROPIC_API_KEY }}
          model: claude-haiku-4-5
          max_turns: 5
          allowed_tools: |
            Read
            Grep
            Glob
            Bash(git diff:*)
            Bash(git log:*)
          prompt: |
            You are reviewing pull request #${{ github.event.pull_request.number }}.

            Read CLAUDE.md and any matching .claude/rules/*.md files for the
            paths that changed. Then run the /review slash command logic
            (see .claude/commands/review.md) against the diff
            `git diff origin/${{ github.base_ref }}...HEAD`.

            For each finding, post one inline GitHub review comment using
            the action's review-comment tool. Group by file. Format each
            finding as:

              [SEVERITY: high|medium|low] <issue>
              Suggested fix: <fix>

            Only report genuine architecture/convention violations. Do not
            comment on style preferences or local idioms.

            If there are no findings, post a single top-level review
            comment: "No architecture issues found."
```

Notes on `anthropics/claude-code-action@v1`:
- The action's `allowed_tools` input is a newline-separated list. Each
  entry is a tool name; `Bash(<pattern>)` whitelists specific
  shell commands.
- The action picks up `CLAUDE.md` automatically when the working
  directory has one. No `cwd:` override needed.
- `max_turns: 5` is the agentic-loop cap — the action terminates after
  five `tool_use` cycles even if the model wants to continue.

---

## Step 3 — Smoke test on a throwaway PR

1. Create a branch with one obvious violation, e.g. a handler that
   directly references `IRecipesDbContext` instead of a repository.
2. Open a PR.
3. Within ~90s, expect a review comment flagging the violation, citing
   either CLAUDE.md or `.claude/rules/backend.md`.
4. Push a fix commit. Confirm the workflow re-runs (`synchronize`
   trigger) and posts an updated review.
5. Mark the PR as draft. Confirm subsequent pushes do **not** trigger
   the workflow.
6. Delete the branch.

---

## Step 4 — Prompt tuning (one iteration)

After three to five real PRs, expect noise. Common adjustments:

- Add to the prompt: "Do not comment on changes to `*.md` files unless
  they reference invalid file paths."
- Add to the prompt: "Do not duplicate findings across multiple
  comments — one finding per file maximum."
- Tighten `allowed_tools` if the model is using `Bash(git log:*)` for
  noise rather than substance.

Each tuning change is a one-line edit to the `prompt:` block in the
workflow. Commit and observe.

---

## Step 5 — Optional CLAUDE.md mention

Add to `CLAUDE.md` (after the "Claude Code workflow guidance"
section):

```markdown
## Automated PR review

Every non-draft pull request gets an automated architecture review from
Claude (claude-haiku-4-5) running the `/review` slash command. Findings
are posted as inline review comments. The review is advisory — failure
does not block merge.
```

---

## Step 6 — CCAF doc

Create `Backend/Docs/CCAF/M3-2-claude-pr-reviewer.md` covering:
- What this implements
- CCAF subtopics table (3.6 headline, 3.2 + 3.3 secondary)
- Why `claude-haiku-4-5` (cost, latency, sufficient for review)
- Why `max_turns: 5` (read-only review, no need for deep loops)
- Why advisory rather than blocking (false-positive rate vs. dev velocity)
- Cost-monitoring checklist
