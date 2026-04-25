using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.Provenance;

public sealed class InMemoryProvenanceStore : IProvenanceStore
{
    private readonly ConcurrentQueue<ProvenanceRecord> _records = new();

    public Guid Record(string feature, string model, string promptVersion)
    {
        var id = Guid.NewGuid();
        _records.Enqueue(new ProvenanceRecord(id, feature, model, promptVersion, DateTime.UtcNow));
        return id;
    }

    public IReadOnlyList<ProvenanceRecord> GetRecent(int limit)
        => _records.TakeLast(limit).Reverse().ToList().AsReadOnly();
}
