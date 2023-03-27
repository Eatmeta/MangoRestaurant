using Mango.MessageBus;

namespace Mango.Services.PaymentAPI.RabbitMqSender;

public interface IRabbitMqPaymentMessageSender
{
    void SendMessage(BaseMessage baseMessage);
}