using ErrorOr;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Infrastructure.Services;

public sealed class RecipeImportOrchestrator : IRecipeImportOrchestrator
{
    private readonly IRecipeImportService _recipeImportService;
    private readonly IValidator<ImportedRecipeDto> _validator;
    private readonly ILogger<RecipeImportOrchestrator> _logger;

    public RecipeImportOrchestrator(
        IRecipeImportService recipeImportService,
        IValidator<ImportedRecipeDto> validator,
        ILogger<RecipeImportOrchestrator> logger)
    {
        _recipeImportService = recipeImportService;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ErrorOr<ImportedRecipeDto>> ImportAsync(string text, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Starting recipe import orchestration. InputLength: {InputLength}, Provider: {Provider}",
            text.Length,
            _recipeImportService.GetType().Name);

        var firstExtraction = await _recipeImportService.ImportAsync(text, cancellationToken);
        var firstDto = Map(firstExtraction);

        var firstValidation = await _validator.ValidateAsync(firstDto, cancellationToken);

        if (firstValidation.IsValid)
        {
            _logger.LogInformation(
                "Recipe import succeeded on first attempt. Confidence: {Confidence}, NeedsReview: {NeedsReview}, IngredientCount: {IngredientCount}, StepCount: {StepCount}",
                firstDto.Confidence,
                firstDto.NeedsReview,
                firstDto.Ingredients.Count,
                firstDto.Steps.Count);

            return firstDto;
        }

        var firstErrors = firstValidation.Errors
            .Select(x => x.ErrorMessage)
            .ToArray();

        _logger.LogWarning(
            "First recipe import attempt failed validation. ErrorCount: {ErrorCount}, Errors: {Errors}",
            firstErrors.Length,
            string.Join(" | ", firstErrors));

        var retryPrompt = BuildRetryPrompt(text, firstErrors);

        _logger.LogInformation("Retrying recipe import with validation feedback.");

        var secondExtraction = await _recipeImportService.ImportAsync(retryPrompt, cancellationToken);
        var secondDto = Map(secondExtraction);

        var secondValidation = await _validator.ValidateAsync(secondDto, cancellationToken);

        if (secondValidation.IsValid)
        {
            _logger.LogInformation(
                "Recipe import succeeded on retry. Confidence: {Confidence}, NeedsReview: {NeedsReview}, IngredientCount: {IngredientCount}, StepCount: {StepCount}",
                secondDto.Confidence,
                secondDto.NeedsReview,
                secondDto.Ingredients.Count,
                secondDto.Steps.Count);

            return secondDto;
        }

        var secondErrors = secondValidation.Errors
            .Select(x => x.ErrorMessage)
            .ToArray();

        _logger.LogWarning(
            "Second recipe import attempt failed validation. Returning review-required result. ErrorCount: {ErrorCount}, Errors: {Errors}",
            secondErrors.Length,
            string.Join(" | ", secondErrors));

        return secondDto with
        {
            NeedsReview = true,
            Confidence = Math.Min(secondDto.Confidence, 0.5),
            Notes = AppendValidationNotes(secondDto.Notes, secondErrors)
        };
    }

    private static ImportedRecipeDto Map(RecipeExtractionResult extraction)
    {
        return new ImportedRecipeDto(
            extraction.Title,
            extraction.Servings,
            extraction.Ingredients
                .Select(i => new ImportedIngredientDto(i.Name, i.Quantity, i.Unit, i.Notes))
                .ToList(),
            extraction.Steps.ToList(),
            extraction.Notes,
            extraction.Confidence,
            extraction.NeedsReview);
    }

    private static string BuildRetryPrompt(string originalText, IEnumerable<string> errors)
    {
        return $"""
Original recipe text:
{originalText}

The previous extraction had these validation errors:
- {string.Join("\n- ", errors)}

Retry the extraction.
Use null for unknown values.
Mark ambiguous content for review.
""";
    }

    private static string AppendValidationNotes(string? existingNotes, IEnumerable<string> errors)
    {
        var validationText = $"Validation issues: {string.Join("; ", errors)}";

        if (string.IsNullOrWhiteSpace(existingNotes))
        {
            return validationText;
        }

        return $"{existingNotes} {validationText}";
    }
}