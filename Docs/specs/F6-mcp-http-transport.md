# F6 — MCP HTTP / Streamable Transport + Bearer Auth

## Summary

Adds an HTTP-streamable MCP endpoint to the existing `Recipes.McpServer` project from F4.
The stdio transport continues to work unchanged. The new HTTP endpoint exposes the same
tools, resources, and prompts, protected by a static bearer token read from the
`MCP_SERVER_TOKEN` environment variable.

No Application or Domain code is modified.

---

## CCAF subtopics covered

| Subtopic | Where |
|---|---|
| Non-stdio MCP transport (HTTP / streamable) | `WithHttpTransport()` wired alongside `WithStdioServerTransport()` in `Program.cs` |
| MCP bearer-token authentication | `McpServerAuthMiddleware` validates `Authorization: Bearer <token>` before routing MCP requests |
| Session management | Handled automatically by the MCP SDK's HTTP transport; each client reconnect gets a new session |
| Remote / cloud deployment readiness | `infra/modules/mcp-server.bicep` — App Service deployment targeting the same F1 free tier as the API |

---

## Architecture

```
Claude Desktop / Claude Code
      │  stdio (unchanged from F4)
      │
      │  OR
      │
      ▼
HTTP  POST /mcp  (Bearer auth)
      │
      ▼
Recipes.McpServer  (console → ASP.NET Core host)
      │  HTTP
      ▼
Recipes.Api  (http://localhost:5000 / Azure App Service)
```

The server switches from a generic `Host` to a minimal `WebApplication` host so it can
serve HTTP requests. The stdio transport is re-added via
`AddSingleSessionMcpServer().WithStdioServerTransport()` when `--stdio` is passed as a
command-line argument; otherwise the HTTP transport is used.

---

## Transport switching

The server decides which transport to use based on a `--stdio` flag:

```
dotnet run --project Backend/src/Recipes.McpServer --no-launch-profile -- --stdio
```

Without `--stdio`, the process starts as an ASP.NET Core app listening on
`ASPNETCORE_URLS` (default `http://localhost:5010` to avoid conflict with the API on
5000).

`.claude/mcp.json` (for Claude Code) continues to use `--stdio` mode.

---

## Authentication

When running in HTTP mode, every request to `/mcp` must include:

```
Authorization: Bearer <token>
```

Where `<token>` matches the value of `MCP_SERVER_TOKEN` env variable. If the variable is
not set, the server starts but logs a warning and accepts **all** requests (dev
convenience). If the token is set and the header is missing or wrong, the server responds
`401 Unauthorized`.

Implementation: a minimal ASP.NET Core middleware `McpBearerAuthMiddleware` added before
the MCP route.

---

## API contract

| Method | Path | Auth | Description |
|---|---|---|---|
| `POST` | `/mcp` | Bearer | MCP streamable HTTP endpoint (initialize, tool/resource/prompt calls) |
| `GET`  | `/mcp` | Bearer | SSE stream for server-to-client notifications |
| `DELETE` | `/mcp` | Bearer | Terminate session |
| `GET`  | `/health` | None | Liveness probe — returns `200 OK` |

---

## Configuration

| Key | Env variable | Default | Purpose |
|---|---|---|---|
| `RecipesApi:BaseUrl` | `RECIPES_API_BASE_URL` | `http://localhost:5000` | API base address |
| `MCP_SERVER_TOKEN` | `MCP_SERVER_TOKEN` | *(none)* | Bearer token; empty = no auth |
| `ASPNETCORE_URLS` | `ASPNETCORE_URLS` | `http://localhost:5010` | HTTP listen address |

---

## Acceptance criteria

1. `dotnet build Backend/Recipes.sln` passes.
2. `dotnet run --project Backend/src/Recipes.McpServer -- --stdio` starts and waits on stdin (same as F4 AC#2).
3. `dotnet run --project Backend/src/Recipes.McpServer` starts and serves HTTP on port 5010. `GET /health` returns `200`.
4. With `MCP_SERVER_TOKEN=secret`, a request to `POST /mcp` without `Authorization` returns `401`.
5. With a valid `Authorization: Bearer secret` header, MCP Inspector can list all 12 tools, 3 resources, 3 prompts.
6. Without `MCP_SERVER_TOKEN` set, all requests are accepted (dev mode).
7. `infra/modules/mcp-server.bicep` contains a valid App Service deployment targeting `net10.0`.

---

## Out of scope

- TLS / HTTPS termination (handled by Azure App Service front-door).
- OAuth / PKCE flow (F6 is static bearer only).
- Client-side certificate auth.
- Angular/React UI changes.

---

## Files to create

| Path | Purpose |
|---|---|
| `Backend/src/Recipes.McpServer/McpBearerAuthMiddleware.cs` | ASP.NET Core middleware for bearer token validation |
| `infra/modules/mcp-server.bicep` | Bicep module for deploying the MCP server to Azure App Service |

## Files to modify

| Path | Change |
|---|---|
| `Backend/src/Recipes.McpServer/Program.cs` | Switch to `WebApplication` host; add HTTP transport + auth middleware; keep `--stdio` path |
| `Backend/src/Recipes.McpServer/README.md` | Add HTTP mode usage, auth setup, and MCP Inspector HTTP example |
