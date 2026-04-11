using ErrorOr;

namespace Recipes.Application.Recipes.ImportRecipeFromText;

public interface IRecipeImportOrchestrator
{
    Task<ErrorOr<ImportedRecipeDto>> ImportAsync(string text, CancellationToken cancellationToken);
}