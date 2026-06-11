using Fresh.Core.DTOs.Store;

namespace Fresh.Core.Interfaces;

public interface IStoreService
{
    Task<IEnumerable<StoreResponse>> GetAllAsync();
    Task<StoreResponse?> GetByIdAsync(int id);
    Task<StoreResponse> CreateAsync(StoreRequest request);
    Task<StoreResponse?> UpdateAsync(int id, StoreRequest request);
    Task<bool> DeleteAsync(int id);
    Task AddUserToStoreAsync(int storeId, int userId, bool isDefault = false);
    Task RemoveUserFromStoreAsync(int storeId, int userId);
    Task<IEnumerable<StoreSummary>> GetUserStoresAsync(int userId);
    Task<IEnumerable<StoreUserResponse>> GetStoreUsersAsync(int storeId);
}
