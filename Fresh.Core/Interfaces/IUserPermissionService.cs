using Fresh.Core.DTOs.AppPage;
using Fresh.Core.DTOs.UserPermission;

namespace Fresh.Core.Interfaces;

public interface IUserPermissionService
{
    Task<IEnumerable<UserPermissionResponse>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AppPageResponse>> GetMenuForUserAsync(int userId);
    Task<IEnumerable<UserPermissionResponse>> UpdateUserPermissionsAsync(int userId, IEnumerable<UserPermissionRequest> requests);
}