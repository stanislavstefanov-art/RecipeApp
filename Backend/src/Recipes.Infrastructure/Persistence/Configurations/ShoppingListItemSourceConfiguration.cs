using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class ShoppingListItemSourceConfiguration : IEntityTypeConfiguration<ShoppingListItemSource>
{
    public void Configure(EntityTypeBuilder<ShoppingListItemSource> builder)
    {
        builder.ToTable("ShoppingListItemSources");

        builder.HasKey(x => new { x.ShoppingListItemId, x.RecipeId });

        builder.Property(x => x.ShoppingListItemId)
            .HasConversion(
                id => id.Value,
                value => ShoppingListItemId.From(value));

        builder.Property(x => x.RecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value));

        builder.Property(x => x.RecipeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Portions)
            .HasColumnType("decimal(18,2)")
            .HasDefaultValue(1m);
    }
}
