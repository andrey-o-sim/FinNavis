using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class ImportCreatedAccountLinkConfiguration : IEntityTypeConfiguration<ImportCreatedAccountLink>
{
    public void Configure(EntityTypeBuilder<ImportCreatedAccountLink> builder)
    {
        builder.ToTable("import_created_account_links");

        builder.HasKey(link => link.Id).HasName("pk_import_created_account_links");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(link => link.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(link => link.ImportRunId)
            .HasColumnName("import_run_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Undo walks the accounts one run created to decide which to remove. Also the FK index on
        // import_run_id.
        builder.HasIndex(link => link.ImportRunId)
            .HasDatabaseName("idx_import_created_account_links_import_run_id");

        // An account was created by at most one run. Also the FK index the cascade below needs on
        // every account delete.
        builder.HasIndex(link => link.AccountId)
            .IsUnique()
            .HasDatabaseName("ux_import_created_account_links_account_id");

        // Cascade: an account the owner deletes by hand leaves undo nothing to remove. Without it
        // the delete would be refused for a reason the owner cannot see.
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(link => link.AccountId)
            .HasConstraintName("fk_import_created_account_links_account")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
