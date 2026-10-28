using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Category"/> to <c>categories</c> (<c>data-model.md</c>).
/// </summary>
/// <remarks>
/// <b>The group link has no cascade of any kind.</b> AC-12 blocks deleting a category that history
/// names, and AC-29 clears this column from the Application layer in one transaction. With
/// <c>NO ACTION</c>, forgetting that <c>UPDATE</c> fails loudly on the foreign key instead of
/// quietly rewriting rows (<c>data-model.md</c> decision 8).
/// </remarks>
public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("categories");

        builder.HasKey(category => category.Id).HasName("pk_categories");

        // The id is a UUID v7 minted in the Application layer (ADR-0005), never by the provider.
        builder.Property(category => category.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(Category.MaxNameLength)
            .IsRequired();

        // Stored as a string, never as an int (CLAUDE.md). No CHECK on the allowed values —
        // they are a domain rule, held by the enum in code.
        builder.Property(category => category.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        // Nullable: a group is optional (sad.md §12). A category with no group is not in a group
        // called Default — the breakdown produces that bucket and never stores it.
        builder.Property(category => category.CategoryGroupId)
            .HasColumnName("category_group_id")
            .HasColumnType("uuid");

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // AC-36: unique per side, so "Gifts" can exist once as spending and once as earning.
        builder.HasIndex(category => new { category.Name, category.Kind })
            .IsUnique()
            .HasDatabaseName("ux_categories_name_kind");

        // FK index, required by .claude/rules/migrations.md. Queries it serves: flow 3 joins
        // categories to their group for the breakdown (AC-18, AC-19); AC-33 lists categories
        // narrowed to one group; AC-29 finds the categories to clear before the group row goes.
        builder.HasIndex(category => category.CategoryGroupId)
            .HasDatabaseName("idx_categories_category_group_id");

        // No navigation on either side: the breakdown resolves the mapping at query time
        // (ADR-0002), so nothing needs the group loaded with the category.
        builder.HasOne<CategoryGroup>()
            .WithMany()
            .HasForeignKey(category => category.CategoryGroupId)
            .HasConstraintName("fk_categories_category_group")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
