using System.Collections.Concurrent;
using Recipes.Application.MealPlans.PlanningWorkflow;

namespace Recipes.Infrastructure.AI.Claude.Workflow;

public sealed class InMemoryWorkflowSessionStore : IWorkflowSessionStore
{
    private const int MaxCapacity  = 500;
    private static readonly TimeSpan SessionTtl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<Guid, WorkflowSession> _sessions = new();

    public Guid Save(WorkflowResult result, int numberOfDays, IReadOnlyList<int> mealTypes)
    {
        var sessionId = Guid.NewGuid();
        var now       = DateTime.UtcNow;

        var session = new WorkflowSession(
            sessionId,
            result,
            numberOfDays,
            mealTypes,
            CreatedAt: now,
            ExpiresAt: now + SessionTtl);

        _sessions[sessionId] = session;
        Evict();

        return sessionId;
    }

    public WorkflowSession? Get(Guid sessionId)
    {
        if (!_sessions.TryGetValue(sessionId, out var session))
            return null;

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _sessions.TryRemove(sessionId, out _);
            return null;
        }

        return session;
    }

    private void Evict()
    {
        if (_sessions.Count <= MaxCapacity)
            return;

        var oldest = _sessions.Values
            .OrderBy(s => s.CreatedAt)
            .Take(_sessions.Count - MaxCapacity)
            .Select(s => s.SessionId);

        foreach (var id in oldest)
            _sessions.TryRemove(id, out _);
    }
}
