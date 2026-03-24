using Fresh.Core.DTOs.User;

namespace Fresh.Core.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponse>> GetAllAsync(bool onlyActive = true);
    Task<UserResponse?> GetByIdAsync(int id);
    Task<UserResponse?> UpdateAsync(int id, UserUpdateRequest request);
}
