using ErrorOr;
using MediatR;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed record GetWorkflowSessionQuery(Guid SessionId)
    : IRequest<ErrorOr<WorkflowSession>>;

public sealed class GetWorkflowSessionHandler
    : IRequestHandler<GetWorkflowSessionQuery, ErrorOr<WorkflowSession>>
{
    private readonly IWorkflowSessionStore _store;

    public GetWorkflowSessionHandler(IWorkflowSessionStore store)
    {
        _store = store;
    }

    public Task<ErrorOr<WorkflowSession>> Handle(
        GetWorkflowSessionQuery request,
        CancellationToken cancellationToken)
    {
        var session = _store.Get(request.SessionId);

        return session is null
            ? Task.FromResult<ErrorOr<WorkflowSession>>(
                Error.NotFound("Session.NotFound", $"Workflow session '{request.SessionId}' was not found or has expired."))
            : Task.FromResult<ErrorOr<WorkflowSession>>(session);
    }
}
