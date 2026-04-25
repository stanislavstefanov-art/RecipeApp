namespace Recipes.Application.Common.AI;

public interface IToolCallTelemetry
{
    void Record(ToolCallRecord record);
    IReadOnlyList<ToolCallRecord> GetRecent(int limit);
}

public sealed record ToolCallRecord
{
    public Guid     Id                   { get; init; } = Guid.NewGuid();
    public DateTime Timestamp            { get; init; } = DateTime.UtcNow;
    public string   AgentName            { get; init; } = "";
    public string   ToolName             { get; init; } = "";
    public int      InputBytes           { get; init; }
    public int      OutputBytes          { get; init; }
    public long     LatencyMs            { get; init; }
    public bool     IsError              { get; init; }
    public int?     InputTokens          { get; init; }
    public int?     OutputTokens         { get; init; }
    public int?     CacheReadTokens      { get; init; }
    public int?     CacheCreationTokens  { get; init; }
}
