using ErrorOr;
using MediatR;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ReviewRecipeDraft;

public sealed record ReviewRecipeDraftCommand(string RecipeText) : IRequest<ErrorOr<RecipeDraftReviewDto>>;

public sealed class ReviewRecipeDraftHandler
    : IRequestHandler<ReviewRecipeDraftCommand, ErrorOr<RecipeDraftReviewDto>>
{
    private readonly IRecipeImportOrchestrator _importer;
    private readonly IRecipeDraftReviewService _reviewer;
    private readonly IAiErrorStore _aiErrorStore;

    public ReviewRecipeDraftHandler(
        IRecipeImportOrchestrator importer,
        IRecipeDraftReviewService reviewer,
        IAiErrorStore aiErrorStore)
    {
        _importer     = importer;
        _reviewer     = reviewer;
        _aiErrorStore = aiErrorStore;
    }

    public async Task<ErrorOr<RecipeDraftReviewDto>> Handle(
        ReviewRecipeDraftCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RecipeText))
            return Error.Validation("ReviewDraft.Empty", "Recipe text is required.");

        var importResult = await _importer.ImportAsync(request.RecipeText, cancellationToken);
        if (importResult.IsError)
            return importResult.FirstError;

        try
        {
            var review = await _reviewer.ReviewAsync(importResult.Value, cancellationToken);
            return review;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            var envelope = AiErrorClassifier.Classify(ex, "recipe-jury");
            _aiErrorStore.Record(envelope);
            return Error.Failure($"AI.{envelope.Code}", envelope.Message);
        }
    }
}
