using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Api.Auth;
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
        builder.ConfigureAppConfiguration((_, config) =>
        {
            // The real appsettings.json doesn't set Jwt config (it lives in the
            // gitignored Development.json) — provide a deterministic one for tests
            // so JwtBearer can validate the tokens we issue below.
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SigningKey"] = TestSigningKey,
                ["Jwt:Issuer"] = "RecipesApp",
                ["Jwt:Audience"] = "RecipesApp",
                ["Jwt:LifetimeDays"] = "1",
                ["RecipeImport:Provider"] = "Stub",
                ["MealPlanSuggestion:Provider"] = "Stub",
                ["IngredientSubstitution:Provider"] = "Stub",
                ["RecipeCritique:Provider"] = "Stub",
                ["RecipeScaling:Provider"] = "Stub",
                ["RecipeBatchAnalysis:Provider"] = "Stub",
                ["RecipeDraftReview:Provider"] = "Stub",
                ["ExpenseInsight:Provider"] = "Stub",
            });
        });

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RecipesDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<RecipesDbContext>(options =>
                options.UseSqlServer(_db.GetConnectionString()));
        });

        builder.UseEnvironment("Development");
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }
}
