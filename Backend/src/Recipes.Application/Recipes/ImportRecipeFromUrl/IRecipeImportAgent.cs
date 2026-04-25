using ErrorOr;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public interface IRecipeImportAgent
{
    Task<ErrorOr<ImportedRecipeDto>> RunAsync(string sourceUrl, CancellationToken cancellationToken);
}
