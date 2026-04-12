using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class HouseholdMemberConfiguration : IEntityTypeConfiguration<HouseholdMember>
{
    public void Configure(EntityTypeBuilder<HouseholdMember> builder)
    {
        builder.ToTable("HouseholdMembers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => HouseholdMemberId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.HouseholdId)
            .HasConversion(
                id => id.Value,
                value => HouseholdId.From(value))
            .IsRequired();

        builder.Property(x => x.PersonId)
            .HasConversion(
                id => id.Value,
                value => PersonId.From(value))
            .IsRequired();

        builder.HasIndex(x => new { x.HouseholdId, x.PersonId })
            .IsUnique();
    }
}