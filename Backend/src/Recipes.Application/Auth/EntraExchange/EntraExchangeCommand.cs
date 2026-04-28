using ErrorOr;
using MediatR;
using Recipes.Application.Auth;

namespace Recipes.Application.Auth.EntraExchange;

public sealed record EntraExchangeCommand(string IdToken) : IRequest<ErrorOr<AuthResultDto>>;
