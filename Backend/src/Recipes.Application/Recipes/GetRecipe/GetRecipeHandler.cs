using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Recipes.GetRecipe;

public sealed class GetRecipeHandler : IRequestHandler<GetRecipeQuery, RecipeDto?>
{
    private readonly IRecipesDbContext _db;

    public GetRecipeHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<RecipeDto?> Handle(GetRecipeQuery request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);

        var recipe = await _db.Recipes
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

        if (recipe is null)
        {
            return null;
        }

        return new RecipeDto(
            recipe.Id.Value,
            recipe.Name.Value,
            recipe.Ingredients.Select(i => new IngredientDto(i.Name, i.Quantity, i.Unit)).ToList(),
            recipe.Steps.Select(s => new RecipeStepDto(s.Order, s.Instruction)).ToList());
    }
}

