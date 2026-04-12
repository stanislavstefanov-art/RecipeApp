namespace Recipes.Application.Expenses.GetExpenseInsights;

public interface IExpenseInsightService
{
    Task<ExpenseInsightDto> AnalyzeAsync(
        ExpenseInsightInputDto input,
        CancellationToken cancellationToken);
}