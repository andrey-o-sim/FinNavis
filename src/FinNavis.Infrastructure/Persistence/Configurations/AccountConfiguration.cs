using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id).HasName("pk_accounts");

        builder.Property(account => account.Id)
            .HasColumnName("id")
            // Guid.CreateVersion7() in Account.Create is the only place an id is made.
            // The database must not invent one.
            .ValueGeneratedNever();

        builder.Property(account => account.Name)
            .HasColumnName("name")
            // Taken from the entity so the column and the validation message cannot drift.
            .HasMaxLength(Account.MaxNameLength)
            .IsRequired();

        builder.Property(account => account.Type)
            .HasColumnName("type")
            // Stored as text, never as an int. "Cash" survives reordering the enum.
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(account => account.InitialBalance)
            .HasColumnName("initial_balance")
            // HasPrecision, not HasColumnType: same numeric(14,2) column, no provider name in
            // the model. Scale 2 is the only scale Account.ValidateInitialBalance admits, so
            // PostgreSQL never rounds a value the API accepted. Precision 14 matches
            // Account.MaxAbsoluteBalance.
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // No index beyond the primary key, on purpose. See ListAsync in EfAccountRepository.
    }
}
