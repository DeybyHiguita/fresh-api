namespace Fresh.Core.Interfaces;

public interface IGoogleDriveService
{
    /// <summary>
    /// Crea una carpeta en Google Drive dentro de la carpeta raíz
    /// </summary>
    /// <param name="folderName">Nombre de la carpeta</param>
    /// <returns>ID de la carpeta creada</returns>
    Task<string> CreateFolderAsync(string folderName);

    /// <summary>
    /// Obtiene o crea una carpeta para el empleado
    /// </summary>
    /// <param name="employeeId">ID del empleado</param>
    /// <param name="employeeName">Nombre del empleado (para nombrar la carpeta)</param>
    /// <returns>ID de la carpeta del empleado</returns>
    Task<string> GetOrCreateEmployeeFolderAsync(int employeeId, string employeeName);

    /// <summary>
    /// Sube un archivo a Google Drive
    /// </summary>
    /// <param name="folderId">ID de la carpeta destino</param>
    /// <param name="fileName">Nombre del archivo</param>
    /// <param name="contentType">Tipo MIME del archivo</param>
    /// <param name="fileStream">Stream del archivo</param>
    /// <returns>Tupla con (FileId, WebViewLink)</returns>
    Task<(string fileId, string webLink)> UploadFileAsync(string folderId, string fileName, string contentType, Stream fileStream);

    /// <summary>
    /// Descarga un archivo de Google Drive
    /// </summary>
    /// <param name="fileId">ID del archivo en Google Drive</param>
    /// <returns>Stream del archivo</returns>
    Task<Stream> DownloadFileAsync(string fileId);

    /// <summary>
    /// Elimina un archivo de Google Drive
    /// </summary>
    /// <param name="fileId">ID del archivo</param>
    Task DeleteFileAsync(string fileId);

    /// <summary>
    /// Obtiene el link de visualización del archivo
    /// </summary>
    /// <param name="fileId">ID del archivo</param>
    /// <returns>URL para ver el archivo</returns>
    Task<string> GetFileWebLinkAsync(string fileId);
}
