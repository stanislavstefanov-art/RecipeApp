using ErrorOr;
using MediatR;
using Recipes.Application.Recipes.GetRecipe;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.ScaleRecipe;

public sealed record ScaleRecipeCommand(
    Guid RecipeId,
    int FromServings,
    int ToServings) : IRequest<ErrorOr<ScaledRecipeDto>>;

public sealed class ScaleRecipeHandler : IRequestHandler<ScaleRecipeCommand, ErrorOr<ScaledRecipeDto>>
{
    private readonly IRecipeRepository _repository;
    private readonly IRecipeScalingService _scalingService;

    public ScaleRecipeHandler(IRecipeRepository repository, IRecipeScalingService scalingService)
    {
        _repository     = repository;
        _scalingService = scalingService;
    }

    public async Task<ErrorOr<ScaledRecipeDto>> Handle(
        ScaleRecipeCommand request,
        CancellationToken cancellationToken)
    {
        if (request.FromServings <= 0 || request.ToServings <= 0)
            return Error.Validation("ScaleRecipe.InvalidServings", "Servings must be greater than zero.");

        var recipeId = RecipeId.From(request.RecipeId);
        var recipe   = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        var dto = new RecipeDto(
            recipe.Id.Value,
            recipe.Name.Value,
            recipe.Ingredients.Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit)).ToList(),
            recipe.Steps.Select(s => new RecipeStepDto(s.Order, s.Instruction)).ToList());

        var scaled = await _scalingService.ScaleAsync(dto, request.FromServings, request.ToServings, cancellationToken);
        return scaled;
    }
}
