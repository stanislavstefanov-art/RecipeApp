# F9 — Claude Code Sub-Agent: Implementation Plan

Reference spec: `Docs/specs/F9-claude-code-agent.md`

Build order: agent definition file → CLAUDE.md update → CCAF doc.
No backend changes needed.

---

## Step 1 — Create the agent definition file

Create `.claude/agents/recipe-assistant.md` with:

**Frontmatter:**
```yaml
name: recipe-assistant
description: Use this agent for any query about the RecipesApp live data — recipes,
  meal plans, shopping lists, households, or monthly expenses. The agent reads directly
  from the running API via the recipes-app MCP server. Do NOT use for code changes;
  this agent has no file-editing tools.
tools:
  - mcp__recipes-app__list_recipes
  - mcp__recipes-app__get_recipe
  - mcp__recipes-app__search_recipes_by_ingredient
  - mcp__recipes-app__list_meal_plans
  - mcp__recipes-app__get_meal_plan
  - mcp__recipes-app__list_shopping_lists
  - mcp__recipes-app__get_shopping_list
  - mcp__recipes-app__mark_item_pending
  - mcp__recipes-app__mark_item_purchased
  - mcp__recipes-app__get_monthly_expense_report
  - mcp__recipes-app__list_households
  - mcp__recipes-app__get_household
```

**Tool naming:** `mcp__<server-key>__<tool-name>` where `<server-key>` is the key in
`.claude/mcp.json` (`recipes-app`) and `<tool-name>` matches the `[McpServerTool(Name)]`
attribute in the MCP server assembly.

**System prompt:** describe the agent's capabilities, workflow (narrow tools first,
confirm before mutations), graceful degradation when API is down, presentation style.

---

## Step 2 — Update CLAUDE.md

Add a "Using the RecipeApp agent" section before "Claude Code workflow guidance":

- Prerequisite: `dotnet run --project Backend/src/Recipes.Api`
- Invocation: `@recipe-assistant <query>` or natural-language routing
- Example queries covering recipes, meal plans, shopping lists, expenses

---

## Step 3 — CCAF doc

Create `Backend/Docs/CCAF/F9-claude-code-agent.md` covering:
- Claude Code ecosystem integration subtopic
- Agent definition file format and fields
- Tool scoping rationale (least privilege)
- MCP server registration (already in `.claude/mcp.json`)
- Routing via `description:` field
