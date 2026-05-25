namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class ReceiptItem : Entity
{
    public ReceiptItemId Id { get; private set; } = ReceiptItemId.New();
    public ExpenseId ExpenseId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal? Quantity { get; private set; }
    public decimal? UnitPrice { get; private set; }
    public decimal? TotalPrice { get; private set; }

    private ReceiptItem() { }

    public ReceiptItem(ExpenseId expenseId, string description, decimal? quantity, decimal? unitPrice, decimal? totalPrice)
    {
        ExpenseId = expenseId;
        Description = description.Trim();
        Quantity = quantity.HasValue ? Math.Round(quantity.Value, 4, MidpointRounding.AwayFromZero) : null;
        UnitPrice = unitPrice.HasValue ? Math.Round(unitPrice.Value, 4, MidpointRounding.AwayFromZero) : null;
        TotalPrice = totalPrice.HasValue ? Math.Round(totalPrice.Value, 2, MidpointRounding.AwayFromZero) : null;
    }
}
