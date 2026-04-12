using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class MealPlanPersonAssignmentConfiguration : IEntityTypeConfiguration<MealPlanPersonAssignment>
{
    public void Configure(EntityTypeBuilder<MealPlanPersonAssignment> builder)
    {
        builder.ToTable("MealPlanPersonAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => MealPlanPersonAssignmentId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.MealPlanEntryId)
            .HasConversion(
                id => id.Value,
                value => MealPlanEntryId.From(value))
            .IsRequired();

        builder.Property(x => x.PersonId)
            .HasConversion(
                id => id.Value,
                value => PersonId.From(value))
            .IsRequired();

        builder.Property(x => x.AssignedRecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .IsRequired();

        builder.Property(x => x.RecipeVariationId)
        .HasConversion(
        id => id.HasValue ? id.Value.Value : (Guid?)null,
        value => value.HasValue ? RecipeVariationId.From(value.Value) : null);

        builder.Property(x => x.PortionMultiplier)
            .HasPrecision(8, 2)
            .IsRequired();

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(x => x.MealPlanEntryId);
        builder.HasIndex(x => x.PersonId);
    }
}