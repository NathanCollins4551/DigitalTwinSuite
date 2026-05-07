using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterLoadedSpoolConfiguration : IEntityTypeConfiguration<PrinterLoadedSpool>
{
    public void Configure(EntityTypeBuilder<PrinterLoadedSpool> builder)
    {
        builder.ToTable("printer_loaded_spools");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SlotIndex).IsRequired();

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

        builder.Property(x => x.IsActive).IsRequired();

        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.PrinterAmsUnit)
            .WithMany()
            .HasForeignKey(x => x.PrinterAmsUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => new { x.PrinterId, x.SlotIndex, x.IsActive });
    }
}