using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Expenses.CreateExpense;
using Recipes.Application.Expenses.GetExpenseInsights;
using Recipes.Application.Expenses.GetMonthlyExpenseReport;
using Recipes.Application.Expenses.ListExpenses;

namespace Recipes.Api.Endpoints;

public static class ExpensesEndpoints
{
    public static IEndpointRouteBuilder MapExpensesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses")
            .WithTags("Expenses");

        group.MapPost("/", async (CreateExpenseRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateExpenseCommand(
                    request.Amount,
                    request.Currency,
                    request.ExpenseDate,
                    request.Category,
                    request.Description,
                    request.SourceType,
                    request.SourceReferenceId),
                ct);

            return result.ToHttpResult(response => Results.Created($"/api/expenses/{response.Id}", response));
        });

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ListExpensesQuery(), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapGet("/monthly-report", async (int year, int month, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMonthlyExpenseReportQuery(year, month), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        group.MapGet("/insights", async (int year, int month, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetExpenseInsightsQuery(year, month), ct);
            return result.ToHttpResult(response => Results.Ok(response));
        });

        return app;
    }
}

public sealed record CreateExpenseRequest(
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string Description,
    int SourceType,
    Guid? SourceReferenceId);