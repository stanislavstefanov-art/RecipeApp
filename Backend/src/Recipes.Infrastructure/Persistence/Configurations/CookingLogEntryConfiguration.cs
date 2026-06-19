using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class CookingLogEntryConfiguration : IEntityTypeConfiguration<CookingLogEntry>
{
    public void Configure(EntityTypeBuilder<CookingLogEntry> builder)
    {
        builder.ToTable("CookingLogEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(
                id => id.Value,
                value => CookingLogEntryId.From(value))
            .ValueGeneratedNever();

        builder.Property(e => e.RecipeId)
            .HasConversion(
                id => id.Value,
                value => RecipeId.From(value))
            .IsRequired();

        builder.Property(e => e.UserId)
            .HasConversion(
                id => id.Value,
                value => UserId.From(value))
            .IsRequired();

        builder.Property(e => e.HouseholdId)
            .HasConversion(
                id => id.HasValue ? id.Value.Value : (Guid?)null,
                value => value.HasValue ? HouseholdId.From(value.Value) : null)
            .IsRequired(false);

        builder.Property(e => e.Notes).HasMaxLength(500);

        builder.HasOne<Recipe>()
            .WithMany()
            .HasForeignKey(e => e.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => new { e.RecipeId, e.UserId });

        builder.OwnsMany(e => e.PreparedBy, b =>
        {
            b.ToTable("CookingLogPreparers");
            b.WithOwner().HasForeignKey("CookingLogEntryId");
            b.Property(p => p.PersonId)
                .HasConversion(
                    id => id.Value,
                    value => PersonId.From(value))
                .HasColumnName("PersonId")
                .IsRequired();
            b.HasKey("CookingLogEntryId", "PersonId");
        });
    }
}
