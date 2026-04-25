using MediatR;
using Recipes.Application.Common.AI;

namespace Recipes.Application.Admin.GetProvenance;

public sealed record GetProvenanceQuery(int Limit = 100) : IRequest<IReadOnlyList<ProvenanceRecord>>;

public sealed class GetProvenanceHandler
    : IRequestHandler<GetProvenanceQuery, IReadOnlyList<ProvenanceRecord>>
{
    private readonly IProvenanceStore _store;

    public GetProvenanceHandler(IProvenanceStore store) => _store = store;

    public Task<IReadOnlyList<ProvenanceRecord>> Handle(
        GetProvenanceQuery request,
        CancellationToken cancellationToken)
        => Task.FromResult(_store.GetRecent(Math.Clamp(request.Limit, 1, 1000)));
}
