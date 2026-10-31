using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class ImportRunConfiguration : IEntityTypeConfiguration<ImportRun>
{
    public void Configure(EntityTypeBuilder<ImportRun> builder)
    {
        builder.ToTable("import_runs");

        builder.HasKey(run => run.Id).HasName("pk_import_runs");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(run => run.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        // The name the file came with. Shown back as plain text, never interpreted.
        builder.Property(run => run.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(ImportRun.MaxFileNameLength)
            .IsRequired();

        // Stored as a string, never as an int. No CHECK on the allowed values —
        // they are a domain rule, held by the enum in code.
        builder.Property(run => run.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(run => run.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // A null is read together with kind: a history migration never had an account, while a
        // statement with a null had one that was deleted since.
        builder.Property(run => run.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid");

        builder.Property(run => run.RecordedRowCount)
            .HasColumnName("recorded_row_count")
            .IsRequired();

        builder.Property(run => run.AlreadyPresentRowCount)
            .HasColumnName("already_present_row_count")
            .IsRequired();

        builder.Property(run => run.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // FK index. Without it, the SET NULL below scans this table on every account delete.
        builder.HasIndex(run => run.AccountId)
            .HasDatabaseName("idx_import_runs_account_id");

        // SET NULL rather than a refusal: an old run must not block an account delete for ever.
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(run => run.AccountId)
            .HasConstraintName("fk_import_runs_account")
            .OnDelete(DeleteBehavior.SetNull);

        // Nothing deletes a run — an undone run stays to keep its report — so no child table
        // cascades from it. The link tables cascade from the ledger row they point at instead.
        builder.HasMany(run => run.TransactionLinks)
            .WithOne()
            .HasForeignKey(link => link.ImportRunId)
            .HasConstraintName("fk_import_transaction_links_import_run")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(run => run.TransferLinks)
            .WithOne()
            .HasForeignKey(link => link.ImportRunId)
            .HasConstraintName("fk_import_transfer_links_import_run")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(run => run.CreatedAccountLinks)
            .WithOne()
            .HasForeignKey(link => link.ImportRunId)
            .HasConstraintName("fk_import_created_account_links_import_run")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasMany(run => run.CreatedCategoryLinks)
            .WithOne()
            .HasForeignKey(link => link.ImportRunId)
            .HasConstraintName("fk_import_created_category_links_import_run")
            .OnDelete(DeleteBehavior.NoAction);

        // A set-aside row was never in the ledger, so an undo has nothing to take back from it.
        // It stays readable on the report of the undone run.
        builder.HasMany(run => run.SetAsideRows)
            .WithOne()
            .HasForeignKey(row => row.ImportRunId)
            .HasConstraintName("fk_import_set_aside_rows_import_run")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
