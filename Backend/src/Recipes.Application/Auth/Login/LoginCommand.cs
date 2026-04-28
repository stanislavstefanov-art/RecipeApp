using ErrorOr;
using MediatR;

namespace Recipes.Application.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<ErrorOr<AuthResultDto>>;
