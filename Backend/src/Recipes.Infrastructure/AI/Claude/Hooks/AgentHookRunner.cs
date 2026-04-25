using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.Claude.Hooks;

public sealed class AgentHookRunner : IAgentHookRunner
{
    private readonly IEnumerable<IAgentHook> _hooks;

    public AgentHookRunner(IEnumerable<IAgentHook> hooks)
    {
        _hooks = hooks;
    }

    public async Task<HookDecision> RunBeforeAsync(AgentHookContext ctx, CancellationToken ct)
    {
        foreach (var hook in _hooks)
        {
            var decision = await hook.OnBeforeToolCallAsync(ctx, ct);
            if (decision is HookDecision.BlockDecision)
                return decision;
        }
        return HookDecision.Allow();
    }

    public async Task RunAfterAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct)
    {
        foreach (var hook in _hooks)
            await hook.OnAfterToolCallAsync(ctx, outcome, ct);
    }
}
