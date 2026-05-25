using MediatR;
using Recipes.Api.Extensions;
using Recipes.Application.Expenses.CreateExpense;
using Recipes.Application.Expenses.DeleteExpense;
using Recipes.Application.Expenses.ExtractReceipt;
using Recipes.Application.Expenses.GetExpenseInsights;
using Recipes.Application.Expenses.GetMonthlyExpenseReport;
using Recipes.Application.Expenses.ListExpenses;

namespace Recipes.Api.Endpoints;

public static class ExpensesEndpoints
{
    public static IEndpointRouteBuilder MapExpensesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/expenses")
            .WithTags("Expenses")
            .RequireAuthorization();

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
                    request.SourceReferenceId,
                    request.HouseholdId),
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

        group.MapDelete("/{expenseId:guid}", async (Guid expenseId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteExpenseCommand(expenseId), ct);
            return result.ToHttpResult(_ => Results.NoContent());
        });

        group.MapPost("/extract-receipt", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            const long maxBytes = 4 * 1024 * 1024;
            if (file.Length > maxBytes)
                return Results.BadRequest("File exceeds the maximum size of 4 MB (Azure Document Intelligence free tier limit).");

            var allowed = new[] { "image/jpeg", "image/png", "image/webp", "application/pdf" };
            if (!allowed.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
                return Results.BadRequest("Only JPEG, PNG, WebP, and PDF files are accepted.");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);

            var result = await sender.Send(new ExtractReceiptCommand(ms.ToArray(), file.ContentType), ct);
            return result.ToHttpResult(dto => Results.Ok(dto));
        }).DisableAntiforgery();

        return app;
    }
}

public sealed record CreateExpenseRequest(
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string? Description,
    int SourceType,
    Guid? SourceReferenceId,
    Guid HouseholdId);