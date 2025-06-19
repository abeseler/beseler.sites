using System.Security.Cryptography;
using System.Text;

namespace BeselerNet.Shared.Core;

public static class Encryptor
{
    /// <summary>
    /// Encrypts the specified plain text using AES-GCM with the provided key.
    /// </summary>
    /// <param name="plainText">The text to encrypt. If <paramref name="plainText"/> is <see langword="null"/> or empty,  the method returns the
    /// input value unchanged.</param>
    /// <param name="key">A Base64-encoded string representing the encryption key. The key must be 128, 192, or 256 bits  in length. This
    /// parameter cannot be <see langword="null"/> or empty.</param>
    /// <returns>A Base64-encoded string containing the encrypted data, including the nonce, ciphertext, and  authentication tag.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> is <see langword="null"/>, empty, not a valid Base64 string, or does not
    /// represent a key of 128, 192, or 256 bits.</exception>
    /// <exception cref="CryptographicException">Thrown if the encryption process fails.</exception>
    public static string Encrypt(string plainText, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (string.IsNullOrEmpty(plainText))
        {
            return plainText;
        }

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Key is not a valid Base64 string.", nameof(key));
        }

        if (keyBytes.Length is not 16 and not 24 and not 32)
        {
            throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(key));
        }

        try
        {
            Span<byte> nonce = stackalloc byte[12];
            RandomNumberGenerator.Fill(nonce);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            Span<byte> ciphertext = plainBytes.Length < 256 ? stackalloc byte[plainBytes.Length] : new byte[plainBytes.Length];
            Span<byte> tag = stackalloc byte[16];

            using (var gcm = new AesGcm(keyBytes, 16))
            {
                gcm.Encrypt(nonce, plainBytes, ciphertext, tag);
            }

            var encryptedBytes = new byte[12 + plainBytes.Length + 16];
            nonce.CopyTo(encryptedBytes.AsSpan(0));
            ciphertext.CopyTo(encryptedBytes.AsSpan(12));
            tag.CopyTo(encryptedBytes.AsSpan(12 + plainBytes.Length));

            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Encryption failed.", ex);
        }
    }

    /// <summary>
    /// Decrypts the specified encrypted text using the provided key.
    /// </summary>
    /// <param name="encryptedText">The encrypted text to decrypt, encoded as a Base64 string. Must be a valid Base64 string and at least 28 bytes
    /// long.</param>
    /// <param name="key">The encryption key, encoded as a Base64 string. Must be a valid Base64 string and represent a key size of 128,
    /// 192, or 256 bits.</param>
    /// <returns>The decrypted plaintext as a UTF-8 string. If <paramref name="encryptedText"/> is null or empty, the method
    /// returns the same value.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="encryptedText"/> is not a valid Base64 string, is too short, or if <paramref
    /// name="key"/> is not a valid Base64 string or does not represent a valid key size.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption or authentication fails.</exception>
    public static string Decrypt(string encryptedText, string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));

        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        byte[] encryptedBytes;
        try
        {
            encryptedBytes = Convert.FromBase64String(encryptedText);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Encrypted text is not a valid Base64 string.", nameof(encryptedText));
        }

        if (encryptedBytes.Length < 28)
        {
            throw new ArgumentException("Encrypted text is too short.", nameof(encryptedText));
        }            

        byte[] keyBytes;
        try
        {
            keyBytes = Convert.FromBase64String(key);
        }
        catch (FormatException)
        {
            throw new ArgumentException("Key is not a valid Base64 string.", nameof(key));
        }

        if (keyBytes.Length is not 16 and not 24 and not 32)
            throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(key));

        try
        {
            var nonce = encryptedBytes.AsSpan()[..12];
            var tag = encryptedBytes.AsSpan()[^16..];
            var ciphertext = encryptedBytes.AsSpan()[12..^16];
            Span<byte> plainBytes = ciphertext.Length < 256 ? stackalloc byte[ciphertext.Length] : new byte[ciphertext.Length];

            using (var gcm = new AesGcm(keyBytes, 16))
            {
                gcm.Decrypt(nonce, ciphertext, tag, plainBytes);
            }

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Decryption or authentication failed.", ex);
        }
    }

    /// <summary>
    /// Generates a cryptographic key of the specified size and returns it as a Base64-encoded string.
    /// </summary>
    /// <param name="keySize">The size of the key, in bits. Valid values are 128, 192, or 256.</param>
    /// <returns>A Base64-encoded string representing the generated cryptographic key.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="keySize"/> is not 128, 192, or 256.</exception>
    public static string GenerateKey(int keySize = 256)
    {
        if (keySize is not 128 and not 192 and not 256)
        {
            throw new ArgumentException("Key size must be 128, 192, or 256 bits.", nameof(keySize));
        }

        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return Convert.ToBase64String(aes.Key);
    }
}
