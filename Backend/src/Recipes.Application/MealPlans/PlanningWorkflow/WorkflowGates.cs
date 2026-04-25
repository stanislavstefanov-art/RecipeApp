namespace Recipes.Application.MealPlans.PlanningWorkflow;

public sealed class MinSlotsCoveredGate : IWorkflowGate
{
    public string Name  => "MinSlotsCovered";
    public string Stage => "slot_coverage";

    public GateCheckResult Check(WorkflowGateContext ctx)
    {
        int expected = ctx.ExpectedDays * ctx.MealTypes.Count;
        int actual   = ctx.Draft.Entries.Count;
        return actual >= expected
            ? new(Name, true,  $"{actual}/{expected} slots covered.")
            : new(Name, false, $"Only {actual}/{expected} slots assigned.");
    }
}

public sealed class RecipeDiversityGate : IWorkflowGate
{
    public string Name  => "RecipeDiversity";
    public string Stage => "diversity_review";

    public GateCheckResult Check(WorkflowGateContext ctx)
    {
        var byMealType = ctx.Draft.Entries
            .GroupBy(e => e.MealType);

        foreach (var group in byMealType)
        {
            var sorted = group.OrderBy(e => e.PlannedDate).ToList();
            for (int i = 1; i < sorted.Count; i++)
            {
                var prev = sorted[i - 1];
                var curr = sorted[i];
                if (curr.BaseRecipeId == prev.BaseRecipeId &&
                    curr.PlannedDate  == prev.PlannedDate.AddDays(1))
                {
                    return new(Name, false,
                        $"Recipe {curr.BaseRecipeId} repeated on consecutive days " +
                        $"({prev.PlannedDate:yyyy-MM-dd} and {curr.PlannedDate:yyyy-MM-dd}) " +
                        $"for meal type {curr.MealType}.");
                }
            }
        }

        return new(Name, true, "No consecutive-day recipe repeats detected.");
    }
}

public sealed class MemberCoverageGate : IWorkflowGate
{
    public string Name  => "MemberCoverage";
    public string Stage => "member_assignment";

    public GateCheckResult Check(WorkflowGateContext ctx)
    {
        var unassigned = ctx.Draft.Entries
            .Where(e => e.Assignments.Count == 0)
            .ToList();

        return unassigned.Count == 0
            ? new(Name, true,  "All entries have at least one person assigned.")
            : new(Name, false, $"{unassigned.Count} entry/entries have no person assignments.");
    }
}
