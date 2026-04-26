using ErrorOr;
using MediatR;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.BatchAnalyseRecipes;

public sealed record SubmitRecipeBatchAnalysisCommand(
    IReadOnlyList<Guid> RecipeIds) : IRequest<ErrorOr<BatchSubmissionDto>>;

public sealed class SubmitRecipeBatchAnalysisHandler
    : IRequestHandler<SubmitRecipeBatchAnalysisCommand, ErrorOr<BatchSubmissionDto>>
{
    private readonly IRecipeRepository _repository;
    private readonly IRecipeBatchAnalysisService _batchService;
    private readonly IAiErrorStore _aiErrorStore;

    public SubmitRecipeBatchAnalysisHandler(
        IRecipeRepository repository,
        IRecipeBatchAnalysisService batchService,
        IAiErrorStore aiErrorStore)
    {
        _repository   = repository;
        _batchService = batchService;
        _aiErrorStore = aiErrorStore;
    }

    public async Task<ErrorOr<BatchSubmissionDto>> Handle(
        SubmitRecipeBatchAnalysisCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RecipeIds.Count == 0)
            return Error.Validation("BatchAnalysis.Empty", "At least one recipe ID is required.");

        if (request.RecipeIds.Count > 50)
            return Error.Validation("BatchAnalysis.TooLarge", "At most 50 recipes per batch.");

        var recipes = new List<RecipeDto>();
        foreach (var id in request.RecipeIds)
        {
            var recipe = await _repository.GetByIdAsync(RecipeId.From(id), cancellationToken);
            if (recipe is null)
                return Error.NotFound("Recipe.NotFound", $"Recipe '{id}' was not found.");

            recipes.Add(new RecipeDto(
                recipe.Id.Value,
                recipe.Name.Value,
                recipe.Ingredients.Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit)).ToList(),
                recipe.Steps.Select(s => new RecipeStepDto(s.Order, s.Instruction)).ToList()));
        }

        try
        {
            var submission = await _batchService.SubmitBatchAsync(recipes, cancellationToken);
            return submission;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            var envelope = AiErrorClassifier.Classify(ex, "recipe-batch");
            _aiErrorStore.Record(envelope);
            return Error.Failure($"AI.{envelope.Code}", envelope.Message);
        }
    }
}
