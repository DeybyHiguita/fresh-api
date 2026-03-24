using Fresh.Core.DTOs.Permission;

namespace Fresh.Core.Interfaces;

public interface IPermissionService
{
    Task<IEnumerable<UserPermissionsResponse>> GetAllUsersAsync();
    Task<UserPermissionsResponse> GetByUserIdAsync(int userId);
    Task<UserPermissionsResponse> UpdateAsync(int userId, UpdateUserPermissionsRequest request);
    Task InitializeAdminAsync(int userId);
}
