using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterAmsUnitConfiguration : IEntityTypeConfiguration<PrinterAmsUnit>
{
    public void Configure(EntityTypeBuilder<PrinterAmsUnit> builder)
    {
        builder.ToTable("printer_ams_units");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.PrinterId, x.AmsIndex }).IsUnique();

        builder.Property(x => x.AmsDeviceId).HasMaxLength(64);
        builder.Property(x => x.CurrentVersion).HasMaxLength(64);
        builder.Property(x => x.LatestVersion).HasMaxLength(64);
        builder.Property(x => x.ReleaseStatus).HasMaxLength(64);
        builder.Property(x => x.DownloadUrl).HasMaxLength(1024);
    }
}