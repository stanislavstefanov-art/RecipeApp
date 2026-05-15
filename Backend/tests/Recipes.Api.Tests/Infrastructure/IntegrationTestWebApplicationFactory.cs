using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Application.Common.Auth;
using Recipes.Domain.Entities;
using Recipes.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Recipes.Api.Tests.Infrastructure;

public sealed class IntegrationTestWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestSigningKey =
        "integration-test-signing-key-must-be-at-least-256-bits-long-aaaaaaaaaa";

    private readonly MsSqlContainer _db = new MsSqlBuilder().Build();

    static IntegrationTestWebApplicationFactory()
    {
        // Program.cs reads Configuration["Jwt:SigningKey"] BEFORE any callback we
        // could register via ConfigureWebHost / ConfigureAppConfiguration. Env
        // vars are picked up by the default WebApplication.CreateBuilder config
        // sources, so setting them at static-init time guarantees they're visible
        // by the time the host code runs.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", TestSigningKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "RecipesApp");
        Environment.SetEnvironmentVariable("Jwt__Audience", "RecipesApp");
        Environment.SetEnvironmentVariable("Jwt__LifetimeDays", "1");
        Environment.SetEnvironmentVariable("RecipeImport__Provider", "Stub");
        Environment.SetEnvironmentVariable("MealPlanSuggestion__Provider", "Stub");
        Environment.SetEnvironmentVariable("IngredientSubstitution__Provider", "Stub");
        Environment.SetEnvironmentVariable("RecipeCritique__Provider", "Stub");
        Environment.SetEnvironmentVariable("RecipeScaling__Provider", "Stub");
        Environment.SetEnvironmentVariable("RecipeBatchAnalysis__Provider", "Stub");
        Environment.SetEnvironmentVariable("RecipeDraftReview__Provider", "Stub");
        Environment.SetEnvironmentVariable("ExpenseInsight__Provider", "Stub");
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipesDbContext>();
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Creates an HttpClient pre-authenticated with a JWT for a synthetic test user.
    /// All endpoints under <c>/api</c> require auth, so tests should use this instead
    /// of <c>CreateClient()</c>.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var issuer = Services.GetRequiredService<IJwtIssuer>();
        var (token, _) = issuer.Issue(
            User.CreateLocal("test@example.com", "hash-not-used", "Test User"));
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RecipesDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<RecipesDbContext>(options =>
                options.UseSqlServer(_db.GetConnectionString()));

            // Surface unhandled exceptions in the response body during tests so
            // CI logs show the actual error, not just "An error occurred".
            services.AddProblemDetails(opts =>
            {
                opts.CustomizeProblemDetails = ctx =>
                {
                    var ex = ctx.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                    if (ex is not null)
                    {
                        ctx.ProblemDetails.Detail = ex.ToString();
                    }
                };
            });
        });

        builder.UseEnvironment("Development");
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }
}
