using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.Telemetry;

public sealed class InMemoryToolCallTelemetry : IToolCallTelemetry
{
    private const int MaxCapacity = 1_000;
    private readonly ConcurrentQueue<ToolCallRecord> _records = new();

    public void Record(ToolCallRecord record)
    {
        _records.Enqueue(record);
        while (_records.Count > MaxCapacity)
            _records.TryDequeue(out _);
    }

    public IReadOnlyList<ToolCallRecord> GetRecent(int limit) =>
        _records.TakeLast(Math.Min(limit, MaxCapacity)).ToList();
}
