using FinNavis.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FinNavis.Infrastructure.Persistence.Configurations;

public sealed class MappingProfileConfiguration : IEntityTypeConfiguration<MappingProfile>
{
    public void Configure(EntityTypeBuilder<MappingProfile> builder)
    {
        builder.ToTable("mapping_profiles");

        builder.HasKey(profile => profile.Id).HasName("pk_mapping_profiles");

        // The id is a UUID v7 minted in the Application layer, never by the provider.
        builder.Property(profile => profile.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        // The normalised header row of the file, kept readable rather than hashed. It is the
        // identity of a profile: the owner names nothing and picks nothing.
        builder.Property(profile => profile.ShapeKey)
            .HasColumnName("shape_key")
            .HasMaxLength(MappingProfile.MaxShapeKeyLength)
            .IsRequired();

        builder.Property(profile => profile.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        // Every load looks up one profile by the header row of its file. The uniqueness is the
        // point: two profiles for one header would make the lookup ambiguous.
        builder.HasIndex(profile => profile.ShapeKey)
            .IsUnique()
            .HasDatabaseName("ux_mapping_profiles_shape_key");

        // A rule has no life outside its profile, and confirming a shape again replaces its rules
        // as a whole, so the rules go with the profile.
        builder.HasMany(profile => profile.Rules)
            .WithOne()
            .HasForeignKey(rule => rule.MappingProfileId)
            .HasConstraintName("fk_mapping_profile_rules_mapping_profile")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
