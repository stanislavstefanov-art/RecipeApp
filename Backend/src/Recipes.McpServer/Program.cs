using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Recipes.McpServer;
using Recipes.McpServer.Http;

var isStdio = args.Contains("--stdio");

if (isStdio)
{
    // ── stdio mode (Claude Code / Claude Desktop subprocess) ─────────────
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.ClearProviders();

    var apiBaseUrl = Environment.GetEnvironmentVariable("RECIPES_API_BASE_URL")
        ?? builder.Configuration["RecipesApi:BaseUrl"]
        ?? "http://localhost:5000";

    builder.Services
        .AddHttpClient<RecipesApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

    builder.Services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithToolsFromAssembly()
        .WithResourcesFromAssembly()
        .WithPromptsFromAssembly();

    await builder.Build().RunAsync();
}
else
{
    // ── HTTP mode (remote / Azure deployment) ────────────────────────────
    var builder = WebApplication.CreateBuilder(args);

    var apiBaseUrl = Environment.GetEnvironmentVariable("RECIPES_API_BASE_URL")
        ?? builder.Configuration["RecipesApi:BaseUrl"]
        ?? "http://localhost:5000";

    builder.Services
        .AddHttpClient<RecipesApiClient>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly()
        .WithResourcesFromAssembly()
        .WithPromptsFromAssembly();

    var app = builder.Build();

    var mcpToken = Environment.GetEnvironmentVariable("MCP_SERVER_TOKEN");
    if (!string.IsNullOrEmpty(mcpToken))
        app.UseMiddleware<McpBearerAuthMiddleware>(mcpToken);
    else
        app.Logger.LogWarning("MCP_SERVER_TOKEN not set — all /mcp requests accepted (dev mode)");

    app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
    app.MapMcp("/mcp");

    await app.RunAsync();
}
