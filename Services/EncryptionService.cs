using System;
using System.Security.Cryptography;
using System.Text;

namespace VAM.Services
{
    public static class EncryptionService
    {
        // Using Windows DPAPI for secure encryption
        public static string Encrypt(string plainText)
        {
            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch
            {
                // Fallback to simple Base64 if DPAPI fails
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(plainText));
            }
        }

        public static string Decrypt(string encryptedText)
        {
            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                // Fallback for simple Base64
                try
                {
                    return Encoding.UTF8.GetString(Convert.FromBase64String(encryptedText));
                }
                catch
                {
                    return encryptedText;
                }
            }
        }
    }
}
