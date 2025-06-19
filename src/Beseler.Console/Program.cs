using BeselerNet.Shared.Core;

var key = Encryptor.GenerateKey();
Console.WriteLine($"Key: {key}\n");

var plainText = "Hello World!";
Console.WriteLine($"Plain Text: {plainText}");
var encryptedText = Encryptor.Encrypt(plainText, key);
Console.WriteLine("Encrypted Text: " + encryptedText);

var decryptedText = Encryptor.Decrypt(encryptedText, key);
Console.WriteLine("Decrypted Text: " + decryptedText);
