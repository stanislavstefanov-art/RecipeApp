using ErrorOr;
using MediatR;

namespace Recipes.Application.Expenses.CreateExpense;

public sealed record CreateExpenseCommand(
    decimal Amount,
    string Currency,
    DateOnly ExpenseDate,
    int Category,
    string? Description,
    int SourceType,
    Guid? SourceReferenceId,
    Guid HouseholdId,
    IReadOnlyList<CreateExpenseItemDto>? Items = null) : IRequest<ErrorOr<CreateExpenseResponse>>;

public sealed record CreateExpenseItemDto(
    string Description,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? TotalPrice);
