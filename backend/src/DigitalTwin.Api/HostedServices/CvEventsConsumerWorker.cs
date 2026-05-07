using System.Text;
using System.Text.Json;
using DigitalTwin.Application.Inventory.Dtos;
using DigitalTwin.Infrastructure.Inventory;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DigitalTwin.Api.HostedServices;

public class CvEventsConsumerWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CvEventsConsumerWorker> _logger;
    private readonly IConfiguration _configuration;

    private IConnection? _connection;
    private IChannel? _channel;

    public CvEventsConsumerWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<CvEventsConsumerWorker> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var host = _configuration["RabbitMq:Host"] ?? "localhost";
        var port = int.TryParse(_configuration["RabbitMq:Port"], out var parsedPort) ? parsedPort : 5672;
        var username = _configuration["RabbitMq:Username"] ?? "guest";
        var password = _configuration["RabbitMq:Password"] ?? "guest";

        var factory = new ConnectionFactory
        {
            HostName = host,
            Port = port,
            UserName = username,
            Password = password
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: "cv.events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: "backend.cv.events",
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync("backend.cv.events", "cv.events", "cv.zone.inventory", cancellationToken: cancellationToken);
        await _channel.QueueBindAsync("backend.cv.events", "cv.events", "cv.zone.anomaly", cancellationToken: cancellationToken);

        await _channel.BasicQosAsync(0, 10, false, cancellationToken);

        _logger.LogInformation("CvEventsConsumerWorker connected to RabbitMQ.");
        await base.StartAsync(cancellationToken);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_channel is null)
            throw new InvalidOperationException("RabbitMQ channel is not initialized.");

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, args) =>
        {
            try
            {
                var routingKey = args.RoutingKey;
                var body = Encoding.UTF8.GetString(args.Body.ToArray());

                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<CvZoneStateService>();

                if (routingKey == "cv.zone.inventory")
                {
                    var message = JsonSerializer.Deserialize<CvZoneInventoryMessageDto>(body)
                        ?? throw new InvalidOperationException("Failed to deserialize zone.inventory message.");

                    await service.UpsertInventoryMessageAsync(message, stoppingToken);
                }
                else if (routingKey == "cv.zone.anomaly")
                {
                    var message = JsonSerializer.Deserialize<CvZoneAnomalyMessageDto>(body)
                        ?? throw new InvalidOperationException("Failed to deserialize zone.anomaly message.");

                    await service.UpsertAnomalyMessageAsync(message, stoppingToken);
                }
                else
                {
                    _logger.LogWarning("Unhandled routing key: {RoutingKey}", routingKey);
                }

                await _channel.BasicAckAsync(args.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed processing CV RabbitMQ message.");

                if (_channel is not null)
                {
                    await _channel.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true, cancellationToken: stoppingToken);
                }
            }
        };

        _ = _channel.BasicConsumeAsync(
            queue: "backend.cv.events",
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null)
        {
            await _channel.CloseAsync(cancellationToken);
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.CloseAsync(cancellationToken);
            await _connection.DisposeAsync();
        }

        _logger.LogInformation("CvEventsConsumerWorker stopped.");
        await base.StopAsync(cancellationToken);
    }
}