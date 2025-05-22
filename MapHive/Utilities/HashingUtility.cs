namespace MapHive.Utilities;

using System.Security.Cryptography;
using System.Text;

public static class HashingUtility
{
    public static string HashPassword(string password)
    {
        // Normalize the password to ensure consistent hashing
        password = password.Normalize(System.Text.NormalizationForm.FormKD);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));

        // Use lowercase hex format for consistency
        StringBuilder builder = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            _ = builder.Append(bytes[i].ToString("x2"));
        }

        // Return lowercase hex string
        return builder.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Hashes an IP address using SHA256 for secure storage
    /// </summary>
    /// <param name="ipAddress">The IP address to hash</param>
    /// <returns>A SHA256 hash of the IP address</returns>
    public static string HashIpAddress(string ipAddress)
    {
        if (string.IsNullOrEmpty(value: ipAddress))
        {
            return string.Empty;
        }
        byte[] bytes = SHA256.HashData(source: Encoding.UTF8.GetBytes(s: ipAddress));

        StringBuilder builder = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            _ = builder.Append(value: bytes[i].ToString(format: "x2"));
        }

        return builder.ToString();
    }
}
