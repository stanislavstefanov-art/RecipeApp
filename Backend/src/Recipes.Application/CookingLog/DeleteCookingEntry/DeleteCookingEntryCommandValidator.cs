using FluentValidation;

namespace Recipes.Application.CookingLog.DeleteCookingEntry;

public sealed class DeleteCookingEntryCommandValidator : AbstractValidator<DeleteCookingEntryCommand>
{
    public DeleteCookingEntryCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
