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

        var first = FilterPersons(FilterInvalidEntries(EnforceAppropriateForMealTypes(EnforceMealsPerCook(await _client.SuggestAsync(request, prompt, schema, cancellationToken), request.AvailableRecipes), request.AvailableRecipes), request.MealTypes), request.PersonsPerMealType);
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

        var second = FilterPersons(FilterInvalidEntries(EnforceAppropriateForMealTypes(EnforceMealsPerCook(await _client.SuggestAsync(retryRequest, prompt, schema, cancellationToken), request.AvailableRecipes), request.AvailableRecipes), request.MealTypes), request.PersonsPerMealType);
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

    private static MealPlanSuggestionDto FilterPersons(
        MealPlanSuggestionDto dto,
        IReadOnlyDictionary<int, IReadOnlyList<Guid>>? personsPerMealType)
    {
        if (personsPerMealType is null || personsPerMealType.Count == 0) return dto;

        var corrected = false;
        var entries = dto.Entries.Select(entry =>
        {
            if (!personsPerMealType.TryGetValue(entry.MealType, out var allowed)) return entry;
            var allowedSet = new HashSet<Guid>(allowed);
            var filtered = entry.Assignments.Where(a => allowedSet.Contains(a.PersonId)).ToList();
            if (filtered.Count == entry.Assignments.Count) return entry;
            corrected = true;
            return entry with { Assignments = filtered };
        }).ToList();

        return corrected ? dto with { Entries = entries, NeedsReview = true } : dto;
    }

    private static MealPlanSuggestionDto EnforceAppropriateForMealTypes(
        MealPlanSuggestionDto dto,
        IReadOnlyList<AvailableRecipeDto> availableRecipes)
    {
        var restrictions = availableRecipes
            .Where(r => r.AppropriateForMealTypes.Count > 0)
            .ToDictionary(r => r.RecipeId, r => new HashSet<int>(r.AppropriateForMealTypes));

        if (restrictions.Count == 0) return dto;

        var dropped = false;
        var entries = dto.Entries.Where(e =>
        {
            if (!restrictions.TryGetValue(e.BaseRecipeId, out var allowed)) return true;
            if (allowed.Contains(e.MealType)) return true;
            dropped = true;
            return false;
        }).ToList();

        return dropped ? dto with { Entries = entries, NeedsReview = true } : dto;
    }

    private static MealPlanSuggestionDto EnforceMealsPerCook(
        MealPlanSuggestionDto dto,
        IReadOnlyList<AvailableRecipeDto> availableRecipes)
    {
        var cap = availableRecipes.ToDictionary(r => r.RecipeId, r => r.MealsPerCook);
        var usageCount = new Dictionary<Guid, int>();
        var dropped = false;

        var entries = new List<MealPlanSuggestionEntryDto>();
        foreach (var entry in dto.Entries)
        {
            usageCount.TryGetValue(entry.BaseRecipeId, out var used);
            var allowed = cap.TryGetValue(entry.BaseRecipeId, out var c) ? c : 1;

            if (used >= allowed)
            {
                dropped = true;
                continue;
            }

            usageCount[entry.BaseRecipeId] = used + 1;
            entries.Add(entry);
        }

        return dropped ? dto with { Entries = entries, NeedsReview = true } : dto;
    }

    private static MealPlanSuggestionDto FilterInvalidEntries(MealPlanSuggestionDto dto, IReadOnlyList<int> allowedMealTypes)
    {
        var allowed = new HashSet<int>(allowedMealTypes);
        var fallback = allowedMealTypes[0];
        var corrected = false;

        var entries = dto.Entries.Select(e =>
        {
            if (allowed.Contains(e.MealType)) return e;
            corrected = true;
            return e with { MealType = fallback };
        }).ToList();

        return corrected ? dto with { Entries = entries, NeedsReview = true } : dto;
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