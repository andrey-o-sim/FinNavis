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

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(account => account.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(account => account.Name)
            .HasColumnName("name")
            .HasMaxLength(Account.MaxNameLength)
            .IsRequired();

        builder.Property(account => account.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // A name identifies an account. The Application layer checks first and reports the
        // refusal wording; this index is the last guard.
        builder.HasIndex(account => account.Name)
            .IsUnique()
            .HasDatabaseName("ux_accounts_name");
    }
}
