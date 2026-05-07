using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterTaskConfiguration : IEntityTypeConfiguration<PrinterTask>
{
    public void Configure(EntityTypeBuilder<PrinterTask> builder)
    {
        builder.ToTable("printer_tasks");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.ExternalTaskId).IsUnique();
        builder.HasIndex(x => x.TaskAlias).IsUnique();
        builder.HasIndex(x => x.DeviceId);

        builder.Property(x => x.DeviceId).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TaskAlias).IsRequired().HasMaxLength(64);
        builder.Property(x => x.DeviceName).HasMaxLength(256);
        builder.Property(x => x.DeviceModel).HasMaxLength(64);
        builder.Property(x => x.DesignTitle).HasMaxLength(512);
        builder.Property(x => x.DesignTitleTranslated).HasMaxLength(512);
        builder.Property(x => x.StatusText).HasMaxLength(64);
        builder.Property(x => x.Mode).HasMaxLength(64);
        builder.Property(x => x.BedType).HasMaxLength(64);
        builder.Property(x => x.CoverUrl).HasMaxLength(1024);
        builder.Property(x => x.WeightGrams).HasPrecision(10, 2);
        builder.Property(x => x.RawJson).IsRequired();

        builder.HasOne(x => x.Printer)
            .WithMany()
            .HasForeignKey(x => x.PrinterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.AmsDetails)
            .WithOne(x => x.PrinterTask)
            .HasForeignKey(x => x.PrinterTaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
