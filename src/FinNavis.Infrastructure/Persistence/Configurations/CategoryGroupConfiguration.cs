using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
{
    public void Configure(EntityTypeBuilder<CategoryGroup> builder)
    {
        builder.ToTable("category_groups");

        builder.HasKey(group => group.Id).HasName("pk_category_groups");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(group => group.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(group => group.Name)
            .HasColumnName("name")
            .HasMaxLength(CategoryGroup.MaxNameLength)
            .IsRequired();

        // Stored as a string, never as an int. No CHECK on the allowed values —
        // they are a domain rule, held by the enum in code.
        builder.Property(group => group.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(group => group.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Unique per side, not globally. A spending "Gifts" and an earning "Gifts" are two
        // separate groups.
        builder.HasIndex(group => new { group.Name, group.Kind })
            .IsUnique()
            .HasDatabaseName("ux_category_groups_name_kind");
    }
}
