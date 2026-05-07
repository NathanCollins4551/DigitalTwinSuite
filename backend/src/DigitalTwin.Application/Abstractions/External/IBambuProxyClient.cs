using DigitalTwin.Application.Printers.Dtos;

namespace DigitalTwin.Application.Abstractions.External;

public interface IBambuProxyClient
{
    Task<BindResponseDto> GetBindAsync(CancellationToken cancellationToken = default);
    Task<VersionResponseDto> GetVersionAsync(CancellationToken cancellationToken = default);

    Task<MessageResponseDto> GetMessagesAsync(CancellationToken cancellationToken = default);
    Task<TaskResponseDto> GetTasksAsync(CancellationToken cancellationToken = default);
}