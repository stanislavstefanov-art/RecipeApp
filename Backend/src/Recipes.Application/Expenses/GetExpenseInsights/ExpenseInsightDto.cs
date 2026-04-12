namespace Recipes.Application.Expenses.GetExpenseInsights;

public sealed record ExpenseInsightDto(
    string Summary,
    IReadOnlyList<string> KeyFindings,
    IReadOnlyList<string> Recommendations,
    double Confidence,
    bool NeedsReview,
    string? Notes);