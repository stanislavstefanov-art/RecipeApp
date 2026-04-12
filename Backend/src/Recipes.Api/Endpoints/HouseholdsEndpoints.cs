using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Households.AddPersonToHousehold;
using Recipes.Application.Households.CreateHousehold;
using Recipes.Application.Households.GetHousehold;
using Recipes.Application.Households.ListHouseholds;

namespace Recipes.Api.Endpoints;

public static class HouseholdsEndpoints
{
    public static IEndpointRouteBuilder MapHouseholdsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/households")
            .WithTags("Households");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListHouseholdsQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/", async (CreateHouseholdRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateHouseholdCommand(request.Name), ct);
            return result.ToHttpResult(response => Results.Created($"/api/households/{response.Id}", response));
        });

        group.MapGet("/{householdId:guid}", async (Guid householdId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetHouseholdQuery(householdId), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/{householdId:guid}/members/{personId:guid}", async (
            Guid householdId,
            Guid personId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new AddPersonToHouseholdCommand(householdId, personId), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

public sealed record CreateHouseholdRequest(string Name);