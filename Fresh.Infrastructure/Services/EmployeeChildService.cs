using Fresh.Core.DTOs.EmployeeChild;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class EmployeeChildService : IEmployeeChildService
{
    private readonly FreshDbContext _context;

    public EmployeeChildService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeChildResponse>> GetByEmployeeAsync(int employeeId)
    {
        var children = await _context.EmployeeChildren
            .Include(c => c.Employee)
            .Where(c => c.EmployeeId == employeeId)
            .OrderBy(c => c.FirstName)
            .ToListAsync();

        return children.Select(MapToResponse);
    }

    public async Task<EmployeeChildResponse?> GetByIdAsync(int id)
    {
        var child = await _context.EmployeeChildren
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);

        return child is not null ? MapToResponse(child) : null;
    }

    public async Task<EmployeeChildResponse> CreateAsync(int employeeId, EmployeeChildRequest request)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee is null)
            throw new InvalidOperationException("Empleado no encontrado");

        var child = new EmployeeChild
        {
            EmployeeId = employeeId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            BirthDate = request.BirthDate,
            Gender = request.Gender,
            IsStudent = request.IsStudent,
            SchoolName = request.SchoolName,
            HasDisability = request.HasDisability,
            DisabilityType = request.DisabilityType,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeChildren.Add(child);
        await _context.SaveChangesAsync();

        await _context.Entry(child).Reference(c => c.Employee).LoadAsync();

        return MapToResponse(child);
    }

    public async Task<EmployeeChildResponse?> UpdateAsync(int id, EmployeeChildRequest request)
    {
        var child = await _context.EmployeeChildren
            .Include(c => c.Employee)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (child is null) return null;

        child.FirstName = request.FirstName;
        child.LastName = request.LastName;
        child.DocumentType = request.DocumentType;
        child.DocumentNumber = request.DocumentNumber;
        child.BirthDate = request.BirthDate;
        child.Gender = request.Gender;
        child.IsStudent = request.IsStudent;
        child.SchoolName = request.SchoolName;
        child.HasDisability = request.HasDisability;
        child.DisabilityType = request.DisabilityType;
        child.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(child);
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var child = await _context.EmployeeChildren.FindAsync(id);
        if (child is null) return false;

        child.IsActive = !child.IsActive;
        child.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var child = await _context.EmployeeChildren.FindAsync(id);
        if (child is null) return false;

        _context.EmployeeChildren.Remove(child);
        await _context.SaveChangesAsync();

        return true;
    }

    private EmployeeChildResponse MapToResponse(EmployeeChild child)
    {
        var documentsCount = _context.EmployeeChildDocuments.Count(d => d.ChildId == child.Id);

        return new EmployeeChildResponse
        {
            Id = child.Id,
            EmployeeId = child.EmployeeId,
            EmployeeName = child.Employee?.FullName ?? "",
            FirstName = child.FirstName,
            LastName = child.LastName,
            FullName = child.FullName,
            DocumentType = child.DocumentType,
            DocumentNumber = child.DocumentNumber,
            BirthDate = child.BirthDate,
            Age = child.Age,
            Gender = child.Gender,
            IsStudent = child.IsStudent,
            SchoolName = child.SchoolName,
            HasDisability = child.HasDisability,
            DisabilityType = child.DisabilityType,
            IsActive = child.IsActive,
            DocumentsCount = documentsCount,
            CreatedAt = child.CreatedAt
        };
    }
}
