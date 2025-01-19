using MediatR;

namespace eShop.Ordering.Domain.Events;

public class OrderStatusChangedToCompleteDomainEvent : INotification
{
    public int OrderId { get; }

    public OrderStatusChangedToCompleteDomainEvent(int orderId)
    {
        OrderId = orderId;
    }
}
