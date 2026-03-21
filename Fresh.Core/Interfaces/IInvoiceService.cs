using Fresh.Core.DTOs.Invoice;

namespace Fresh.Core.Interfaces;

public interface IInvoiceService
{
    Task<IEnumerable<InvoiceResponse>> GetAllAsync();
    Task<InvoiceResponse?> GetByIdAsync(int id);
    Task<InvoiceResponse?> GetByOrderIdAsync(int orderId);
    Task<InvoiceResponse> CreateAsync(InvoiceRequest request);
}