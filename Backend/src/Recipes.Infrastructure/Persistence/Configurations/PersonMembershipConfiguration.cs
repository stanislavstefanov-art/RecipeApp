using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class PersonMembershipConfiguration : IEntityTypeConfiguration<PersonMembership>
{
    public void Configure(EntityTypeBuilder<PersonMembership> builder)
    {
        builder.ToTable("PersonMemberships");

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

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(x => x.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
