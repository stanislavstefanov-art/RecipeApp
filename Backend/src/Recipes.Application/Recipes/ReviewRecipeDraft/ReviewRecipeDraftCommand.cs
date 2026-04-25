using ErrorOr;
using MediatR;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ReviewRecipeDraft;

public sealed record ReviewRecipeDraftCommand(string RecipeText) : IRequest<ErrorOr<RecipeDraftReviewDto>>;

public sealed class ReviewRecipeDraftHandler
    : IRequestHandler<ReviewRecipeDraftCommand, ErrorOr<RecipeDraftReviewDto>>
{
    private readonly IRecipeImportOrchestrator _importer;
    private readonly IRecipeDraftReviewService _reviewer;

    public ReviewRecipeDraftHandler(
        IRecipeImportOrchestrator importer,
        IRecipeDraftReviewService reviewer)
    {
        _importer = importer;
        _reviewer = reviewer;
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

        var review = await _reviewer.ReviewAsync(importResult.Value, cancellationToken);
        return review;
    }
}
