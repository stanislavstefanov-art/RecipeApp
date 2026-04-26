# M3-3 — `@claude` Mention Bot

## What this implements

A GitHub Actions workflow (`claude-mention.yml`) that listens for
`@claude` in issue and pull-request comments, dispatches a Claude Code
session to answer the question by reading the repository, and posts the
reply as a follow-up comment. The bot is strictly read-only — it cannot
edit files or push commits. Loop protection prevents the bot from
responding to its own replies. A concurrency group per thread cancels
an in-flight run if a new `@claude` appears on the same thread.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.6 — CI/CD Integration** (interactive variant) | `.github/workflows/claude-mention.yml` — Claude as a human-triggered CI agent; complements M3-2's automatic gating |
| **3.2 — Custom Slash Commands** (dynamic dispatch) | The bot can be directed to invoke any slash command in `.claude/commands/`, turning the command directory into a conversational API |

## Key decisions

- **`comment.user.type != 'Bot'`** — filters all bot accounts in one
  condition rather than an enumerated allow-list, so future bots are
  automatically excluded without touching the workflow.
- **`concurrency.cancel-in-progress: true` keyed on issue/PR number** —
  a chatty back-and-forth on the same thread produces at most one
  in-flight Claude run at a time; older runs are cancelled, not queued.
- **`eyes` reaction before Claude starts** — gives immediate feedback
  that the trigger was recognised, so users don't post duplicate
  `@claude` messages thinking nothing happened.
- **`created` trigger only (not `edited`)** — editing a comment could
  be used to amplify runs; this constraint removes that surface.
- **`vars.CLAUDE_BOT_ENABLED` kill-switch** — a repository variable
  (not a secret) so it can be toggled from the GitHub UI without a
  code change. Treating an unset variable as `true` means the default
  state is enabled; ops staff can disable quickly without a commit.
- **BOT_REPLY extraction** — Claude is asked to end with a
  single-line `BOT_REPLY:` marker so the github-script step can extract
  a clean reply from a potentially multi-paragraph run log.
