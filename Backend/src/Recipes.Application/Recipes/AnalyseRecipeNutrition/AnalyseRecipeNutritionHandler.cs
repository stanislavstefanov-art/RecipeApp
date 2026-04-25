using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.AnalyseRecipeNutrition;

public sealed class AnalyseRecipeNutritionHandler
    : IRequestHandler<AnalyseRecipeNutritionCommand, ErrorOr<NutritionAnalysisDto>>
{
    private readonly INutritionAnalysisAgent _agent;

    public AnalyseRecipeNutritionHandler(INutritionAnalysisAgent agent) => _agent = agent;

    public Task<ErrorOr<NutritionAnalysisDto>> Handle(
        AnalyseRecipeNutritionCommand request,
        CancellationToken cancellationToken)
        => _agent.RunAsync(request.RecipeId, cancellationToken);
}
