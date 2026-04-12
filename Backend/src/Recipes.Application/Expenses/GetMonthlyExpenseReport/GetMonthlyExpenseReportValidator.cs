using FluentValidation;

namespace Recipes.Application.Expenses.GetMonthlyExpenseReport;

public sealed class GetMonthlyExpenseReportValidator : AbstractValidator<GetMonthlyExpenseReportQuery>
{
    public GetMonthlyExpenseReportValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 3000);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}