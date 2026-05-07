using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterConfiguration : IEntityTypeConfiguration<Printer>
{
    public void Configure(EntityTypeBuilder<Printer> builder)
    {
        builder.ToTable("printers");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.DeviceId).IsRequired().HasMaxLength(64);
        builder.HasIndex(x => x.DeviceId).IsUnique();

        builder.Property(x => x.Name).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PrintStatus).HasMaxLength(64);
        builder.Property(x => x.ModelName).HasMaxLength(64);
        builder.Property(x => x.ProductName).HasMaxLength(64);
        builder.Property(x => x.Structure).HasMaxLength(64);
        builder.Property(x => x.NozzleDiameterMm).HasPrecision(5, 2);

        builder.HasOne(x => x.FirmwareStatus)
            .WithOne(x => x.Printer)
            .HasForeignKey<PrinterFirmwareStatus>(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.AmsUnits)
            .WithOne(x => x.Printer)
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}