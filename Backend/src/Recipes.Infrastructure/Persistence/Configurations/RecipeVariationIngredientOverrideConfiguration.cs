using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class RecipeVariationIngredientOverrideConfiguration : IEntityTypeConfiguration<RecipeVariationIngredientOverride>
{
    public void Configure(EntityTypeBuilder<RecipeVariationIngredientOverride> builder)
    {
        builder.ToTable("RecipeVariationIngredientOverrides");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipeVariationId)
            .HasConversion(
                id => id.Value,
                value => RecipeVariationId.From(value))
            .IsRequired();

        builder.Property(x => x.IngredientName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Quantity)
            .HasPrecision(18, 2);

        builder.Property(x => x.Unit)
            .HasMaxLength(50);

        builder.Property(x => x.IsRemoved)
            .IsRequired();

        builder.HasIndex(x => x.RecipeVariationId);
    }
}