using FluentValidation;
using Microsoft.Extensions.Logging;
using Recipes.Application.Common.AI;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeMealPlanSuggestionService : IMealPlanSuggestionService
{
    private readonly IClaudeMealPlanSuggestionClient _client;
    private readonly IClaudeAssetProvider _assetProvider;
    private readonly IValidator<MealPlanSuggestionDto> _validator;
    private readonly ILogger<ClaudeMealPlanSuggestionService> _logger;

    public ClaudeMealPlanSuggestionService(
        IClaudeMealPlanSuggestionClient client,
        IClaudeAssetProvider assetProvider,
        IValidator<MealPlanSuggestionDto> validator,
        ILogger<ClaudeMealPlanSuggestionService> logger)
    {
        _client = client;
        _assetProvider = assetProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<MealPlanSuggestionDto> SuggestAsync(
        MealPlanSuggestionRequestDto request,
        CancellationToken cancellationToken)
    {
        var prompt = await _assetProvider.GetMealPlanSuggestionPromptAsync(cancellationToken);
        var schema = await _assetProvider.GetMealPlanSuggestionSchemaAsync(cancellationToken);

        _logger.LogInformation(
            "Starting Claude meal plan suggestion. Name: {Name}, Days: {Days}, MealTypeCount: {MealTypeCount}",
            request.Name,
            request.NumberOfDays,
            request.MealTypes.Count);

        var first = await _client.SuggestAsync(request, prompt, schema, cancellationToken);
        var firstValidation = await _validator.ValidateAsync(first, cancellationToken);

        if (firstValidation.IsValid)
        {
            _logger.LogInformation(
                "Meal plan suggestion succeeded on first attempt. Confidence: {Confidence}, NeedsReview: {NeedsReview}, EntryCount: {EntryCount}",
                first.Confidence,
                first.NeedsReview,
                first.Entries.Count);

            return first;
        }

        var firstErrors = firstValidation.Errors.Select(x => x.ErrorMessage).ToArray();

        _logger.LogWarning(
            "Meal plan suggestion failed validation on first attempt. Errors: {Errors}",
            string.Join(" | ", firstErrors));

        var retryRequest = request with
        {
            Name = $"{request.Name} (retry)"
        };

        var second = await _client.SuggestAsync(retryRequest, prompt, schema, cancellationToken);
        var secondValidation = await _validator.ValidateAsync(second, cancellationToken);

        if (secondValidation.IsValid)
        {
            _logger.LogInformation(
                "Meal plan suggestion succeeded on retry. Confidence: {Confidence}, NeedsReview: {NeedsReview}, EntryCount: {EntryCount}",
                second.Confidence,
                second.NeedsReview,
                second.Entries.Count);

            return second;
        }

        var secondErrors = secondValidation.Errors.Select(x => x.ErrorMessage).ToArray();

        _logger.LogWarning(
            "Meal plan suggestion failed validation on retry. Returning review-required result. Errors: {Errors}",
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