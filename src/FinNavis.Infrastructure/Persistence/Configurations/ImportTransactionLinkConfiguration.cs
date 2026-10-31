using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class ImportTransactionLinkConfiguration : IEntityTypeConfiguration<ImportTransactionLink>
{
    public void Configure(EntityTypeBuilder<ImportTransactionLink> builder)
    {
        builder.ToTable("import_transaction_links");

        builder.HasKey(link => link.Id).HasName("pk_import_transaction_links");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(link => link.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(link => link.ImportRunId)
            .HasColumnName("import_run_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.TransactionId)
            .HasColumnName("transaction_id")
            .HasColumnType("uuid")
            .IsRequired();

        // SHA-256 over what the run wrote, so undo can tell whether the owner edited the
        // transaction since. The fields, in this order: account_id, category_id, amount,
        // occurred_on, note. A field added to a transaction later and left out of this list is a
        // field whose edits undo silently stops noticing — add it to MovementFingerprint too.
        builder.Property(link => link.Fingerprint)
            .HasColumnName("fingerprint")
            .HasMaxLength(MovementFingerprint.Length)
            .IsRequired();

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // The run report reads everything one run brought, and undo walks the same links to
        // delete them. Also the FK index on import_run_id.
        builder.HasIndex(link => link.ImportRunId)
            .HasDatabaseName("idx_import_transaction_links_import_run_id");

        // A transaction belongs to at most one run: two runs claiming the same row would make
        // undo ambiguous. Also the FK index the cascade below needs on every transaction delete.
        builder.HasIndex(link => link.TransactionId)
            .IsUnique()
            .HasDatabaseName("ux_import_transaction_links_transaction_id");

        // Cascade, unlike the rest of the ledger: a transaction the owner deletes by hand must
        // simply be absent at undo time. Without it the delete would be refused for every
        // imported row.
        builder.HasOne<Transaction>()
            .WithMany()
            .HasForeignKey(link => link.TransactionId)
            .HasConstraintName("fk_import_transaction_links_transaction")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
