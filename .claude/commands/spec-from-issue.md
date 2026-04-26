---
allowed-tools: Read, Write, Bash
argument-hint: "GitHub issue number, e.g. 42"
---

# /spec-from-issue

Generate a feature spec under `Docs/specs/` from GitHub issue #$ARGUMENTS.

## Steps

1. Run `gh issue view $ARGUMENTS --json number,title,body,labels`. If
   the call fails (issue does not exist, `gh` not installed), surface
   the error message and stop.

2. Read `Docs/specs/C3-error-envelopes.md` as the reference template.
   Note the sections: Summary, CCAF subtopics covered, Architecture,
   Files to create, Files to modify, Acceptance criteria.

3. Slugify the issue title: lowercase, replace non-alphanumerics with
   `-`, collapse runs of `-`, trim leading/trailing `-`. Target path:
   `Docs/specs/<number>-<slug>.md`.

   If a file already exists at that path, stop and tell the user —
   do not overwrite.

4. Generate the spec by mapping the issue body content into the
   template sections. Where the issue is silent or ambiguous, leave an
   HTML comment placeholder: `<!-- TODO: clarify <topic> -->`.
   Do not invent file paths, acceptance criteria, or CCAF subtopic
   assignments — leave those as TODOs.

5. Write the spec via `Write`. Print the absolute path and:
   "Spec drafted at <path>. Review the TODO markers, then ask me to
   draft the implementation plan."

## Constraints

- Do not generate the plan file in this invocation. The plan is a
  separate step so the user can correct the spec first.
- Do not push changes or open PRs.
- Do not modify any existing file.
