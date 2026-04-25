namespace Recipes.Application.Common.AI;

public sealed record EscalationRecord(
    Guid EscalationId,
    Guid SessionId,
    string Reason,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    string? ReviewNotes);

public interface IEscalationStore
{
    void Create(Guid sessionId, string reason);
    bool HasPending(Guid sessionId);
    void Resolve(Guid sessionId, string? reviewNotes);
    IReadOnlyList<EscalationRecord> GetAll(string status); // "all" | "pending" | "resolved"
}
