using Microsoft.AspNetCore.Http;

namespace Recipes.McpServer;

public sealed class McpBearerAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _expectedToken;

    public McpBearerAuthMiddleware(RequestDelegate next, string expectedToken)
    {
        _next          = next;
        _expectedToken = expectedToken;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments("/mcp"))
        {
            var authHeader = context.Request.Headers.Authorization.ToString();
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                || authHeader["Bearer ".Length..].Trim() != _expectedToken)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.Headers.WWWAuthenticate = "Bearer";
                await context.Response.WriteAsync("Unauthorized");
                return;
            }
        }

        await _next(context);
    }
}
