using FinNavis.Domain.Entities;
using FinNavis.Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("transactions");

        builder.HasKey(transaction => transaction.Id).HasName("pk_transactions");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        // Being time-ordered, it doubles as the history tiebreaker on (occurred_on, id).
        builder.Property(transaction => transaction.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(transaction => transaction.AccountId)
            .HasColumnName("account_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(transaction => transaction.CategoryId)
            .HasColumnName("category_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(transaction => transaction.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(19,4)")
            .HasConversion<MoneyConverter>()
            .IsRequired();

        // A calendar day with no time and no timezone, so a late-evening purchase cannot slide
        // into the next month.
        builder.Property(transaction => transaction.OccurredOn)
            .HasColumnName("occurred_on")
            .HasColumnType("date")
            .IsRequired();

        builder.Property(transaction => transaction.Note)
            .HasColumnName("note")
            .HasMaxLength(Transaction.MaxNoteLength);

        // Always null in this feature. csv-import and message capture fill it later.
        builder.Property(transaction => transaction.ExternalRef)
            .HasColumnName("external_ref")
            .HasMaxLength(Transaction.MaxExternalRefLength);

        builder.Property(transaction => transaction.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // The breakdown filters on the period alone (leading column); the history page filters on
        // period and account. One index covers both.
        builder.HasIndex(transaction => new { transaction.OccurredOn, transaction.AccountId })
            .HasDatabaseName("idx_transactions_occurred_on_account_id");

        // The balance sums every transaction per account over all time, and the account delete
        // asks whether any transaction names it. Neither filters by date, so the composite above
        // cannot serve them. Also the FK index on account_id.
        builder.HasIndex(transaction => transaction.AccountId)
            .HasDatabaseName("idx_transactions_account_id");

        // The category delete asks whether any transaction names it. Also the FK index on
        // category_id.
        builder.HasIndex(transaction => transaction.CategoryId)
            .HasDatabaseName("idx_transactions_category_id");

        // Unused here. PostgreSQL treats nulls as distinct in a unique index, so any number of
        // null rows is allowed — which is every row this feature writes.
        builder.HasIndex(transaction => transaction.ExternalRef)
            .IsUnique()
            .HasDatabaseName("ux_transactions_external_ref");

        // No navigation on either side, and no cascade: the Application layer refuses to delete an
        // account or a category that any movement still names, and a cascade would do the opposite.
        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(transaction => transaction.AccountId)
            .HasConstraintName("fk_transactions_account")
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(transaction => transaction.CategoryId)
            .HasConstraintName("fk_transactions_category")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
