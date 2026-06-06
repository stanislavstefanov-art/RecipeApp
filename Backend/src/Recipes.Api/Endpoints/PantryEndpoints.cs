using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Pantry.AddPantryItem;
using Recipes.Application.Pantry.GetPantryItems;
using Recipes.Application.Pantry.RemovePantryItem;

namespace Recipes.Api.Endpoints;

public static class PantryEndpoints
{
    public static IEndpointRouteBuilder MapPantryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pantry")
            .WithTags("Pantry")
            .RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPantryItemsQuery(), ct);
            return result.ToHttpResult(items => Results.Ok(items));
        });

        group.MapPost("/", async (AddPantryItemRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddPantryItemCommand(request.IngredientName, request.Notes), ct);
            return result.ToHttpResult(item => Results.Created($"/api/pantry/{item.Id}", item));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemovePantryItemCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        return app;
    }
}

file sealed record AddPantryItemRequest(string IngredientName, string? Notes);
