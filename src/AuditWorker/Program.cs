using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Shared.Caching;
using Shared.Messaging;
using Shared.Observability;
using System.Text;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddAppTelemetry("AuditWorker");
builder.Services.AddSingleton<IRedisClient, RedisClient>();
var host = builder.Build();

using var scope = host.Services.CreateScope();
var cache = scope.ServiceProvider.GetRequiredService<IRedisClient>();

var factory = new ConnectionFactory() { HostName = "rabbitmq" };
using var connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
using var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

channel.ExchangeDeclareAsync(exchange: "audits.exchange", type: ExchangeType.Fanout, durable: true).GetAwaiter().GetResult();
var queueName = channel.QueueDeclareAsync().GetAwaiter().GetResult().QueueName;
channel.QueueBindAsync(queue: queueName, exchange: "audits.exchange", routingKey: "").GetAwaiter().GetResult();

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    try
    {
        var evt = JsonSerializer.Deserialize<AuditScheduledEvent>(message);
        if (evt is not null)
        {
            var cacheKey = $"audit:{evt.Id}:summary";
            await cache.SetAsync(cacheKey, new
            {
                evt.Id,
                evt.Title,
                evt.ScheduledDate,
                evt.AssignedTo,
                CachedAt = DateTime.UtcNow
            }, TimeSpan.FromHours(1));
            Console.WriteLine($"[Worker] Cached summary -> {cacheKey}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Worker] Error: {ex.Message}");
    }
};

channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer).GetAwaiter().GetResult();

Console.WriteLine("AuditWorker running. Press [enter] to exit.");
Console.ReadLine();
