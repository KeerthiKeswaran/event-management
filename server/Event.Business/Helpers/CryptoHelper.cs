using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Event.Business.Helpers
{
    public static class CryptoHelper
    {
        // 32-byte key for AES-256 (must match the frontend key)
        private static readonly string SecretKey = "EventManagementSuperSecretKey32!"; 

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            byte[] keyBytes = Encoding.UTF8.GetBytes(SecretKey.PadRight(32).Substring(0, 32));
            byte[] iv = new byte[16]; // Initialization Vector (0s for simplicity here, but usually random)

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }
    }
}
