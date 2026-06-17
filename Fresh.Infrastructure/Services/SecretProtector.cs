using System.Security.Cryptography;
using System.Text;
using Fresh.Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Fresh.Infrastructure.Services;

/// <summary>
/// Cifra/descifra cadenas con AES-256 (clave derivada de un secreto en configuración).
/// El texto cifrado incluye el IV, así que cada cifrado produce un resultado distinto.
/// </summary>
public class SecretProtector : ISecretProtector
{
    private readonly byte[] _key;

    public SecretProtector(IConfiguration config)
    {
        var secret = config["SecretProtector:Key"]
            ?? config["ShareToken:Secret"]
            ?? "FreshApp_Secrets_DefaultKey_ChangeInProd_2024!";
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return string.Empty;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        var plaintext = Encoding.UTF8.GetBytes(plainText);
        using var enc = aes.CreateEncryptor();
        var cipher = enc.TransformFinalBlock(plaintext, 0, plaintext.Length);

        var payload = new byte[aes.IV.Length + cipher.Length];
        aes.IV.CopyTo(payload, 0);
        cipher.CopyTo(payload, aes.IV.Length);

        return Convert.ToBase64String(payload);
    }

    public string? Decrypt(string? cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return null;

        try
        {
            var data = Convert.FromBase64String(cipherText);
            if (data.Length <= 16) return null;

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV  = data[..16];

            using var dec = aes.CreateDecryptor();
            var plain = dec.TransformFinalBlock(data, 16, data.Length - 16);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }
}
