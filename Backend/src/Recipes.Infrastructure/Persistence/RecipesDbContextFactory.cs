using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Recipes.Application.Abstractions;
using Recipes.Domain.Events;

namespace Recipes.Infrastructure.Persistence;

public sealed class RecipesDbContextFactory : IDesignTimeDbContextFactory<RecipesDbContext>
{
    public RecipesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__RecipesDb")
            ?? "Server=(localdb)\\mssqllocaldb;Database=RecipesDb;Trusted_Connection=True;";

        var optionsBuilder = new DbContextOptionsBuilder<RecipesDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new RecipesDbContext(optionsBuilder.Options, new NoOpDomainEventDispatcher());
    }

    private sealed class NoOpDomainEventDispatcher : IDomainEventDispatcher
    {
        public Task DispatchAsync(
            IReadOnlyList<IDomainEvent> domainEvents,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}