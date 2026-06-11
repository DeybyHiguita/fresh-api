using Fresh.Core.DTOs.CashPeriod;

namespace Fresh.Core.Interfaces;

public interface ICashPeriodService
{
    Task<IEnumerable<CashPeriodResponse>> GetAllAsync(int storeId = 0);
    Task<CashPeriodResponse?> GetByIdAsync(int id);
    Task<CashPeriodResponse> CreateAsync(CashPeriodRequest request, int storeId);
    Task<CashPeriodResponse?> ClosePeriodAsync(int id);
}
