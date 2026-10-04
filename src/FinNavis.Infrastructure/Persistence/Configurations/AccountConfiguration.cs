using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("accounts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // Domain makes the id (Guid.CreateVersion7()).

        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired(); // No HasMaxLength: the column stays PostgreSQL text.

        builder.Property(a => a.OpeningBalance)
            .HasColumnName("opening_balance")
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(a => a.Note)
            .HasColumnName("note")
            .HasMaxLength(500);

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Unique: two accounts may not share a name ignoring case and surrounding spaces. Also
        // serves the duplicate check and the list ordering.
        builder.HasIndex(a => a.NormalizedName)
            .IsUnique()
            .HasDatabaseName("ix_accounts_normalized_name");
    }
}
