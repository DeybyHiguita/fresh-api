using Fresh.Core.DTOs.EmployeeDocument;
using Fresh.Core.Entities;
using Fresh.Core.Interfaces;
using Fresh.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fresh.Infrastructure.Services;

public class EmployeeDocumentService : IEmployeeDocumentService
{
    private readonly FreshDbContext _context;
    private readonly IGoogleDriveService _driveService;
    private readonly ILogger<EmployeeDocumentService> _logger;

    public EmployeeDocumentService(
        FreshDbContext context,
        IGoogleDriveService driveService,
        ILogger<EmployeeDocumentService> logger)
    {
        _context = context;
        _driveService = driveService;
        _logger = logger;
    }

    public async Task<IEnumerable<EmployeeDocumentResponse>> GetByEmployeeAsync(int employeeId)
    {
        var documents = await _context.EmployeeDocuments
            .Include(d => d.Employee)
            .Include(d => d.DocumentType)
            .Where(d => d.EmployeeId == employeeId)
            .OrderBy(d => d.DocumentType!.SortOrder)
            .ToListAsync();

        var responses = new List<EmployeeDocumentResponse>();
        foreach (var doc in documents)
        {
            responses.Add(await MapToResponseAsync(doc));
        }

        return responses;
    }

    public async Task<EmployeeDocumentResponse?> GetByIdAsync(int id)
    {
        var document = await _context.EmployeeDocuments
            .Include(d => d.Employee)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id);

