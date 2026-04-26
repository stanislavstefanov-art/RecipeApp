# M3-3 — `@claude` Mention Bot

## Summary

Extends the Claude-in-CI surface from "automatic PR review" (M3-2) to
"on-demand Q&A in issues and PR threads". A new workflow listens for
comments containing `@claude` on issues, PR conversations, or
PR review threads, and dispatches a Claude Code session that responds
in-thread.

The bot is read-only by default: it can `Read`, `Grep`, `Glob`,
inspect git history, and post a comment. It cannot `Edit`, `Write`, or
push code. A future enhancement (out of scope) could enable
`/implement` mode that opens a follow-up PR.

Triggered on `issue_comment: [created]` and
`pull_request_review_comment: [created]`. The workflow guards against
loops (skip if commenter is `github-actions[bot]`) and rate-limits one
invocation per comment (the trigger is fire-and-forget).

Depends on M3-1 (baseline CI) and the same `ANTHROPIC_API_KEY` secret
used by M3-2.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (interactive variant) | `.github/workflows/claude-mention.yml` shows Claude as a *human-triggered* CI agent, complementing M3-2's automatic gating |
| **3.2 — Custom Slash Commands** (dynamic dispatch) | The bot can be asked to invoke any slash command in `.claude/commands/`, e.g. `@claude run /review on this file` — turning the slash-command directory into an API |

## Architecture

```
Comment "@claude how does ScaleRecipeHandler decide retry?" on issue #42
        │
        └─ claude-mention.yml (ubuntu-latest)
              ├─ if: contains(github.event.comment.body, '@claude')
              │     and github.event.comment.user.login != 'github-actions[bot]'
              ├─ checkout
              ├─ anthropics/claude-code-action@v1
              │     ├─ model: claude-haiku-4-5
              │     ├─ max_turns: 8
              │     ├─ allowed-tools: Read, Grep, Glob, Bash(git log:*), Bash(git diff:*)
              │     └─ prompt: <comment body, with @claude stripped>
              └─ posts a follow-up comment via GH_TOKEN
```

## Trigger surface

| Event | Trigger? | Why |
|---|---|---|
| `issue_comment` (issue body, top-level PR comment) | Yes | General Q&A |
| `pull_request_review_comment` (inline code comment) | Yes | "what does this line do?" |
| `pull_request_review` (overall review submitted) | No | Avoids double-firing alongside review_comment |
| `issues.opened` (new issue) | No | Out of scope; could be added later as `@claude triage` |

## Loop / abuse guardrails

- `if:` filter excludes bot users (`github-actions[bot]`,
  `dependabot[bot]`).
- The action sets a thumbs-up reaction on the triggering comment as
  acknowledgement so users know it's processing — and so successive
  edits don't spam invocations (the workflow only fires on `created`,
  not `edited`).
- `max_turns: 8` cap (slightly higher than M3-2 because Q&A may need a
  few exploration steps).
- `timeout-minutes: 6` workflow-level cap.
- `concurrency` group keyed on issue/PR number — a new mention
  cancels the previous in-flight run on the same thread, so a quick
  back-and-forth doesn't queue up runs.

## Cost / scope guardrails

- `claude-haiku-4-5` only.
- No `Edit`/`Write` tools — bot cannot mutate the repo.
- Token allowance: ~3000 input tokens of repo context per turn.
- Optional repo variable `CLAUDE_BOT_ENABLED=false` to kill-switch the
  workflow without deleting the file.

## Files to create

| Path | Purpose |
|---|---|
| `.github/workflows/claude-mention.yml` | The mention-bot workflow |
| `Backend/Docs/CCAF/M3-3-claude-mention-bot.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `CLAUDE.md` | Add a short "Mention bot" section pointing at the workflow and listing the allowed tool surface |

## Acceptance criteria

1. Posting `@claude what does the AiErrorClassifier do?` on a closed
   PR results in a Claude reply within ~60 seconds.
2. Posting the same comment as the `github-actions[bot]` user does
   **not** trigger the workflow (guards against loops).
3. Editing an existing comment to add `@claude` does **not** retrigger
   the workflow (the trigger is `created` only).
4. Posting two `@claude` comments back-to-back on the same thread
   results in only the second run completing — the first is cancelled
   by the `concurrency` group.
5. Setting `vars.CLAUDE_BOT_ENABLED=false` disables the workflow
   without removing the file.
6. The bot cannot push commits or open PRs (verified by inspecting
   `allowed_tools`).
