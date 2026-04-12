using FluentValidation;

namespace Recipes.Application.Expenses.GetExpenseInsights;

public sealed class GetExpenseInsightsValidator : AbstractValidator<GetExpenseInsightsQuery>
{
    public GetExpenseInsightsValidator()
    {
        RuleFor(x => x.Year).InclusiveBetween(2000, 3000);
        RuleFor(x => x.Month).InclusiveBetween(1, 12);
    }
}