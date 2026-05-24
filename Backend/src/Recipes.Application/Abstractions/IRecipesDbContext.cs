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
    DbSet<Expense> Expenses { get; }
    DbSet<Person> Persons { get; }
    DbSet<Household> Households { get; }
    DbSet<PersonMembership> PersonMemberships { get; }
    DbSet<HouseholdMember> HouseholdMembers { get; }
    DbSet<User> Users { get; }
    DbSet<MealPlanPersonAssignment> MealPlanPersonAssignments { get; }
    DbSet<RecipeVariation> RecipeVariations { get; }
    DbSet<RecipeVariationIngredientOverride> RecipeVariationIngredientOverrides { get; }
    DbSet<MeasurementUnit> MeasurementUnits { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}