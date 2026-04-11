using FluentAssertions;
using Recipes.Application.MealPlans.AcceptMealPlanSuggestion;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Tests.MealPlans.AcceptMealPlanSuggestion;

public sealed class AcceptMealPlanSuggestionHandlerTests
{
    [Fact]
    public async Task Should_Create_MealPlan_From_Suggestion()
    {
        var recipes = new List<Recipe>
        {
            new("Pasta"),
            new("Soup")
        };

        var mealPlanRepository = new FakeMealPlanRepository();
        var recipeRepository = new FakeRecipeRepository(recipes);

        var handler = new AcceptMealPlanSuggestionHandler(mealPlanRepository, recipeRepository);

        var result = await handler.Handle(
            new AcceptMealPlanSuggestionCommand(
                "Weekly dinners",
                [
                    new AcceptMealPlanSuggestionEntryDto(recipes[0].Id.Value, new DateOnly(2026, 4, 21), 3),
                    new AcceptMealPlanSuggestionEntryDto(recipes[1].Id.Value, new DateOnly(2026, 4, 22), 3)
                ]),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        mealPlanRepository.Stored.Should().HaveCount(1);
        mealPlanRepository.Stored.Single().Entries.Should().HaveCount(2);
    }

    private sealed class FakeMealPlanRepository : IMealPlanRepository
    {
        public List<MealPlan> Stored { get; } = new();

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

        public void Add(Recipe recipe)
        {
            _recipes.Add(recipe);
        }

        public void Remove(Recipe recipe)
        {
            _recipes.Remove(recipe);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}