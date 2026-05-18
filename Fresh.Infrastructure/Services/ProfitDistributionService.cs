using Fresh.Core.DTOs.Investment;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class ProfitDistributionService : IProfitDistributionService
{
    private readonly FreshDbContext _context;

    public ProfitDistributionService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ProfitDistributionResponse>> GetAllAsync()
    {
        var items = await _context.ProfitDistributions
            .Include(d => d.CreatedBy)
            .Include(d => d.Shares)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return items.Select(MapToResponse);
    }

    public async Task<ProfitDistributionResponse?> GetByIdAsync(int id)
    {
        var dist = await _context.ProfitDistributions
            .Include(d => d.CreatedBy)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.Id == id);

        return dist == null ? null : MapToResponse(dist);
    }

    public async Task<ProfitDistributionResponse> CreateAsync(int userId, ProfitDistributionRequest request)
    {
        var creator = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException($"Usuario {userId} no encontrado.");

        var distribution = new ProfitDistribution
        {
            TotalProfit = request.TotalProfit,
            Notes = request.Notes,
            CreatedById = userId,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        foreach (var s in request.Shares)
        {
            distribution.Shares.Add(new ProfitDistributionShare
            {
                InvestorId = s.InvestorId,
                InvestorName = s.InvestorName,
                InvestedCapital = s.InvestedCapital,
                ParticipationPct = s.ParticipationPct,
                ShareAmount = s.ShareAmount,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }

        _context.ProfitDistributions.Add(distribution);
        await _context.SaveChangesAsync();

        distribution.CreatedBy = creator;
        return MapToResponse(distribution);
    }

    public async Task DeleteAsync(int id)
    {
        var dist = await _context.ProfitDistributions.FindAsync(id)
            ?? throw new KeyNotFoundException($"Distribución {id} no encontrada.");

        _context.ProfitDistributions.Remove(dist);
        await _context.SaveChangesAsync();
    }

    private static ProfitDistributionResponse MapToResponse(ProfitDistribution d) => new()
    {
        Id = d.Id,
        TotalProfit = d.TotalProfit,
        Notes = d.Notes,
        CreatedById = d.CreatedById,
        CreatedByName = d.CreatedBy?.Name ?? string.Empty,
        CreatedAt = d.CreatedAt,
        Shares = d.Shares.Select(s => new ProfitDistributionShareResponse
        {
            Id = s.Id,
            InvestorId = s.InvestorId,
            InvestorName = s.InvestorName,
            InvestedCapital = s.InvestedCapital,
            ParticipationPct = s.ParticipationPct,
            ShareAmount = s.ShareAmount,
        }).ToList(),
    };
}
