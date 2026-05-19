using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Fresh.Api.Services;

public class GoogleDriveService
{
    private readonly IConfiguration _config;
    private readonly string _folderId;
    private string RedirectUri => _config["GoogleDrive:RedirectUri"] ?? "http://localhost:4200";

    // File where the refresh token is persisted at runtime
    private static readonly string TokenFilePath = Path.Combine(
        AppContext.BaseDirectory, "secrets", "drive-refresh-token.txt");

    public GoogleDriveService(IConfiguration config)
    {
        _config = config;
        _folderId = config["GoogleDrive:FolderId"]
            ?? throw new InvalidOperationException("GoogleDrive:FolderId not configured");
    }

    /// <summary>Returns true if a refresh token is available.</summary>
    public bool IsAuthorized() => !string.IsNullOrWhiteSpace(GetRefreshToken());

    private string? GetRefreshToken()
    {
        // 1. Check runtime token file (set via OAuth flow)
        if (File.Exists(TokenFilePath))
        {
            var t = File.ReadAllText(TokenFilePath).Trim();
            if (!string.IsNullOrEmpty(t)) return t;
        }
        // 2. Fallback to appsettings.json
        return _config["GoogleDrive:RefreshToken"];
    }

    /// <summary>Persists the refresh token to the token file.</summary>
    public void SaveRefreshToken(string refreshToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(TokenFilePath)!);
        File.WriteAllText(TokenFilePath, refreshToken);
    }

    private DriveService BuildDriveService()
    {
        var clientId     = _config["GoogleDrive:ClientId"]     ?? throw new InvalidOperationException("GoogleDrive:ClientId not configured");
        var clientSecret = _config["GoogleDrive:ClientSecret"] ?? throw new InvalidOperationException("GoogleDrive:ClientSecret not configured");
        var refreshToken = GetRefreshToken();

        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException("Drive no está autorizado. Por favor conecta Google Drive desde la aplicación.");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[] { DriveService.ScopeConstants.DriveFile }
        });

        var token = new TokenResponse { RefreshToken = refreshToken };
        var credential = new UserCredential(flow, "user", token);

        return new DriveService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Fresh App"
        });
    }

    /// <summary>Returns the OAuth2 authorization URL for the consent screen.</summary>
    public string GetAuthorizationUrl(string? returnPath = null)
    {
        var clientId     = _config["GoogleDrive:ClientId"]     ?? throw new InvalidOperationException("GoogleDrive:ClientId not configured");
        var clientSecret = _config["GoogleDrive:ClientSecret"] ?? throw new InvalidOperationException("GoogleDrive:ClientSecret not configured");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[] { DriveService.ScopeConstants.DriveFile }
        });

        var request = flow.CreateAuthorizationCodeRequest(RedirectUri);
        // Pass returnPath as state so Angular knows where to go back after auth
        if (!string.IsNullOrWhiteSpace(returnPath))
            request.State = Uri.EscapeDataString(returnPath);

        return request.Build().ToString();
    }

    /// <summary>Exchanges an authorization code for a refresh token and saves it.</summary>
    public async Task<string> ExchangeCodeForRefreshTokenAsync(string code)
    {
        var clientId     = _config["GoogleDrive:ClientId"]     ?? throw new InvalidOperationException("GoogleDrive:ClientId not configured");
        var clientSecret = _config["GoogleDrive:ClientSecret"] ?? throw new InvalidOperationException("GoogleDrive:ClientSecret not configured");

        var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret },
            Scopes = new[] { DriveService.ScopeConstants.DriveFile }
        });

        var token = await flow.ExchangeCodeForTokenAsync("user", code, RedirectUri, CancellationToken.None);
        var refreshToken = token.RefreshToken;
        if (!string.IsNullOrEmpty(refreshToken))
            SaveRefreshToken(refreshToken);

        return refreshToken ?? throw new Exception("Google no devolvió un refresh token. Asegúrate de revocar el acceso previo en myaccount.google.com/permissions y vuelve a intentarlo.");
    }

    /// <summary>Lists subfolders inside the configured root folder.</summary>
    public async Task<List<(string Id, string Name)>> ListSubfoldersAsync()
    {
        var svc = BuildDriveService();
        var req = svc.Files.List();
        req.Q = $"'{_folderId}' in parents and mimeType='application/vnd.google-apps.folder' and trashed=false";
        req.Fields = "files(id,name)";
        req.OrderBy = "name desc";
        var result = await req.ExecuteAsync();
        return result.Files.Select(f => (f.Id, f.Name)).ToList();
    }

    /// <summary>
    /// Returns the ID of the subfolder named after the current month (e.g. "Mayo 2026").
    /// Creates it if it doesn't exist yet.
    /// </summary>
    public async Task<string> GetOrCreateMonthFolderAsync()
    {
        var monthName = DateTime.Now.ToString("MMMM yyyy", new System.Globalization.CultureInfo("es-CO"));
        // Capitalize first letter
        monthName = char.ToUpper(monthName[0]) + monthName[1..];

        var svc = BuildDriveService();
        var search = svc.Files.List();
        search.Q = $"'{_folderId}' in parents and mimeType='application/vnd.google-apps.folder' and name='{monthName}' and trashed=false";
        search.Fields = "files(id,name)";
        var existing = await search.ExecuteAsync();

        if (existing.Files.Count > 0)
            return existing.Files[0].Id;

        // Create it
        var folderMeta = new Google.Apis.Drive.v3.Data.File
        {
            Name = monthName,
            MimeType = "application/vnd.google-apps.folder",
            Parents = new List<string> { _folderId }
        };
        var created = await svc.Files.Create(folderMeta).ExecuteAsync();
        return created.Id;
    }

    /// <summary>Uploads a base64-encoded image to Google Drive and returns the shareable file URL.</summary>
    public async Task<string> UploadInvoiceAsync(string base64Image, string mimeType, string fileName, string? subFolderId = null)
    {
        var driveService = BuildDriveService();

        var bytes = Convert.FromBase64String(base64Image);
        using var stream = new MemoryStream(bytes);

        var targetFolder = subFolderId ?? _folderId;
        var fileMetadata = new Google.Apis.Drive.v3.Data.File
        {
            Name = fileName,
            Parents = new List<string> { targetFolder }
        };

        var request = driveService.Files.Create(fileMetadata, stream, mimeType);
        request.Fields = "id, webViewLink";

        var uploadProgress = await request.UploadAsync();

        if (uploadProgress.Status != Google.Apis.Upload.UploadStatus.Completed)
            throw new Exception($"Drive upload failed: {uploadProgress.Exception?.Message}");

        var file = request.ResponseBody;
        return file.WebViewLink ?? $"https://drive.google.com/file/d/{file.Id}/view";
    }
}
