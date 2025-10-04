namespace Shared.Messaging;

public interface IRabbitMqClient
{
    void Publish<T>(T message);
}