using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.CookingLog.DeleteCookingEntry;
using Recipes.Application.CookingLog.GetRecipeCookingHistory;
using Recipes.Application.CookingLog.LogCookingEntry;

namespace Recipes.Api.Endpoints;

public static class CookingLogEndpoints
{
    public static IEndpointRouteBuilder MapCookingLogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/cooking-log")
            .WithTags("CookingLog")
            .RequireAuthorization();

        group.MapPost("/", async (LogCookingEntryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new LogCookingEntryCommand(request.RecipeId, request.CookedOn, request.Servings, request.Notes), ct);
            return result.ToHttpResult(dto => Results.Created($"/api/cooking-log/{dto.Id}", dto));
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCookingEntryCommand(id), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapGet("/recipe/{recipeId:guid}", async (Guid recipeId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetRecipeCookingHistoryQuery(recipeId), ct);
            return result.ToHttpResult(list => Results.Ok(list));
        });

        return app;
    }
}

file sealed record LogCookingEntryRequest(Guid RecipeId, DateOnly CookedOn, int Servings, string? Notes);
