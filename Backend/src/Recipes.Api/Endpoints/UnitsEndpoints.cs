using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Units.CreateUnit;
using Recipes.Application.Units.DeleteUnit;
using Recipes.Application.Units.ListUnits;

namespace Recipes.Api.Endpoints;

public static class UnitsEndpoints
{
    public static IEndpointRouteBuilder MapUnitsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/units")
            .WithTags("Units")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListUnitsQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/", async (CreateUnitRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateUnitCommand(request.Name, request.Abbreviation), ct);
            return result.ToHttpResult(dto => Results.Created($"/api/units/{dto.Id}", dto));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteUnitCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

public sealed record CreateUnitRequest(string Name, string Abbreviation);
