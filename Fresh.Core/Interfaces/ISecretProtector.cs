namespace Fresh.Core.Interfaces;

/// <summary>
/// Cifra/descifra cadenas sensibles (ej: API keys) para guardarlas en BD.
/// </summary>
public interface ISecretProtector
{
    string Encrypt(string plainText);
    string? Decrypt(string? cipherText);
}
