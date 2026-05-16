using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.UploadRecipeImage;

public sealed record UploadRecipeImageCommand(
    Guid RecipeId,
    Stream Content,
    string ContentType,
    string Extension) : IRequest<ErrorOr<string>>;
