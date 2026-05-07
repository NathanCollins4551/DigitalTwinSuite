using DigitalTwin.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DigitalTwin.Infrastructure.Persistence.Configurations;

public class SchedulerControlConfiguration : IEntityTypeConfiguration<SchedulerControl>
{
    public void Configure(EntityTypeBuilder<SchedulerControl> builder)
    {
        builder.ToTable("scheduler_controls");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PauseReason)
            .HasMaxLength(1000);
    }
}