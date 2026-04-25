using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.AnalyseRecipeNutrition;

public sealed record AnalyseRecipeNutritionCommand(Guid RecipeId)
    : IRequest<ErrorOr<NutritionAnalysisDto>>;

public interface INutritionAnalysisAgent
{
    Task<ErrorOr<NutritionAnalysisDto>> RunAsync(Guid recipeId, CancellationToken ct);
}
