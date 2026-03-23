using Fresh.Core.DTOs.EquipmentCategory;

namespace Fresh.Core.Interfaces;

public interface IEquipmentCategoryService
{
    Task<IEnumerable<EquipmentCategoryResponse>> GetAllAsync(bool onlyActive = true);
    Task<EquipmentCategoryResponse?> GetByIdAsync(int id);
    Task<EquipmentCategoryResponse> CreateAsync(EquipmentCategoryRequest request);
    Task<EquipmentCategoryResponse?> UpdateAsync(int id, EquipmentCategoryRequest request);
    Task<bool> DeleteAsync(int id);
}
