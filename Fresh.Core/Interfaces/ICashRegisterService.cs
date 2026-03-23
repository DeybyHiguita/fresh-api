using Fresh.Core.DTOs.CashRegister;

namespace Fresh.Core.Interfaces;

public interface ICashRegisterService
{
    Task<IEnumerable<CashRegisterResponse>> GetAllAsync(int? periodId = null);
    Task<CashRegisterResponse?> GetByIdAsync(int id);
    Task<CashSystemTotalsResponse?> GetSystemTotalsAsync(int id);
    Task<CashRegisterResponse> OpenRegisterAsync(OpenCashRegisterRequest request);
    Task<CashRegisterResponse?> CloseRegisterAsync(int id, CloseCashRegisterRequest request);
}
