using ErrorOr;
using MediatR;
using Recipes.Application.Common.AI;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.CritiqueRecipe;

public sealed record CritiqueRecipeCommand(Guid RecipeId) : IRequest<ErrorOr<RecipeCritiqueDto>>;

public sealed class CritiqueRecipeHandler : IRequestHandler<CritiqueRecipeCommand, ErrorOr<RecipeCritiqueDto>>
{
    private readonly IRecipeRepository _repository;
    private readonly IRecipeCritiqueService _critiqueService;
    private readonly IAiErrorStore _aiErrorStore;

    public CritiqueRecipeHandler(
        IRecipeRepository repository,
        IRecipeCritiqueService critiqueService,
        IAiErrorStore aiErrorStore)
    {
        _repository    = repository;
        _critiqueService = critiqueService;
        _aiErrorStore  = aiErrorStore;
    }

    public async Task<ErrorOr<RecipeCritiqueDto>> Handle(
        CritiqueRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        var dto = new RecipeDto(
            recipe.Id.Value,
            recipe.Name.Value,
            recipe.Ingredients.Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit)).ToList(),
            recipe.Steps.Select(s => new RecipeStepDto(s.Order, s.Instruction)).ToList());

        try
        {
            var critique = await _critiqueService.CritiqueAsync(dto, cancellationToken);
            return critique;
        }
        catch (Exception ex) when (ex is InvalidOperationException or HttpRequestException)
        {
            var envelope = AiErrorClassifier.Classify(ex, "recipe-critique");
            _aiErrorStore.Record(envelope);
            return Error.Failure($"AI.{envelope.Code}", envelope.Message);
        }
    }
}
