using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Recipes.Application.Expenses.GetExpenseInsights;
using Recipes.Infrastructure.AI.Claude.Models;
using Recipes.Infrastructure.Options;

namespace Recipes.Infrastructure.AI.Claude.Clients;

public sealed class ClaudeExpenseInsightClient : IClaudeExpenseInsightClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<ClaudeExpenseInsightClient> _logger;
    private readonly ClaudeOptions _options;

    public ClaudeExpenseInsightClient(
        HttpClient httpClient,
        ILogger<ClaudeExpenseInsightClient> logger,
        IOptions<ClaudeOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<ExpenseInsightDto> AnalyzeAsync(
        ExpenseInsightInputDto input,
        string promptTemplate,
        string jsonSchema,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException("Claude API key is missing.");
        }

        var systemPrompt = BuildSystemPrompt(promptTemplate, jsonSchema);
        var userPrompt = BuildUserPrompt(input);

        var payload = new ClaudeMessagesRequest(
            Model: _options.Model,
            MaxTokens: _options.MaxTokens,
            System: systemPrompt,
            Messages:
            [
                new ClaudeMessage(
                    Role: "user",
                    Content:
                    [
                        new ClaudeContentBlock(
                            Type: "text",
                            Text: userPrompt)
                    ])
            ]);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        httpRequest.Headers.Add("x-api-key", _options.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        httpRequest.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation(
            "Calling Claude for expense insights. Year: {Year}, Month: {Month}, Model: {Model}",
            input.MonthlyReport.Year,
            input.MonthlyReport.Month,
            _options.Model);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Claude expense insight call failed. StatusCode: {StatusCode}, Body: {Body}",
                (int)response.StatusCode,
                responseBody);

            throw new InvalidOperationException(
                $"Claude expense insight failed with status {(int)response.StatusCode}: {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<ClaudeMessagesResponse>(responseBody, JsonOptions)
                     ?? throw new InvalidOperationException("Claude response could not be deserialized.");

        var cleanedJson = ClaudeResponseParser.StripMarkdownFences(ClaudeResponseParser.ExtractText(parsed));

        if (string.IsNullOrWhiteSpace(cleanedJson))
        {
            throw new InvalidOperationException("Claude response did not contain text content.");
        }

        try
        {
            var result = JsonSerializer.Deserialize<ExpenseInsightDto>(cleanedJson, JsonOptions);

            if (result is null)
            {
                throw new InvalidOperationException("Claude returned empty JSON content.");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse Claude expense insight JSON. Raw text: {Text}", cleanedJson);
            throw;
        }
    }

    private static string BuildSystemPrompt(string promptTemplate, string jsonSchema) =>
        $"""
{promptTemplate}

Return only valid JSON.
Do not wrap the JSON in markdown fences.
The JSON must match this schema exactly:

{jsonSchema}
""";

    private static string BuildUserPrompt(ExpenseInsightInputDto input)
    {
        var r = input.MonthlyReport;
        var sb = new StringBuilder();

        sb.AppendLine($"Monthly report: {r.Year}-{r.Month:D2}");
        sb.AppendLine($"Total: {r.TotalAmount} {r.Currency}");
        sb.AppendLine($"Expenses: {r.ExpenseCount}, Average: {r.AverageExpenseAmount} {r.Currency}");

        if (r.ByCategory.Count > 0)
        {
            sb.AppendLine("Category breakdown:");
            foreach (var cat in r.ByCategory)
            {
                sb.AppendLine($"  - Category {cat.Category}: {cat.TotalAmount} {r.Currency} ({cat.Count} expenses, {cat.Percentage}%)");
            }
        }

        if (r.LargestExpense is { } largest)
        {
            sb.AppendLine($"Largest expense: {largest.Amount} {r.Currency} on {largest.ExpenseDate} – {largest.Description} (category {largest.Category})");
        }

        if (input.Expenses.Count > 0)
        {
            sb.AppendLine("Individual expenses (most recent first):");
            foreach (var e in input.Expenses.OrderByDescending(x => x.ExpenseDate).Take(20))
            {
                var desc = string.IsNullOrWhiteSpace(e.Description) ? "(no description)" : e.Description;
                sb.AppendLine($"  - {e.ExpenseDate}: {e.Amount} {e.Currency}, category {e.Category}, {desc}");
            }
        }

        return sb.ToString();
    }
}
