using ErrorOr;
using MediatR;
using Recipes.Application.Expenses.GetMonthlyExpenseReport;
using Recipes.Application.Expenses.ListExpenses;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.GetExpenseInsights;

public sealed class GetExpenseInsightsHandler
    : IRequestHandler<GetExpenseInsightsQuery, ErrorOr<ExpenseInsightDto>>
{
    private readonly IExpenseRepository _expenseRepository;
    private readonly IRequestHandler<GetMonthlyExpenseReportQuery, ErrorOr<MonthlyExpenseReportDto>> _monthlyReportHandler;
    private readonly IExpenseInsightService _expenseInsightService;

    public GetExpenseInsightsHandler(
        IExpenseRepository expenseRepository,
        IRequestHandler<GetMonthlyExpenseReportQuery, ErrorOr<MonthlyExpenseReportDto>> monthlyReportHandler,
        IExpenseInsightService expenseInsightService)
    {
        _expenseRepository = expenseRepository;
        _monthlyReportHandler = monthlyReportHandler;
        _expenseInsightService = expenseInsightService;
    }

    public async Task<ErrorOr<ExpenseInsightDto>> Handle(
        GetExpenseInsightsQuery request,
        CancellationToken cancellationToken)
    {
        var monthlyReportResult = await _monthlyReportHandler.Handle(
            new GetMonthlyExpenseReportQuery(request.Year, request.Month),
            cancellationToken);

        if (monthlyReportResult.IsError)
        {
            return monthlyReportResult.Errors;
        }

        var expenses = await _expenseRepository.GetByMonthAsync(request.Year, request.Month, cancellationToken);

        var expenseDtos = expenses.Select(x => new ExpenseDto(
            x.Id.Value,
            x.Amount,
            x.Currency,
            x.ExpenseDate,
            (int)x.Category,
            x.Description,
            (int)x.SourceType,
            x.SourceReferenceId)).ToList();

        var input = new ExpenseInsightInputDto(
            monthlyReportResult.Value,
            expenseDtos);

        var insight = await _expenseInsightService.AnalyzeAsync(input, cancellationToken);
        return insight;
    }
}