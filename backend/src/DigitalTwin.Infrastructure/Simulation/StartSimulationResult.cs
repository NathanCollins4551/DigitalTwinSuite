namespace DigitalTwin.Infrastructure.Simulation;

public class StartSimulationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = null!;

    public Guid? PrinterTaskId { get; set; }
    public long? ExternalTaskId { get; set; }
}