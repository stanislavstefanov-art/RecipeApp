using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.ImportRecipeFromText;

public sealed record ImportRecipeFromTextCommand(string Text) : IRequest<ErrorOr<ImportedRecipeDto>>;