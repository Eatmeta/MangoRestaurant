using System.Text;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mango.Services.OrderAPI.Messaging;

public class RabbitMqPaymentConsumer : BackgroundService
{
    private IConnection _connection;
    private IModel _channel;
    private readonly IConfiguration _configuration;
    private readonly OrderRepository _orderRepository;
    private readonly string _exchangeName;
    private readonly string _paymentOrderUpdateQueueName;
    
    public RabbitMqPaymentConsumer(OrderRepository orderRepository, IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _configuration = configuration;
        _exchangeName = _configuration["RabbitMqSettings:ExchangeName"];
        _paymentOrderUpdateQueueName = _configuration["RabbitMqSettings:PaymentOrderUpdateQueueName"];
        
        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct);
        _channel.QueueDeclare(_paymentOrderUpdateQueueName, false, false, false, null);
        _channel.QueueBind(_paymentOrderUpdateQueueName, _exchangeName, "PaymentOrder");
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (channel, eventArgs) =>
        {
            var content = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var updatePaymentResultMessage = JsonConvert.DeserializeObject<UpdatePaymentResultMessage>(content);
            HandleMessage(updatePaymentResultMessage).GetAwaiter().GetResult();

            _channel.BasicAck(eventArgs.DeliveryTag, false);
        };
        _channel.BasicConsume(_paymentOrderUpdateQueueName, false, consumer);

        return Task.CompletedTask;
    }

    private async Task HandleMessage(UpdatePaymentResultMessage updatePaymentResultMessage)
    {
        try
        {
            await _orderRepository.UpdateOrderPaymentStatus(updatePaymentResultMessage.OrderId,
                updatePaymentResultMessage.Status);
        }
        catch (Exception e)
        {
            throw;
        }
    }
}