        return document is not null ? await MapToResponseAsync(document) : null;
    }

    public async Task<EmployeeDocumentResponse> UploadAsync(int employeeId, IFormFile file, EmployeeDocumentRequest request, int uploadedBy)
    {
        var employee = await _context.Employees.FindAsync(employeeId);
        if (employee is null)
            throw new InvalidOperationException("Empleado no encontrado");

        var documentType = await _context.EmployeeDocumentTypes.FindAsync(request.DocumentTypeId);
        if (documentType is null)
            throw new InvalidOperationException("Tipo de documento no encontrado");

        // Validar tamaño
        if (file.Length > documentType.MaxFileSize)
            throw new InvalidOperationException($"El archivo excede el tamaño máximo permitido de {documentType.MaxFileSize / 1048576}MB");

        // Validar formato
        var extension = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
        var allowedFormats = documentType.AllowedFormats.Split(',').Select(f => f.Trim().ToLower());
        if (!allowedFormats.Contains(extension))
            throw new InvalidOperationException($"Formato no permitido. Formatos válidos: {documentType.AllowedFormats}");

        // Obtener o crear carpeta del empleado en Google Drive
        var employeeFolderName = $"{employee.FirstName} {employee.LastName}";
        var folderId = await _driveService.GetOrCreateEmployeeFolderAsync(employeeId, employeeFolderName);

        // Subir archivo a Google Drive con GUID
        string driveFileId;
        string driveLink;
        var fileGuid = Guid.NewGuid().ToString("N");
        var fileName = $"{fileGuid}.{extension}";

        using (var stream = file.OpenReadStream())
        {
            var result = await _driveService.UploadFileAsync(
                folderId,
                file.FileName,
                file.ContentType,
                stream);

            driveFileId = result.fileId;
            driveLink = result.webLink;
        }

        var document = new EmployeeDocument
        {
            EmployeeId = employeeId,
            DocumentTypeId = request.DocumentTypeId,
            FileName = fileName,
            OriginalName = file.FileName,
            FilePath = null, // Ya no usamos almacenamiento local
            FileSize = (int)file.Length,
            MimeType = file.ContentType,
            GoogleDriveFileId = driveFileId,
            GoogleDriveLink = driveLink,
            Notes = request.Notes,
            ExpirationDate = request.ExpirationDate,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.EmployeeDocuments.Add(document);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Documento subido a Google Drive para empleado {EmployeeId}: {FileName} -> {DriveFileId}",
            employeeId, file.FileName, driveFileId);

        // Recargar con relaciones
        await _context.Entry(document).Reference(d => d.Employee).LoadAsync();
        await _context.Entry(document).Reference(d => d.DocumentType).LoadAsync();

        return await MapToResponseAsync(document);
    }

    public async Task<EmployeeDocumentResponse?> UpdateAsync(int id, EmployeeDocumentRequest request)
    {
        var document = await _context.EmployeeDocuments
            .Include(d => d.Employee)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null) return null;

        document.Notes = request.Notes;
        document.ExpirationDate = request.ExpirationDate;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToResponseAsync(document);
    }

    public async Task<EmployeeDocumentResponse?> VerifyAsync(int id, int verifiedBy)
    {
        var document = await _context.EmployeeDocuments
            .Include(d => d.Employee)
            .Include(d => d.DocumentType)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (document is null) return null;

        document.IsVerified = true;
        document.VerifiedBy = verifiedBy;
        document.VerifiedAt = DateTime.UtcNow;
        document.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToResponseAsync(document);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var document = await _context.EmployeeDocuments.FindAsync(id);
        if (document is null) return false;

        // Eliminar archivo de Google Drive
        if (!string.IsNullOrEmpty(document.GoogleDriveFileId))
        {
            await _driveService.DeleteFileAsync(document.GoogleDriveFileId);
        }

        // Eliminar archivo local si existe (legacy)
        if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
        {
            File.Delete(document.FilePath);
        }

        _context.EmployeeDocuments.Remove(document);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<(byte[] content, string fileName, string contentType)> DownloadAsync(int id)
    {
        var document = await _context.EmployeeDocuments.FindAsync(id);
        if (document is null)
            throw new InvalidOperationException("Documento no encontrado");

        // Si tiene Google Drive ID, descargar de Drive
        if (!string.IsNullOrEmpty(document.GoogleDriveFileId))
        {
            using var stream = await _driveService.DownloadFileAsync(document.GoogleDriveFileId);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            return (memoryStream.ToArray(), document.OriginalName, document.MimeType ?? "application/octet-stream");
        }

        // Fallback a archivo local (legacy)
        if (!string.IsNullOrEmpty(document.FilePath) && File.Exists(document.FilePath))
        {
            var content = await File.ReadAllBytesAsync(document.FilePath);
            return (content, document.OriginalName, document.MimeType ?? "application/octet-stream");
        }

        throw new InvalidOperationException("Archivo no encontrado");
    }

    private async Task<EmployeeDocumentResponse> MapToResponseAsync(EmployeeDocument document)
    {
        string? uploadedByName = null;
        string? verifiedByName = null;

        if (document.UploadedBy.HasValue)
        {
            var uploader = await _context.Users.FindAsync(document.UploadedBy.Value);
            uploadedByName = uploader?.Name;
        }

        if (document.VerifiedBy.HasValue)
        {
            var verifier = await _context.Users.FindAsync(document.VerifiedBy.Value);
            verifiedByName = verifier?.Name;
        }

        return new EmployeeDocumentResponse
        {
            Id = document.Id,
            EmployeeId = document.EmployeeId,
            EmployeeName = document.Employee?.FullName ?? "",
            DocumentTypeId = document.DocumentTypeId,
            DocumentTypeName = document.DocumentType?.Name ?? "",
            FileName = document.FileName,
            OriginalName = document.OriginalName,
            FilePath = document.FilePath,
            FileSize = document.FileSize,
            MimeType = document.MimeType,
            GoogleDriveFileId = document.GoogleDriveFileId,
            GoogleDriveLink = document.GoogleDriveLink,
            Notes = document.Notes,
            ExpirationDate = document.ExpirationDate,
            IsVerified = document.IsVerified,
            VerifiedByName = verifiedByName,
            VerifiedAt = document.VerifiedAt,
            UploadedByName = uploadedByName,
            CreatedAt = document.CreatedAt
        };
    }
}
