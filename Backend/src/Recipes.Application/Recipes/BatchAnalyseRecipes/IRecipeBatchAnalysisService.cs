using Recipes.Application.Recipes.GetRecipe;

namespace Recipes.Application.Recipes.BatchAnalyseRecipes;

public interface IRecipeBatchAnalysisService
{
    Task<BatchSubmissionDto> SubmitBatchAsync(IReadOnlyList<RecipeDto> recipes, CancellationToken cancellationToken);
    Task<BatchResultsDto> GetResultsAsync(string batchId, CancellationToken cancellationToken);
}
