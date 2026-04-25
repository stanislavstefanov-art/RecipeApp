using MediatR;
using Recipes.Application.Common.AI;

namespace Recipes.Application.Admin.GetCalibrationReport;

public sealed record GetCalibrationReportQuery : IRequest<CalibrationReportDto>;

public sealed class GetCalibrationReportHandler
    : IRequestHandler<GetCalibrationReportQuery, CalibrationReportDto>
{
    private readonly IConfidenceCalibrationStore _store;

    public GetCalibrationReportHandler(IConfidenceCalibrationStore store)
        => _store = store;

    public Task<CalibrationReportDto> Handle(
        GetCalibrationReportQuery request,
        CancellationToken cancellationToken)
    {
        var buckets = _store.GetAll()
            .GroupBy(r => (r.Source, r.ReportedConfidence))
            .Select(g =>
            {
                var approved   = g.Count(r => r.ActualOutcome == true);
                var rejected   = g.Count(r => r.ActualOutcome == false);
                var unresolved = g.Count(r => r.ActualOutcome is null);
                var denom      = approved + rejected;
                return new CalibrationBucketDto(
                    g.Key.Source,
                    g.Key.ReportedConfidence,
                    g.Count(),
                    approved,
                    rejected,
                    unresolved,
                    denom == 0 ? 0.0 : Math.Round((double)approved / denom, 3));
            })
            .OrderBy(b => b.Source).ThenBy(b => b.Confidence)
            .ToList();

        return Task.FromResult(new CalibrationReportDto(buckets));
    }
}
