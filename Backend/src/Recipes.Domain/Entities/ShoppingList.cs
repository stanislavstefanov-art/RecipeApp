namespace Recipes.Domain.Entities;

using Recipes.Domain.Enums;
using Recipes.Domain.Events;
using Recipes.Domain.Primitives;

public sealed class ShoppingList : Entity
{
    private readonly List<ShoppingListItem> _items = new();

    public ShoppingListId Id { get; private set; } = ShoppingListId.New();
    public string Name { get; private set; } = string.Empty;
    public IReadOnlyCollection<ShoppingListItem> Items => _items.AsReadOnly();

    private ShoppingList() { }

    public ShoppingList(string name)
    {
        Rename(name);
        RaiseDomainEvent(new ShoppingListCreated(Id, Name));
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Shopping list name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }

    public void AddItem(
        Product product,
        decimal quantity,
        string unit,
        string? notes = null,
        ShoppingListItemSourceType sourceType = ShoppingListItemSourceType.Manual,
        Guid? sourceReferenceId = null)
    {
        ArgumentNullException.ThrowIfNull(product);

        var existing = _items.FirstOrDefault(x =>
            x.ProductId == product.Id &&
            string.Equals(x.Unit, unit, StringComparison.OrdinalIgnoreCase) &&
            x.MatchesSource(sourceType, sourceReferenceId));

        if (existing is not null)
        {
            existing.IncreaseQuantity(quantity);
            existing.MergeNotes(notes);

            RaiseDomainEvent(new ShoppingListItemQuantityIncreased(
                Id,
                existing.Id,
                existing.ProductId,
                existing.ProductName,
                existing.Quantity,
                existing.Unit));

            return;
        }

        var item = new ShoppingListItem(
            Id,
            product.Id,
            product.Name,
            quantity,
            unit,
            notes,
            sourceType,
            sourceReferenceId);

        _items.Add(item);

        RaiseDomainEvent(new ShoppingListItemAdded(
            Id,
            item.Id,
            item.ProductId,
            item.ProductName,
            item.Quantity,
            item.Unit));
    }

    public void RemoveGeneratedItems(ShoppingListItemSourceType sourceType, Guid sourceReferenceId)
    {
        var toRemove = _items
            .Where(x => x.SourceType == sourceType && x.SourceReferenceId == sourceReferenceId)
            .ToList();

        foreach (var item in toRemove)
        {
            _items.Remove(item);

            RaiseDomainEvent(new ShoppingListItemRemoved(
                Id,
                item.Id,
                item.ProductId,
                item.ProductName));
        }
    }

    public void MarkItemPurchased(ShoppingListItemId itemId)
    {
        var item = _items.SingleOrDefault(x => x.Id == itemId)
                   ?? throw new InvalidOperationException($"Shopping list item '{itemId}' was not found.");

        if (item.IsPurchased)
        {
            return;
        }

        item.MarkPurchased();

        RaiseDomainEvent(new ShoppingListItemPurchased(
            Id,
            item.Id,
            item.ProductId,
            item.ProductName));
    }

    public void MarkItemPending(ShoppingListItemId itemId)
    {
        var item = _items.SingleOrDefault(x => x.Id == itemId)
                   ?? throw new InvalidOperationException($"Shopping list item '{itemId}' was not found.");

        if (!item.IsPurchased)
        {
            return;
        }

        item.MarkPending();
    }

    public void RemoveItem(ShoppingListItemId itemId)
    {
        var item = _items.SingleOrDefault(x => x.Id == itemId)
                   ?? throw new InvalidOperationException($"Shopping list item '{itemId}' was not found.");

        _items.Remove(item);

        RaiseDomainEvent(new ShoppingListItemRemoved(
            Id,
            item.Id,
            item.ProductId,
            item.ProductName));
    }
}