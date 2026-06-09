using System.Security.Cryptography;
using System.Text;

namespace Fresh.Api.Services;

/// <summary>
/// Cifra/descifra un batchId con AES-256 para generar tokens de URL seguros.
/// Stateless: no requiere BD ni sesión.
/// </summary>
public class ShareTokenService
{
    private readonly byte[] _key;

    public ShareTokenService(IConfiguration config)
    {
        var secret = config["ShareToken:Secret"]
            ?? "FreshApp_BatchShare_DefaultKey_ChangeInProd_2024!";
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Protect(int batchId)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var plaintext = Encoding.UTF8.GetBytes(batchId.ToString());
        using var enc = aes.CreateEncryptor();
        var cipher = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var payload = new byte[aes.IV.Length + cipher.Length];
        aes.IV.CopyTo(payload, 0);
        cipher.CopyTo(payload, aes.IV.Length);

        return Base64UrlEncode(payload);
    }

    public int? Unprotect(string token)
    {
        try
        {
            var data = Base64UrlDecode(token);
            if (data.Length <= 16) return null;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV   = data[..16];

            using var dec = aes.CreateDecryptor();
            var plain = dec.TransformFinalBlock(data, 16, data.Length - 16);

            return int.TryParse(Encoding.UTF8.GetString(plain), out var id) ? id : null;
        }
        catch { return null; }
    }

    // ── Base64Url (sin padding) ───────────────────────────────────────────────

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static byte[] Base64UrlDecode(string s)
    {
        s = s.Replace('-', '+').Replace('_', '/');
        s += (s.Length % 4) switch { 2 => "==", 3 => "=", _ => "" };
        return Convert.FromBase64String(s);
    }
}
