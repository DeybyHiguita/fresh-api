using Fresh.Core.DTOs.Equipment;

namespace Fresh.Core.Interfaces;

public interface IEquipmentService
{
    Task<IEnumerable<EquipmentResponse>> GetAllAsync(string? status = null);
    Task<EquipmentResponse?> GetByIdAsync(int id);
    Task<EquipmentResponse> CreateAsync(EquipmentRequest request);
    Task<EquipmentResponse?> UpdateAsync(int id, EquipmentRequest request);
    Task<EquipmentResponse?> UpdateStatusAsync(int id, string newStatus);
}
