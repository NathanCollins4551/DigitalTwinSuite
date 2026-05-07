using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterFirmwareStatusConfiguration : IEntityTypeConfiguration<PrinterFirmwareStatus>
{
    public void Configure(EntityTypeBuilder<PrinterFirmwareStatus> builder)
    {
        builder.ToTable("printer_firmware_statuses");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PrinterId).IsUnique();

        builder.Property(x => x.CurrentVersion).HasMaxLength(64);
        builder.Property(x => x.LatestVersion).HasMaxLength(64);
        builder.Property(x => x.ReleaseStatus).HasMaxLength(64);
        builder.Property(x => x.DownloadUrl).HasMaxLength(1024);
    }
}