    

namespace eShop.Ordering.API.Application.Commands;

    public record CompleteOrderCommand(int OrderId) : IRequest<bool>;

