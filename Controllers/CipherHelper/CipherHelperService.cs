using System.Security.Cryptography;
using System.Text;

namespace ManageTask.Controllers.CipherHelper
{
    public static class CipherHelperService
    {
        public static string EncryptPassword(string plainText)
        {
            return plainText;
        }

        public static string DecryptPassword(string cipherText)
        {
            return cipherText;
        }


        public static string GenerateSecureTokenHashed()
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(32); // 256-bit
            string token =  Convert.ToBase64String(bytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");

            return HashToken(token);
        }


        public static string HashToken(string token)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes); 
        }
    }
}
