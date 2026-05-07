using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class PrinterSimulationControlConfiguration : IEntityTypeConfiguration<PrinterSimulationControl>
{
    public void Configure(EntityTypeBuilder<PrinterSimulationControl> builder)
    {
        builder.ToTable("printer_simulation_controls");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.PrinterId).IsUnique();
        builder.HasIndex(x => x.IsLocked);
        builder.HasIndex(x => x.LockedUntilUtc);

        builder.Property(x => x.SimulationState)
            .HasMaxLength(32);

        builder.HasOne(x => x.Printer)
            .WithOne(x => x.SimulationControl)
            .HasForeignKey<PrinterSimulationControl>(x => x.PrinterId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}