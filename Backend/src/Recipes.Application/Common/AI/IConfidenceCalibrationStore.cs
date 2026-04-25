namespace Recipes.Application.Common.AI;

public sealed record CalibrationRecord(
    Guid PredictionId,
    string Source,
    string ReportedConfidence,
    bool? ActualOutcome,
    DateTime CreatedAt,
    DateTime? ResolvedAt);

public interface IConfidenceCalibrationStore
{
    void RecordPrediction(Guid predictionId, string source, string confidence);
    void RecordOutcome(Guid predictionId, bool actualOutcome);
    IReadOnlyList<CalibrationRecord> GetAll();
}
