using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace Shared.Messaging;

public class RabbitMqClient : IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private const string ExchangeName = "audits.exchange";

    public RabbitMqClient(string hostName = "rabbitmq")
    {
        var factory = new ConnectionFactory() { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true);
    }

    public virtual void Publish<T>(T message)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        _channel.BasicPublish(exchange: ExchangeName, routingKey: "", basicProperties: null, body: body);
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
