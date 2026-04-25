using ErrorOr;
using MediatR;

namespace Recipes.Application.Recipes.BatchAnalyseRecipes;

public sealed record GetRecipeBatchAnalysisResultsQuery(
    string BatchId) : IRequest<ErrorOr<BatchResultsDto>>;

public sealed class GetRecipeBatchAnalysisResultsHandler
    : IRequestHandler<GetRecipeBatchAnalysisResultsQuery, ErrorOr<BatchResultsDto>>
{
    private readonly IRecipeBatchAnalysisService _batchService;

    public GetRecipeBatchAnalysisResultsHandler(IRecipeBatchAnalysisService batchService)
    {
        _batchService = batchService;
    }

    public async Task<ErrorOr<BatchResultsDto>> Handle(
        GetRecipeBatchAnalysisResultsQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.BatchId))
            return Error.Validation("BatchAnalysis.InvalidId", "Batch ID is required.");

        var results = await _batchService.GetResultsAsync(request.BatchId, cancellationToken);
        return results;
    }
}
