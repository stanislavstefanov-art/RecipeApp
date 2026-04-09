using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;

namespace Recipes.Application.Abstractions;

public interface IRecipesDbContext
{
    DbSet<Recipe> Recipes { get; }
    DbSet<Ingredient> Ingredients { get; }
    DbSet<RecipeStep> RecipeSteps { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

