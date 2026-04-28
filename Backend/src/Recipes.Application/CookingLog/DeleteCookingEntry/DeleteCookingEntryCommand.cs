using ErrorOr;
using MediatR;

namespace Recipes.Application.CookingLog.DeleteCookingEntry;

public sealed record DeleteCookingEntryCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
