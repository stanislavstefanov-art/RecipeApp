using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class RecipeVariationConfiguration : IEntityTypeConfiguration<RecipeVariation>
{
    public void Configure(EntityTypeBuilder<RecipeVariation> builder)
    {
        builder.ToTable("RecipeVariations");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => RecipeVariationId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.RecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property(x => x.IngredientAdjustmentNotes)
            .HasMaxLength(2000);

        builder.HasIndex(x => new { x.RecipeId, x.Name })
            .IsUnique();

        builder.HasMany(x => x.IngredientOverrides)
            .WithOne()
            .HasForeignKey(x => x.RecipeVariationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}