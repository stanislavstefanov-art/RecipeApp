using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence;

public sealed class RecipesDbContext : DbContext, IRecipesDbContext
{
    private readonly IDomainEventDispatcher _dispatcher;

    public RecipesDbContext(
        DbContextOptions<RecipesDbContext> options,
        IDomainEventDispatcher dispatcher)
        : base(options)
    {
        _dispatcher = dispatcher;
    }

    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> Ingredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();
    public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<Household> Households => Set<Household>();
    public DbSet<HouseholdMember> HouseholdMembers => Set<HouseholdMember>();
    public DbSet<MealPlanPersonAssignment> MealPlanPersonAssignments => Set<MealPlanPersonAssignment>();
    public DbSet<RecipeVariation> RecipeVariations => Set<RecipeVariation>();
    public DbSet<RecipeVariationIngredientOverride> RecipeVariationIngredientOverrides => Set<RecipeVariationIngredientOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecipesDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = ChangeTracker
            .Entries<Entity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .SelectMany(e => e.DomainEvents)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in ChangeTracker.Entries<Entity>().Select(e => e.Entity))
            entity.ClearDomainEvents();

        if (events.Count > 0)
            await _dispatcher.DispatchAsync(events, cancellationToken);

        return result;
    }
}