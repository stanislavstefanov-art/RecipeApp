using FluentAssertions;
using Recipes.Application.MealPlans.UpdateMealPlanPersonAssignment;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.MealPlans.UpdateMealPlanPersonAssignment;

public sealed class UpdateMealPlanPersonAssignmentHandlerTests
{
    [Fact]
    public async Task Should_Update_Assignment()
    {
        var person = new Person("Sister");
        var household = new Household("Family");
        household.AddMember(person);

        var recipe1 = new Recipe("Gyuvetche");
        var recipe2 = new Recipe("Fish soup");

        var mealPlan = new MealPlan("Weekly meals", household.Id);
        mealPlan.AddRecipe(
            recipe1,
            new DateOnly(2026, 4, 20),
            MealType.Dinner,
            MealScope.Shared,
            [
                (person.Id, recipe1.Id, null, 1.0m, null)
            ]);

        var entryId = mealPlan.Entries.Single().Id;

        var handler = new UpdateMealPlanPersonAssignmentHandler(
            new FakeMealPlanRepository([mealPlan]),
            new FakeRecipeRepository([recipe1, recipe2]),
            new FakeHouseholdRepository([household]));

        var result = await handler.Handle(
            new UpdateMealPlanPersonAssignmentCommand(
                mealPlan.Id.Value,
                entryId.Value,
                person.Id.Value,
                recipe2.Id.Value,
                null,
                1.25m,
                "Updated assignment"),
            CancellationToken.None);

        result.IsError.Should().BeFalse();

        var assignment = mealPlan.Entries.Single().PersonAssignments.Single();
        assignment.AssignedRecipeId.Should().Be(recipe2.Id);
        assignment.PortionMultiplier.Should().Be(1.25m);
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        private readonly List<MealPlan> _mealPlans;
        public FakeMealPlanRepository(IEnumerable<MealPlan> mealPlans) => _mealPlans = mealPlans.ToList();
        public Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_mealPlans.SingleOrDefault(x => x.Id == id));
        public Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
        {
            _mealPlans.Add(mealPlan);
            return Task.CompletedTask;
        }
        public Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<MealPlan>)_mealPlans);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        private readonly List<Recipe> _recipes;
        public FakeRecipeRepository(IEnumerable<Recipe> recipes) => _recipes = recipes.ToList();
        public Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_recipes.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes);
        public Task<IReadOnlyList<Recipe>> SearchByIngredientNameAsync(string ingredientName, CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Recipe>)_recipes
                .Where(x => x.Ingredients.Any(i => i.Name.Contains(ingredientName, StringComparison.OrdinalIgnoreCase)))
                .ToList());
        public void Add(Recipe recipe) => _recipes.Add(recipe);
        public void Remove(Recipe recipe) => _recipes.Remove(recipe);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeHouseholdRepository : IHouseholdRepository
    {
        private readonly List<Household> _households;
        public FakeHouseholdRepository(IEnumerable<Household> households) => _households = households.ToList();
        public Task<Household?> GetByIdAsync(HouseholdId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_households.SingleOrDefault(x => x.Id == id));
        public Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Household>)_households);
        public Task AddAsync(Household household, CancellationToken cancellationToken = default)
        {
            _households.Add(household);
            return Task.CompletedTask;
        }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}