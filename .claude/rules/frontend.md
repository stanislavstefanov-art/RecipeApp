---
paths: ["Frontend/**/*"]
---

## Frontend conventions (placeholder — populated when frontend is scaffolded)

- Framework: React 19 + TypeScript + Vite
- Server state: TanStack Query only — no Redux for server data
- UI state: Zustand stores
- Styling: Tailwind CSS
- Components in /components, pages in /pages, hooks in /hooks
- Always use TypeScript strict mode
- API calls through a typed client in /services — never fetch directly from components