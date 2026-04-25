# F9 — Claude Code Sub-Agent backed by the MCP Server

## Summary

Adds a Claude Code sub-agent definition that gives developers a specialised assistant
for querying live RecipesApp data from within their Claude Code session. The agent is
backed by the `recipes-app` MCP server registered in `.claude/mcp.json` (from F4/F6)
and is scoped to the 12 MCP tools only — no file-editing capabilities.

No backend code is modified.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| Claude Code ecosystem integration | `.claude/agents/recipe-assistant.md` — auto-discovered by Claude Code |
| Agent definition files | Frontmatter: `name`, `description`, `tools`; system prompt for behaviour |
| MCP server registration | `.claude/mcp.json` already committed — zero additional setup per developer |
| Tool scoping in sub-agents | `tools:` list restricts agent to `mcp__recipes-app__*` only |
| Agent routing | `description:` field acts as routing signal for Claude Code |

---

## Architecture

```
Developer: "@recipe-assistant What recipes contain chicken?"
          │
          ▼
Claude Code reads .claude/agents/recipe-assistant.md
          │  routes because description matches "query about live data"
          ▼
recipe-assistant sub-agent
  (tools: mcp__recipes-app__list_recipes, mcp__recipes-app__search_recipes_by_ingredient, …)
          │
          ▼
.claude/mcp.json → dotnet run Backend/src/Recipes.McpServer -- --stdio
          │
          ▼
Recipes.Api (http://localhost:5000)
```

---

## Files to create

| Path | Purpose |
|---|---|
| `.claude/agents/recipe-assistant.md` | Sub-agent definition: frontmatter + system prompt |
| `Backend/Docs/CCAF/F9-claude-code-agent.md` | CCAF documentation |

## Files to modify

| Path | Change |
|---|---|
| `CLAUDE.md` | Add "Using the RecipeApp agent" section with prerequisites and example queries |

---

## Agent definition

**Frontmatter fields:**

| Field | Value |
|---|---|
| `name` | `recipe-assistant` |
| `description` | Used by Claude Code to route data-query requests to this agent |
| `tools` | 12 entries: `mcp__recipes-app__list_recipes`, `get_recipe`, `search_recipes_by_ingredient`, `list_meal_plans`, `get_meal_plan`, `list_shopping_lists`, `get_shopping_list`, `mark_item_pending`, `mark_item_purchased`, `get_monthly_expense_report`, `list_households`, `get_household` |

**System prompt behaviour:**
- Call narrow discovery tools first (list before get)
- Confirm before mutating state (mark_item_purchased / mark_item_pending)
- Degrade gracefully when the API is not running
- Present tabular data as markdown tables

---

## Acceptance criteria

1. The `.claude/agents/recipe-assistant.md` file exists and is committed.
2. `@recipe-assistant list all recipes` in a Claude Code session calls `list_recipes`
   via the MCP server (requires API running).
3. The agent cannot use Edit, Write, or Bash — only the listed MCP tools.
4. `CLAUDE.md` contains a "Using the RecipeApp agent" section with prerequisites.
5. No backend build or test changes required.
