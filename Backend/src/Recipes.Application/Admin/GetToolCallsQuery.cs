using MediatR;
using Recipes.Application.Common.AI;

namespace Recipes.Application.Admin;

public sealed record GetToolCallsQuery(int Limit = 100) : IRequest<IReadOnlyList<ToolCallDto>>;

public sealed record ToolCallDto(
    Guid     Id,
    DateTime Timestamp,
    string   AgentName,
    string   ToolName,
    int      InputBytes,
    int      OutputBytes,
    long     LatencyMs,
    bool     IsError,
    int?     InputTokens,
    int?     OutputTokens,
    int?     CacheReadTokens,
    int?     CacheCreationTokens);

public sealed class GetToolCallsHandler(IToolCallTelemetry telemetry)
    : IRequestHandler<GetToolCallsQuery, IReadOnlyList<ToolCallDto>>
{
    public Task<IReadOnlyList<ToolCallDto>> Handle(
        GetToolCallsQuery request, CancellationToken cancellationToken)
    {
        var records = telemetry.GetRecent(Math.Clamp(request.Limit, 1, 1_000));
        IReadOnlyList<ToolCallDto> result = records
            .Select(r => new ToolCallDto(
                r.Id, r.Timestamp, r.AgentName, r.ToolName,
                r.InputBytes, r.OutputBytes, r.LatencyMs, r.IsError,
                r.InputTokens, r.OutputTokens, r.CacheReadTokens, r.CacheCreationTokens))
            .ToList();
        return Task.FromResult(result);
    }
}
