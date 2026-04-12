using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

namespace Recipes.Domain.Events;

public sealed record ExpenseCreated(
    ExpenseId ExpenseId,
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    ExpenseCategory Category,
    ExpenseSourceType SourceType,
    Guid? SourceReferenceId) : IDomainEvent;