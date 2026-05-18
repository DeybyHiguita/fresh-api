using System.ComponentModel.DataAnnotations;

namespace Fresh.Core.DTOs.Investment;

public class InvestmentRequest
{
    [Required]
    public int InvestorId { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a 0")]
    public decimal Amount { get; set; }
    
    [Required]
    public DateOnly InvestmentDate { get; set; }
    
    public string? Description { get; set; }

    /// <summary>Cuando se especifica, sobreescribe el estado actual (p.ej. Pendiente → Activo).</summary>
    public string? Status { get; set; }
}
