using System.Text.Json;
using Fresh.Core.DTOs.Sidebar;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class SidebarNavConfigService : ISidebarNavConfigService
{
    private const string ConfigKey = "sidebar_nav_config";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly FreshDbContext _context;

    public SidebarNavConfigService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<SidebarNavConfigDto?> GetAsync()
    {
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == ConfigKey);

        if (setting is null || string.IsNullOrWhiteSpace(setting.Value))
            return null;

        try
        {
            return JsonSerializer.Deserialize<SidebarNavConfigDto>(setting.Value, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public async Task<SidebarNavConfigDto> SaveAsync(SidebarNavConfigDto config)
    {
        var json    = JsonSerializer.Serialize(config, JsonOpts);
        var setting = await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == ConfigKey);

        if (setting is null)
        {
            _context.AppSettings.Add(new AppSetting
            {
                Key         = ConfigKey,
                Value       = json,
                Description = "Sidebar navigation layout (groups, order, custom items)",
                UpdatedAt   = DateTime.UtcNow,
            });
        }
        else
        {
            setting.Value     = json;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return config;
    }
}
