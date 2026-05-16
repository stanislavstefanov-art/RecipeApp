using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Recipes.UploadRecipeImage;

public sealed class UploadRecipeImageHandler : IRequestHandler<UploadRecipeImageCommand, ErrorOr<string>>
{
    private readonly IRecipeRepository _repository;
    private readonly IBlobStorageService _blobStorage;
    private readonly ICurrentUser _currentUser;

    public UploadRecipeImageHandler(
        IRecipeRepository repository,
        IBlobStorageService blobStorage,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _blobStorage = blobStorage;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<string>> Handle(UploadRecipeImageCommand request, CancellationToken cancellationToken)
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
            var oldBlobName = ExtractBlobName(recipe.ImageUrl);
            if (oldBlobName is not null)
                await _blobStorage.DeleteAsync(oldBlobName, cancellationToken);
        }

        var blobName = $"{request.RecipeId}/{Guid.NewGuid()}{request.Extension}";
        var url = await _blobStorage.UploadAsync(blobName, request.Content, request.ContentType, cancellationToken);

        recipe.SetImageUrl(url);
        await _repository.SaveChangesAsync(cancellationToken);

        return url;
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
