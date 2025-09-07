using BeselerNet.Shared.Core;

var keyBytes = Encryptor.GenerateKey();
var keyBase64 = Convert.ToBase64String(keyBytes);
Console.WriteLine($"Key: {keyBase64}\n");

var plainText = "Hello World!";
var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);

Console.WriteLine($"Plain Text: {plainText}");
var encryptedBytes = Encryptor.Encrypt(plainTextBytes, keyBytes);
var encryptedText = Convert.ToBase64String(encryptedBytes);
Console.WriteLine("Encrypted Text: " + encryptedText);

var decryptedBytes = Encryptor.Decrypt(encryptedBytes, keyBytes);
var decryptedText = System.Text.Encoding.UTF8.GetString(decryptedBytes);
Console.WriteLine("Decrypted Text: " + decryptedText);
