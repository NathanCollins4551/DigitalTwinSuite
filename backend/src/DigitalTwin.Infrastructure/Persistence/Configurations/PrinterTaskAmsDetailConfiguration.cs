using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterTaskAmsDetailConfiguration : IEntityTypeConfiguration<PrinterTaskAmsDetail>
{
    public void Configure(EntityTypeBuilder<PrinterTaskAmsDetail> builder)
    {
        builder.ToTable("printer_task_ams_details");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.PrinterTaskId, x.Ams, x.AmsId, x.SlotId, x.NozzleId });

        builder.Property(x => x.FilamentId).HasMaxLength(128);
        builder.Property(x => x.FilamentType).HasMaxLength(128);
        builder.Property(x => x.TargetFilamentType).HasMaxLength(128);
        builder.Property(x => x.SourceColor).HasMaxLength(32);
        builder.Property(x => x.TargetColor).HasMaxLength(32);
        builder.Property(x => x.WeightGrams).HasPrecision(10, 2);
    }
}