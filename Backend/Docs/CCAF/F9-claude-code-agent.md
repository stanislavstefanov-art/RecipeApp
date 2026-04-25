# F9 — Claude Code Sub-Agent backed by the MCP Server

## What this implements

A Claude Code sub-agent definition (`.claude/agents/recipe-assistant.md`) that gives
developers a specialised assistant for querying live RecipesApp data directly from the
Claude Code session, without touching the codebase or running manual curl commands.

The sub-agent uses the `recipes-app` MCP server (F4/F6) registered in `.claude/mcp.json`
and is scoped exclusively to the 12 MCP tools exposed by that server.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| Claude Code ecosystem integration | `.claude/agents/recipe-assistant.md` — agent definition file picked up automatically by Claude Code |
| Agent definition files | Frontmatter fields: `name`, `description`, `tools`; system prompt for agent behaviour |
| MCP server registration | `.claude/mcp.json` — already registers the MCP server for all developers in the repo |
| Tool scoping in sub-agents | `tools:` array in frontmatter restricts the agent to only the 12 `mcp__recipes-app__*` tools — no file-editing capabilities |
| Agent routing | `description:` field tells Claude Code when to delegate to this agent vs. the default session |

---

## Architecture

```
Developer in Claude Code session
          │
          │  "What recipes contain chicken?"
          │  (Claude Code routes to recipe-assistant agent)
          ▼
.claude/agents/recipe-assistant.md
          │  (has only mcp__recipes-app__* tools)
          ▼
.claude/mcp.json  →  dotnet run Backend/src/Recipes.McpServer -- --stdio
          │
          ▼
Recipes.McpServer (stdio subprocess)
          │  HTTP
          ▼
Recipes.Api  (http://localhost:5000)
          │
          ▼
SQL Server (local dev database)
```

The agent runs as a sub-agent inside the developer's existing Claude Code session.
It shares the MCP server subprocess started by the session; no second process is launched.

---

## Agent definition file

**Path:** `.claude/agents/recipe-assistant.md`

**Key frontmatter fields:**

| Field | Value | Purpose |
|---|---|---|
| `name` | `recipe-assistant` | Agent identifier; used with `@recipe-assistant` prefix |
| `description` | "Use this agent for any query about the RecipesApp live data…" | Routing signal — Claude Code reads this to decide when to delegate |
| `tools` | 12 `mcp__recipes-app__*` entries | Hard-scopes the agent to MCP tools only — no Edit, Write, Bash |

**Tool naming convention:** `mcp__<server-name>__<tool-name>` where `<server-name>`
matches the key in `.claude/mcp.json` (`recipes-app`) and `<tool-name>` matches the
`[McpServerTool(Name = "...")]` attribute in the server assembly.

---

## MCP server registration (already in place from F4/F6)

`.claude/mcp.json` at the repo root is automatically picked up by Claude Code:

```json
{
  "mcpServers": {
    "recipes-app": {
      "command": "dotnet",
      "args": ["run", "--project", "Backend/src/Recipes.McpServer",
               "--no-launch-profile", "--", "--stdio"],
      "env": { "RECIPES_API_BASE_URL": "http://localhost:5000" }
    }
  }
}
```

Because the file is committed, every developer who clones the repo and opens it in
Claude Code automatically gets the MCP server and the sub-agent — zero manual setup
beyond starting the API.

---

## Key design decisions

### Tool scoping to MCP tools only
The agent's `tools:` list contains only the 12 `mcp__recipes-app__*` entries. This is
intentional: an agent whose description says "answer questions about live data" should
not silently gain the ability to edit files, run shell commands, or read the entire
codebase. Explicit tool scoping enforces the principle of least privilege at the
agent level.

### Description as routing signal
Claude Code uses the `description:` field to decide whether to route a user request to
this agent or keep it in the default session. The description explicitly names the
domains (recipes, meal plans, shopping lists, households, expenses) and explicitly
excludes code changes ("Do NOT use for code changes"). This prevents accidental routing
of refactoring or debugging questions to an agent that cannot help with them.

### System prompt focuses on data-query workflow
The agent's system prompt teaches it to call narrow discovery tools first (list before
get), confirm before mutating state (mark_item_purchased), and degrade gracefully when
the API is not running. This makes the agent useful across the full development workflow,
not just a thin wrapper over raw tool calls.

### No new code required
F9 is purely configuration and documentation. The MCP server binary, the tool
implementations, and the `mcp.json` registration all existed from F4/F6. F9 adds the
agent *definition layer* that wires those capabilities into Claude Code's sub-agent
routing system.

---

## Usage

Prerequisites: the Recipes API must be running.

```bash
dotnet run --project Backend/src/Recipes.Api
```

Then in any Claude Code session in this repo, use `@recipe-assistant` or describe a
data query and Claude Code will route to the agent automatically:

```
@recipe-assistant What recipes contain chicken?
@recipe-assistant Show me this week's meal plan for household {id}
@recipe-assistant What's still on shopping list {id}?
@recipe-assistant What was the total spend in March 2026?
```
