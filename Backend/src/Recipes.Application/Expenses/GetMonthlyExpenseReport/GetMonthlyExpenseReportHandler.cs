using ErrorOr;
using MediatR;
using Recipes.Domain.Enums;
using Recipes.Domain.Repositories;

namespace Recipes.Application.Expenses.GetMonthlyExpenseReport;

public sealed class GetMonthlyExpenseReportHandler
    : IRequestHandler<GetMonthlyExpenseReportQuery, ErrorOr<MonthlyExpenseReportDto>>
{
    private readonly IExpenseRepository _expenseRepository;

    public GetMonthlyExpenseReportHandler(IExpenseRepository expenseRepository)
    {
        _expenseRepository = expenseRepository;
    }

    public async Task<ErrorOr<MonthlyExpenseReportDto>> Handle(
        GetMonthlyExpenseReportQuery request,
        CancellationToken cancellationToken)
    {
        var expenses = await _expenseRepository.GetByMonthAsync(request.Year, request.Month, cancellationToken);

        if (expenses.Count == 0)
        {
            return new MonthlyExpenseReportDto(
                request.Year,
                request.Month,
                0,
                "N/A",
                0,
                0,
                null,
                0,
                null,
                []);
        }

        var total = expenses.Sum(x => x.Amount);
        var currency = expenses.First().Currency;
        var expenseCount = expenses.Count;
        var average = decimal.Round(total / expenseCount, 2, MidpointRounding.AwayFromZero);

        var grouped = expenses
            .GroupBy(x => x.Category)
            .Select(g =>
            {
                var amount = g.Sum(x => x.Amount);
                var percentage = total == 0 ? 0 : Math.Round((amount / total) * 100, 2);

                return new MonthlyExpenseCategoryBreakdownDto(
                    g.Key.ToString(),
                    amount,
                    percentage);
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var topCategory = grouped.FirstOrDefault()?.Category;

        var foodAmount = expenses
            .Where(x => x.Category == ExpenseCategory.Food)
            .Sum(x => x.Amount);

        var foodPercentage = total == 0
            ? 0
            : Math.Round((foodAmount / total) * 100, 2);

        var largestExpense = expenses
            .OrderByDescending(x => x.Amount)
            .First();

        return new MonthlyExpenseReportDto(
            request.Year,
            request.Month,
            total,
            currency,
            expenseCount,
            average,
            topCategory,
            foodPercentage,
            new MonthlyExpenseLargestItemDto(
                largestExpense.Amount,
                largestExpense.Description,
                largestExpense.ExpenseDate,
                largestExpense.Category.ToString()),
            grouped);
    }
}