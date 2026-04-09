using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Recipes.SearchRecipesByIngredient;

public sealed class SearchRecipesByIngredientHandler
    : IRequestHandler<SearchRecipesByIngredientQuery, IReadOnlyList<RecipeSearchResultDto>>
{
    private readonly IRecipesDbContext _db;

    public SearchRecipesByIngredientHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecipeSearchResultDto>> Handle(
        SearchRecipesByIngredientQuery request,
        CancellationToken cancellationToken)
    {
        var ingredient = request.Ingredient.Trim();

        var recipeIds = await _db.Ingredients
            .AsNoTracking()
            .Where(i => EF.Functions.Like(i.Name, $"%{ingredient}%"))
            .Select(i => EF.Property<Guid>(i, "RecipeId"))
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _db.Recipes
            .AsNoTracking()
            .Where(r => recipeIds.Contains(r.Id.Value))
            .OrderBy(r => r.Name)
            .Select(r => new RecipeSearchResultDto(r.Id.Value, r.Name.Value))
            .ToListAsync(cancellationToken);
    }
}

