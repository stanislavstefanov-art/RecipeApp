namespace Recipes.Application.Common.AI;

public sealed record ProvenanceRecord(
    Guid Id,
    string Feature,
    string Model,
    string PromptVersion,
    DateTime GeneratedAt);

public interface IProvenanceStore
{
    Guid Record(string feature, string model, string promptVersion);
    IReadOnlyList<ProvenanceRecord> GetRecent(int limit);
}
