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

    /// <summary>The test user seeded in <see cref="InitializeAsync"/>.</summary>
    public User TestUser { get; } = User.CreateLocal("test@example.com", "hash-not-used", "Test User");

    /// <summary>The household the <see cref="TestUser"/> is a member of.</summary>
    public Household TestHousehold { get; } = new("Test Household");

    static IntegrationTestWebApplicationFactory()
    {
        // Program.cs reads Configuration["Jwt:SigningKey"] before any callback we
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

        // Seed a user + household so the household-scoped handlers (CreateRecipe,
        // ListRecipes, etc.) find a membership when they look up the current user.
        TestHousehold.AddUser(TestUser.Id, DateTimeOffset.UtcNow);
        db.Users.Add(TestUser);
        db.Households.Add(TestHousehold);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates an HttpClient pre-authenticated with a JWT for the seeded
    /// <see cref="TestUser"/>. All endpoints under <c>/api</c> require auth, so
    /// tests should use this instead of <c>CreateClient()</c>.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var issuer = Services.GetRequiredService<IJwtIssuer>();
        var (token, _) = issuer.Issue(TestUser);
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
