using System.Security.Cryptography;

namespace BeselerNet.Shared.Core;

public static class Encryptor
{
    /// <summary>
    /// Encrypts the specified input data using AES-GCM with the provided key.
    /// </summary>
    /// <param name="input">The data to encrypt. If <paramref name="input"/> is empty, the method returns an empty array.</param>
    /// <param name="key">A byte array representing the encryption key. The key must be 128, 192, or 256 bits in length. This parameter cannot be
    /// <see langword="null"/> or empty.</param>
    /// <returns>A byte array containing the encrypted data, including the nonce, ciphertext, and authentication tag.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> is <see langword="null"/>, empty, or does not represent a key of 128,
    /// 192, or 256 bits.</exception>
    /// <exception cref="CryptographicException">Thrown if the encryption process fails.</exception>
    public static byte[] Encrypt(ReadOnlySpan<byte> input, byte[] key)
    {
        if (input is not { Length: > 0 })
        {
            return [];
        }

        if (key is not { Length: 16 or 24 or 32 })
        {
            throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(key));
        }

        try
        {
            Span<byte> nonce = stackalloc byte[12];
            RandomNumberGenerator.Fill(nonce);

            Span<byte> ciphertext = input.Length < 256 ? stackalloc byte[input.Length] : new byte[input.Length];
            Span<byte> tag = stackalloc byte[16];

            using (var gcm = new AesGcm(key, 16))
            {
                gcm.Encrypt(nonce, input, ciphertext, tag);
            }

            var encryptedBytes = new byte[12 + input.Length + 16];
            nonce.CopyTo(encryptedBytes.AsSpan(0));
            ciphertext.CopyTo(encryptedBytes.AsSpan(12));
            tag.CopyTo(encryptedBytes.AsSpan(12 + input.Length));
            return encryptedBytes;
        }
        catch (Exception ex)
        {
            throw new CryptographicException("Encryption failed.", ex);
        }
    }

    /// <summary>
    /// Decrypts the specified encrypted data using AES-GCM with the provided key.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt. Must be at least 28 bytes long (12 bytes for the nonce, at least 0 bytes for the
    /// ciphertext, and 16 bytes for the authentication tag).</param>
    /// <param name="key">A byte array representing the decryption key. The key must be 128, 192, or 256 bits in length. This parameter cannot be
    /// <see langword="null"/> or empty.</param>
    /// <returns>A byte array containing the decrypted data.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="encryptedBytes"/> is too short or if <paramref name="key"/> is <see
    /// langword="null"/>, empty, or does not represent a key of 128, 192, or 256 bits.</exception>
    /// <exception cref="CryptographicException">Thrown if decryption or authentication fails.</exception>
    public static byte[] Decrypt(ReadOnlySpan<byte> encryptedBytes, byte[] key)
    {
        if (encryptedBytes.Length < 28)
        {
            throw new ArgumentException("Encrypted data is too short.", nameof(encryptedBytes));
        }
        if (key is not { Length: 16 or 24 or 32 })
        {
            throw new ArgumentException("Key must be 128, 192, or 256 bits.", nameof(key));
        }

        try
        {
            var nonce = encryptedBytes[..12];
            var tag = encryptedBytes[^16..];
            var ciphertext = encryptedBytes[12..^16];
            Span<byte> plainBytes = ciphertext.Length < 256 ? stackalloc byte[ciphertext.Length] : new byte[ciphertext.Length];
            using (var gcm = new AesGcm(key, 16))
            {
                gcm.Decrypt(nonce, ciphertext, tag, plainBytes);
            }
            return plainBytes.ToArray();
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
    public static byte[] GenerateKey(int keySize = 256)
    {
        if (keySize is not 128 and not 192 and not 256)
        {
            throw new ArgumentException("Key size must be 128, 192, or 256 bits.", nameof(keySize));
        }

        using var aes = Aes.Create();
        aes.KeySize = keySize;
        aes.GenerateKey();
        return aes.Key;
    }
}
