using Fresh.Core.DTOs.AppSettings;
using Fresh.Core.DTOs.Store;

namespace Fresh.Core.DTOs.Auth;

public class AuthResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public int StoreId { get; set; }
    public bool IsSuperAdmin { get; set; }
    public List<StoreSummary> Stores { get; set; } = [];
    public AppSettingsResponse Settings { get; set; } = new();
}
