using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class CvZoneSpoolConfiguration : IEntityTypeConfiguration<CvZoneSpool>
{
    public void Configure(EntityTypeBuilder<CvZoneSpool> builder)
    {
        builder.ToTable("cv_zone_spools");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CameraId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ZoneName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SpoolCode)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.MaterialType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ColorName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ColorHex)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.LastSeenAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.CameraId, x.ZoneName, x.SpoolCode })
            .IsUnique();

        builder.HasIndex(x => new { x.MaterialType, x.ColorName });
    }
}