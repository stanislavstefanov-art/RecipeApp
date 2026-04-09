using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Recipes.UpdateRecipe;

public sealed class UpdateRecipeHandler : IRequestHandler<UpdateRecipeCommand, UpdateRecipeResult>
{
    private readonly IRecipesDbContext _db;

    public UpdateRecipeHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<UpdateRecipeResult> Handle(UpdateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);

        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

        if (recipe is null)
        {
            return UpdateRecipeResult.NotFound;
        }

        recipe.Rename(request.Name);
        await _db.SaveChangesAsync(cancellationToken);

        return UpdateRecipeResult.Updated;
    }
}

