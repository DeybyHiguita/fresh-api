namespace Fresh.Core.DTOs.Sidebar;

public class SidebarNavConfigDto
{
    public List<SidebarGroupDto> Groups { get; set; } = [];
    public List<SidebarNavItemDto> CustomItems { get; set; } = [];
}

public class SidebarGroupDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<string> Routes { get; set; } = [];
}

public class SidebarNavItemDto
{
    public string Route { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
    public bool IsCustom { get; set; }
}
