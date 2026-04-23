using FluentValidation;

namespace Recipes.Application.Expenses.GetExpenseInsights;

public sealed class ExpenseInsightDtoValidator : AbstractValidator<ExpenseInsightDto>
{
    public ExpenseInsightDtoValidator()
    {
        RuleFor(x => x.Summary).NotEmpty();
        RuleFor(x => x.KeyFindings).NotNull();
        RuleFor(x => x.Recommendations).NotNull();
        RuleFor(x => x.Confidence).InclusiveBetween(0, 1);
    }
}
