using Fresh.Core.DTOs.Order;

namespace Fresh.Core.Interfaces;

public interface IOrderService
{
    Task<IEnumerable<OrderResponse>> GetAllAsync(int storeId = 0);
    Task<OrderResponse?> GetByIdAsync(int id);
    Task<OrderResponse> CreateAsync(OrderRequest request, int storeId = 0);
    Task<OrderResponse?> UpdateStatusAsync(int id, string newStatus, string? notes = null);
    Task<OrderResponse?> UpdatePaymentMethodAsync(int id, string paymentMethod, int? customerId = null);
    Task<OrderResponse?> UpdateItemsAsync(int id, List<OrderItemRequest> items);
    
    /// <summary>
    /// Busca órdenes pendientes con pago por transferencia que coincidan con el monto.
    /// </summary>
    Task<List<OrderMatchDto>> FindPendingTransferOrdersAsync(decimal amount);
}
