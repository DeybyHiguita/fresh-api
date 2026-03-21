using Fresh.Core.DTOs.ExpenseType;

namespace Fresh.Core.Interfaces;

public interface IExpenseTypeService
{
    Task<IEnumerable<ExpenseTypeResponse>> GetAllAsync(bool onlyActive = true);
    Task<ExpenseTypeResponse?> GetByIdAsync(int id);
    Task<ExpenseTypeResponse> CreateAsync(ExpenseTypeRequest request);
    Task<ExpenseTypeResponse?> UpdateAsync(int id, ExpenseTypeRequest request);
    Task<bool> DeleteAsync(int id); // Será Soft Delete
}