using ErrorOr;
using MediatR;
using Recipes.Application.MealPlans.SuggestMealPlan;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.MealPlans.SuggestMealPlanMultiAgent;

public sealed class SuggestMealPlanMultiAgentHandler
    : IRequestHandler<SuggestMealPlanMultiAgentCommand, ErrorOr<MealPlanSuggestionDto>>
{
    private readonly IHouseholdRepository _householdRepository;
    private readonly IPersonRepository _personRepository;
    private readonly IMealPlanOrchestratorAgent _orchestrator;

    public SuggestMealPlanMultiAgentHandler(
        IHouseholdRepository householdRepository,
        IPersonRepository personRepository,
        IMealPlanOrchestratorAgent orchestrator)
    {
        _householdRepository = householdRepository;
        _personRepository    = personRepository;
        _orchestrator        = orchestrator;
    }

    public async Task<ErrorOr<MealPlanSuggestionDto>> Handle(
        SuggestMealPlanMultiAgentCommand request,
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
        var members = allPersons.Where(p => memberIds.Contains(p.Id)).ToList();

        var householdProfile = new HouseholdPlanningProfileDto(
            household.Id.Value,
            household.Name,
            members.Select(p => new PersonPlanningProfileDto(
                p.Id.Value,
                p.Name,
                p.DietaryPreferences.Select(d => (int)d).ToList(),
                p.HealthConcerns.Select(h => (int)h).ToList(),
                p.Notes)).ToList());

        return await _orchestrator.RunAsync(request, householdProfile, cancellationToken);
    }
}
