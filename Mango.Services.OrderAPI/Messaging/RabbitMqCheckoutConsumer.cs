using System.Text;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models;
using Mango.Services.OrderAPI.RabbitMqSender;
using Mango.Services.OrderAPI.Repository;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Mango.Services.OrderAPI.Messaging;

public class RabbitMqCheckoutConsumer : BackgroundService
{
    private readonly OrderRepository _orderRepository;
    private IConnection _connection;
    private IModel _channel;
    private readonly IRabbitMqOrderMessageSender _rabbitMqOrderMessageSender;
    private readonly IConfiguration _configuration;

    public RabbitMqCheckoutConsumer(OrderRepository orderRepository,
        IRabbitMqOrderMessageSender rabbitMqOrderMessageSender, IConfiguration configuration)
    {
        _orderRepository = orderRepository;
        _rabbitMqOrderMessageSender = rabbitMqOrderMessageSender;
        _configuration = configuration;

        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        _channel.QueueDeclare(_configuration["RabbitMqSettings:QueueName"], false, false, false, null);
    }

    protected override Task ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);

        consumer.Received += (channel, eventArgs) =>
        {
            var content = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
            var checkoutHeaderDto = JsonConvert.DeserializeObject<CheckoutHeaderDto>(content);
            HandleMessage(checkoutHeaderDto).GetAwaiter().GetResult();

            _channel.BasicAck(eventArgs.DeliveryTag, false);
        };
        _channel.BasicConsume(_configuration["RabbitMqSettings:QueueName"], false, consumer);

        return Task.CompletedTask;
    }

    private async Task HandleMessage(CheckoutHeaderDto checkoutHeaderDto)
    {
        var orderHeader = new OrderHeader
        {
            UserId = checkoutHeaderDto.UserId,
            FirstName = checkoutHeaderDto.FirstName,
            LastName = checkoutHeaderDto.LastName,
            OrderDetails = new List<OrderDetails>(),
            CardNumber = checkoutHeaderDto.CardNumber,
            CouponCode = checkoutHeaderDto.CouponCode,
            CVV = checkoutHeaderDto.CVV,
            DiscountTotal = checkoutHeaderDto.DiscountTotal,
            Email = checkoutHeaderDto.Email,
            ExpiryMonthYear = checkoutHeaderDto.ExpiryMonthYear,
            OrderTime = DateTime.Now,
            OrderTotal = checkoutHeaderDto.OrderTotal,
            PaymentStatus = false,
            Phone = checkoutHeaderDto.Phone,
            PickupDateTime = checkoutHeaderDto.PickupDateTime
        };

        foreach (var detailList in checkoutHeaderDto.CartDetails)
        {
            var orderDetails = new OrderDetails
            {
                ProductId = detailList.ProductId,
                ProductName = detailList.Product.Name,
                Price = detailList.Product.Price,
                Count = detailList.Count
            };
            orderHeader.CartTotalItems += detailList.Count;
            orderHeader.OrderDetails.Add(orderDetails);
        }

        await _orderRepository.AddOrder(orderHeader);

        var paymentRequestMessage = new PaymentRequestMessage
        {
            Name = orderHeader.FirstName + " " + orderHeader.LastName,
            CardNumber = orderHeader.CardNumber,
            CVV = orderHeader.CVV,
            ExpiryMonthYear = orderHeader.ExpiryMonthYear,
            OrderId = orderHeader.OrderHeaderId,
            OrderTotal = orderHeader.OrderTotal,
            Email = orderHeader.Email
        };

        try
        {
            _rabbitMqOrderMessageSender.SendMessage(paymentRequestMessage,
                _configuration["RabbitMqSettings:PaymentQueueName"]);
        }
        catch (Exception e)
        {
            throw;
        }
    }
}