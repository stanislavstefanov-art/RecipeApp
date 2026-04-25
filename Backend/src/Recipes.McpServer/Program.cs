using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

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
