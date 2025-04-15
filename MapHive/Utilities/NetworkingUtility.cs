using System.Security.Cryptography;
using System.Text;

namespace MapHive.Utilities
{
    public static class NetworkingUtility
    {
        /// <summary>
        /// Hashes an IP address using SHA256 for secure storage
        /// </summary>
        /// <param name="ipAddress">The IP address to hash</param>
        /// <returns>A SHA256 hash of the IP address</returns>
        public static string HashIpAddress(string ipAddress)
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                return string.Empty;
            }

            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(ipAddress));
            
            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            
            return builder.ToString();
        }
    }
}
