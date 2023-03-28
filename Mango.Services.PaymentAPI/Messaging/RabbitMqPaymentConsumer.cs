using System.Text;
using Mango.Services.PaymentAPI.Messages;
using Mango.Services.PaymentAPI.RabbitMqSender;
using Newtonsoft.Json;
using PaymentProcessor;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mango.Services.PaymentAPI.Messaging;

public class RabbitMqPaymentConsumer : BackgroundService
    {
        private IConnection _connection;
        private IModel _channel;
        private readonly IRabbitMqPaymentMessageSender _rabbitMqPaymentMessageSender;
        private readonly IProcessPayment _processPayment;
        private readonly IConfiguration _configuration;
        private readonly string _paymentQueueName;

        public RabbitMqPaymentConsumer(IRabbitMqPaymentMessageSender rabbitMqPaymentMessageSender,
            IProcessPayment processPayment, IConfiguration configuration)
        {
            _processPayment = processPayment;
            _rabbitMqPaymentMessageSender = rabbitMqPaymentMessageSender;
            _configuration = configuration;
            _paymentQueueName = _configuration["RabbitMqSettings:PaymentQueueName"];
            
            var factory = new ConnectionFactory
            {
                HostName = "rabbitmq",
                UserName = "guest",
                Password = "guest"
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_paymentQueueName, false, false, false, arguments: null);
        }
        protected override Task ExecuteAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (channel, eventArgs) =>
            {
                var content = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var paymentRequestMessage = JsonConvert.DeserializeObject<PaymentRequestMessage>(content);
                HandleMessage(paymentRequestMessage).GetAwaiter().GetResult();

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            };
            _channel.BasicConsume(_paymentQueueName, false, consumer);

            return Task.CompletedTask;
        }

        private async Task HandleMessage(PaymentRequestMessage paymentRequestMessage)
        {
            var result = _processPayment.PaymentProcessor();

            var updatePaymentResultMessage = new UpdatePaymentResultMessage
            {
                Status = result,
                OrderId = paymentRequestMessage.OrderId,
                Email = paymentRequestMessage.Email
            };

            try
            {
                _rabbitMqPaymentMessageSender.SendMessage(updatePaymentResultMessage);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }