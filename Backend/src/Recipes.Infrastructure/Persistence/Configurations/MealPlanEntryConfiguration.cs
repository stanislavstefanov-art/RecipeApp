using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class MealPlanEntryConfiguration : IEntityTypeConfiguration<MealPlanEntry>
{
    public void Configure(EntityTypeBuilder<MealPlanEntry> builder)
    {
        builder.ToTable("MealPlanEntries");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => MealPlanEntryId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.MealPlanId)
            .HasConversion(
                id => id.Value,
                value => MealPlanId.From(value))
            .IsRequired();

        builder.Property(x => x.RecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .IsRequired();

        builder.Property(x => x.PlannedDate)
            .IsRequired();

        builder.Property(x => x.MealType)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(x => x.MealPlanId);
        builder.HasIndex(x => x.RecipeId);
    }
}