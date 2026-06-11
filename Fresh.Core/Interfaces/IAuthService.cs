using Fresh.Core.DTOs.Auth;

namespace Fresh.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task RegisterAsync(RegisterRequest request);
    Task<AuthResponse> SwitchStoreAsync(int userId, int storeId);
}
