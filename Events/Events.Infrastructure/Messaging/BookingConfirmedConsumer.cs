using System.Text.Json;
using Confluent.Kafka;
using Contracts;
using Events.Application.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Events.Infrastructure.Messaging;

public sealed class BookingConfirmedConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingConfirmedConsumer> _logger;
    private readonly string _bootstrapServers;
    private readonly string _consumerGroup;

    public BookingConfirmedConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BookingConfirmedConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
                            ?? throw new InvalidOperationException("Kafka:BootstrapServers не настроен.");
        _consumerGroup = configuration["Kafka:ConsumerGroup"]
                         ?? throw new InvalidOperationException("Kafka:ConsumerGroup не настроен.");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        using var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = _consumerGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = true
        }).Build();

        consumer.Subscribe(BookingConfirmed.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string> result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Ошибка чтения из топика {Topic}", BookingConfirmed.Topic);
                    await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
                    continue;
                }

                if (result?.Message?.Value is null)
                    continue;

                try
                {
                    await HandleAsync(result.Message.Value, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Сообщение из топика {Topic} пропущено", BookingConfirmed.Topic);
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Подписчик топика {Topic} остановлен", BookingConfirmed.Topic);
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        BookingConfirmed? message;
        try
        {
            message = JsonSerializer.Deserialize<BookingConfirmed>(payload);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Не удалось разобрать BookingConfirmed");
            return;
        }

        if (message is null)
        {
            _logger.LogWarning("Пустое сообщение BookingConfirmed пропущено");
            return;
        }

        if (message.SeatCount <= 0)
        {
            _logger.LogWarning(
                "Бронь {BookingId} пропущена: некорректное количество мест {SeatCount}",
                message.BookingId,
                message.SeatCount);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var events = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var ev = await events.GetByIdAsync(message.EventId, cancellationToken);
        if (ev is null)
        {
            _logger.LogWarning(
                "Событие {EventId} не найдено, бронь {BookingId} пропущена",
                message.EventId,
                message.BookingId);
            return;
        }

        if (!ev.TryReserveSeats(message.SeatCount))
        {
            _logger.LogWarning(
                "Нет свободных мест для события {EventId}, бронь {BookingId} пропущена",
                message.EventId,
                message.BookingId);
            return;
        }

        await events.SaveChangesAsync(cancellationToken);
    }
}
