using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.MealPlans.AcceptMealPlanSuggestion;
using Recipes.Application.MealPlans.AddRecipeToMealPlan;
using Recipes.Application.MealPlans.CreateMealPlan;
using Recipes.Application.MealPlans.GenerateShoppingListFromMealPlan;
using Recipes.Application.MealPlans.GetMealPlan;
using Recipes.Application.MealPlans.ListMealPlans;
using Recipes.Application.MealPlans.SuggestMealPlan;

namespace Recipes.Api.Endpoints;

public static class MealPlansEndpoints
{
    public static IEndpointRouteBuilder MapMealPlansEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/meal-plans")
            .WithTags("MealPlans");

        group.MapPost("/", async (CreateMealPlanRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateMealPlanCommand(request.Name), ct);
            return result.ToHttpResult(response => Results.Created($"/api/meal-plans/{response.Id}", response));
        });

        group.MapGet("/{mealPlanId:guid}", async (Guid mealPlanId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMealPlanQuery(mealPlanId), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/{mealPlanId:guid}/entries", async (Guid mealPlanId, AddMealPlanEntryRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AddRecipeToMealPlanCommand(
                    mealPlanId,
                    request.RecipeId,
                    request.PlannedDate,
                    request.MealType),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/{mealPlanId:guid}/shopping-lists/{shoppingListId:guid}", async (
            Guid mealPlanId,
            Guid shoppingListId,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GenerateShoppingListFromMealPlanCommand(mealPlanId, shoppingListId),
                ct);

            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/suggest", async (SuggestMealPlanRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new SuggestMealPlanCommand(
                    request.Name,
                    request.StartDate,
                    request.NumberOfDays,
                    request.MealTypes),
                ct);

            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapPost("/accept-suggestion", async (AcceptMealPlanSuggestionRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new AcceptMealPlanSuggestionCommand(
                    request.Name,
                    request.Entries.Select(x => new AcceptMealPlanSuggestionEntryDto(
                        x.RecipeId,
                        x.PlannedDate,
                        x.MealType)).ToList()),
                ct);

            return result.ToHttpResult(response => Results.Created($"/api/meal-plans/{response.MealPlanId}", response));
        });

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListMealPlansQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        return app;
    }
}

public sealed record CreateMealPlanRequest(string Name);
public sealed record AddMealPlanEntryRequest(Guid RecipeId, DateOnly PlannedDate, int MealType);
public sealed record SuggestMealPlanRequest(string Name, DateOnly StartDate, int NumberOfDays, IReadOnlyList<int> MealTypes);
public sealed record AcceptMealPlanSuggestionRequest(string Name, IReadOnlyList<AcceptMealPlanSuggestionEntryRequest> Entries);
public sealed record AcceptMealPlanSuggestionEntryRequest(Guid RecipeId, DateOnly PlannedDate, int MealType);