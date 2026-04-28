using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .ValueGeneratedNever();

        builder.Property(r => r.Name)
            .HasConversion(
                name => name.Value,
                value => new RecipeName(value))
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.HouseholdId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? HouseholdId.From(value.Value) : null)
            .IsRequired(false);

        builder.HasIndex(r => r.HouseholdId)
            .HasFilter("[HouseholdId] IS NOT NULL");

        builder.HasMany(r => r.Ingredients)
            .WithOne()
            .HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Variations)
            .WithOne()
            .HasForeignKey(v => v.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);    

        builder.HasMany(r => r.Steps)
            .WithOne()
            .HasForeignKey(s => s.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(r => r.Ratings)
            .WithOne()
            .HasForeignKey(r => r.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

