using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace Shared.Messaging;

public class RabbitMqClient : IRabbitMqClient, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string ExchangeName = "audits.exchange";
    private readonly string _host;

    public RabbitMqClient(IConfiguration? configuration = null, string? hostName = null)
    {
        _host = hostName
                 ?? configuration?["Messaging:RabbitMq:Host"]
                 ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                 ?? "rabbitmq";

        var factory = new ConnectionFactory { HostName = _host };
        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true);
    }

    public virtual void Publish<T>(T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var basicProperties = new BasicProperties();

        _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: "",
            mandatory: false,
            basicProperties: basicProperties,
            body: body,
            cancellationToken: default
        );
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
