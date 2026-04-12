using FluentAssertions;
using Recipes.Application.MealPlans.AddRecipeToMealPlan;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.MealPlans.AddRecipeToMealPlan;

public sealed class AddRecipeToMealPlanHandlerTests
{
    [Fact]
    public async Task Should_Add_Recipe_To_MealPlan()
    {
        var person = new Person("Stanislav");
        var household = new Household("Family");
        household.AddMember(person);

        var mealPlan = new MealPlan("Weekly meals", household.Id);
        var recipe = new Recipe("Pasta");

        var mealPlanRepository = new FakeMealPlanRepository([mealPlan]);
        var recipeRepository = new FakeRecipeRepository([recipe]);
        var householdRepository = new FakeHouseholdRepository([household]);

        var handler = new AddRecipeToMealPlanHandler(
            mealPlanRepository,
            recipeRepository,
            householdRepository);

        var result = await handler.Handle(
            new AddRecipeToMealPlanCommand(
                mealPlan.Id.Value,
                recipe.Id.Value,
                new DateOnly(2026, 4, 21),
                (int)MealType.Dinner,
                (int)MealScope.Shared,
                [
                    new MealPlanPersonAssignmentInputDto(
                        person.Id.Value,
                        recipe.Id.Value,
                        null,
                        1.0m,
                        null)
                ]),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        mealPlan.Entries.Should().HaveCount(1);
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        private readonly List<MealPlan> _mealPlans;

        public FakeMealPlanRepository(IEnumerable<MealPlan> mealPlans)
        {
            _mealPlans = mealPlans.ToList();
        }

        public Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_mealPlans.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
        {
            _mealPlans.Add(mealPlan);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<MealPlan>)_mealPlans);

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        private readonly List<Recipe> _recipes;

        public FakeRecipeRepository(IEnumerable<Recipe> recipes)
        {
            _recipes = recipes.ToList();
        }

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

        public FakeHouseholdRepository(IEnumerable<Household> households)
        {
            _households = households.ToList();
        }

        public Task<Household?> GetByIdAsync(HouseholdId id, CancellationToken cancellationToken = default)
            => Task.FromResult(_households.SingleOrDefault(x => x.Id == id));

        public Task<IReadOnlyList<Household>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<Household>)_households);

        public Task AddAsync(Household household, CancellationToken cancellationToken = default)
        {
            _households.Add(household);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}