using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Ordering.Application.IntegrationEvents.Events;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.EventBus.Abstractions;

namespace Ordering.Application.Commands
{
    public class CompleteOrderCommandHandler : IRequestHandler<CompleteOrderCommand, bool>
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IEventBus _eventBus;

        public CompleteOrderCommandHandler(IOrderRepository orderRepository, IEventBus eventBus)
        {
            _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        public async Task<bool> Handle(CompleteOrderCommand request, CancellationToken cancellationToken)
        {
            
            var order = await _orderRepository.GetAsync(request.OrderId);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order with ID {request.OrderId} not found.");
            }

           
            if (!order.BuyerId.HasValue)
            {
                throw new InvalidOperationException($"Order {order.Id} has no BuyerId.");
            }

            
            order.SetStatusToComplete();

            
            await _orderRepository.UnitOfWork.SaveEntitiesAsync(cancellationToken);

            // OrderCompletedIntegrationEvent'i fırlat
            var integrationEvent = new OrderCompletedIntegrationEvent(
                order.Id,
                order.BuyerId.Value, 
                DateTime.UtcNow
            );

            await _eventBus.PublishAsync(integrationEvent);

            return true;
        }
    }

    // Idempotency için identified handler
    public class CompleteOrderIdentifiedCommandHandler : IdentifiedCommandHandler<CompleteOrderCommand, bool>
    {
        public CompleteOrderIdentifiedCommandHandler(
             IMediator mediator,
        IRequestManager requestManager,
        ILogger<IdentifiedCommandHandler<CompleteOrderCommand, bool>> logger)
        : base(mediator, requestManager, logger)
        {
        }

        protected override bool CreateResultForDuplicateRequest()
        {
            return true; 
        }
    }
}
