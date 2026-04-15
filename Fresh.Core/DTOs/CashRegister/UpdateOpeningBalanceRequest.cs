namespace Fresh.Core.DTOs.CashRegister;

/// <summary>
/// Permite a un administrador corregir el saldo inicial de una caja abierta.
/// </summary>
public class UpdateOpeningBalanceRequest
{
    public decimal OpeningBalance { get; set; }
}
