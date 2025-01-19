namespace Ordering.Application.IntegrationEvents.Events
{
    public record OrderCompletedIntegrationEvent(int OrderId, int BuyerId, DateTime CompletionDate)
        : eShop.EventBus.Events.IntegrationEvent;
}
