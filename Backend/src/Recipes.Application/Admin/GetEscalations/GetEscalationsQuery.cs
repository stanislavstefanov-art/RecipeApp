using MediatR;
using Recipes.Application.Common.AI;

namespace Recipes.Application.Admin.GetEscalations;

public sealed record GetEscalationsQuery(string Status = "all")
    : IRequest<IReadOnlyList<EscalationRecord>>;

public sealed class GetEscalationsHandler
    : IRequestHandler<GetEscalationsQuery, IReadOnlyList<EscalationRecord>>
{
    private readonly IEscalationStore _store;

    public GetEscalationsHandler(IEscalationStore store) => _store = store;

    public Task<IReadOnlyList<EscalationRecord>> Handle(
        GetEscalationsQuery request,
        CancellationToken cancellationToken)
        => Task.FromResult(_store.GetAll(request.Status));
}
