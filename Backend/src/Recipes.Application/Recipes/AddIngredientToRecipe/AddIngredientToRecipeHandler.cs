using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Recipes.AddIngredientToRecipe;

public sealed class AddIngredientToRecipeHandler
    : IRequestHandler<AddIngredientToRecipeCommand, AddIngredientToRecipeResult>
{
    private readonly IRecipesDbContext _db;

    public AddIngredientToRecipeHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<AddIngredientToRecipeResult> Handle(
        AddIngredientToRecipeCommand request,
        CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);

        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

        if (recipe is null)
        {
            return AddIngredientToRecipeResult.NotFound;
        }

        recipe.AddIngredient(request.Name, request.Quantity, request.Unit);
        await _db.SaveChangesAsync(cancellationToken);

        return AddIngredientToRecipeResult.Added;
    }
}

