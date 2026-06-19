using FluentValidation;

namespace Recipes.Application.Recipes.UpdateStep;

public sealed class UpdateStepValidator : AbstractValidator<UpdateStepCommand>
{
    public UpdateStepValidator()
    {
        RuleFor(x => x.RecipeId).NotEmpty();
        RuleFor(x => x.StepId).NotEmpty();
        RuleFor(x => x.Instruction).NotEmpty().MaximumLength(1000);
    }
}
