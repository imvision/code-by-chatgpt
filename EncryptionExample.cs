using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EncryptionExample
{
    public class AesEncryption
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public AesEncryption(string key, string iv)
        {
            _key = Encoding.UTF8.GetBytes(key);
            _iv = Encoding.UTF8.GetBytes(iv);
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
            {
                return string.Empty;
            }

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (var memoryStream = new MemoryStream())
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        using (var streamWriter = new StreamWriter(cryptoStream))
                        {
                            streamWriter.Write(plainText);
                        }

                        return Convert.ToBase64String(memoryStream.ToArray());
                    }
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
            {
                return string.Empty;
            }

            using (var aes = Aes.Create())
            {
                aes.Key = _key;
                aes.IV = _iv;

                using (var memoryStream = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    {
                        using (var streamReader = new StreamReader(cryptoStream))
                        {
                            return streamReader.ReadToEnd();
                        }
                    }
                }
            }
        }
    }
}

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            // Set the key and IV
            const string key = "a1b2c3d4e5f6g7h8";
            const string iv = "a1b2c3d4e5f6g7h8";

            // Create a new instance of the AesEncryption class
            var aesEncryption = new AesEncryption(key, iv);

            // Set the plaintext string
            const string plainText = "This is a test message.";

            // Encrypt the plaintext string
            var cipherText = aesEncryption.Encrypt(plainText);

            // Decrypt the ciphertext string
            var decryptedText = aesEncryption.Decrypt(cipherText);

            // Print the original and decrypted strings
            Console.WriteLine($"Original: {plainText}");
            Console.WriteLine($"Decrypted: {decryptedText}");
        }
    }
}
