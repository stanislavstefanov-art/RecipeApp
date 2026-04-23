# FrontendAngular

Angular implementation of the Recipes domain — sibling to the React app at
`/Frontend`. Both consume the same backend API at `/Backend`.

This folder is intentionally near-empty until the Angular project is scaffolded.

## Scaffold (first-time setup)

Run from the repo root. The flags pin the non-default choices captured in
`.claude/rules/frontend-angular.md` and the forthcoming ADRs:

```bash
npx @angular/cli@latest new frontend-angular \
  --directory FrontendAngular \
  --style=css \
  --ssr=false \
  --routing \
  --strict \
  --standalone \
  --zoneless \
  --package-manager=npm
```

After scaffolding:

1. Replace Karma/Jasmine with Jest or Vitest.
2. Add Tailwind CSS.
3. Add `angular-eslint` + Prettier.
4. Add `@softarc/sheriff` and a starter `sheriff.config.ts`.
5. Confirm `ChangeDetectionStrategy.OnPush` is wired as a default (schematic or
   lint rule).

## Dev server

```bash
npm run start   # http://localhost:4200
```

The React app runs on `http://localhost:5173`. Backend CORS must allow both
origins in development.

## Rules and guidance

- Claude Code rules: `.claude/rules/frontend-angular.md` (path-scoped to this
  folder).
- Best-practices reference: `Docs/angular-best-practices.md` (to be added).
- Architectural decisions: `Docs/adr/` (to be added).

Do not share source code with `/Frontend`. The point of keeping both apps is
the side-by-side contrast — reimplement each slice idiomatically per framework.
