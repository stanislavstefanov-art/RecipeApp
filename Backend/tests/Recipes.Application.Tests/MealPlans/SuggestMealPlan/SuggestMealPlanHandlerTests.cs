using FluentAssertions;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;
using Recipes.Infrastructure.AI.Claude.Services.Stubs;

namespace Recipes.Application.Tests.MealPlans.SuggestMealPlan;

public sealed class SuggestMealPlanHandlerTests
{
    [Fact]
    public async Task Should_Return_Suggestion_When_Recipes_Exist()
    {
        var recipes = new List<Recipe>
        {
            new("Pasta"),
            new("Soup")
        };

        var recipeRepository = new FakeRecipeRepository(recipes);
        var suggestionService = new StubMealPlanSuggestionService();

        var handler = new SuggestMealPlanHandler(recipeRepository, suggestionService);

        var result = await handler.Handle(
            new SuggestMealPlanCommand(
                "Weekly plan",
                new DateOnly(2026, 4, 21),
                2,
                [3]),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Entries.Should().NotBeEmpty();
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