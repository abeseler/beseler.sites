using System.Security.Cryptography;

namespace BeselerNet.Shared.Core;
public static class Encryptor
{
    public static (string EncryptedText, string IV) Encrypt(string plainText, string key)
    {
        if (string.IsNullOrEmpty(plainText))
        {
            return (plainText, "");
        }

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.GenerateIV();
        
        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using var sw = new StreamWriter(cs);
        sw.Write(plainText);
        sw.Flush();
        cs.FlushFinalBlock();

        return (Convert.ToBase64String(ms.ToArray()), Convert.ToBase64String(aes.IV));
    }
    public static string Decrypt(string encryptedText, string iv, string key)
    {
        if (string.IsNullOrEmpty(encryptedText))
        {
            return encryptedText;
        }

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.IV = Convert.FromBase64String(iv);

        var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(Convert.FromBase64String(encryptedText));
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return sr.ReadToEnd();
    }

    public static string GenerateKey()
    {
        using var aes = Aes.Create();
        return Convert.ToBase64String(aes.Key);
    }
}
