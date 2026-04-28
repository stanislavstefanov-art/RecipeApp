using ErrorOr;
using MediatR;
using Recipes.Application.Common;
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
    private readonly ICurrentUser _currentUser;

    public GetExpenseInsightsHandler(
        IExpenseRepository expenseRepository,
        IRequestHandler<GetMonthlyExpenseReportQuery, ErrorOr<MonthlyExpenseReportDto>> monthlyReportHandler,
        IExpenseInsightService expenseInsightService,
        ICurrentUser currentUser)
    {
        _expenseRepository = expenseRepository;
        _monthlyReportHandler = monthlyReportHandler;
        _expenseInsightService = expenseInsightService;
        _currentUser = currentUser;
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

        var householdIds = await _currentUser.GetHouseholdIdsAsync(cancellationToken);
        var expenses = await _expenseRepository.GetByMonthAndHouseholdIdsAsync(request.Year, request.Month, householdIds, cancellationToken);

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