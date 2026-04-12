using FluentAssertions;
using Recipes.Application.MealPlans.AcceptMealPlanSuggestion;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.MealPlans.AcceptMealPlanSuggestion;

public sealed class AcceptMealPlanSuggestionHandlerTests
{
    [Fact]
    public async Task Should_Create_MealPlan_From_Suggestion()
    {
        var person = new Person("Stanislav");
        var household = new Household("Family");
        household.AddMember(person);

        var recipes = new List<Recipe>
        {
            new("Pasta"),
            new("Soup")
        };

        var mealPlanRepository = new FakeMealPlanRepository();
        var recipeRepository = new FakeRecipeRepository(recipes);
        var householdRepository = new FakeHouseholdRepository([household]);

        var handler = new AcceptMealPlanSuggestionHandler(
            mealPlanRepository,
            recipeRepository,
            householdRepository);

        var result = await handler.Handle(
            new AcceptMealPlanSuggestionCommand(
                "Weekly dinners",
                household.Id.Value,
                [
                    new AcceptMealPlanSuggestionEntryDto(
                        recipes[0].Id.Value,
                        new DateOnly(2026, 4, 21),
                        (int)MealType.Dinner,
                        (int)MealScope.Shared,
                        [
                            new AcceptMealPlanSuggestionAssignmentDto(
                                person.Id.Value,
                                recipes[0].Id.Value,
                                null,
                                1.0m,
                                null)
                        ]),
                    new AcceptMealPlanSuggestionEntryDto(
                        recipes[1].Id.Value,
                        new DateOnly(2026, 4, 22),
                        (int)MealType.Dinner,
                        (int)MealScope.Shared,
                        [
                            new AcceptMealPlanSuggestionAssignmentDto(
                                person.Id.Value,
                                recipes[1].Id.Value,
                                null,
                                1.0m,
                                null)
                        ])
                ]),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        mealPlanRepository.Stored.Should().HaveCount(1);
        mealPlanRepository.Stored.Single().Entries.Should().HaveCount(2);
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        public List<MealPlan> Stored { get; } = [];

        public Task<MealPlan?> GetByIdAsync(MealPlanId id, CancellationToken cancellationToken = default)
            => Task.FromResult(Stored.SingleOrDefault(x => x.Id == id));

        public Task AddAsync(MealPlan mealPlan, CancellationToken cancellationToken = default)
        {
            Stored.Add(mealPlan);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MealPlan>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult((IReadOnlyList<MealPlan>)Stored);

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