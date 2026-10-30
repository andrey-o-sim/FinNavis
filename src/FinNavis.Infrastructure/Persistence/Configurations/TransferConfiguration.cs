using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(transfer => transfer.Id).HasName("pk_transfers");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
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

        // The same calendar-day rule as a transaction.
        builder.Property(transfer => transfer.OccurredOn)
            .HasColumnName("occurred_on")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(transfer => transfer.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // The balance subtracts transfers out of an account, and the account delete checks them
        // first. Also the FK index.
        builder.HasIndex(transfer => transfer.SourceAccountId)
            .HasDatabaseName("idx_transfers_source_account_id");

        // The balance adds transfers into an account, and the account delete checks them too.
        // Also the FK index.
        builder.HasIndex(transfer => transfer.DestinationAccountId)
            .HasDatabaseName("idx_transfers_destination_account_id");

        // The history page reads the transfers half by period. No composite with a category is
        // wanted: the breakdown never reads this table.
        builder.HasIndex(transfer => transfer.OccurredOn)
            .HasDatabaseName("idx_transfers_occurred_on");

        // No navigation on either side, and no cascade: the Application layer refuses the account
        // delete when any movement still names it.
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
