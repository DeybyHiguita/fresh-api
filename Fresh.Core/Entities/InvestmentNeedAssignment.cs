namespace Fresh.Core.Entities;

public class InvestmentNeedAssignment
{
    public int Id { get; set; }
    public int NeedId { get; set; }
    public int InvestorId { get; set; }
    public decimal SuggestedAmount { get; set; }
    public string Status { get; set; } = "Pendiente"; // Pendiente | Aprobado | Cancelado
    public string? Notes { get; set; }

    // Se rellena al aprobar la solicitud
    public int? InvestmentId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navegación
    public InvestmentNeed? Need { get; set; }
    public User? Investor { get; set; }
    public Investment? Investment { get; set; }
}
