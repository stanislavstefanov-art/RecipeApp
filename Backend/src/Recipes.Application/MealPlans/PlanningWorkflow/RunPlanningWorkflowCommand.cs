using ErrorOr;
using MediatR;
using Recipes.Application.Common.AI;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed record RunPlanningWorkflowCommand(
    string Name,
    Guid HouseholdId,
    DateOnly StartDate,
    int NumberOfDays,
    IReadOnlyList<int> MealTypes) : IRequest<ErrorOr<WorkflowSessionResult>>;

public sealed class RunPlanningWorkflowHandler
    : IRequestHandler<RunPlanningWorkflowCommand, ErrorOr<WorkflowSessionResult>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IMealPlanWorkflowEnforcer _enforcer;
    private readonly IWorkflowSessionStore _sessionStore;
    private readonly IConfidenceCalibrationStore _calibration;
    private readonly IEscalationStore _escalationStore;

    public RunPlanningWorkflowHandler(
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository,
        IMealPlanWorkflowEnforcer enforcer,
        IWorkflowSessionStore sessionStore,
        IConfidenceCalibrationStore calibration,
        IEscalationStore escalationStore)
    {
        _householdRepository = householdRepository;
        _personRepository    = personRepository;
        _enforcer            = enforcer;
        _sessionStore        = sessionStore;
        _calibration         = calibration;
        _escalationStore     = escalationStore;
    }

    public async Task<ErrorOr<WorkflowSessionResult>> Handle(
        RunPlanningWorkflowCommand request,
        CancellationToken cancellationToken)
    {
        var household = await _householdRepository.GetByIdAsync(
            HouseholdId.From(request.HouseholdId), cancellationToken);

        if (household is null)
            return Error.NotFound("Household.NotFound", $"Household '{request.HouseholdId}' was not found.");

        var memberIds = household.Members.Select(m => m.PersonId).ToList();
        if (memberIds.Count == 0)
            return Error.Validation("MealPlan.NoMembers", "The household has no members.");

        var allPersons = await _personRepository.GetAllAsync(cancellationToken);
        var members    = allPersons.Where(p => memberIds.Contains(p.Id)).ToList();

        var householdProfile = new HouseholdPlanningProfileDto(
            household.Id.Value,
            household.Name,
            members.Select(p => new PersonPlanningProfileDto(
                p.Id.Value,
                p.Name,
                p.DietaryPreferences.Select(d => (int)d).ToList(),
                p.HealthConcerns.Select(h => (int)h).ToList(),
                p.Notes)).ToList());

        var workflowResult = await _enforcer.RunAsync(request, householdProfile, cancellationToken);
        var sessionId      = _sessionStore.Save(workflowResult, request.NumberOfDays, request.MealTypes);

        var isEscalated = false;
        if (workflowResult.Status == "pending_approval")
        {
            _calibration.RecordPrediction(sessionId, "workflow", workflowResult.Confidence);
            if (workflowResult.Confidence == "low")
            {
                _escalationStore.Create(sessionId, "low_confidence");
                isEscalated = true;
            }
        }

        return new WorkflowSessionResult(sessionId, workflowResult, isEscalated);
    }
}
