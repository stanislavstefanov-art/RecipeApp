using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.Escalation;

public sealed class InMemoryEscalationStore : IEscalationStore
{
    private readonly ConcurrentDictionary<Guid, EscalationRecord> _records = new();

    public void Create(Guid sessionId, string reason)
        => _records[sessionId] = new EscalationRecord(
            Guid.NewGuid(),
            sessionId,
            reason,
            CreatedAt: DateTime.UtcNow,
            ResolvedAt: null,
            ReviewNotes: null);

    public bool HasPending(Guid sessionId)
        => _records.TryGetValue(sessionId, out var r) && r.ResolvedAt is null;

    public void Resolve(Guid sessionId, string? reviewNotes)
    {
        if (_records.TryGetValue(sessionId, out var existing))
            _records[sessionId] = existing with
            {
                ResolvedAt  = DateTime.UtcNow,
                ReviewNotes = reviewNotes
            };
    }

    public IReadOnlyList<EscalationRecord> GetAll(string status)
    {
        var all = _records.Values.ToList();
        return status switch
        {
            "pending"  => all.Where(r => r.ResolvedAt is null).ToList().AsReadOnly(),
            "resolved" => all.Where(r => r.ResolvedAt is not null).ToList().AsReadOnly(),
            _          => all.AsReadOnly(),
        };
    }
}
