namespace StudioB2B.Application.Common.Interfaces;

/// <summary>
/// Reversible AES-based encryption for sensitive values such as marketplace API keys.
/// The encrypted value is safe to store in the database.
/// </summary>
public interface IKeyEncryptionService
{
    /// <summary>Encrypts a plain-text value.</summary>
    string Encrypt(string plainText);

    /// <summary>Decrypts a previously encrypted value back to plain text.</summary>
    string Decrypt(string cipherText);
}
