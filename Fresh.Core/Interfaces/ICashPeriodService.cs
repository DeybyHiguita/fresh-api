using Fresh.Core.DTOs.CashPeriod;

namespace Fresh.Core.Interfaces;

public interface ICashPeriodService
{
    Task<IEnumerable<CashPeriodResponse>> GetAllAsync();
    Task<CashPeriodResponse?> GetByIdAsync(int id);
    Task<CashPeriodResponse> CreateAsync(CashPeriodRequest request);
    Task<CashPeriodResponse?> ClosePeriodAsync(int id);
}
