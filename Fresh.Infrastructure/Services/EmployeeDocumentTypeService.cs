using Fresh.Core.DTOs.EmployeeDocumentType;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class EmployeeDocumentTypeService : IEmployeeDocumentTypeService
{
    private readonly FreshDbContext _context;

    public EmployeeDocumentTypeService(FreshDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EmployeeDocumentTypeResponse>> GetAllAsync()
    {
        var types = await _context.EmployeeDocumentTypes
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .ToListAsync();

        return types.Select(MapToResponse);
    }

    public async Task<IEnumerable<EmployeeDocumentTypeResponse>> GetActiveAsync()
    {
        var types = await _context.EmployeeDocumentTypes
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .ToListAsync();

        return types.Select(MapToResponse);
    }

    public async Task<IEnumerable<EmployeeDocumentTypeResponse>> GetByAppliesToAsync(string appliesTo)
    {
        var types = await _context.EmployeeDocumentTypes
            .Where(t => t.IsActive && t.AppliesTo == appliesTo)
            .OrderBy(t => t.SortOrder).ThenBy(t => t.Name)
            .ToListAsync();

        return types.Select(MapToResponse);
    }

    public async Task<EmployeeDocumentTypeResponse?> GetByIdAsync(int id)
    {
        var type = await _context.EmployeeDocumentTypes.FindAsync(id);
        return type is not null ? MapToResponse(type) : null;
    }

    public async Task<EmployeeDocumentTypeResponse> CreateAsync(EmployeeDocumentTypeRequest request)
    {
        var exists = await _context.EmployeeDocumentTypes
            .AnyAsync(t => t.Name.ToLower() == request.Name.ToLower() && t.AppliesTo == request.AppliesTo);
        
        if (exists)
            throw new InvalidOperationException("Ya existe un tipo de documento con ese nombre");

        var type = new EmployeeDocumentType
        {
            Name = request.Name,
            Description = request.Description,
            IsRequired = request.IsRequired,
            AppliesTo = request.AppliesTo,
            MaxFileSize = request.MaxFileSize,
            AllowedFormats = request.AllowedFormats,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeDocumentTypes.Add(type);
        await _context.SaveChangesAsync();

        return MapToResponse(type);
    }

    public async Task<EmployeeDocumentTypeResponse?> UpdateAsync(int id, EmployeeDocumentTypeRequest request)
    {
        var type = await _context.EmployeeDocumentTypes.FindAsync(id);
        if (type is null) return null;

        // Validar nombre único
        if (type.Name.ToLower() != request.Name.ToLower())
        {
            var exists = await _context.EmployeeDocumentTypes
                .AnyAsync(t => t.Id != id && t.Name.ToLower() == request.Name.ToLower() && t.AppliesTo == request.AppliesTo);
            
            if (exists)
                throw new InvalidOperationException("Ya existe un tipo de documento con ese nombre");
        }

        type.Name = request.Name;
        type.Description = request.Description;
        type.IsRequired = request.IsRequired;
        type.AppliesTo = request.AppliesTo;
        type.MaxFileSize = request.MaxFileSize;
        type.AllowedFormats = request.AllowedFormats;
        type.SortOrder = request.SortOrder;
        type.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToResponse(type);
    }

    public async Task<bool> ToggleActiveAsync(int id)
    {
        var type = await _context.EmployeeDocumentTypes.FindAsync(id);
        if (type is null) return false;

        type.IsActive = !type.IsActive;
        type.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var type = await _context.EmployeeDocumentTypes.FindAsync(id);
        if (type is null) return false;

        // Verificar que no tenga documentos asociados
        var hasDocuments = await _context.EmployeeDocuments.AnyAsync(d => d.DocumentTypeId == id);
        var hasChildDocuments = await _context.EmployeeChildDocuments.AnyAsync(d => d.DocumentTypeId == id);
        
        if (hasDocuments || hasChildDocuments)
            throw new InvalidOperationException("No se puede eliminar porque hay documentos asociados a este tipo");

        _context.EmployeeDocumentTypes.Remove(type);
        await _context.SaveChangesAsync();

        return true;
    }

    private static EmployeeDocumentTypeResponse MapToResponse(EmployeeDocumentType type) => new()
    {
        Id = type.Id,
        Name = type.Name,
        Description = type.Description,
        IsRequired = type.IsRequired,
        AppliesTo = type.AppliesTo,
        MaxFileSize = type.MaxFileSize,
        AllowedFormats = type.AllowedFormats,
        SortOrder = type.SortOrder,
        IsActive = type.IsActive,
        CreatedAt = type.CreatedAt
    };
}
