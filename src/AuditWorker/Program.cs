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
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(exchange: "audits.exchange", type: ExchangeType.Fanout, durable: true);
var queueName = channel.QueueDeclare().QueueName;
channel.QueueBind(queue: queueName, exchange: "audits.exchange", routingKey: "");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += async (model, ea) =>
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

channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

Console.WriteLine("AuditWorker running. Press [enter] to exit.");
Console.ReadLine();
