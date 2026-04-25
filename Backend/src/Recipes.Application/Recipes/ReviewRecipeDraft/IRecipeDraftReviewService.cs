using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ReviewRecipeDraft;

public interface IRecipeDraftReviewService
{
    Task<RecipeDraftReviewDto> ReviewAsync(ImportedRecipeDto draft, CancellationToken cancellationToken);
}
