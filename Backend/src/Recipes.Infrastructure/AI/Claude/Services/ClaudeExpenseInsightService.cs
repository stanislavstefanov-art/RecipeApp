using FluentValidation;
using Microsoft.Extensions.Logging;
using Recipes.Application.Common.AI;
using Recipes.Application.Expenses.GetExpenseInsights;

namespace Recipes.Infrastructure.AI.Claude.Services;

public sealed class ClaudeExpenseInsightService : IExpenseInsightService
{
    private readonly IClaudeExpenseInsightClient _client;
    private readonly IClaudeAssetProvider _assetProvider;
    private readonly IValidator<ExpenseInsightDto> _validator;
    private readonly ILogger<ClaudeExpenseInsightService> _logger;

    public ClaudeExpenseInsightService(
        IClaudeExpenseInsightClient client,
        IClaudeAssetProvider assetProvider,
        IValidator<ExpenseInsightDto> validator,
        ILogger<ClaudeExpenseInsightService> logger)
    {
        _client = client;
        _assetProvider = assetProvider;
        _validator = validator;
        _logger = logger;
    }

    public async Task<ExpenseInsightDto> AnalyzeAsync(
        ExpenseInsightInputDto input,
        CancellationToken cancellationToken)
    {
        var prompt = await _assetProvider.GetExpenseInsightPromptAsync(cancellationToken);
        var schema = await _assetProvider.GetExpenseInsightSchemaAsync(cancellationToken);

        var first = await _client.AnalyzeAsync(input, prompt, schema, cancellationToken);
        var firstValidation = await _validator.ValidateAsync(first, cancellationToken);

        if (firstValidation.IsValid)
        {
            return first;
        }

        _logger.LogWarning("Expense insight validation failed on first attempt. Retrying.");

        var second = await _client.AnalyzeAsync(input, prompt, schema, cancellationToken);
        var secondValidation = await _validator.ValidateAsync(second, cancellationToken);

        if (secondValidation.IsValid)
        {
            return second;
        }

        return second with
        {
            NeedsReview = true,
            Confidence = Math.Min(second.Confidence, 0.5),
            Notes = AppendValidationNotes(second.Notes, secondValidation.Errors.Select(x => x.ErrorMessage))
        };
    }

    private static string AppendValidationNotes(string? existingNotes, IEnumerable<string> errors)
    {
        var validationText = $"Validation issues: {string.Join("; ", errors)}";

        if (string.IsNullOrWhiteSpace(existingNotes))
        {
            return validationText;
        }

        return $"{existingNotes} {validationText}";
    }
}