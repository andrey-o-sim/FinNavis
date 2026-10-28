using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Transfer"/> to <c>transfers</c> (<c>data-model.md</c>).
/// </summary>
/// <remarks>
/// <b>One row, not two.</b> AC-06 says a transfer is never applied to one side only, and a single
/// row makes a half-applied transfer unrepresentable rather than merely prevented. One amount
/// serves both sides, so AC-04 and AC-15 hold by construction.
/// <para>
/// <b>No <c>category_id</c> and no <c>note</c>.</b> A transfer carries no category (sad.md §12),
/// which is what makes AC-23 a refusal rather than a validation rule, and AC-20 structural: the
/// breakdown reads <c>transactions</c> and can never reach this table.
/// </para>
/// <para>
/// <b><c>source_account_id &lt;&gt; destination_account_id</c> is not a <c>CHECK</c>.</b> AC-05 is
/// a domain invariant, refused in the entity before the row is ever built.
/// </para>
/// </remarks>
public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(transfer => transfer.Id).HasName("pk_transfers");

        // The id is a UUID v7 minted in the Application layer (ADR-0005), never by the provider.
        builder.Property(transfer => transfer.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(transfer => transfer.SourceAccountId)
            .HasColumnName("source_account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(transfer => transfer.DestinationAccountId)
            .HasColumnName("destination_account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(transfer => transfer.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(19,4)")
            .HasConversion<MoneyConverter>()
            .IsRequired();

        // The same calendar-day rule as a transaction (data-model.md decision 1).
        builder.Property(transfer => transfer.OccurredOn)
            .HasColumnName("occurred_on")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(transfer => transfer.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Flow 7 subtracts transfers out of an account, and flow 5 checks them before deleting the
        // account (AC-14, AC-09). Also the FK index.
        builder.HasIndex(transfer => transfer.SourceAccountId)
            .HasDatabaseName("idx_transfers_source_account_id");

        // Flow 7 adds transfers into an account, and flow 5 checks them too (AC-14, AC-09).
        // Also the FK index.
        builder.HasIndex(transfer => transfer.DestinationAccountId)
            .HasDatabaseName("idx_transfers_destination_account_id");

        // Flow 8 reads the transfers half of the history page by period (AC-16). No composite with
        // a category is wanted: the breakdown never reads this table.
        builder.HasIndex(transfer => transfer.OccurredOn)
            .HasDatabaseName("idx_transfers_occurred_on");

        // No navigation on either side, and no cascade: AC-09 refuses the account delete in the
        // Application layer when any movement still names it.
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transfer => transfer.SourceAccountId)
            .HasConstraintName("fk_transfers_source_account")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transfer => transfer.DestinationAccountId)
            .HasConstraintName("fk_transfers_destination_account")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
