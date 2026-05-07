using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterMessageConfiguration : IEntityTypeConfiguration<PrinterMessage>
{
    public void Configure(EntityTypeBuilder<PrinterMessage> builder)
    {
        builder.ToTable("printer_messages");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ExternalMessageId).IsUnique();
        builder.HasIndex(x => x.ExternalTaskId);
        builder.HasIndex(x => x.DeviceId);

        builder.Property(x => x.DeviceId).HasMaxLength(64);
        builder.Property(x => x.DeviceName).HasMaxLength(256);
        builder.Property(x => x.Title).HasMaxLength(512);
        builder.Property(x => x.Detail).HasMaxLength(1024);
        builder.Property(x => x.CoverUrl).HasMaxLength(1024);
        builder.Property(x => x.DesignTitle).HasMaxLength(512);
        builder.Property(x => x.RawJson).IsRequired();

        builder.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.RelatedPrinterTask)
            .WithMany()
            .HasForeignKey(x => x.RelatedPrinterTaskId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}