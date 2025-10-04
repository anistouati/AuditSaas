using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using Microsoft.Extensions.Configuration;

namespace Shared.Messaging;

public class RabbitMqClient : IRabbitMqClient, IDisposable
{
    private readonly IConnection? _connection;
    private readonly IChannel? _channel;
    private const string ExchangeName = "audits.exchange";
    private readonly string _host;

    public RabbitMqClient(IConfiguration? configuration = null, string? hostName = null)
    {
        _host = hostName
                 ?? configuration?["Messaging:RabbitMq:Host"]
                 ?? Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                 ?? "rabbitmq";

        Exception? last = null;
        var attempted = new List<string>();
        foreach (var candidate in new[] { _host, _host == "rabbitmq" ? "localhost" : "rabbitmq" })
        {
            if (attempted.Contains(candidate)) continue;
            attempted.Add(candidate);
            try
            {
                Console.WriteLine($"[RabbitMqClient] Attempting connection to '{candidate}'...");
                var factory = new ConnectionFactory { HostName = candidate };
                _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
                _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                _channel.ExchangeDeclareAsync(exchange: ExchangeName, type: ExchangeType.Fanout, durable: true);
                if (candidate != _host)
                {
                    Console.WriteLine($"[RabbitMqClient] Fallback host succeeded: '{candidate}' (primary was '{_host}').");
                }
                _host = candidate;
                last = null;
                break;
            }
            catch (Exception ex)
            {
                last = ex;
                Console.Error.WriteLine($"[RabbitMqClient] Connection to '{candidate}' failed: {ex.Message}");
            }
        }
        if (_connection == null || _channel == null)
        {
            var ex = last ?? new InvalidOperationException("RabbitMQ connection objects not initialized.");
            Console.Error.WriteLine($"[RabbitMqClient] All connection attempts failed (tried: {string.Join(",", attempted)}). {ex.Message}");
            throw ex;
        }
    }

    public virtual void Publish<T>(T message)
    {
        if (_channel == null)
        {
            throw new InvalidOperationException("RabbitMQ channel not initialized; cannot publish message.");
        }
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
