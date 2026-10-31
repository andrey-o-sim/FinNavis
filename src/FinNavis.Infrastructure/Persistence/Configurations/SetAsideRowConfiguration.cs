using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class SetAsideRowConfiguration : IEntityTypeConfiguration<SetAsideRow>
{
    public void Configure(EntityTypeBuilder<SetAsideRow> builder)
    {
        builder.ToTable("import_set_aside_rows");

        builder.HasKey(row => row.Id).HasName("pk_import_set_aside_rows");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(row => row.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(row => row.ImportRunId)
            .HasColumnName("import_run_id")
            .HasColumnType("uuid")
            .IsRequired();

        // The row's 1-based place in the file, so the owner can find the line in their own copy.
        builder.Property(row => row.RowNumber)
            .HasColumnName("row_number")
            .IsRequired();

        // Stored as a string, never as an int. No CHECK on the allowed values —
        // they are a domain rule, held by the enum in code.
        builder.Property(row => row.Reason)
            .HasColumnName("reason")
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        // A column of its own so the report can name the uncovered category back without parsing
        // raw_line again long after the file is gone. Null when the row named no category.
        builder.Property(row => row.SourceCategory)
            .HasColumnName("source_category")
            .HasMaxLength(SetAsideRow.MaxSourceCategoryLength);

        // Raw bank text, with shop names the owner did not write. Shown back, never interpreted
        // and never logged.
        builder.Property(row => row.RawLine)
            .HasColumnName("raw_line")
            .HasMaxLength(SetAsideRow.MaxRawLineLength)
            .IsRequired();

        builder.Property(row => row.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // The run report lists every set-aside row of one run. Also the FK index on
        // import_run_id.
        builder.HasIndex(row => row.ImportRunId)
            .HasDatabaseName("idx_import_set_aside_rows_import_run_id");
    }
}
