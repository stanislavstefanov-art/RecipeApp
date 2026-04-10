using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Recipes.Application.Abstractions;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.Persistence;

namespace Recipes.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RecipesDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("RecipesDb");
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<IRecipesDbContext>(sp => sp.GetRequiredService<RecipesDbContext>());
        services.AddScoped<IRecipeRepository, RecipeRepository>();

        return services;
    }
}

