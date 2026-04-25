using ErrorOr;
using MediatR;
using Recipes.Application.Recipes.ImportRecipeFromText;

namespace Recipes.Application.Recipes.ImportRecipeFromUrl;

public sealed class ImportRecipeFromUrlHandler
    : IRequestHandler<ImportRecipeFromUrlCommand, ErrorOr<ImportedRecipeDto>>
{
    private readonly IRecipeImportAgent _agent;

    public ImportRecipeFromUrlHandler(IRecipeImportAgent agent) => _agent = agent;

    public Task<ErrorOr<ImportedRecipeDto>> Handle(
        ImportRecipeFromUrlCommand request,
        CancellationToken cancellationToken)
        => _agent.RunAsync(request.SourceUrl, cancellationToken);
}
