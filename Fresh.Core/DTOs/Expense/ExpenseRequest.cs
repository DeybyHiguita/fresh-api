using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Expense;

public class ExpenseRequest
{
    [Required]
    public int ExpenseTypeId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El valor pagado debe ser mayor a cero")]
    public decimal AmountPaid { get; set; }

    [Required]
    public DateOnly PaymentDate { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentMethod { get; set; } = "Efectivo";

    public string? Notes { get; set; }

    // Opcional: vincular con un lote de compra
    public int? PurchaseBatchId { get; set; }
}