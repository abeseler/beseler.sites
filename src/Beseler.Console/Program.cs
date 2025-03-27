using BeselerNet.Shared.Core;

var key = Encryptor.GenerateKey();
Console.WriteLine($"Key: {key}\n");

var (encryptedText, iv) = Encryptor.Encrypt("Hello World", key);
Console.WriteLine("Encrypted Text: " + encryptedText);
Console.WriteLine("IV: " + iv);

var decryptedText = Encryptor.Decrypt(encryptedText, iv, key);
Console.WriteLine("Decrypted Text: " + decryptedText);