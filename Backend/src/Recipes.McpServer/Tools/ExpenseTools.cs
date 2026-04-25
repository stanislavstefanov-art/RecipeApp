using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using Recipes.McpServer.Http;

namespace Recipes.McpServer.Tools;

[McpServerToolType]
public sealed class ExpenseTools
{
    private readonly RecipesApiClient _client;

    public ExpenseTools(RecipesApiClient client) => _client = client;

    [McpServerTool(Name = "get_monthly_expense_report"), Description("Get monthly expense report: totals, categories, top expense.")]
    public async Task<string> GetMonthlyExpenseReportAsync(
        [Description("Year (e.g. 2026).")] int year,
        [Description("Month (1–12).")] int month,
        CancellationToken ct)
    {
        var report = await _client.GetMonthlyExpenseReportAsync(year, month, ct);
        return report is null
            ? $"No expense report for {year}-{month:D2}."
            : JsonSerializer.Serialize(report);
    }
}
