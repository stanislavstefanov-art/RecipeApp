using MediatR;
using Recipes.Application.Admin;

namespace Recipes.Api.Endpoints;

public static class AdminEndpoints
{
    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/admin").WithTags("Admin");

        group.MapGet("/tool-calls", async (
            int limit = 100,
            ISender sender = default!,
            CancellationToken ct = default) =>
        {
            var records = await sender.Send(new GetToolCallsQuery(limit), ct);
            return Results.Ok(records);
        })
        .WithSummary("Return the most recent N tool-call telemetry records (default 100, max 1000).");

        return app;
    }
}
