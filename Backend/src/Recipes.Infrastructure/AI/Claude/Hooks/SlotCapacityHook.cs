using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.Claude.Hooks;

public sealed class SlotCapacityHook : IAgentHook
{
    private readonly ConcurrentDictionary<string, byte> _assignedSlots = new();

    public Task<HookDecision> OnBeforeToolCallAsync(AgentHookContext ctx, CancellationToken ct)
    {
        if (ctx.ToolName != "assign_meal" || ctx.Input is null)
            return Task.FromResult(HookDecision.Allow());

        var input = ctx.Input.Value;
        if (!input.TryGetProperty("date", out var dateProp) ||
            !input.TryGetProperty("mealType", out var mealTypeProp))
            return Task.FromResult(HookDecision.Allow());

        var slot = $"{dateProp.GetString()}/{mealTypeProp.GetInt32()}";

        if (!_assignedSlots.TryAdd(slot, 0))
            return Task.FromResult(
                HookDecision.Block($"Slot {slot} is already assigned. Choose a different day or meal type."));

        return Task.FromResult(HookDecision.Allow());
    }

    public Task OnAfterToolCallAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct)
        => Task.CompletedTask;
}
