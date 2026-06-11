using Fresh.Core.DTOs.Sidebar;

namespace Fresh.Core.Interfaces;

public interface ISidebarNavConfigService
{
    Task<SidebarNavConfigDto?> GetAsync();
    Task<SidebarNavConfigDto> SaveAsync(SidebarNavConfigDto config);
}
