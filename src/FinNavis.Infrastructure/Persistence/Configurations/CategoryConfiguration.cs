using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id).HasName("pk_categories");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(category => category.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(Category.MaxNameLength)
            .IsRequired();

        // Stored as a string, never as an int. No CHECK on the allowed values —
        // they are a domain rule, held by the enum in code.
        builder.Property(category => category.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // Nullable: a group is optional. A category with no group is not in a group
        // called Default — the breakdown produces that bucket and never stores it.
        builder.Property(category => category.CategoryGroupId)
            .HasColumnName("category_group_id")
            .HasColumnType("uuid");

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Unique per side, so "Gifts" can exist once as spending and once as earning.
        builder.HasIndex(category => new { category.Name, category.Kind })
            .IsUnique()
            .HasDatabaseName("ux_categories_name_kind");

        // FK index. The queries it serves: the breakdown joins categories to their group; the
        // category list is narrowed to one group; deleting a group finds the categories to clear
        // first.
        builder.HasIndex(category => category.CategoryGroupId)
            .HasDatabaseName("idx_categories_category_group_id");

        // No navigation on either side: the breakdown resolves the mapping at query time, so
        // nothing needs the group loaded with the category.
        builder.HasOne<CategoryGroup>()
            .WithMany()
            .HasForeignKey(category => category.CategoryGroupId)
            .HasConstraintName("fk_categories_category_group")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
