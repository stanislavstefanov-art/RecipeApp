---
paths: ["**/Migrations/**/*"]
---

## EF Core migration rules

- Never edit generated migration files manually.
- Always review the generated Up() and Down() methods before applying.
- Migration names must describe the change: AddRecipeTagsTable not Migration1.
- Run dotnet ef database update locally before committing a migration.