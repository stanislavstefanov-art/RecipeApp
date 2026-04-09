using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Entities;

namespace Recipes.Application.Recipes.CreateRecipe;

public sealed class CreateRecipeHandler : IRequestHandler<CreateRecipeCommand, CreateRecipeResponse>
{
    private readonly IRecipesDbContext _db;

    public CreateRecipeHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<CreateRecipeResponse> Handle(CreateRecipeCommand request, CancellationToken cancellationToken)
    {
        var recipe = new Recipe(request.Name);

        _db.Recipes.Add(recipe);
        await _db.SaveChangesAsync(cancellationToken);

        return new CreateRecipeResponse(recipe.Id.Value);
    }
}

