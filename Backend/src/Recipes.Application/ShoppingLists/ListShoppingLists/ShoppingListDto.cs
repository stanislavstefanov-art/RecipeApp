namespace Recipes.Application.ShoppingLists.ListShoppingLists;

public sealed record ShoppingListDto(
    Guid Id,
    string Name,
    IReadOnlyList<ShoppingListItemDto> Items);

public sealed record ShoppingListItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    string Unit,
    bool IsPurchased,
    string? Notes,
    int SourceType,
    Guid? SourceReferenceId);