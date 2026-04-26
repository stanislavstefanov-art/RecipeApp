---
context: fork
allowed-tools: Read, Edit, Glob, Grep
argument-hint: "path to a single source file under 500 lines, e.g. Backend/src/Recipes.Application/Recipes/Foo/FooHandler.cs"
---

# Refine

Run a three-pass critique → revise → re-critique loop on $ARGUMENTS.

## Preconditions

1. $ARGUMENTS must point to an existing file.
2. Count the lines. If over 500, respond:
   "This file is too large for a single refine pass. Point me at a
   narrower scope — a specific method, class, or region — and I'll
   refine that."
3. Read the file, CLAUDE.md, and the `.claude/rules/*.md` whose
   `paths:` frontmatter matches $ARGUMENTS.

## Pass 1 — Critique

Identify up to five real issues across these axes (skip axes with
nothing genuine to flag — do not pad to five):

- Correctness or latent bugs
- Adherence to CLAUDE.md and the matching rule file
- Naming and readability
- Unnecessary complexity (premature abstractions, dead branches,
  duplicated logic)
- Test coverage gaps (mention only — do not fix in this skill)

Emit the critique as a numbered list. Each item names the line(s) and
gives a one-sentence rationale.

If you find zero real issues, output "No issues found — file is clean."
and stop. Do not edit anything.

## Pass 2 — Revise

For each numbered finding from pass 1:

- If the fix is **mechanical** (rename, delete dead branch, simplify a
  condition, fix a typo, remove unused using), apply it as a single
  `Edit` call.
- If the fix is **structural** (move a class, change an interface,
  introduce a new abstraction, requires editing other files), skip the
  edit and mark the finding as "Deferred" in the final report.

Apply fixes smallest-change-first to minimise conflicts between edits.

## Pass 3 — Re-critique

Re-read $ARGUMENTS after all edits. Confirm:

- Each pass-1 finding marked "fixed" was actually addressed.
- No regressions were introduced by the edits.
- Using statements and imports are still coherent.

## Final report

```
Refine: <relative path>
Pass 1 findings: N
Auto-fixed:      M
Deferred:        K
  - <one-line summary per deferred item>
Regressions:     0   (or list them)
```

## Constraints

- Single pass only — do not enter a fourth loop.
- Only modify $ARGUMENTS (and only for trivial import-level follow-ups
  in the same file). Do not create new files.
- Do not run tests — the caller runs the test suite after this skill
  completes.
