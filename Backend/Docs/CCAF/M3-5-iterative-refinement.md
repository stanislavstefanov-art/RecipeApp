# M3-5 — Iterative-Refinement Skill

## What this implements

Two Claude Code assets that operationalise the iterative-refinement
workflow used throughout the C-series work:

1. **`/refine` skill** (`.claude/skills/refine/SKILL.md`) — three-pass
   critique → revise → re-critique loop on a single source file. Runs
   in a forked context so the scratchpad doesn't pollute the main
   session. Applied to C1–C6 files it would catch issues a static
   linter misses: naming drift, premature abstractions, missing
   validators, CLAUDE.md rule violations.

2. **`/spec-from-issue` command** (`.claude/commands/spec-from-issue.md`)
   — fetches a GitHub issue and generates a `Docs/specs/<n>-<slug>.md`
   matching the C-series spec template. Deliberately stops before
   generating the plan, enforcing a human review gate between spec
   and implementation.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.5 — Iterative Refinement** | `.claude/skills/refine/SKILL.md` — names and operationalises the three-pass technique, making refinement a first-class workflow step rather than an ad-hoc practice |
| **3.2 — Custom Slash Commands & Skills** | Adds a second skill (`/refine`) and a second command (`/spec-from-issue`), expanding the project's command library beyond the single `review` command |

## Key decisions

- **Three passes, not two or four** — two passes (critique + revise)
  can't catch regressions introduced by the edit; four passes offers
  diminishing returns. Three is the minimal cycle that closes the loop.
- **`context: fork`** — matches the existing `scaffold-slice` skill
  pattern. Forked context keeps the verbose reflection scratchpad out
  of the main session's context window, which is especially valuable
  for large files near the 500-line cap.
- **500-line cap** — files over 500 lines typically contain multiple
  concerns that should be split before refinement. The cap enforces
  that discipline rather than silently producing a shallow review.
- **`/spec-from-issue` stops at the spec** — allowing the command to
  also generate the plan would skip the human review of the spec, which
  is where architectural decisions get made. The deliberate stop after
  writing the spec preserves the spec → human-review → plan workflow
  that C1–C6 demonstrated.
- **`/refine` defers structural findings** — structural fixes (move a
  class, change an interface) require context beyond the single file.
  Deferring them to the report keeps the skill's scope mechanical and
  its edits safe to auto-apply.
