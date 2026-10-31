using FinNavis.Domain.Entities;
using FinNavis.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class ImportTransferLinkConfiguration : IEntityTypeConfiguration<ImportTransferLink>
{
    public void Configure(EntityTypeBuilder<ImportTransferLink> builder)
    {
        builder.ToTable("import_transfer_links");

        builder.HasKey(link => link.Id).HasName("pk_import_transfer_links");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(link => link.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(link => link.ImportRunId)
            .HasColumnName("import_run_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(link => link.TransferId)
            .HasColumnName("transfer_id")
            .HasColumnType("uuid")
            .IsRequired();

        // SHA-256 over what the run wrote, so undo can tell whether the owner edited the
        // transfer since. The fields, in this order: source_account_id, destination_account_id,
        // amount, occurred_on. A field added to a transfer later and left out of this list is a
        // field whose edits undo silently stops noticing — add it to MovementFingerprint too.
        builder.Property(link => link.Fingerprint)
            .HasColumnName("fingerprint")
            .HasMaxLength(MovementFingerprint.Length)
            .IsRequired();

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // The run report reads everything one run brought, and undo walks these links to delete
        // the transfers. Also the FK index on import_run_id.
        builder.HasIndex(link => link.ImportRunId)
            .HasDatabaseName("idx_import_transfer_links_import_run_id");

        // A transfer belongs to at most one run: two runs claiming the same row would make undo
        // ambiguous. Also the FK index the cascade below needs on every transfer delete.
        builder.HasIndex(link => link.TransferId)
            .IsUnique()
            .HasDatabaseName("ux_import_transfer_links_transfer_id");

        // Cascade, unlike the rest of the ledger: a transfer the owner deletes by hand must
        // simply be absent at undo time. Without it the delete would be refused for every
        // imported transfer.
        builder.HasOne<Transfer>()
            .WithMany()
            .HasForeignKey(link => link.TransferId)
            .HasConstraintName("fk_import_transfer_links_transfer")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
