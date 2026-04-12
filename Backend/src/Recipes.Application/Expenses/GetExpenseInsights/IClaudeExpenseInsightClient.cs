namespace Recipes.Application.Expenses.GetExpenseInsights;

public interface IClaudeExpenseInsightClient
{
    Task<ExpenseInsightDto> AnalyzeAsync(
        ExpenseInsightInputDto input,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken);
}