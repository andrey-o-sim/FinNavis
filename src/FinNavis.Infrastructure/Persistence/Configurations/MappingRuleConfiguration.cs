using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class MappingRuleConfiguration : IEntityTypeConfiguration<MappingRule>
{
    public void Configure(EntityTypeBuilder<MappingRule> builder)
    {
        builder.ToTable("mapping_profile_rules");

        builder.HasKey(rule => rule.Id).HasName("pk_mapping_profile_rules");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(rule => rule.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(rule => rule.MappingProfileId)
            .HasColumnName("mapping_profile_id")
            .HasColumnType("uuid")
            .IsRequired();

        // The file's own category text, not one of the owner's categories.
        builder.Property(rule => rule.SourceCategory)
            .HasColumnName("source_category")
            .HasMaxLength(MappingRule.MaxSourceCategoryLength)
            .IsRequired();

        // Stored as a string, never as an int. No CHECK on the allowed values, and none tying
        // target_category_id to the target: both are domain rules, refused in MappingRule.
        builder.Property(rule => rule.Target)
            .HasColumnName("target")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Set exactly when the target is a category, null otherwise.
        builder.Property(rule => rule.TargetCategoryId)
            .HasColumnName("target_category_id")
            .HasColumnType("uuid");

        builder.Property(rule => rule.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Every load reads all the rules of one profile, so the profile id leads. Also the FK
        // index on mapping_profile_id. The uniqueness stops two rules disagreeing about the same
        // file category.
        builder.HasIndex(rule => new { rule.MappingProfileId, rule.SourceCategory })
            .IsUnique()
            .HasDatabaseName("ux_mapping_profile_rules_profile_source_category");

        // FK index. Without it, deleting a category scans this table.
        builder.HasIndex(rule => rule.TargetCategoryId)
            .HasDatabaseName("idx_mapping_profile_rules_target_category_id");

        // No cascade: deleting a category must never silently rewrite the rules the owner
        // confirmed.
        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(rule => rule.TargetCategoryId)
            .HasConstraintName("fk_mapping_profile_rules_category")
            .OnDelete(DeleteBehavior.NoAction);
    }
}
