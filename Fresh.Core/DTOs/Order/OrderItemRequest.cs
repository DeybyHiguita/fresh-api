using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Order;

public class OrderItemRequest
{
    [Required]
    public int MenuItemId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [MaxLength(255)]
    public string? ItemNotes { get; set; }
}

public class OrderRequest
{
    [Required]
    public int UserId { get; set; }

    [MaxLength(150)]
    public string? CustomerName { get; set; }

    [MaxLength(20)]
    public string? CustomerPhone { get; set; }

    [Range(0, double.MaxValue)]
    public decimal Discount { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderType { get; set; } = "Local";

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Efectivo";

    public string? Notes { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "La orden debe tener al menos un producto.")]
    public List<OrderItemRequest> Items { get; set; } = [];
}
