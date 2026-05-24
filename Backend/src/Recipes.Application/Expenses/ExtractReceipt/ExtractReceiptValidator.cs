using FluentValidation;

namespace Recipes.Application.Expenses.ExtractReceipt;

public sealed class ExtractReceiptValidator : AbstractValidator<ExtractReceiptCommand>
{
    public ExtractReceiptValidator()
    {
        RuleFor(x => x.ImageBytes).NotNull().NotEmpty();
        RuleFor(x => x.ContentType).NotEmpty();
    }
}
