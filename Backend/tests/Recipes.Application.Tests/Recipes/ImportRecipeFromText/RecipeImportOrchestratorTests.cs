using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Recipes.Application.Recipes.ImportRecipeFromText;
using Recipes.Infrastructure.AI.Claude.Services.Stubs;
using Recipes.Infrastructure.Services;

namespace Recipes.Application.Tests.Recipes.ImportRecipeFromText;

public sealed class RecipeImportOrchestratorTests
{
    [Fact]
    public async Task Should_Retry_When_First_Import_Is_Invalid()
    {
        var importService = new StubRecipeImportService();
        var validator = new ImportedRecipeDtoValidator();

        var orchestrator = new RecipeImportOrchestrator(importService, validator, NullLogger<RecipeImportOrchestrator>.Instance);

        var result = await orchestrator.ImportAsync(
            "2 eggs, 1 tomato, fry for 5 minutes",
            CancellationToken.None);

        result.IsError.Should().BeFalse();

        var dto = result.Value;
        dto.Title.Should().Be("Imported recipe");
        dto.Steps.Should().NotBeEmpty();
        dto.Confidence.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public async Task Should_Mark_For_Review_When_Both_Attempts_Are_Invalid()
    {
        var importService = new AlwaysInvalidRecipeImportService();
        var validator = new ImportedRecipeDtoValidator();

        var orchestrator = new RecipeImportOrchestrator(importService, validator, NullLogger<RecipeImportOrchestrator>.Instance);

        var result = await orchestrator.ImportAsync(
            "2 eggs, 1 tomato, fry for 5 minutes",
            CancellationToken.None);

        result.IsError.Should().BeFalse();

        var dto = result.Value;
        dto.NeedsReview.Should().BeTrue();
        dto.Confidence.Should().BeLessThanOrEqualTo(0.5);
        dto.Notes.Should().NotBeNullOrWhiteSpace();
        dto.Steps.Should().BeEmpty();
    }

    private sealed class AlwaysInvalidRecipeImportService : IRecipeImportService
    {
        public Task<RecipeExtractionResult> ImportAsync(string text, CancellationToken cancellationToken)
        {
            return Task.FromResult(new RecipeExtractionResult(
                Title: null,
                Servings: null,
                Ingredients:
                [
                    new RecipeExtractionIngredient("Eggs", 2, "pcs", null)
                ],
                Steps: [],
                Notes: "Still invalid",
                Confidence: 0.9,
                NeedsReview: false));
        }
    }
}