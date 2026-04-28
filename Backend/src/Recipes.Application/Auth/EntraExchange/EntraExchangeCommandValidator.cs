using FluentValidation;

namespace Recipes.Application.Auth.EntraExchange;

public sealed class EntraExchangeCommandValidator : AbstractValidator<EntraExchangeCommand>
{
    public EntraExchangeCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .MaximumLength(8 * 1024);
    }
}
