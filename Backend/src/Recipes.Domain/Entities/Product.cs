namespace Recipes.Domain.Entities;

using Recipes.Domain.Primitives;

public sealed class Product : Entity
{
    public ProductId Id { get; private set; } = ProductId.New();
    public string Name { get; private set; } = string.Empty;

    private Product() { }

    public Product(string name)
    {
        Rename(name);
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Product name cannot be empty.", nameof(name));
        }

        Name = name.Trim();
    }
}