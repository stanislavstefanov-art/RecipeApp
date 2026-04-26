# M3-5 — Iterative-Refinement Skill: Implementation Plan

Reference spec: `Docs/specs/M3-5-iterative-refinement.md`

No CI dependencies — this feature is purely local Claude Code assets.
Can ship before, after, or in parallel with M3-1..M3-4.

Build order: refine skill → spec-from-issue command → smoke tests →
CLAUDE.md update → CCAF doc.

---

## Step 1 — Create the refine skill

Create `.claude/skills/refine/SKILL.md`:

```markdown
---
context: fork
allowed-tools: Read, Edit, Glob, Grep
argument-hint: "path to a single source file under 500 lines, e.g. Backend/src/Recipes.Application/Recipes/Foo/FooHandler.cs"
---

# Refine

Run a three-pass critique → revise → re-critique loop on $ARGUMENTS.

## Preconditions

- $ARGUMENTS must point to an existing file.
- The file must be under 500 lines. If larger, respond:
  "This file is too large for a single refine pass. Point me at a
  narrower scope — a single method, class, or extracted region — and
  I'll refine that."
- Read the file, plus CLAUDE.md and the matching `.claude/rules/*.md`
  (whichever path-scope covers $ARGUMENTS).

## Pass 1 — Critique

Identify up to five issues across these axes (skip axes with nothing
to flag — do not pad):

- Correctness or latent bugs
- Adherence to CLAUDE.md and the matching rule file
- Naming and readability
- Unnecessary complexity (premature abstractions, dead branches,
  duplicated logic)
- Test coverage gaps (mention only — do not fix)

Emit the critique as a numbered list. Each item names the line(s) and
gives a one-sentence rationale.

If you find zero real issues, stop here and tell the user
"No issues found, file is clean." Do not edit anything.

## Pass 2 — Revise

For each numbered finding from pass 1:

- If the fix is mechanical (rename, delete dead branch, simplify a
  condition, fix a typo, tighten a type), apply it via a single
  `Edit` call.
- If the fix is structural (move a class, change an interface,
  introduce a new abstraction, edit other files), skip the edit and
  list the finding under "Deferred" at the end.

Apply edits smallest-change-first to minimise risk of conflicts
between fixes.

## Pass 3 — Re-critique

Re-read $ARGUMENTS after the edits. Confirm:

- Each pass-1 finding marked "fixed" actually got addressed.
- No new issues were introduced (regressions).
- Imports and using-statements are still consistent.

## Final report

```
Refine: <relative path>
- Findings: N
- Auto-fixed: M
- Deferred (need human judgement): K
  - <one-line summary per deferred>
- Regressions: 0
```

## Constraints

- One-shot only. Do not enter a four-pass or higher loop.
- Do not modify files other than $ARGUMENTS, except for trivial
  follow-ups in the same folder (e.g. updating a using-statement).
- Do not create new files.
- Do not run tests — the user runs the test suite after refine completes.
```

---

## Step 2 — Create the spec-from-issue command

Create `.claude/commands/spec-from-issue.md`:

```markdown
---
allowed-tools: Read, Write, Bash(gh:*)
argument-hint: "issue number, e.g. 42"
---

# /spec-from-issue

Generate a feature spec under `Docs/specs/` from GitHub issue
#$ARGUMENTS.

## Steps

1. Run `gh issue view $ARGUMENTS --json number,title,body,labels`. If
   the call fails (issue does not exist, repo not linked to GitHub,
   `gh` not installed), surface the error and stop.

2. Read `Docs/specs/C3-error-envelopes.md` as the reference template
   for spec structure. Notice the sections:
   - Summary
   - CCAF subtopics covered
   - Architecture
   - Files to create / Files to modify
   - Acceptance criteria

3. Slugify the issue title: lowercase, replace non-alphanumerics with
   `-`, collapse runs of `-`, trim. Target path:
   `Docs/specs/<number>-<slug>.md`. If a file already exists at that
   path, refuse — do not overwrite.

4. Generate the spec by mapping issue body content into the template
   sections. Where the issue is silent or ambiguous, leave an HTML
   comment placeholder: `<!-- TODO: clarify <topic> -->`. Do not
   invent file paths or acceptance criteria.

5. Write the file via `Write`. Print the absolute path and:

   "Spec drafted at <path>. Review and edit the TODO markers, then
    ask me to draft the implementation plan."

## Constraints

- Do not generate a plan file in the same invocation. The plan is a
  separate human-in-the-loop step so the user can fix the spec first.
- Do not push changes or open PRs.
```

---

## Step 3 — Smoke tests

### Refine skill

1. Pick a moderately busy handler:
   `Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeCommand.cs`.
2. `/refine Backend/src/Recipes.Application/Recipes/CritiqueRecipe/CritiqueRecipeCommand.cs`.
3. Verify: critique block printed, ≤5 small edits applied,
   re-critique with no regressions, final report block.
4. Pick a deliberately tiny clean file (e.g. an
   `INutritionAnalysisAgent` interface that's just a method
   signature). Run `/refine` on it.
5. Verify: "No issues found, file is clean." No edits.
6. Run `/refine Backend/Recipes.sln`.
7. Verify: refusal with the over-500-lines message.

### spec-from-issue command

Requires at least one open issue in the repo.

1. Open a fresh issue titled "Add ingredient pantry tracking" with a
   3-paragraph body.
2. `/spec-from-issue <issue-number>`.
3. Verify: `Docs/specs/<n>-add-ingredient-pantry-tracking.md` exists,
   has all template sections, contains TODO markers where the body
   is silent.
4. Re-run the same command. Verify: refusal (file already exists).
5. `/spec-from-issue 9999999`.
6. Verify: error from `gh` is surfaced, no file written.

---

## Step 4 — CLAUDE.md update

In the "Claude Code workflow guidance" section, append:

```markdown
- Use `/refine <path>` to run a three-pass critique → revise →
  re-critique loop on a single source file under 500 lines. The skill
  forks context so the scratchpad doesn't pollute the main session.
- Use `/spec-from-issue <number>` to draft a `Docs/specs/<n>-*.md`
  file from a GitHub issue body. The command writes the spec only —
  drafting the plan is a follow-up step so you can correct the spec
  first.
```

---

## Step 5 — CCAF doc

Create `Backend/Docs/CCAF/M3-5-iterative-refinement.md` covering:
- What this implements
- CCAF subtopics table (3.5 named, 3.2 expanded library)
- Why three passes (one is too few to catch regressions, four is over-engineering)
- Why `context: fork` (matches `scaffold-slice`; keeps reflection out of main session)
- Why spec-from-issue does not auto-generate the plan (intentional human gate)
- How `/refine` complements `/architecture-check` (refine = local readability;
  architecture-check = invariant enforcement)
