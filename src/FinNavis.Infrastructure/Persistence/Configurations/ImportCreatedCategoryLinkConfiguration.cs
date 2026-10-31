using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class ImportCreatedCategoryLinkConfiguration : IEntityTypeConfiguration<ImportCreatedCategoryLink>
{
    public void Configure(EntityTypeBuilder<ImportCreatedCategoryLink> builder)
    {
        builder.ToTable("import_created_category_links");

        builder.HasKey(link => link.Id).HasName("pk_import_created_category_links");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(link => link.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(link => link.ImportRunId)
            .HasColumnName("import_run_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Undo walks the categories one run created to decide which to remove. Also the FK index
        // on import_run_id.
        builder.HasIndex(link => link.ImportRunId)
            .HasDatabaseName("idx_import_created_category_links_import_run_id");

        // A category was created by at most one run. Also the FK index the cascade below needs on
        // every category delete.
        builder.HasIndex(link => link.CategoryId)
            .IsUnique()
            .HasDatabaseName("ux_import_created_category_links_category_id");

        // Cascade: a category the owner deletes by hand leaves undo nothing to remove.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(link => link.CategoryId)
            .HasConstraintName("fk_import_created_category_links_category")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
