using Recipes.Application.Expenses.GetExpenseInsights;

namespace Recipes.Infrastructure.AI.Claude.Services.Stubs;

public sealed class StubExpenseInsightService : IExpenseInsightService
{
    public Task<ExpenseInsightDto> AnalyzeAsync(
        ExpenseInsightInputDto input,
        CancellationToken cancellationToken)
    {
        var findings = new List<string>
        {
            $"Total spending for the month is {input.MonthlyReport.TotalAmount} {input.MonthlyReport.Currency}.",
            $"Top category is {input.MonthlyReport.TopCategory ?? "N/A"}.",
            $"Food accounts for {input.MonthlyReport.FoodPercentage}% of total spending."
        };

        var recommendations = new List<string>
        {
            "Review the largest expense and check whether it was one-off or recurring.",
            "Track food expenses linked to shopping lists to understand meal-related spending better."
        };

        var dto = new ExpenseInsightDto(
            "Stub monthly spending analysis.",
            findings,
            recommendations,
            0.55,
            true,
            "Stub insight result. Replace with Claude-backed insights later.");

        return Task.FromResult(dto);
    }
}