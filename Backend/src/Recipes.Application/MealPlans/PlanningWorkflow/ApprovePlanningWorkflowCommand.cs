using ErrorOr;
using MediatR;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed record ApprovePlanningWorkflowCommand(
    MealPlanSuggestionDto Draft,
    bool Approved,
    string? ReviewNotes,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes) : IRequest<ErrorOr<MealPlanSuggestionDto>>;

public sealed class ApprovePlanningWorkflowHandler
    : IRequestHandler<ApprovePlanningWorkflowCommand, ErrorOr<MealPlanSuggestionDto>>
{
    private readonly IEnumerable<IWorkflowGate> _gates;

    public ApprovePlanningWorkflowHandler(IEnumerable<IWorkflowGate> gates)
    {
        _gates = gates;
    }

    public Task<ErrorOr<MealPlanSuggestionDto>> Handle(
        ApprovePlanningWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        if (!request.Approved)
            return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
                Error.Validation("Workflow.Rejected", request.ReviewNotes ?? "Plan was rejected."));

        var ctx = new WorkflowGateContext(
            request.Draft,
            request.NumberOfDays,
            request.MealTypes,
            MemberCount: 0);

        foreach (var gate in _gates)
        {
            var gr = gate.Check(ctx);
            if (!gr.Passed)
                return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(
                    Error.Validation("Workflow.GateFailed", gr.Message));
        }

        return Task.FromResult<ErrorOr<MealPlanSuggestionDto>>(request.Draft);
    }
}
