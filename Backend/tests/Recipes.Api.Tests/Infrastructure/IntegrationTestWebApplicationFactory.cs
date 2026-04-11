using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace Recipes.Api.Tests.Infrastructure;

public sealed class IntegrationTestWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder().Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RecipesDbContext>();
        await db.Database.MigrateAsync();
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
        });

        builder.UseEnvironment("Development");
    }

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
    }
}
