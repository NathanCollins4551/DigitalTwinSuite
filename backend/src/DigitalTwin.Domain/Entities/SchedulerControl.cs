namespace DigitalTwin.Domain.Entities;

public class SchedulerControl
{
    public Guid Id { get; set; }

    public bool IsPaused { get; set; }
    public string? PauseReason { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}