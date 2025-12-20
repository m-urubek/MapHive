namespace MapHive.Utilities;

using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

public static class HashingUtility
{
    private const int SaltSize = 16; // 128 bits
    private const int HashSize = 32; // 256 bits
    // Security parameters tuned for brute-force resistance and Azure server constraints:
    // - Memory: 64 MB balances GPU attack resistance with Azure memory limits
    //   (memory is temporary, only used during hash operation ~200-500ms)
    // - Iterations: 6 compensates for lower memory with higher time cost
    // - Parallelism: 4 threads balances security and performance
    // Target: ~200-500ms per hash to make brute-force attacks impractical
    // Note: If you have more RAM available, consider increasing MemorySize to 96-128 MB
    private const int MemorySize = 65536; // 64 MB - Azure-friendly while still secure
    private const int Iterations = 6; // Increased to compensate for lower memory
    private const int DegreeOfParallelism = 4; // Number of parallel threads
    private const char Delimiter = ':';

    /// <summary>
    /// Hashes a password using Argon2id (recommended variant)
    /// Format: argon2id:m:{memory}:t:{iterations}:p:{parallelism}:{salt_base64}:{hash_base64}
    /// </summary>
    /// <param name="password">The password to hash</param>
    /// <returns>A string containing Argon2 parameters, salt, and hash</returns>
    public static string HashPassword(string password)
    {
        // Normalize the password to ensure consistent hashing
        password = password.Normalize(System.Text.NormalizationForm.FormKD);

        // Generate a random salt
        byte[] salt = new byte[SaltSize];
        using RandomNumberGenerator rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);

        // Hash the password using Argon2id
        byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
        using Argon2id argon2 = new(passwordBytes);
        argon2.Salt = salt;
        argon2.DegreeOfParallelism = DegreeOfParallelism;
        argon2.MemorySize = MemorySize;
        argon2.Iterations = Iterations;
        byte[] hash = argon2.GetBytes(HashSize);

        // Format: argon2id:m:memory:t:iterations:p:parallelism:salt:hash
        return $"argon2id{Delimiter}m{Delimiter}{MemorySize}{Delimiter}t{Delimiter}{Iterations}{Delimiter}p{Delimiter}{DegreeOfParallelism}{Delimiter}{Convert.ToBase64String(salt)}{Delimiter}{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifies a password against a stored hash (supports Argon2id, PBKDF2, and legacy SHA256)
    /// </summary>
    /// <param name="password">The password to verify</param>
    /// <param name="storedHash">The stored hash</param>
    /// <returns>True if the password matches, false otherwise</returns>
    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        // Normalize the password
        password = password.Normalize(System.Text.NormalizationForm.FormKD);

        // Check if it's an Argon2 hash
        if (storedHash.StartsWith(value: "argon2id", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return VerifyArgon2Password(password: password, storedHash: storedHash);
        }

        // Check if it's a PBKDF2 hash (format: iterations:salt:hash)
        string[] parts = storedHash.Split(Delimiter);
        if (parts.Length == 3 && int.TryParse(parts[0], out _))
        {
            return VerifyPbkdf2Password(password: password, storedHash: storedHash);
        }

        // Legacy SHA256 hash format - try to verify using old method for backward compatibility
        return VerifyLegacyPassword(password: password, storedHash: storedHash);
    }

    /// <summary>
    /// Verifies a password against a stored Argon2id hash
    /// </summary>
    private static bool VerifyArgon2Password(string password, string storedHash)
    {
        try
        {
            // Parse format: argon2id:m:memory:t:iterations:p:parallelism:salt:hash
            string[] parts = storedHash.Split(Delimiter);
            if (parts.Length != 9 || parts[0].ToLowerInvariant() != "argon2id")
            {
                return false;
            }

            if (!int.TryParse(parts[2], out int memorySize) ||
                !int.TryParse(parts[4], out int iterations) ||
                !int.TryParse(parts[6], out int degreeOfParallelism))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[7]);
            byte[] expectedHash = Convert.FromBase64String(parts[8]);

            // Compute the hash of the provided password
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            using Argon2id argon2 = new(passwordBytes);
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = degreeOfParallelism;
            argon2.MemorySize = memorySize;
            argon2.Iterations = iterations;
            byte[] computedHash = argon2.GetBytes(expectedHash.Length);

            // Compare the hashes using constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(left: computedHash, right: expectedHash);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies a password against a stored PBKDF2 hash (for backward compatibility)
    /// </summary>
    private static bool VerifyPbkdf2Password(string password, string storedHash)
    {
        try
        {
            // Parse format: iterations:salt:hash
            string[] parts = storedHash.Split(Delimiter);
            if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(parts[1]);
            byte[] hash = Convert.FromBase64String(parts[2]);

            // Compute the hash of the provided password using PBKDF2
            byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
                password: Encoding.UTF8.GetBytes(password),
                salt: salt,
                iterations: iterations,
                hashAlgorithm: HashAlgorithmName.SHA256,
                outputLength: hash.Length);

            // Compare the hashes using constant-time comparison to prevent timing attacks
            return CryptographicOperations.FixedTimeEquals(left: computedHash, right: hash);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Verifies a password against a legacy SHA256 hash (for backward compatibility during migration)
    /// </summary>
    private static bool VerifyLegacyPassword(string password, string storedHash)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
        StringBuilder builder = new();
        for (int i = 0; i < bytes.Length; i++)
        {
            _ = builder.Append(bytes[i].ToString("x2"));
        }
        string hashedPassword = builder.ToString().ToLowerInvariant();
        return string.Equals(a: hashedPassword, b: storedHash, comparisonType: StringComparison.OrdinalIgnoreCase);
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
