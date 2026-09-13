using System;
using System.Security.Cryptography;
using System.Text;

namespace Messenger_Vasanov_ISP33
{
    internal class DataEncryption
    {
        public static byte[] Encrypt(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }
            byte[] textBytes = Encoding.UTF8.GetBytes(text);
            return ProtectedData.Protect(textBytes, null, DataProtectionScope.CurrentUser);
        }

        public static string Decrypt(byte[] encryptedBytes)
        {
            if (encryptedBytes == null || encryptedBytes.Length == 0)
            {
                return "";
            }  
            byte[] decryptedBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}