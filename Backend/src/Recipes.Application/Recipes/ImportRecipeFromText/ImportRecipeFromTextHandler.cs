using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.ImportRecipeFromText;

public sealed class ImportRecipeFromTextHandler
    : IRequestHandler<ImportRecipeFromTextCommand, ErrorOr<ImportedRecipeDto>>
{
    private readonly IRecipeImportOrchestrator _orchestrator;

    public ImportRecipeFromTextHandler(IRecipeImportOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public Task<ErrorOr<ImportedRecipeDto>> Handle(
        ImportRecipeFromTextCommand request,
        CancellationToken cancellationToken)
    {
        return _orchestrator.ImportAsync(request.Text, cancellationToken);
    }
}