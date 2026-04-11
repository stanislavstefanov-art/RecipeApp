using FluentValidation;
using Microsoft.Extensions.Logging;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.SuggestIngredientSubstitutions;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeIngredientSubstitutionSuggestionService
    : IIngredientSubstitutionSuggestionService
{
    private readonly IClaudeIngredientSubstitutionClient _client;
    private readonly IClaudeAssetProvider _assetProvider;
    private readonly IValidator<IngredientSubstitutionSuggestionDto> _validator;
    private readonly ILogger<ClaudeIngredientSubstitutionSuggestionService> _logger;

    public ClaudeIngredientSubstitutionSuggestionService(
        IClaudeIngredientSubstitutionClient client,
        IClaudeAssetProvider assetProvider,
        IValidator<IngredientSubstitutionSuggestionDto> validator,
        ILogger<ClaudeIngredientSubstitutionSuggestionService> logger)
    {
        _client = client;
        _assetProvider = assetProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<IngredientSubstitutionSuggestionDto> SuggestAsync(
        IngredientSubstitutionRequestDto request,
        CancellationToken cancellationToken)
    {
        var prompt = await _assetProvider.GetIngredientSubstitutionPromptAsync(cancellationToken);
        var schema = await _assetProvider.GetIngredientSubstitutionSchemaAsync(cancellationToken);

        _logger.LogInformation(
            "Starting Claude ingredient substitution suggestion for {Ingredient}",
            request.IngredientName);

        var first = await _client.SuggestAsync(request, prompt, schema, cancellationToken);
        var firstValidation = await _validator.ValidateAsync(first, cancellationToken);

        if (firstValidation.IsValid)
        {
            _logger.LogInformation(
                "Ingredient substitution succeeded on first attempt. Confidence: {Confidence}, NeedsReview: {NeedsReview}",
                first.Confidence,
                first.NeedsReview);

            return first;
        }

        var firstErrors = firstValidation.Errors.Select(x => x.ErrorMessage).ToArray();

        _logger.LogWarning(
            "Ingredient substitution failed validation on first attempt. Errors: {Errors}",
            string.Join(" | ", firstErrors));

        var second = await _client.SuggestAsync(request, prompt, schema, cancellationToken);
        var secondValidation = await _validator.ValidateAsync(second, cancellationToken);

        if (secondValidation.IsValid)
        {
            _logger.LogInformation(
                "Ingredient substitution succeeded on retry. Confidence: {Confidence}, NeedsReview: {NeedsReview}",
                second.Confidence,
                second.NeedsReview);

            return second;
        }

        var secondErrors = secondValidation.Errors.Select(x => x.ErrorMessage).ToArray();

        _logger.LogWarning(
            "Ingredient substitution failed validation on retry. Returning review-required result. Errors: {Errors}",
            string.Join(" | ", secondErrors));

        return second with
        {
            NeedsReview = true,
            Confidence = Math.Min(second.Confidence, 0.5),
            Notes = AppendValidationNotes(second.Notes, secondErrors)
        };
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