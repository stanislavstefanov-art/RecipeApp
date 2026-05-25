using FluentValidation;
using Recipes.Domain.Enums;

namespace Recipes.Application.Expenses.CreateExpense;

public sealed class CreateExpenseValidator : AbstractValidator<CreateExpenseCommand>
{
    public CreateExpenseValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().MaximumLength(10);
        RuleFor(x => x.Description).MaximumLength(500).When(x => !string.IsNullOrEmpty(x.Description));
        RuleFor(x => x.Category).Must(x => Enum.IsDefined(typeof(ExpenseCategory), x));
        RuleFor(x => x.SourceType).Must(x => Enum.IsDefined(typeof(ExpenseSourceType), x));
        RuleFor(x => x.HouseholdId).NotEmpty();
    }
}