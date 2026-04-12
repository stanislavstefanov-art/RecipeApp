using Microsoft.Extensions.Logging;
using Recipes.Application.Expenses.GetExpenseInsights;

namespace Recipes.Infrastructure.AI.Claude.Clients;

public sealed class ClaudeExpenseInsightClient : IClaudeExpenseInsightClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeExpenseInsightClient> _logger;

    public ClaudeExpenseInsightClient(
        HttpClient httpClient,
        ILogger<ClaudeExpenseInsightClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<ExpenseInsightDto> AnalyzeAsync(
        ExpenseInsightInputDto input,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Claude expense insight client is not implemented yet.");
        throw new NotImplementedException("Implement Claude expense insight HTTP integration next.");
    }
}