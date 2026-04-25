namespace Recipes.Application.Common.AI;

public interface IAgentHookRunner
{
    Task<HookDecision> RunBeforeAsync(AgentHookContext ctx, CancellationToken ct);
    Task RunAfterAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct);
}
