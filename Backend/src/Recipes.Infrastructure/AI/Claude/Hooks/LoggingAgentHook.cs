using Microsoft.Extensions.Logging;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.Claude.Hooks;

public sealed class LoggingAgentHook : IAgentHook
{
    private readonly ILogger<LoggingAgentHook> _logger;

    public LoggingAgentHook(ILogger<LoggingAgentHook> logger)
    {
        _logger = logger;
    }

    public Task<HookDecision> OnBeforeToolCallAsync(AgentHookContext ctx, CancellationToken ct)
    {
        _logger.LogDebug(
            "AgentHook Before | Agent={AgentName} Tool={ToolName} Iteration={Iteration}",
            ctx.AgentName, ctx.ToolName, ctx.Iteration);

        return Task.FromResult(HookDecision.Allow());
    }

    public Task OnAfterToolCallAsync(AgentHookContext ctx, ToolCallOutcome outcome, CancellationToken ct)
    {
        if (outcome.IsError)
            _logger.LogInformation(
                "AgentHook After  | Agent={AgentName} Tool={ToolName} IsError=true Latency={LatencyMs}ms",
                ctx.AgentName, ctx.ToolName, outcome.LatencyMs);
        else
            _logger.LogDebug(
                "AgentHook After  | Agent={AgentName} Tool={ToolName} IsError=false Latency={LatencyMs}ms",
                ctx.AgentName, ctx.ToolName, outcome.LatencyMs);

        return Task.CompletedTask;
    }
}
