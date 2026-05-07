using System.Text;

namespace DigitalTwin.Domain.Entities;

public class PrinterTask
{
    private const int TaskAliasTokenMaxLength = 24;

    public Guid Id { get; set; }

    public long ExternalTaskId { get; set; }              // tasks[].id
    public string TaskAlias { get; set; } = null!;
    public Guid? PrinterId { get; set; }
    public Printer? Printer { get; set; }

    public string DeviceId { get; set; } = null!;
    public string? DeviceName { get; set; }
    public string? DeviceModel { get; set; }

    public string? DesignTitle { get; set; }
    public string? DesignTitleTranslated { get; set; }
    public string? StatusText { get; set; }               // optional normalized later
    public int? FailedType { get; set; }
    public string? Mode { get; set; }
    public string? BedType { get; set; }

    public int? CostTimeSeconds { get; set; }
    public int? LengthMm { get; set; }
    public decimal? WeightGrams { get; set; }

    public DateTimeOffset? StartTimeUtc { get; set; }
    public DateTimeOffset? EndTimeUtc { get; set; }

    public string? CoverUrl { get; set; }

    public string RawJson { get; set; } = null!;

    public DateTimeOffset SourceUpdatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }

    public ICollection<PrinterTaskAmsDetail> AmsDetails { get; set; } = new List<PrinterTaskAmsDetail>();

    public bool IsSimulated { get; set; }
    public DateTimeOffset? SimulatedCompleteAtUtc { get; set; }

    public static string BuildTaskAlias(string? deviceName, string? deviceId, long externalTaskId)
    {
        var printerToken = NormalizeAliasToken(deviceName);
        if (string.IsNullOrWhiteSpace(printerToken))
        {
            printerToken = NormalizeAliasToken(deviceId);
        }

        if (string.IsNullOrWhiteSpace(printerToken))
        {
            printerToken = "PRINTER";
        }

        return $"PT-{printerToken}-{externalTaskId.ToString("X").ToUpperInvariant()}";
    }

    private static string NormalizeAliasToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(Math.Min(value.Length, TaskAliasTokenMaxLength));
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }

            if (builder.Length >= TaskAliasTokenMaxLength)
            {
                break;
            }
        }

        return builder.ToString().Trim('-');
    }
}
