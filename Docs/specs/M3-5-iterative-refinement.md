# M3-5 — Iterative-Refinement Skill

## Summary

The C1–C6 work demonstrated an iterative refinement loop —
spec → plan → implement → review — but no explicit Claude Code skill
operationalises the **review-and-revise inner loop** that polishes a
single file. This feature ships a `refine` skill that runs a
three-pass critique-revise-recritique cycle on a target file, plus an
optional `/spec-from-issue` slash command that produces the C-series
spec template from a GitHub issue body.

Both fit naturally with M3-3 (the mention bot can invoke
`/spec-from-issue` from an issue thread), but neither requires any of
the CI features to function — they are pure local-Claude-Code assets.

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| **3.5 — Iterative Refinement** | `.claude/skills/refine/SKILL.md` runs an explicit critique → revise → re-critique loop on a single file, naming and operationalising the technique |
| **3.2 — Custom Slash Commands & Skills** | Adds a second skill (`refine`) and a second command (`/spec-from-issue`), expanding the project's tiny library of one-of-each |

## The refine skill

```yaml
---
context: fork
allowed-tools: Read, Edit, Glob, Grep
argument-hint: "path to the file to refine, e.g. Backend/src/Recipes.Application/Recipes/Foo/FooHandler.cs"
---
```

**Behaviour:**

1. **Pass 1 — critique.** Read the target file. Identify up to five
   issues across these axes:
   - Correctness / latent bugs
   - Adherence to CLAUDE.md and matching `.claude/rules/*.md`
   - Naming and readability
   - Unnecessary complexity (premature abstractions, dead branches)
   - Test coverage gaps (mention but do not fix — tests are out of scope)
   Emit the critique as a numbered list to the user.

2. **Pass 2 — revise.** Apply each fix as a separate `Edit` call,
   smallest change first. If a fix is non-mechanical (architecture
   change, multi-file refactor) skip and note it for the user.

3. **Pass 3 — re-critique.** Re-read the file post-edit. Confirm each
   pass-1 finding was addressed; flag any new issues introduced by the
   edits.

4. **Final report.** A short summary: N findings, M auto-fixed, K
   deferred (with reasons), 0 regressions (or list them).

**Constraints:**

- Skill runs in a forked context (`context: fork`) so the noisy
  three-pass scratchpad doesn't pollute the main session — same
  pattern as `scaffold-slice`.
- Tool whitelist: `Read, Edit, Glob, Grep`. No `Write` (refine
  doesn't create files), no `Bash` (refine doesn't run tests — that's
  a separate concern).
- If the file is over 500 lines, refuse politely and ask the user to
  point at a smaller scope (a single method, a single class). Refining
  large files is a different problem.

## The spec-from-issue command

```yaml
---
allowed-tools: Read, Write, Bash(gh:*)
argument-hint: "issue number, e.g. 42"
---
```

**Behaviour:**

1. Fetch the issue via `gh issue view $ARGUMENTS --json title,body,labels`.
2. Generate `Docs/specs/<id>-<slug>.md` matching the C-series template
   (`Summary`, `CCAF subtopics covered`, `Architecture`,
   `Files to create`, `Files to modify`, `Acceptance criteria`).
3. Where the issue is ambiguous, leave a `<!-- TODO: ... -->` marker
   rather than guessing.
4. Print the generated path and the next steps:
   "Review the spec, then ask Claude to draft the matching plan."

The command **does not** also generate the plan — that's deliberately
a separate human-in-the-loop step so the user can correct the spec
first.

## Files to create

| Path | Purpose |
|---|---|
| `.claude/skills/refine/SKILL.md` | The refine skill |
| `.claude/commands/spec-from-issue.md` | The spec-from-issue slash command |
| `Backend/Docs/CCAF/M3-5-iterative-refinement.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `CLAUDE.md` | Under "Claude Code workflow guidance", add a bullet documenting the `/refine` and `/spec-from-issue` commands |

## Acceptance criteria

1. `/refine Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeHandler.cs`
   produces a critique → edits → re-critique transcript and modifies
   the file with at most five small edits.
2. `/refine Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeHandler.cs`
   on a file the skill considers clean (no findings) returns a
   "no issues found, file is clean" message and does not edit the
   file.
3. `/refine Backend/Recipes.sln` (a 500+ line file) returns a polite
   refusal asking for a narrower scope.
4. `/spec-from-issue 42` (with issue 42 existing) writes
   `Docs/specs/42-<slug>.md` and prints the path.
5. `/spec-from-issue 99999` (non-existent issue) fails gracefully via
   the underlying `gh` error.
6. Both commands use `claude-haiku-4-5` (the project default) when
   invoked from the mention bot.
