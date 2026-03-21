using Fresh.Core.DTOs.Invoice;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class InvoiceService : IInvoiceService
{
    private readonly FreshDbContext _context;

    public InvoiceService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<InvoiceResponse>> GetAllAsync()
    {
        var invoices = await _context.Invoices
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
            
        return invoices.Select(MapToResponse);
    }

    public async Task<InvoiceResponse?> GetByIdAsync(int id)
    {
        var invoice = await _context.Invoices.FindAsync(id);
        return invoice == null ? null : MapToResponse(invoice);
    }

    public async Task<InvoiceResponse?> GetByOrderIdAsync(int orderId)
    {
        var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.OrderId == orderId);
        return invoice == null ? null : MapToResponse(invoice);
    }

    public async Task<InvoiceResponse> CreateAsync(InvoiceRequest request)
    {
        // 1. Validar que la orden exista
        var order = await _context.Orders.FindAsync(request.OrderId);
        if (order == null)
            throw new KeyNotFoundException($"La orden con ID {request.OrderId} no existe.");

        // 2. Validar que no exista ya una factura para esta orden
        var existingInvoice = await _context.Invoices.AnyAsync(i => i.OrderId == request.OrderId);
        if (existingInvoice)
            throw new InvalidOperationException($"La orden {request.OrderId} ya ha sido facturada.");

        // 3. Cálculos financieros
        decimal totalAmount = order.Subtotal - order.Discount + request.TaxAmount;
        decimal changeAmount = 0m;

        if (request.PaymentMethod.Equals("Efectivo", StringComparison.OrdinalIgnoreCase))
        {
            if (request.CashTendered < totalAmount)
                throw new InvalidOperationException("El efectivo recibido no puede ser menor al total de la factura.");
                
            changeAmount = request.CashTendered - totalAmount;
        }

        // 4. Crear la entidad con número provisional; se actualiza tras obtener el ID
        var invoice = new Invoice
        {
            OrderId = request.OrderId,
            InvoiceNumber = "POS-TEMP",
            CustomerDocument = request.CustomerDocument,
            // Si el request no trae nombre, tomamos el de la orden
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? order.CustomerName : request.CustomerName,
            Subtotal = order.Subtotal,
            TaxAmount = request.TaxAmount,
            DiscountAmount = order.Discount,
            TotalAmount = totalAmount,
            PaymentMethod = request.PaymentMethod,
            CashTendered = request.CashTendered,
            ChangeAmount = changeAmount,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync();

        // 5. Actualizar número de factura usando el ID generado
        invoice.InvoiceNumber = $"POS-{invoice.Id:D5}";
        await _context.SaveChangesAsync();

        return MapToResponse(invoice);
    }

    private static InvoiceResponse MapToResponse(Invoice i) => new()
    {
        Id = i.Id,
        OrderId = i.OrderId,
        InvoiceNumber = i.InvoiceNumber,
        CustomerDocument = i.CustomerDocument,
        CustomerName = i.CustomerName,
        Subtotal = i.Subtotal,
        TaxAmount = i.TaxAmount,
        DiscountAmount = i.DiscountAmount,
        TotalAmount = i.TotalAmount,
        PaymentMethod = i.PaymentMethod,
        CashTendered = i.CashTendered,
        ChangeAmount = i.ChangeAmount,
        CreatedAt = i.CreatedAt
    };
}