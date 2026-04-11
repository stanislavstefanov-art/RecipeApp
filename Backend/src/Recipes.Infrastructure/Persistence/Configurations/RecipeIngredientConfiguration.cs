using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasConversion(
                id => id.Value,
                value => RecipeIngredientId.From(value))
            .ValueGeneratedNever();

        builder.Property(i => i.RecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .IsRequired();

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Unit)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(i => i.Quantity)
            .HasPrecision(18, 3);

        builder.HasIndex(i => i.RecipeId);
    }
}