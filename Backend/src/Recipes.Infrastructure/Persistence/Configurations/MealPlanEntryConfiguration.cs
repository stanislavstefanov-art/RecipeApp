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

        builder.Property(x => x.BaseRecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .IsRequired();

        builder.Property(x => x.PlannedDate)
            .IsRequired();

        builder.Property(x => x.MealType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.Scope)
            .HasConversion<int>()
            .IsRequired();

        builder.HasMany(x => x.PersonAssignments)
            .WithOne()
            .HasForeignKey(x => x.MealPlanEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.MealPlanId);
        builder.HasIndex(x => x.BaseRecipeId);
    }
}