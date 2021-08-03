using System;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace RabbitMQWatermarkApp.Services
{
    public class RabbitMQClientService : IDisposable
    {
        private readonly ConnectionFactory _connectionFactory;
        private IConnection _connection;
        private IModel _channel;
        public static string ExchangeName = "ImageDirectExchange";
        public static string RoutingWatermark = "watermark-route-image";
        public static string QueueName = "queue-watermark-image";
        private readonly ILogger<RabbitMQClientService> _logger;

        public RabbitMQClientService(ConnectionFactory connectionFactory, ILogger<RabbitMQClientService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public IModel Connect()
        {
            _connection = _connectionFactory.CreateConnection();
            if(_channel?.IsOpen == true) // if(_channel is { IsOpen: true })
            {
                return _channel;
            }

            _channel = _connection.CreateModel();
            _channel.ExchangeDeclare(ExchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
            _channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false, null);
            _channel.QueueBind(exchange: ExchangeName, queue: QueueName, routingKey: RoutingWatermark);

            _logger.LogInformation("Connection has been established with RabbitMQ.");

            return _channel;
        }

        public void Dispose()
        {
            _channel?.Close();
            _channel?.Dispose();
            _channel = default; // if it is boolean, it'll set false. If it is string it'll set empty string etc. (it set default value by its type)

            _connection?.Close();
            _connection?.Dispose();

            _logger.LogInformation("Connection has been disposed with RabbitMQ.");
        }
    }
}
