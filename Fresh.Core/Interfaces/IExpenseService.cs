using Fresh.Core.DTOs.Expense;

namespace Fresh.Core.Interfaces;

public interface IExpenseService
{
    Task<IEnumerable<ExpenseResponse>> GetAllAsync();
    Task<ExpenseResponse?> GetByIdAsync(int id);
    Task<ExpenseResponse> CreateAsync(ExpenseRequest request);
    Task<ExpenseResponse?> UpdateAsync(int id, ExpenseRequest request);
    Task<IEnumerable<ExpenseResponse>> GetByMonthYearAsync(int month, int year);
}