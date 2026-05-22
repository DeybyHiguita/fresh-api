using Fresh.Core.Interfaces;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fresh.Infrastructure.Services;

public class GoogleDriveService : IGoogleDriveService
{
    private readonly DriveService _driveService;
    private readonly string _rootFolderId;
    private readonly ILogger<GoogleDriveService> _logger;
    private readonly Dictionary<int, string> _employeeFolderCache = new();

    public GoogleDriveService(IConfiguration configuration, ILogger<GoogleDriveService> logger)
    {
        _logger = logger;
        
        // ID de la carpeta raíz en Google Drive para documentos de empleados
        _rootFolderId = configuration["GoogleDrive:EmployeeDocumentsFolderId"] 
            ?? configuration["GoogleDrive:RootFolderId"] 
            ?? throw new InvalidOperationException("GoogleDrive:EmployeeDocumentsFolderId no está configurado");
        
        // Ruta al archivo de credenciales de la cuenta de servicio
        var credentialsPath = configuration["GoogleDrive:ServiceAccountKeyPath"] 
            ?? configuration["GoogleDrive:CredentialsPath"] 
            ?? "secrets/google-drive-service-account.json";

        GoogleCredential credential;
        
        // Intentar cargar credenciales desde archivo o variable de entorno
        var credentialsJson = configuration["GoogleDrive:CredentialsJson"];
        if (!string.IsNullOrEmpty(credentialsJson))
        {
            // Credenciales desde variable de entorno (para producción)
            credential = GoogleCredential.FromJson(credentialsJson)
                .CreateScoped(DriveService.Scope.DriveFile);
        }
        else if (File.Exists(credentialsPath))
        {
            // Credenciales desde archivo (para desarrollo)
            using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
            credential = GoogleCredential.FromStream(stream)
                .CreateScoped(DriveService.Scope.DriveFile);
        }
        else
        {
            throw new InvalidOperationException(
                $"No se encontraron credenciales de Google Drive. " +
                $"Configure GoogleDrive:CredentialsJson o coloque el archivo en {credentialsPath}");
        }

        _driveService = new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Fresh App"
        });

        _logger.LogInformation("Google Drive Service inicializado. Carpeta raíz: {RootFolderId}", _rootFolderId);
    }

    public async Task<string> CreateFolderAsync(string folderName)
    {
        var folderMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = folderName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { _rootFolderId }
        };

        var request = _driveService.Files.Create(folderMetadata);
        request.Fields = "id";
        
        var folder = await request.ExecuteAsync();
        
        _logger.LogInformation("Carpeta creada: {FolderName} con ID: {FolderId}", folderName, folder.Id);
        
        return folder.Id;
    }

    public async Task<string> GetOrCreateEmployeeFolderAsync(int employeeId, string employeeName)
    {
        // Verificar caché en memoria
        if (_employeeFolderCache.TryGetValue(employeeId, out var cachedFolderId))
        {
            return cachedFolderId;
        }

        // Nombre de la carpeta del empleado: "ID_Nombre"
        var folderName = $"{employeeId}_{SanitizeFolderName(employeeName)}";

        // Buscar si ya existe la carpeta
        var listRequest = _driveService.Files.List();
        listRequest.Q = $"mimeType='application/vnd.google-apps.folder' and name='{folderName}' and '{_rootFolderId}' in parents and trashed=false";
        listRequest.Fields = "files(id, name)";
        
        var result = await listRequest.ExecuteAsync();
        
        if (result.Files.Count > 0)
        {
            var folderId = result.Files[0].Id;
            _employeeFolderCache[employeeId] = folderId;
            return folderId;
        }

        // Crear nueva carpeta
        var newFolderId = await CreateFolderAsync(folderName);
        _employeeFolderCache[employeeId] = newFolderId;
        
        return newFolderId;
    }

    public async Task<(string fileId, string webLink)> UploadFileAsync(
        string folderId, 
        string fileName, 
        string contentType, 
        Stream fileStream)
    {
        // Generar GUID para el nombre del archivo
        var fileGuid = Guid.NewGuid().ToString();
        var extension = Path.GetExtension(fileName);
        var driveFileName = $"{fileGuid}{extension}";

        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = driveFileName,
            Parents = new List<string> { folderId },
            Description = $"Original: {fileName}"
        };

        var request = _driveService.Files.Create(fileMetadata, fileStream, contentType);
        request.Fields = "id, webViewLink, webContentLink";
        
        var progress = await request.UploadAsync();
        
        if (progress.Status != Google.Apis.Upload.UploadStatus.Completed)
        {
            throw new InvalidOperationException($"Error al subir archivo: {progress.Exception?.Message}");
        }

        var file = request.ResponseBody;
        
        // Hacer el archivo accesible por link
        await SetFilePermissionsAsync(file.Id);
        
        _logger.LogInformation("Archivo subido: {FileName} -> {DriveFileName} con ID: {FileId}", 
            fileName, driveFileName, file.Id);

        return (file.Id, file.WebViewLink ?? file.WebContentLink ?? "");
    }

    public async Task<Stream> DownloadFileAsync(string fileId)
    {
        var request = _driveService.Files.Get(fileId);
        var memoryStream = new MemoryStream();
        
        await request.DownloadAsync(memoryStream);
        memoryStream.Position = 0;
        
        return memoryStream;
    }

    public async Task DeleteFileAsync(string fileId)
    {
        try
        {
            await _driveService.Files.Delete(fileId).ExecuteAsync();
            _logger.LogInformation("Archivo eliminado de Google Drive: {FileId}", fileId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error al eliminar archivo de Google Drive: {FileId}", fileId);
            // No lanzamos excepción para no bloquear otras operaciones
        }
    }

    public async Task<string> GetFileWebLinkAsync(string fileId)
    {
        var request = _driveService.Files.Get(fileId);
        request.Fields = "webViewLink, webContentLink";
        
        var file = await request.ExecuteAsync();
        
        return file.WebViewLink ?? file.WebContentLink ?? "";
    }

    private async Task SetFilePermissionsAsync(string fileId)
    {
        var permission = new Google.Apis.Drive.v3.Data.Permission
        {
            Type = "anyone",
            Role = "reader"
        };

        await _driveService.Permissions.Create(permission, fileId).ExecuteAsync();
    }

    private static string SanitizeFolderName(string name)
    {
        // Remover caracteres no válidos para nombres de carpeta
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return sanitized.Replace(" ", "_");
    }
}
