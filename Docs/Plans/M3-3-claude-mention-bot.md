# M3-3 — `@claude` Mention Bot: Implementation Plan

Reference spec: `Docs/specs/M3-3-claude-mention-bot.md`

Depends on: M3-1 (baseline CI), M3-2 (workflow precedent + secret).

Build order: workflow → smoke test → kill-switch → CLAUDE.md note → CCAF doc.

---

## Step 1 — claude-mention.yml

Create `.github/workflows/claude-mention.yml`:

```yaml
name: claude-mention

on:
  issue_comment:
    types: [created]
  pull_request_review_comment:
    types: [created]

permissions:
  contents: read
  issues: write
  pull-requests: write

concurrency:
  group: claude-mention-${{ github.event.issue.number || github.event.pull_request.number }}
  cancel-in-progress: true

jobs:
  respond:
    if: >-
      contains(github.event.comment.body, '@claude') &&
      github.event.comment.user.type != 'Bot' &&
      vars.CLAUDE_BOT_ENABLED != 'false'
    runs-on: ubuntu-latest
    timeout-minutes: 6
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0

      - name: React with eyes
        uses: actions/github-script@v7
        with:
          script: |
            const { owner, repo } = context.repo;
            const comment_id = context.payload.comment.id;
            const isReviewComment = context.eventName === 'pull_request_review_comment';
            await (isReviewComment
              ? github.rest.reactions.createForPullRequestReviewComment
              : github.rest.reactions.createForIssueComment)({
                  owner, repo, comment_id, content: 'eyes'
              });

      - name: Claude responds
        uses: anthropics/claude-code-action@v1
        with:
          anthropic_api_key: ${{ secrets.ANTHROPIC_API_KEY }}
          model: claude-haiku-4-5
          max_turns: 8
          allowed_tools: |
            Read
            Grep
            Glob
            Bash(git log:*)
            Bash(git diff:*)
          prompt: |
            You were summoned by a comment on
            ${{ github.event.issue.html_url || github.event.pull_request.html_url }}.

            The user's request follows after the `@claude` mention. Strip
            "@claude" from the start, then answer their question by
            reading the repository (Read/Grep/Glob).

            Comment body:
            ---
            ${{ github.event.comment.body }}
            ---

            When you have an answer, post a single GitHub comment in
            reply (using the action's reply tool). Keep it under 30
            lines. Reference file:line for any code you cite. If the
            user asks you to do something destructive (edit, push, open
            a PR), explain that you are read-only and decline.
```

Notes:
- `concurrency.cancel-in-progress: true` ensures back-to-back mentions
  on the same thread cancel the prior run.
- `actions/github-script@v7` posts the eyes reaction so users know
  the bot saw their comment. The branching covers both issue and
  PR-review-comment endpoints.
- `vars.CLAUDE_BOT_ENABLED` is a repo-level variable (not a secret).
  Set to `false` from the GitHub UI to disable.

---

## Step 2 — Smoke test

1. Open a fresh issue: "Test for the mention bot".
2. Comment `@claude What does Recipes.Application.Common.AI.AiErrorClassifier do?`.
3. Within ~60s, expect:
   - An `eyes` reaction on the comment.
   - A reply comment summarising the classifier with file:line refs.
4. Post a follow-up: `@claude please push a commit fixing the typo`.
   The reply should decline ("I'm read-only").
5. As the same user, edit the original comment (don't post a new one).
   Confirm the workflow does **not** re-fire.
6. Close the issue.

---

## Step 3 — Kill-switch test

1. Set repo variable `CLAUDE_BOT_ENABLED=false`
   (`Settings → Secrets and variables → Actions → Variables`).
2. Post `@claude ping` on any open issue.
3. Confirm no workflow run is queued.
4. Set the variable back to `true` (or delete it — the `!= 'false'`
   guard treats unset as enabled).

---

## Step 4 — CLAUDE.md update

Append to `CLAUDE.md` after the "Automated PR review" section
introduced by M3-2:

```markdown
## Mention bot

Comment `@claude <question>` on any issue or pull-request thread to
ask Claude (claude-haiku-4-5) about the codebase. The bot is read-only
— it can inspect files and git history but cannot push commits or open
PRs. Disable temporarily by setting the repo variable
`CLAUDE_BOT_ENABLED=false`.
```

---

## Step 5 — CCAF doc

Create `Backend/Docs/CCAF/M3-3-claude-mention-bot.md` covering:
- What this implements
- CCAF subtopics table (3.6 interactive, 3.2 dynamic dispatch)
- Why `concurrency.cancel-in-progress` matters (chatty threads)
- Why we filter `comment.user.type != 'Bot'` instead of explicit
  `dependabot[bot]` (catches all bot accounts in one rule)
- Why edits don't retrigger (avoids amplification loops)
- Future extension: `@claude implement <task>` mode that opens a PR
