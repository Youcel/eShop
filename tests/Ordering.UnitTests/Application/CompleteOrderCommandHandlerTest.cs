using System;
using System.Threading;
using System.Threading.Tasks;
using eShop.EventBus.Abstractions;
using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using Moq;
using Ordering.Application.Commands;
using Ordering.Application.IntegrationEvents.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using eShop.EventBus.Events;

namespace eShop.Ordering.UnitTests.Application
{
    [TestClass]
    public class CompleteOrderCommandHandlerTest
    {
        private readonly Mock<IOrderRepository> _orderRepositoryMock;
        private readonly Mock<IEventBus> _eventBusMock;
        private readonly CompleteOrderCommandHandler _handler;

        public CompleteOrderCommandHandlerTest()
        {
            _orderRepositoryMock = new Mock<IOrderRepository>();
            _eventBusMock = new Mock<IEventBus>();

            
            _handler = new CompleteOrderCommandHandler(_orderRepositoryMock.Object, _eventBusMock.Object);
        }

        [TestMethod]
        public async Task Handle_ValidOrder_CompletesSuccessfully()
        {
            // Arrange
            var order = new Order("userId", "userName",
                                  new Address("street", "city", "state", "country", "zipcode"),
                                  1, "1234", "5678", "Ali Veli", DateTime.UtcNow.AddYears(1));
            order.BuyerId = 2;

            // Log: Order nesnesi bilgisi
            Console.WriteLine($"[Test Log] Order Created: {order.Id}, BuyerId: {order.BuyerId}");

            var command = new CompleteOrderCommand(1);

            // Mock ayarları: GetAsync
            _orderRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<int>())).ReturnsAsync(order)
                .Callback<int>(orderId => Console.WriteLine($"[Test Log] GetAsync Called with OrderId: {orderId}"));

            // Mock ayarları: UnitOfWork
            _orderRepositoryMock.Setup(repo => repo.UnitOfWork.SaveEntitiesAsync(It.IsAny<CancellationToken>()))
                                .ReturnsAsync(true)
                                .Callback<CancellationToken>(token => Console.WriteLine("[Test Log] SaveEntitiesAsync Called"));

            // Mock ayarları: EventBus
            _eventBusMock.Setup(bus => bus.PublishAsync(It.IsAny<IntegrationEvent>()))
                         .Returns(Task.CompletedTask)
                         .Callback<IntegrationEvent>(e =>
                         {
                             if (e is OrderCompletedIntegrationEvent orderCompletedEvent)
                             {
                                 Console.WriteLine($"[Test Log] PublishAsync Called with Event: OrderId={orderCompletedEvent.OrderId}, BuyerId={orderCompletedEvent.BuyerId}");
                             }
                         });

            // Act
            Console.WriteLine("[Test Log] Starting Handler...");
            var result = await _handler.Handle(command, CancellationToken.None);
            Console.WriteLine($"[Test Log] Handler Completed. Result: {result}");

            // Assert
            Assert.IsTrue(result);
            _eventBusMock.Verify(bus => bus.PublishAsync(It.IsAny<OrderCompletedIntegrationEvent>()), Times.Once);
        }

        [TestMethod]
        public async Task Handle_OrderNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var command = new CompleteOrderCommand(1);
            _orderRepositoryMock.Setup(repo => repo.GetAsync(It.IsAny<int>()))
                .ReturnsAsync((Order)null)
                .Callback<int>(orderId => Console.WriteLine($"[Test Log] GetAsync Called with OrderId: {orderId} (Result: null)"));

            // Act & Assert
            Console.WriteLine("[Test Log] Starting Handler for OrderNotFound...");
            await Assert.ThrowsExceptionAsync<KeyNotFoundException>(() => _handler.Handle(command, CancellationToken.None));
        }
    }
}
