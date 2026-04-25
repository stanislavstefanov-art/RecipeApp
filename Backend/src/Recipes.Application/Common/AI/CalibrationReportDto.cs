namespace Recipes.Application.Common.AI;

public sealed record CalibrationBucketDto(
    string Source,
    string Confidence,
    int Total,
    int Approved,
    int Rejected,
    int Unresolved,
    double ApprovalRate);

public sealed record CalibrationReportDto(IReadOnlyList<CalibrationBucketDto> Buckets);
