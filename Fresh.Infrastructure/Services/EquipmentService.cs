using Fresh.Core.DTOs.Equipment;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Fresh.Infrastructure.Services;

public class EquipmentService : IEquipmentService
{
    private readonly FreshDbContext _context;

    public EquipmentService(FreshDbContext context) { _context = context; }

    public async Task<IEnumerable<EquipmentResponse>> GetAllAsync(string? status = null)
    {
        var query = _context.Equipments.Include(e => e.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status.ToLower() == status.ToLower());

        var equipments = await query.OrderBy(e => e.Name).ToListAsync();
        return equipments.Select(MapToResponse);
    }

    public async Task<EquipmentResponse?> GetByIdAsync(int id)
    {
        var equipment = await _context.Equipments
            .Include(e => e.Category)
            .FirstOrDefaultAsync(e => e.Id == id);
        return equipment == null ? null : MapToResponse(equipment);
    }

    public async Task<EquipmentResponse> CreateAsync(EquipmentRequest request)
    {
        var categoryExists = await _context.EquipmentCategories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) throw new KeyNotFoundException("La categoría seleccionada no existe.");

        var equipment = new Equipment
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Brand = request.Brand,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            PurchaseDate = request.PurchaseDate,
            PurchasePrice = request.PurchasePrice,
            Status = request.Status,
            Location = request.Location,
            Notes = request.Notes,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Equipments.Add(equipment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(equipment.Id) ?? throw new Exception("Error al mapear equipo.");
    }

    public async Task<EquipmentResponse?> UpdateAsync(int id, EquipmentRequest request)
    {
        var equipment = await _context.Equipments.FindAsync(id);
        if (equipment == null) return null;

        var categoryExists = await _context.EquipmentCategories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) throw new KeyNotFoundException("La categoría seleccionada no existe.");

        equipment.CategoryId = request.CategoryId;
        equipment.Name = request.Name;
        equipment.Brand = request.Brand;
        equipment.Model = request.Model;
        equipment.SerialNumber = request.SerialNumber;
        equipment.PurchaseDate = request.PurchaseDate;
        equipment.PurchasePrice = request.PurchasePrice;
        equipment.Status = request.Status;
        equipment.Location = request.Location;
        equipment.Notes = request.Notes;
        equipment.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Equipments.Update(equipment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(equipment.Id);
    }

    public async Task<EquipmentResponse?> UpdateStatusAsync(int id, string newStatus)
    {
        var equipment = await _context.Equipments.FindAsync(id);
        if (equipment == null) return null;

        equipment.Status = newStatus;
        equipment.UpdatedAt = DateTimeOffset.UtcNow;

        _context.Equipments.Update(equipment);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(equipment.Id);
    }

    private static EquipmentResponse MapToResponse(Equipment e) => new()
    {
        Id = e.Id,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? "Sin Categoría",
        Name = e.Name, Brand = e.Brand, Model = e.Model,
        SerialNumber = e.SerialNumber, PurchaseDate = e.PurchaseDate,
        PurchasePrice = e.PurchasePrice, Status = e.Status,
        Location = e.Location, Notes = e.Notes,
        CreatedAt = e.CreatedAt, UpdatedAt = e.UpdatedAt
    };
}
