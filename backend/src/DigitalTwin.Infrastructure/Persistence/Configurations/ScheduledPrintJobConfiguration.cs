using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class ScheduledPrintJobConfiguration : IEntityTypeConfiguration<ScheduledPrintJob>
{
    public void Configure(EntityTypeBuilder<ScheduledPrintJob> builder)
    {
        builder.ToTable("scheduled_print_jobs");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.Priority);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => x.RequestedStartAfterUtc);
        builder.HasIndex(x => x.DueAtUtc);

        builder.Property(x => x.FileName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.DisplayName)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.RequestedMaterialType)
            .HasMaxLength(64);

        builder.Property(x => x.RequestedColor)
            .HasMaxLength(32);

        builder.Property(x => x.EstimatedFilamentGrams)
            .HasPrecision(10, 2);

        builder.Property(x => x.Notes)
            .HasMaxLength(2000);

        builder.HasOne(x => x.PreferredPrinter)
            .WithMany()
            .HasForeignKey(x => x.PreferredPrinterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.AssignedPrinter)
            .WithMany()
            .HasForeignKey(x => x.AssignedPrinterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.StartedPrinterTask)
            .WithMany()
            .HasForeignKey(x => x.StartedPrinterTaskId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.SchedulerDecisionReason)
            .HasMaxLength(2000);

        builder.Property(x => x.CompatibilityNote)
            .HasMaxLength(2000);
    }
}