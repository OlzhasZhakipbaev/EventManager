using System.Text.Json;
using Bookings.Application.Messaging;
using Confluent.Kafka;
using Contracts;

namespace Bookings.Infrastructure.Messaging;

public sealed class KafkaBookingConfirmedPublisher : IBookingConfirmedPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;

    public KafkaBookingConfirmedPublisher(string bootstrapServers)
    {
        if (string.IsNullOrWhiteSpace(bootstrapServers))
            throw new InvalidOperationException("Kafka:BootstrapServers не настроен.");

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = bootstrapServers
        }).Build();
    }

    public Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default)
    {
        return _producer.ProduceAsync(
            BookingConfirmed.Topic,
            new Message<string, string>
            {
                Key = message.EventId.ToString(),
                Value = JsonSerializer.Serialize(message)
            },
            cancellationToken);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
