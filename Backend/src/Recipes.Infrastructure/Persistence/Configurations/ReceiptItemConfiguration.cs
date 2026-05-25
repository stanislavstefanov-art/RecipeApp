using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class ReceiptItemConfiguration : IEntityTypeConfiguration<ReceiptItem>
{
    public void Configure(EntityTypeBuilder<ReceiptItem> builder)
    {
        builder.ToTable("ReceiptItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => ReceiptItemId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.ExpenseId)
            .HasConversion(
                id => id.Value,
                value => ExpenseId.From(value))
            .IsRequired();

        builder.HasIndex(x => x.ExpenseId);

        builder.Property(x => x.Description)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.UnitPrice)
            .HasPrecision(18, 4)
            .IsRequired(false);

        builder.Property(x => x.TotalPrice)
            .HasPrecision(18, 2)
            .IsRequired(false);
    }
}
