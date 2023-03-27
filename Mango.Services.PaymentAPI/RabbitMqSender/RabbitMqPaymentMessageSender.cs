using System.Text;
using Mango.MessageBus;
using Mango.Services.PaymentAPI.RabbitMqSender;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace Mango.Services.PaymentAPI.RabbitMQSender;

public class RabbitMqPaymentMessageSender : IRabbitMqPaymentMessageSender
{
    private readonly string _hostname;
    private readonly string _password;
    private readonly string _username;

    private IConnection _connection;

    private readonly string _exchangeName;
    private readonly string _paymentEmailUpdateQueueName;
    private readonly string _paymentOrderUpdateQueueName;
    private readonly IConfiguration _configuration;

    public RabbitMqPaymentMessageSender(IConfiguration configuration)
    {
        _configuration = configuration;
        _exchangeName = _configuration["RabbitMqSettings:ExchangeName"];
        _paymentEmailUpdateQueueName = _configuration["RabbitMqSettings:PaymentEmailUpdateQueueName"];
        _paymentOrderUpdateQueueName = _configuration["RabbitMqSettings:PaymentOrderUpdateQueueName"];
        
        _hostname = "rabbitmq";
        _password = "guest";
        _username = "guest";
    }

    public void SendMessage(BaseMessage message)
    {
        if (ConnectionExists())
        {
            using var channel = _connection.CreateModel();
            channel.ExchangeDeclare(_exchangeName, ExchangeType.Direct, durable: false);
            channel.QueueDeclare(_paymentOrderUpdateQueueName, false, false, false, null);
            channel.QueueDeclare(_paymentEmailUpdateQueueName, false, false, false, null);

            channel.QueueBind(_paymentEmailUpdateQueueName, _exchangeName, "PaymentEmail");
            channel.QueueBind(_paymentOrderUpdateQueueName, _exchangeName, "PaymentOrder");

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            channel.BasicPublish(exchange: _exchangeName, "PaymentEmail", basicProperties: null, body: body);
            channel.BasicPublish(exchange: _exchangeName, "PaymentOrder", basicProperties: null, body: body);
        }
    }

    private void CreateConnection()
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _hostname,
                UserName = _username,
                Password = _password
            };
            _connection = factory.CreateConnection();
        }
        catch (Exception)
        {
            //log exception
        }
    }

    private bool ConnectionExists()
    {
        if (_connection != null)
        {
            return true;
        }
        CreateConnection();
        return _connection != null;
    }
}