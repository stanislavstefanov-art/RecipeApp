using ErrorOr;
using MediatR;

namespace Recipes.Application.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName) : IRequest<ErrorOr<AuthResultDto>>;
