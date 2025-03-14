using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MapHive.Utilities
{
    public static class NetworkUtilities
    {
        /// <summary>
        /// Generates a device identifier using a combination of IP address, user agent, and client-provided fingerprint.
        /// This is more reliable than trying to get the actual MAC address in a web environment.
        /// </summary>
        /// <param name="ipAddress">The client's IP address</param>
        /// <param name="userAgent">The client's user agent string</param>
        /// <param name="clientFingerprint">Browser fingerprint generated by Fingerprint.js</param>
        /// <returns>A unique identifier for the device</returns>
        public static string GenerateDeviceIdentifier(string ipAddress, string userAgent, string clientFingerprint = "")
        {
            if (string.IsNullOrEmpty(ipAddress))
            {
                ipAddress = "0.0.0.0";
            }
            
            if (string.IsNullOrEmpty(userAgent))
            {
                userAgent = "unknown-agent";
            }

            // For local development testing, return a mock device ID
            if (ipAddress is "127.0.0.1" or "::1")
            {
                // If client fingerprint is provided, use it to create a unique ID for local testing
                if (!string.IsNullOrEmpty(clientFingerprint))
                {
                    return $"DEV:{HashString(clientFingerprint).Substring(0, 12)}";
                }
                return "10:11:12:13:14:15"; // Default mock device ID for local testing
            }

            // If we have a client-provided fingerprint, prioritize it
            if (!string.IsNullOrEmpty(clientFingerprint))
            {
                // Create a unique identifier by combining the fingerprint with some IP info
                // This helps prevent spoofing by requiring both the fingerprint and the IP
                string ipFragment = ipAddress.Split('.')[0]; // Just use the first octet
                return HashToMacFormat($"{clientFingerprint}|{ipFragment}");
            }

            // Fallback to server-side fingerprinting
            string fingerprintData = $"{ipAddress}|{ExtractBrowserFingerprint(userAgent)}";
            return HashToMacFormat(fingerprintData);
        }

        /// <summary>
        /// Extract key identifying information from the user agent
        /// </summary>
        private static string ExtractBrowserFingerprint(string userAgent)
        {
            // Extract the most stable parts of the user agent
            // This regex tries to extract: Browser name/version and OS
            string pattern = @"(MSIE|Trident|(?:Firefox|Chrome|Safari|Opera|Edge))[\/\s](\d+\.\d+).*?(Windows|Mac|Linux|Android|iOS)[^;)]*";
            Match match = Regex.Match(userAgent, pattern, RegexOptions.IgnoreCase);
            
            if (match.Success)
            {
                return match.Value;
            }
            
            // Fallback to a simple hash of the entire user agent
            return userAgent;
        }

        /// <summary>
        /// Creates a MAC-like hash from the input string
        /// </summary>
        private static string HashToMacFormat(string input)
        {
            // Create a deterministic hash from the input
            string hash = HashString(input);
            
            // Take first 12 characters and format like a MAC address
            string macFormat = string.Empty;
            for (int i = 0; i < 6; i++)
            {
                if (i * 2 + 1 < hash.Length)
                {
                    macFormat += hash.Substring(i * 2, 2);
                    if (i < 5) macFormat += ":";
                }
            }
            
            return macFormat;
        }

        /// <summary>
        /// Creates a deterministic hash from an input string
        /// </summary>
        private static string HashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                
                // Return a hex string
                StringBuilder builder = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}