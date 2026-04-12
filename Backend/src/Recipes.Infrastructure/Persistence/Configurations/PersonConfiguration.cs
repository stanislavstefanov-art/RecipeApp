using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Recipes.Domain.Entities;
using Recipes.Domain.Enums;
using Recipes.Domain.Primitives;

namespace Recipes.Infrastructure.Persistence.Configurations;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(
                id => id.Value,
                value => PersonId.From(value))
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Notes)
            .HasMaxLength(1000);

        builder.Property<List<DietaryPreference>>("_dietaryPreferences")
            .HasColumnName("DietaryPreferences")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<DietaryPreference>>(v, (JsonSerializerOptions?)null) ?? new List<DietaryPreference>())
            .IsRequired();

        builder.Property<List<HealthConcern>>("_healthConcerns")
            .HasColumnName("HealthConcerns")
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<HealthConcern>>(v, (JsonSerializerOptions?)null) ?? new List<HealthConcern>())
            .IsRequired();
    }
}