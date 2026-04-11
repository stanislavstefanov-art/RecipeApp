namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class ShoppingListItem
{
    public ShoppingListItemId Id { get; private set; } = ShoppingListItemId.New();
    public ShoppingListId ShoppingListId { get; private set; }
    public ProductId ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = string.Empty;
    public bool IsPurchased { get; private set; }

    private ShoppingListItem() { }

    internal ShoppingListItem(
        ShoppingListId shoppingListId,
        ProductId productId,
        string productName,
        decimal quantity,
        string unit)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(productName));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            throw new ArgumentException("Unit cannot be empty.", nameof(unit));
        }

        ShoppingListId = shoppingListId;
        ProductId = productId;
        ProductName = productName.Trim();
        Quantity = quantity;
        Unit = unit.Trim();
        IsPurchased = false;
    }

    internal void IncreaseQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");
        }

        Quantity += quantity;
    }

    internal void MarkPurchased()
    {
        IsPurchased = true;
    }

    internal void MarkPending()
    {
        IsPurchased = false;
    }
}