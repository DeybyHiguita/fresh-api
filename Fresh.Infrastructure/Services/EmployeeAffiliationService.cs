using Fresh.Core.DTOs.EmployeeAffiliation;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class EmployeeAffiliationService : IEmployeeAffiliationService
{
    private readonly FreshDbContext _context;

    public EmployeeAffiliationService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeAffiliationResponse>> GetByEmployeeAsync(int employeeId)
    {
        var affiliations = await _context.EmployeeAffiliations
            .Include(a => a.Employee)
            .Where(a => a.EmployeeId == employeeId)
            .OrderBy(a => a.AffiliationType)
            .ToListAsync();

        return affiliations.Select(MapToResponse);
    }

    public async Task<EmployeeAffiliationResponse?> GetByIdAsync(int id)
    {
        var affiliation = await _context.EmployeeAffiliations
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.Id == id);

        return affiliation is not null ? MapToResponse(affiliation) : null;
    }

    public async Task<EmployeeAffiliationResponse?> GetByTypeAsync(int employeeId, string affiliationType)
    {
        var affiliation = await _context.EmployeeAffiliations
            .Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AffiliationType == affiliationType);

        return affiliation is not null ? MapToResponse(affiliation) : null;
    }

    public async Task<EmployeeAffiliationResponse> CreateOrUpdateAsync(int employeeId, EmployeeAffiliationRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee is null)
            throw new InvalidOperationException("Empleado no encontrado");

        // Buscar si ya existe una afiliación de este tipo
        var existing = await _context.EmployeeAffiliations
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.AffiliationType == request.AffiliationType);

        if (existing is not null)
        {
            // Actualizar existente
            existing.EntityName = request.EntityName;
            existing.EntityCode = request.EntityCode;
            existing.Status = request.Status;
            existing.AffiliationDate = request.AffiliationDate;
            existing.EffectiveDate = request.EffectiveDate;
            existing.PlanType = request.PlanType;
            existing.CoveragePercentage = request.CoveragePercentage;
            existing.ContributionBase = request.ContributionBase;
            existing.Notes = request.Notes;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            await _context.Entry(existing).Reference(a => a.Employee).LoadAsync();

            return MapToResponse(existing);
        }
        else
        {
            // Crear nueva
            var affiliation = new EmployeeAffiliation
            {
                EmployeeId = employeeId,
                AffiliationType = request.AffiliationType,
                EntityName = request.EntityName,
                EntityCode = request.EntityCode,
                Status = request.Status,
                AffiliationDate = request.AffiliationDate,
                EffectiveDate = request.EffectiveDate,
                PlanType = request.PlanType,
                CoveragePercentage = request.CoveragePercentage,
                ContributionBase = request.ContributionBase,
                Notes = request.Notes,
                CreatedAt = DateTime.UtcNow
            };

            _context.EmployeeAffiliations.Add(affiliation);
            await _context.SaveChangesAsync();

            await _context.Entry(affiliation).Reference(a => a.Employee).LoadAsync();

            return MapToResponse(affiliation);
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var affiliation = await _context.EmployeeAffiliations.FindAsync(id);
        if (affiliation is null) return false;

        _context.EmployeeAffiliations.Remove(affiliation);
        await _context.SaveChangesAsync();

        return true;
    }

    private static EmployeeAffiliationResponse MapToResponse(EmployeeAffiliation affiliation) => new()
    {
        Id = affiliation.Id,
        EmployeeId = affiliation.EmployeeId,
        EmployeeName = affiliation.Employee?.FullName ?? "",
        AffiliationType = affiliation.AffiliationType,
        AffiliationTypeDisplay = affiliation.AffiliationTypeDisplay,
        EntityName = affiliation.EntityName,
        EntityCode = affiliation.EntityCode,
        Status = affiliation.Status,
        StatusDisplay = affiliation.StatusDisplay,
        AffiliationDate = affiliation.AffiliationDate,
        EffectiveDate = affiliation.EffectiveDate,
        PlanType = affiliation.PlanType,
        CoveragePercentage = affiliation.CoveragePercentage,
        ContributionBase = affiliation.ContributionBase,
        Notes = affiliation.Notes,
        CreatedAt = affiliation.CreatedAt,
        UpdatedAt = affiliation.UpdatedAt
    };
}
