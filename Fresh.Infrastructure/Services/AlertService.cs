using Fresh.Core.DTOs.Alert;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class AlertService : IAlertService
{
    private readonly FreshDbContext _context;

    public AlertService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AlertResponse>> GetAllAsync()
    {
        var alerts = await _context.AppAlerts
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
        return alerts.Select(Map);
    }

    public async Task<AlertResponse> CreateAsync(AlertRequest request, int userId)
    {
        var alert = new AppAlert
        {
            Title           = request.Title.Trim(),
            Message         = request.Message.Trim(),
            AlertType       = request.AlertType,
            CreatedByUserId = userId,
            CreatedAt       = DateTime.UtcNow,
            SendCount       = 0,
        };
        _context.AppAlerts.Add(alert);
        await _context.SaveChangesAsync();
        return Map(alert);
    }

    public async Task<AlertResponse?> UpdateAsync(int id, AlertRequest request)
    {
        var alert = await _context.AppAlerts.FindAsync(id);
        if (alert == null) return null;

        alert.Title     = request.Title.Trim();
        alert.Message   = request.Message.Trim();
        alert.AlertType = request.AlertType;
        await _context.SaveChangesAsync();
        return Map(alert);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var alert = await _context.AppAlerts.FindAsync(id);
        if (alert == null) return false;
        _context.AppAlerts.Remove(alert);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<AlertResponse?> MarkSentAsync(int id)
    {
        var alert = await _context.AppAlerts.FindAsync(id);
        if (alert == null) return null;
        alert.LastSentAt = DateTime.UtcNow;
        alert.SendCount++;
        await _context.SaveChangesAsync();
        return Map(alert);
    }

    private static AlertResponse Map(AppAlert a) => new()
    {
        Id              = a.Id,
        Title           = a.Title,
        Message         = a.Message,
        AlertType       = a.AlertType,
        CreatedByUserId = a.CreatedByUserId,
        CreatedAt       = a.CreatedAt,
        LastSentAt      = a.LastSentAt,
        SendCount       = a.SendCount,
    };
}
