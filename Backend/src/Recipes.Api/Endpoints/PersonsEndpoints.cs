using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Persons.CreatePerson;
using Recipes.Application.Persons.GetPerson;
using Recipes.Application.Persons.ListPersons;

namespace Recipes.Api.Endpoints;

public static class PersonsEndpoints
{
    public static IEndpointRouteBuilder MapPersonsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/persons")
            .WithTags("Persons");

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListPersonsQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/", async (CreatePersonRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreatePersonCommand(
                    request.Name,
                    request.DietaryPreferences,
                    request.HealthConcerns,
                    request.Notes),
                ct);

            return result.ToHttpResult(response => Results.Created($"/api/persons/{response.Id}", response));
        });

        group.MapGet("/{personId:guid}", async (Guid personId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPersonQuery(personId), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        return app;
    }
}

public sealed record CreatePersonRequest(
    string Name,
    IReadOnlyList<int> DietaryPreferences,
    IReadOnlyList<int> HealthConcerns,
    string? Notes);