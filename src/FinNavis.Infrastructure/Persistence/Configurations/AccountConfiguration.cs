using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Account"/> to <c>accounts</c> (<c>data-model.md</c>).
/// </summary>
/// <remarks>
/// <b>No balance column and no <c>updated_at</c>.</b> AC-14 sums the movements at read time
/// (ADR-0002), and nothing in the PRD reads a last-changed value (<c>data-model.md</c> decision 5).
/// </remarks>
public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(account => account.Id).HasName("pk_accounts");

        // The id is a UUID v7 minted in the Application layer (ADR-0005), never by the provider.
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

        // AC-35: a name identifies an account. The Application layer checks first and reports the
        // refusal wording; this index is the last guard (sad.md §6 flow 13).
        builder.HasIndex(account => account.Name)
            .IsUnique()
            .HasDatabaseName("ux_accounts_name");
    }
}
