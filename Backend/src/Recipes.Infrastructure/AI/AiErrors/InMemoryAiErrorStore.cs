using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.AiErrors;

public sealed class InMemoryAiErrorStore : IAiErrorStore
{
    private readonly ConcurrentQueue<AiErrorEnvelope> _records = new();

    public void Record(AiErrorEnvelope envelope) => _records.Enqueue(envelope);

    public IReadOnlyList<AiErrorEnvelope> GetRecent(int limit)
        => _records.TakeLast(limit).Reverse().ToList().AsReadOnly();
}
