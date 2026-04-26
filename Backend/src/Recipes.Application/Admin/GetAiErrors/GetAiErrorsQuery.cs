using MediatR;
using Recipes.Application.Common.AI;

namespace Recipes.Application.Admin.GetAiErrors;

public sealed record GetAiErrorsQuery(int Limit = 100) : IRequest<IReadOnlyList<AiErrorEnvelope>>;

public sealed class GetAiErrorsHandler
    : IRequestHandler<GetAiErrorsQuery, IReadOnlyList<AiErrorEnvelope>>
{
    private readonly IAiErrorStore _store;

    public GetAiErrorsHandler(IAiErrorStore store) => _store = store;

    public Task<IReadOnlyList<AiErrorEnvelope>> Handle(
        GetAiErrorsQuery request,
        CancellationToken cancellationToken)
        => Task.FromResult(_store.GetRecent(Math.Clamp(request.Limit, 1, 1000)));
}
