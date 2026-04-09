using MediatR;
using Microsoft.EntityFrameworkCore;
using Recipes.Application.Abstractions;
using Recipes.Domain.Primitives;

namespace Recipes.Application.Recipes.ListRecipes;

public sealed class ListRecipesHandler : IRequestHandler<ListRecipesQuery, IReadOnlyList<RecipeListItemDto>>
{
    private readonly IRecipesDbContext _db;

    public ListRecipesHandler(IRecipesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RecipeListItemDto>> Handle(ListRecipesQuery request, CancellationToken cancellationToken)
    {
        return await _db.Recipes
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RecipeListItemDto(r.Id.Value, r.Name.Value))
            .ToListAsync(cancellationToken);
    }
}

