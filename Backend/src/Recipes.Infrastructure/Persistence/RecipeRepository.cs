using Microsoft.EntityFrameworkCore;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Infrastructure.Persistence;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly RecipesDbContext _db;

    public RecipeRepository(RecipesDbContext db)
    {
        _db = db;
    }

    public async Task<Recipe?> GetByIdAsync(RecipeId id, CancellationToken cancellationToken = default)
        => await _db.Recipes
            .Include(r => r.Ingredients)
            .Include(r => r.Steps)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Recipes
            .AsNoTracking()
            .OrderBy(r => r.Name.Value)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Recipe>> SearchByIngredientNameAsync(
        string ingredientName, CancellationToken cancellationToken = default)
        => await _db.Recipes
            .AsNoTracking()
            .Where(r => r.Ingredients.Any(i => EF.Functions.Like(i.Name, $"%{ingredientName}%")))
            .OrderBy(r => r.Name.Value)
            .ToListAsync(cancellationToken);

    public void Add(Recipe recipe) => _db.Recipes.Add(recipe);

    public void Remove(Recipe recipe) => _db.Recipes.Remove(recipe);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await _db.SaveChangesAsync(cancellationToken);
}
