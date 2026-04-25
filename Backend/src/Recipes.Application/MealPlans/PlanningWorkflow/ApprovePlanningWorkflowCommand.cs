using ErrorOr;
using MediatR;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed record ApprovePlanningWorkflowCommand(
    Guid? SessionId,
    MealPlanSuggestionDto? Draft,
    bool Approved,
    string? ReviewNotes,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes) : IRequest<ErrorOr<MealPlanSuggestionDto>>;

public sealed class ApprovePlanningWorkflowHandler
    : IRequestHandler<ApprovePlanningWorkflowCommand, ErrorOr<MealPlanSuggestionDto>>
{
    private readonly IEnumerable<IWorkflowGate> _gates;
    private readonly IWorkflowSessionStore _sessionStore;

    public ApprovePlanningWorkflowHandler(
        IEnumerable<IWorkflowGate> gates,
        IWorkflowSessionStore sessionStore)
    {
        _gates        = gates;
        _sessionStore = sessionStore;
    }

    public Task<ErrorOr<MealPlanSuggestionDto>> Handle(
        ApprovePlanningWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Approved)
            return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
                Error.Validation("Workflow.Rejected", request.ReviewNotes ?? "Plan was rejected."));

        MealPlanSuggestionDto draft;
        int numberOfDays;
        IReadOnlyList<int> mealTypes;

        if (request.SessionId.HasValue)
        {
            var session = _sessionStore.Get(request.SessionId.Value);
            if (session is null)
                return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
                    Error.NotFound("Session.NotFound",
                        $"Workflow session '{request.SessionId}' was not found or has expired."));

            if (session.WorkflowResult.Draft is null)
                return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
                    Error.Validation("Session.NoDraft",
                        $"Session '{request.SessionId}' has no draft to approve (status: {session.WorkflowResult.Status})."));

            draft        = session.WorkflowResult.Draft;
            numberOfDays = session.NumberOfDays;
            mealTypes    = session.MealTypes;
        }
        else
        {
            draft        = request.Draft!;
            numberOfDays = request.NumberOfDays;
            mealTypes    = request.MealTypes;
        }

        var ctx = new WorkflowGateContext(draft, numberOfDays, mealTypes, MemberCount: 0);

        foreach (var gate in _gates)
        {
            var gr = gate.Check(ctx);
            if (!gr.Passed)
                return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
                    Error.Validation("Workflow.GateFailed", gr.Message));
        }

        return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(draft);
    }
}
