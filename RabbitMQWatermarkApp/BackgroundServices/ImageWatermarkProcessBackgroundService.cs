using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQWatermarkApp.Services;

namespace RabbitMQWatermarkApp.BackgroundServices
{
    public class ImageWatermarkProcessBackgroundService : BackgroundService
    {
        private readonly RabbitMQClientService _rabbitMQClientService;
        private IModel _channel;

        public ImageWatermarkProcessBackgroundService(RabbitMQClientService rabbitMQClientService)
        {
            _rabbitMQClientService = rabbitMQClientService;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _channel = _rabbitMQClientService.Connect();
            _channel.BasicQos(0, 1, false);

            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);
            _channel.BasicConsume(RabbitMQClientService.QueueName, autoAck: false, consumer: consumer);

            consumer.Received += Consumer_Received;

            return Task.CompletedTask;
        }   

        private Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            try
            {
                var productImageCreatedEvent = JsonSerializer.Deserialize<ProductImageCreatedEvent>(Encoding.UTF8.GetString(@event.Body.ToArray()));
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", productImageCreatedEvent.ImageName);

                using var img = Image.FromFile(path);
                using var graphic = Graphics.FromImage(img);

                var text = "alibulut.net";
                var font = new Font(FontFamily.GenericMonospace, 40, FontStyle.Bold, GraphicsUnit.Pixel);
                var textSize = graphic.MeasureString(text, font);
                var color = Color.FromKnownColor(KnownColor.Black);
                var brush = new SolidBrush(color);
                var position = new Point(100, 100);

                graphic.DrawString(text, font, brush, position);
                img.Save("wwwroot/images/watermarks/" + productImageCreatedEvent.ImageName);

                img.Dispose();
                graphic.Dispose();

                _channel.BasicAck(@event.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return Task.CompletedTask;
        }
    }
}
