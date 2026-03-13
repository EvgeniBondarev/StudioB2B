using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using IKeyEncryptionService = StudioB2B.Infrastructure.Interfaces.IKeyEncryptionService;

namespace StudioB2B.Infrastructure.Services;

/// <summary>
/// AES-256 reversible encryption for marketplace API keys.
/// The 32-byte key is read from <c>Encryption:Key</c> in configuration
/// (base64-encoded). A random 16-byte IV is prepended to the cipher text.
/// </summary>
public class KeyEncryptionService : IKeyEncryptionService
{
    private readonly byte[] _key;

    public KeyEncryptionService(IConfiguration configuration)
    {
        var base64Key = configuration["Encryption:Key"];
        byte[]? keyBytes = null;

        if (!string.IsNullOrWhiteSpace(base64Key))
        {
            try
            {
                keyBytes = Convert.FromBase64String(base64Key);
            }
            catch (FormatException)
            {
                // fall back to derived key below if configuration value is invalid
                keyBytes = null;
            }
        }

        if (keyBytes is null || keyBytes.Length != 32)
        {
            // Derive a stable key from the application name as a safe fallback
            // (production deployments should always set Encryption:Key explicitly)
            keyBytes = SHA256.HashData("StudioB2B-DefaultEncKey"u8.ToArray());
        }

        _key = keyBytes;
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length); // prepend the 16-byte IV

        using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs, Encoding.UTF8))
        {
            sw.Write(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            var bytes = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = _key;

            var iv = bytes[..16];
            var payload = bytes[16..];

            aes.IV = iv;

            using var ms = new MemoryStream(payload);
            using var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);
            return sr.ReadToEnd();
        }
        catch
        {
            // If decryption fails the value was stored as plain text (migration path)
            return cipherText;
        }
    }
}
