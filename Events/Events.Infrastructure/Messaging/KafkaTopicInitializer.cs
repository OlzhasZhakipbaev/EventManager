using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Events.Infrastructure.Messaging;

public sealed class KafkaTopicInitializer : IHostedService
{
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaTopicInitializer> _logger;

    public KafkaTopicInitializer(IConfiguration configuration, ILogger<KafkaTopicInitializer> logger)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
                            ?? throw new InvalidOperationException("Kafka:BootstrapServers не настроен.");
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var admin = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers
            }).Build();

            var metadata = admin.GetMetadata(BookingConfirmed.Topic, TimeSpan.FromSeconds(10));
            var topic = metadata.Topics.SingleOrDefault(t => t.Topic == BookingConfirmed.Topic);
            if (topic is not null && topic.Error.Code == ErrorCode.NoError)
                return;

            await admin.CreateTopicsAsync(new[]
            {
                new TopicSpecification
                {
                    Name = BookingConfirmed.Topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            });
        }
        catch (CreateTopicsException ex) when (ex.Results.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            _logger.LogInformation("Топик {Topic} уже существует", BookingConfirmed.Topic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось создать топик {Topic}", BookingConfirmed.Topic);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
