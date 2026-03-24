using Fresh.Core.DTOs.Log;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class LogService : ILogService
{
    private readonly FreshDbContext _context;

    public LogService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<PagedLogResponse> GetAllAsync(LogFilterRequest filter)
    {
        var query = _context.Logs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.LogLevel))
            query = query.Where(l => l.LogLevel == filter.LogLevel);

        if (!string.IsNullOrWhiteSpace(filter.EntityName))
            query = query.Where(l => l.EntityName == filter.EntityName);

        if (!string.IsNullOrWhiteSpace(filter.UserId))
            query = query.Where(l => l.UserId == filter.UserId);

        if (!string.IsNullOrWhiteSpace(filter.TransactionStatus))
            query = query.Where(l => l.TransactionStatus == filter.TransactionStatus);

        if (!string.IsNullOrWhiteSpace(filter.TransactionId))
            query = query.Where(l => l.TransactionId == filter.TransactionId);

        if (!string.IsNullOrWhiteSpace(filter.HttpMethod))
        {
            var prefix = filter.HttpMethod.ToUpper() + " ";
            query = query.Where(l => l.Operation != null && l.Operation.StartsWith(prefix));
        }

        if (!string.IsNullOrWhiteSpace(filter.Operation))
            query = query.Where(l => l.Operation != null && l.Operation.Contains(filter.Operation));

        if (filter.From.HasValue)
            query = query.Where(l => l.LogDate >= filter.From.Value);

        if (filter.To.HasValue)
            query = query.Where(l => l.LogDate <= filter.To.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.LogDate)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(l => MapToResponse(l))
            .ToListAsync();

        return new PagedLogResponse
        {
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize),
            Items = items
        };
    }

    public async Task<LogResponse?> GetByIdAsync(long id)
    {
        var log = await _context.Logs.FindAsync(id);
        return log == null ? null : MapToResponse(log);
    }

    public async Task<LogResponse> CreateAsync(LogRequest request)
    {
        var log = new Log
        {
            TransactionId = request.TransactionId,
            CorrelationId = request.CorrelationId,
            LogDate = DateTimeOffset.UtcNow,
            LogLevel = request.LogLevel,
            Operation = request.Operation,
            EntityName = request.EntityName,
            EntityId = request.EntityId,
            UserId = request.UserId,
            TransactionStatus = request.TransactionStatus,
            DurationMs = request.DurationMs,
            Logger = request.Logger,
            Message = request.Message,
            Exception = request.Exception,
            TransactionData = request.TransactionData,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();

        return MapToResponse(log);
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var log = await _context.Logs.FindAsync(id);
        if (log == null) return false;

        _context.Logs.Remove(log);
        await _context.SaveChangesAsync();

        return true;
    }

    private static LogResponse MapToResponse(Log log) => new()
    {
        Id = log.Id,
        TransactionId = log.TransactionId,
        CorrelationId = log.CorrelationId,
        LogDate = log.LogDate,
        LogLevel = log.LogLevel,
        Operation = log.Operation,
        EntityName = log.EntityName,
        EntityId = log.EntityId,
        UserId = log.UserId,
        TransactionStatus = log.TransactionStatus,
        DurationMs = log.DurationMs,
        Logger = log.Logger,
        Message = log.Message,
        Exception = log.Exception,
        TransactionData = log.TransactionData,
        CreatedAt = log.CreatedAt
    };
}
