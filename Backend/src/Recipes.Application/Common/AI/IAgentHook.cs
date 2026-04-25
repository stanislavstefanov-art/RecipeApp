using System.Text.Json;

namespace Recipes.Application.Common.AI;

public abstract record HookDecision
{
    public static HookDecision Allow() => AllowDecision.Instance;
    public static HookDecision Block(string reason) => new BlockDecision(reason);

    public sealed record AllowDecision : HookDecision
    {
        internal static readonly AllowDecision Instance = new();
    }

    public sealed record BlockDecision(string Reason) : HookDecision;
}

public sealed record AgentHookContext(
    string AgentName,
    string ToolName,
    JsonElement? Input,
    int Iteration);

public sealed record ToolCallOutcome(
    bool IsError,
    string? Content,
    long LatencyMs);

public interface IAgentHook
{
    Task<HookDecision> OnBeforeToolCallAsync(AgentHookContext ctx, CancellationToken ct);
    Task OnAfterToolCallAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct);
}
