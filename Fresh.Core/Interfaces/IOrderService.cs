using Fresh.Core.DTOs.Order;

namespace Fresh.Core.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderResponse>> GetAllAsync();
    Task<OrderResponse?> GetByIdAsync(int id);
    Task<OrderResponse> CreateAsync(OrderRequest request);
    Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus, string? notes = null);
    Task<OrderResponse?> UpdatePaymentMethodAsync(int id, string paymentMethod);
    Task<OrderResponse?> UpdateItemsAsync(int id, List<OrderItemRequest> items);
}
