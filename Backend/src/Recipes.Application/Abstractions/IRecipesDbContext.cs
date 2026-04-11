using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;

namespace Recipes.Application.Abstractions;

public interface IRecipesDbContext
{
    DbSet<Recipe> Recipes { get; }
    DbSet<RecipeIngredient> Ingredients { get; }
    DbSet<RecipeStep> RecipeSteps { get; }
    DbSet<Product> Products { get; }
    DbSet<ShoppingList> ShoppingLists { get; }
    DbSet<ShoppingListItem> ShoppingListItems { get; }
    DbSet<MealPlan> MealPlans { get; }
    DbSet<MealPlanEntry> MealPlanEntries { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}