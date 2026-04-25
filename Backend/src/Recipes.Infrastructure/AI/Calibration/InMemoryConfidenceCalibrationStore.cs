using System.Collections.Concurrent;
using Recipes.Application.Common.AI;

namespace Recipes.Infrastructure.AI.Calibration;

public sealed class InMemoryConfidenceCalibrationStore : IConfidenceCalibrationStore
{
    private readonly ConcurrentDictionary<Guid, CalibrationRecord> _records = new();

    public void RecordPrediction(Guid predictionId, string source, string confidence)
        => _records[predictionId] = new CalibrationRecord(
            predictionId,
            source,
            confidence,
            ActualOutcome: null,
            CreatedAt: DateTime.UtcNow,
            ResolvedAt: null);

    public void RecordOutcome(Guid predictionId, bool actualOutcome)
    {
        if (_records.TryGetValue(predictionId, out var existing))
            _records[predictionId] = existing with
            {
                ActualOutcome = actualOutcome,
                ResolvedAt    = DateTime.UtcNow
            };
    }

    public IReadOnlyList<CalibrationRecord> GetAll()
        => _records.Values.ToList().AsReadOnly();
}
