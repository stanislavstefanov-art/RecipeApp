namespace Recipes.Application.Common.AI;

public sealed record AiErrorEnvelope(
    Guid Id,
    string Code,
    string Source,
    string Message,
    bool IsRetryable,
    DateTime OccurredAt);

public interface IAiErrorStore
{
    void Record(AiErrorEnvelope envelope);
    IReadOnlyList<AiErrorEnvelope> GetRecent(int limit);
}
