using FluentValidation;

namespace Recipes.Application.Units.DeleteUnit;

public sealed class DeleteUnitValidator : AbstractValidator<DeleteUnitCommand>
{
    public DeleteUnitValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}
