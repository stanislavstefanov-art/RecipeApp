using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class PantryItemConfiguration : IEntityTypeConfiguration<PantryItem>
{
    public void Configure(EntityTypeBuilder<PantryItem> builder)
    {
        builder.ToTable("PantryItems");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => PantryItemId.From(v))
            .ValueGeneratedNever();

        builder.Property(x => x.UserId)
            .HasConversion(id => id.Value, v => UserId.From(v))
            .IsRequired();

        builder.Property(x => x.IngredientName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.UserId);
    }
}
