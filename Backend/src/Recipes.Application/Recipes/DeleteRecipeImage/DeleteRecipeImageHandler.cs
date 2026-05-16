using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.DeleteRecipeImage;

public sealed class DeleteRecipeImageHandler : IRequestHandler<DeleteRecipeImageCommand, ErrorOr<Deleted>>
{
    private readonly IRecipeRepository _repository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ICurrentUser _currentUser;

    public DeleteRecipeImageHandler(
        IRecipeRepository repository,
        IBlobStorageService blobStorage,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteRecipeImageCommand request, CancellationToken cancellationToken)
    {
        var recipeId = RecipeId.From(request.RecipeId);
        var recipe = await _repository.GetByIdAsync(recipeId, cancellationToken);

        if (recipe is null)
            return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");

        if (recipe.HouseholdId.HasValue)
        {
            var memberIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
            if (!memberIds.Contains(recipe.HouseholdId.Value))
                return Error.NotFound("Recipe.NotFound", $"Recipe '{request.RecipeId}' was not found.");
        }

        if (recipe.ImageUrl is not null)
        {
            var blobName = ExtractBlobName(recipe.ImageUrl);
            if (blobName is not null)
                await _blobStorage.DeleteAsync(blobName, cancellationToken);

            recipe.SetImageUrl(null);
            await _repository.SaveChangesAsync(cancellationToken);
        }

        return Result.Deleted;
    }

    private static string? ExtractBlobName(string imageUrl)
    {
        try
        {
            var path = new Uri(imageUrl).AbsolutePath.TrimStart('/');
            var slash = path.IndexOf('/');
            return slash >= 0 ? path[(slash + 1)..] : null;
        }
        catch
        {
            return null;
        }
    }
}
