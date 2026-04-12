using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
{
    public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
    {
        builder.ToTable("ShoppingListItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => ShoppingListItemId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.ShoppingListId)
            .HasConversion(
                id => id.Value,
                value => ShoppingListId.From(value))
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasConversion(
                id => id.Value,
                value => ProductId.From(value))
            .IsRequired();

        builder.Property(x => x.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 3);

        builder.HasIndex(x => x.ShoppingListId);
        builder.HasIndex(x => x.ProductId);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.SourceType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.SourceReferenceId);

        builder.HasIndex(x => new { x.SourceType, x.SourceReferenceId });
    }
}