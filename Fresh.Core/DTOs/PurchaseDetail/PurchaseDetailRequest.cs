using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.PurchaseDetail;

public class PurchaseDetailRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0")]
    public decimal Quantity { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El valor total no puede ser negativo")]
    public decimal TotalValue { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio por unidad no puede ser negativo")]
    public decimal UnitPrice { get; set; }
}
