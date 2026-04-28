using ErrorOr;
using MediatR;
using Recipes.Application.Common;
using Recipes.Domain.Primitives;
using Recipes.Domain.Repositories;

namespace Recipes.Application.CookingLog.DeleteCookingEntry;

public sealed class DeleteCookingEntryCommandHandler : IRequestHandler<DeleteCookingEntryCommand, ErrorOr<Deleted>>
{
    private readonly ICookingLogRepository _cookingLog;
    private readonly ICurrentUser _currentUser;

    public DeleteCookingEntryCommandHandler(ICookingLogRepository cookingLog, ICurrentUser currentUser)
    {
        _cookingLog = cookingLog;
        _currentUser = currentUser;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteCookingEntryCommand request, CancellationToken cancellationToken)
    {
        var entryId = CookingLogEntryId.From(request.Id);
        var entry = await _cookingLog.GetByIdAsync(entryId, cancellationToken);

        if (entry is null || entry.UserId != _currentUser.UserId)
            return Error.NotFound("CookingLogEntry.NotFound", "Cooking log entry not found.");

        _cookingLog.Remove(entry);
        await _cookingLog.SaveChangesAsync(cancellationToken);

        return Result.Deleted;
    }
}
