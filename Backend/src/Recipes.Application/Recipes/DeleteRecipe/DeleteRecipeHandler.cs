using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Recipes.DeleteRecipe;

public sealed class DeleteRecipeHandler : IRequestHandler<DeleteRecipeCommand, DeleteRecipeResult>
{
    private readonly IRecipesDbContext _db;

    public DeleteRecipeHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<DeleteRecipeResult> Handle(DeleteRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.Id);

        var recipe = await _db.Recipes
            .FirstOrDefaultAsync(r => r.Id == recipeId, cancellationToken);

        if (recipe is null)
        {
            return DeleteRecipeResult.NotFound;
        }

        _db.Recipes.Remove(recipe);
        await _db.SaveChangesAsync(cancellationToken);

        return DeleteRecipeResult.Deleted;
    }
}

