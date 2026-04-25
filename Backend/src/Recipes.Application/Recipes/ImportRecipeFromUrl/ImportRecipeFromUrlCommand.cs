using ErrorOr;
using MediatR;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public sealed record ImportRecipeFromUrlCommand(string SourceUrl)
    : IRequest<ErrorOr<ImportedRecipeDto>>;
