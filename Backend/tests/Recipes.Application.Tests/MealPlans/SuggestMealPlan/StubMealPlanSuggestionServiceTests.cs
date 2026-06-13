using FluentAssertions;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Infrastructure.AI.Claude.Services.Stubs;

namespace Recipes.Application.Tests.MealPlans.SuggestMealPlan;

public sealed class StubMealPlanSuggestionServiceTests
{
    private static MealPlanSuggestionRequestDto BuildRequest(
        IReadOnlyList<AvailableRecipeDto> recipes,
        int numberOfDays = 7,
        IReadOnlyList<int>? mealTypes = null)
    {
        var personId = Guid.NewGuid();
        return new MealPlanSuggestionRequestDto(
            "Test Plan",
            new DateOnly(2026, 6, 16),
            numberOfDays,
            mealTypes ?? [2, 3],
            new HouseholdPlanningProfileDto(
                Guid.NewGuid(),
                "Family",
                [new PersonPlanningProfileDto(personId, "Alice", [], [], null)]),
            recipes);
    }

    private static AvailableRecipeDto MakeRecipe(string name, int mealsPerCook, IReadOnlyList<int>? appropriateFor = null) =>
        new(Guid.NewGuid(), name, 1, mealsPerCook, appropriateFor ?? [], []);

    [Fact]
    public async Task Recipe_WithMealsPerCookOne_AppearAtMostOnce()
    {
        var recipe = MakeRecipe("Кебапчета", 1);
        var filler = Enumerable.Range(0, 20).Select(i => MakeRecipe($"Filler{i}", 1)).ToList();
        var allRecipes = new List<AvailableRecipeDto> { recipe }.Concat(filler).ToList();

        var service = new StubMealPlanSuggestionService();
        var result = await service.SuggestAsync(BuildRequest(allRecipes), CancellationToken.None);

        var timesUsed = result.Entries.Count(e => e.BaseRecipeId == recipe.RecipeId);
        timesUsed.Should().BeLessThanOrEqualTo(1,
            "a recipe with mealsPerCook=1 must not appear more than once across the entire plan");
    }

    [Fact]
    public async Task Recipe_WithMealsPerCookTwo_AppearAtMostTwice()
    {
        var recipe = MakeRecipe("Боб", 2);
        var filler = Enumerable.Range(0, 20).Select(i => MakeRecipe($"Filler{i}", 2)).ToList();
        var allRecipes = new List<AvailableRecipeDto> { recipe }.Concat(filler).ToList();

        var service = new StubMealPlanSuggestionService();
        var result = await service.SuggestAsync(BuildRequest(allRecipes), CancellationToken.None);

        var timesUsed = result.Entries.Count(e => e.BaseRecipeId == recipe.RecipeId);
        timesUsed.Should().BeLessThanOrEqualTo(2,
            "a recipe with mealsPerCook=2 must not appear more than twice across the entire plan");
    }

    [Fact]
    public async Task AllEntries_HavePositivePortionMultipliers()
    {
        var recipes = Enumerable.Range(0, 5).Select(i => MakeRecipe($"Recipe{i}", 1)).ToList();

        var service = new StubMealPlanSuggestionService();
        var result = await service.SuggestAsync(BuildRequest(recipes), CancellationToken.None);

        result.Entries
            .SelectMany(e => e.Assignments)
            .Should().OnlyContain(a => a.PortionMultiplier > 0);
    }

    [Fact]
    public async Task WhenRecipePoolExhausted_StillProducesEntriesForAllSlots()
    {
        // Only 2 recipes each mealsPerCook=1, but 7 days × 2 meal types = 14 slots.
        // The stub must fall back gracefully rather than throwing.
        var recipes = new List<AvailableRecipeDto>
        {
            MakeRecipe("Pasta", 1),
            MakeRecipe("Soup", 1),
        };

        var service = new StubMealPlanSuggestionService();
        var act = async () => await service.SuggestAsync(BuildRequest(recipes), CancellationToken.None);

        await act.Should().NotThrowAsync("the stub must degrade gracefully when the pool is exhausted");
        var result = await service.SuggestAsync(BuildRequest(recipes), CancellationToken.None);
        result.Entries.Should().HaveCount(14);
    }
}
