using Fresh.Core.DTOs.Expense;

namespace Fresh.Core.Interfaces;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseResponse>> GetAllAsync(int storeId = 0);
    Task<ExpenseResponse?> GetByIdAsync(int id);
    Task<ExpenseResponse> CreateAsync(ExpenseRequest request, int storeId);
    Task<ExpenseResponse?> UpdateAsync(int id, ExpenseRequest request);
    Task<IEnumerable<ExpenseResponse>> GetByMonthYearAsync(int month, int year, int storeId = 0);
}