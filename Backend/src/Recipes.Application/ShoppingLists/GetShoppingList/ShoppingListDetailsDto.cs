namespace Recipes.Application.ShoppingLists.GetShoppingList;

public sealed record ShoppingListDetailsDto(
    Guid Id,
    string Name,
    IReadOnlyList<ShoppingListDetailsItemDto> Items);

public sealed record ShoppingListDetailsItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    string Unit,
    bool IsPurchased,
    string? Notes,
    int SourceType,
    Guid? SourceReferenceId);