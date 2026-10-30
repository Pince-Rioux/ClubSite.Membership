using System.Security.Cryptography;
using System.Text;

namespace VereinMember.Services.Encryption;

/// <summary>
/// AES-256-GCM encryption for sensitive personal data (IBAN, BIC).
/// Key is provided via configuration or environment variable MEMBERSHIP_ENCRYPTION_KEY.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
}

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public AesEncryptionService(string base64Key)
    {
        // Expect 32 bytes (256-bit) encoded as base64
        _key = Convert.FromBase64String(base64Key)
            ?? throw new ArgumentException("Encryption key must be a valid base64-encoded 256-bit key.");
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext;

        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(12); // 96-bit nonce for GCM

        using var aes = new AesGcm(_key, 16); // 128-bit tag
        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        // Prepend nonce and tag to ciphertext for storage
        var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, nonce.Length);
        cipherBytes.CopyTo(result, nonce.Length + tag.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        if (string.IsNullOrEmpty(ciphertext)) return ciphertext;

        var data = Convert.FromBase64String(ciphertext);
        var nonce = data[..12];
        var tag = data[12..28];
        var cipherBytes = data[28..];

        using var aes = new AesGcm(_key, 16); // 128-bit tag
        var plainBytes = new byte[cipherBytes.Length];
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}