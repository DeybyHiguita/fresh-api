using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.PurchaseDetail;

public class BatchUpdateRequest
{
    [Required]
    public List<BatchUpdateItem> Updates { get; set; } = [];
}

public class BatchUpdateItem
{
    [Required]
    public int Id { get; set; }

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
    public decimal UnitPrice { get; set; }

    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "El valor total no puede ser negativo")]
    public decimal TotalValue { get; set; }
}
