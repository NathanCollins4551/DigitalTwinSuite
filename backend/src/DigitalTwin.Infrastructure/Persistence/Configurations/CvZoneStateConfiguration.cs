using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class CvZoneStateConfiguration : IEntityTypeConfiguration<CvZoneState>
{
    public void Configure(EntityTypeBuilder<CvZoneState> builder)
    {
        builder.ToTable("cv_zone_states");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CameraId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.ZoneName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SpoolIdsJson)
            .IsRequired();

        builder.Property(x => x.UnknownSpoolCount)
            .IsRequired();

        builder.Property(x => x.OtherObjectCount)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new { x.CameraId, x.ZoneName })
            .IsUnique();
    }
}