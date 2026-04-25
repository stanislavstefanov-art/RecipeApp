# F6 — MCP HTTP / Streamable Transport + Bearer Auth

## What this implements

Adds an HTTP-streamable MCP endpoint to the `Recipes.McpServer` project built in F4.
The stdio transport continues to work unchanged. The new HTTP endpoint exposes the same
12 tools, 3 resources, and 3 prompts, protected by a static bearer token read from the
`MCP_SERVER_TOKEN` environment variable.

---

## CCAF subtopics covered

| Subtopic | Where in code |
|---|---|
| Non-stdio MCP transport (HTTP / streamable) | `WithHttpTransport()` + `app.MapMcp("/mcp")` in `Program.cs` HTTP branch |
| MCP bearer-token authentication | `McpBearerAuthMiddleware` — validates `Authorization: Bearer <token>` before routing to `/mcp` |
| Session management | Handled automatically by the MCP SDK's HTTP transport; each client reconnect gets a new session |
| Remote / cloud deployment readiness | `infra/modules/mcp-server.bicep` — App Service deployment targeting `DOTNETCORE|10.0` (F1 free tier) |

---

## Architecture

```
Claude Desktop / Claude Code
      │  stdio (--stdio flag, unchanged from F4)
      │
      │  OR
      │
      ▼
HTTP  POST /mcp  (Authorization: Bearer <token>)
      │
      ▼
Recipes.McpServer  (dual-mode: generic Host or WebApplication)
      │  HTTP
      ▼
Recipes.Api  (http://localhost:5000 / Azure App Service)
```

---

## Transport switching

`Program.cs` checks `args.Contains("--stdio")` and builds a different host:

| Mode | Host type | Transport |
|---|---|---|
| `--stdio` | `Host.CreateApplicationBuilder` (generic host) | `WithStdioServerTransport()` — logging suppressed to protect stdout pipe |
| default | `WebApplication.CreateBuilder` (ASP.NET Core) | `WithHttpTransport()` + `MapMcp("/mcp")` |

The same tool/resource/prompt assemblies are discovered in both modes via
`WithToolsFromAssembly().WithResourcesFromAssembly().WithPromptsFromAssembly()`.

**Key reason for logging suppression in stdio mode:** MCP uses stdin/stdout as the
protocol pipe. Any bytes written to stdout by the logging framework corrupt the framing,
causing the client to fail JSON-RPC parsing. `builder.Logging.ClearProviders()` prevents
this without disabling structured logging in HTTP mode.

---

## Authentication

`McpBearerAuthMiddleware` is a minimal ASP.NET Core middleware inserted before the MCP
route when `MCP_SERVER_TOKEN` is set:

```csharp
if (!string.IsNullOrEmpty(mcpToken))
    app.UseMiddleware<McpBearerAuthMiddleware>(mcpToken);
else
    app.Logger.LogWarning("MCP_SERVER_TOKEN not set — all /mcp requests accepted (dev mode)");
```

The middleware only intercepts paths starting with `/mcp`, leaving `/health` unprotected
for liveness probes. It performs a constant-time-safe string comparison against the
configured token and returns `401 Unauthorized` with a `WWW-Authenticate: Bearer` header
on failure.

**Design choice — static bearer vs. OAuth/PKCE:** OAuth would require a separate
authorization server and client registration flow that is disproportionate for a
self-hosted dev/exam scenario. Static bearer is sufficient for demonstrating the
transport + auth subtopic and matches what MCP Inspector supports natively.

---

## Azure deployment (Bicep)

`infra/modules/mcp-server.bicep` provisions:

- A separate App Service Plan (F1 free tier, Linux) for the MCP server
- A Web App with `DOTNETCORE|10.0` runtime
- `MCP_SERVER_TOKEN` sourced from Key Vault via `@Microsoft.KeyVault(...)` reference
- `RECIPES_API_BASE_URL` set to the API App Service hostname
- System-assigned managed identity for Key Vault access (same pattern as the API module)

The MCP server runs as a separate App Service rather than being co-hosted with the API
because they have different scaling characteristics and the MCP server must be reachable
independently for remote Claude Desktop / Claude Code connections.

---

## API contract

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/mcp` | Bearer | MCP streamable HTTP endpoint (initialize, tool/resource/prompt calls) |
| `GET` | `/mcp` | Bearer | SSE stream for server-to-client notifications |
| `DELETE` | `/mcp` | Bearer | Terminate session |
| `GET` | `/health` | None | Liveness probe — returns `200 OK { status: "healthy" }` |

---

## Key decisions

### Single project, dual transport
Rather than two separate executables, a single `Program.cs` branches on `--stdio`.
This keeps all tool/resource/prompt logic in one assembly and avoids duplication of
`RecipesApiClient` registration. The branching is trivially testable by passing args.

### `MCP_SERVER_TOKEN` absent → open access (dev mode)
Setting up a token is a manual step. Blocking startup when the variable is absent would
make local development unnecessarily friction-heavy. The warning log makes it visible
without being a hard failure.

### `.claude/mcp.json` updated to pass `-- --stdio`
Claude Code launches the server as a subprocess via `.claude/mcp.json`. After F6 the
server defaults to HTTP mode when no args are passed, so the registration was updated to
explicitly pass `-- --stdio`, preserving the F4 behaviour for Claude Code users.
