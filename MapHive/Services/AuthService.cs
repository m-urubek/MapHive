using MapHive.Models;
using MapHive.Models.Exceptions;
using MapHive.Singletons;
using System.Security.Cryptography;
using System.Text;

namespace MapHive.Services
{
    public class AuthService : IAuthService
    {
        public Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
        {
            // Check if username already exists
            if (CurrentRequest.UserRepository.CheckUsernameExists(request.Username))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Check if IP is blacklisted
            if (CurrentRequest.UserRepository.IsBlacklisted(ipAddress))
            {
                throw new WarningException($"Registration attempt from blacklisted IP");
            }

            // Create the user
            User user = new()
            {
                Username = request.Username,
                PasswordHash = this.HashPassword(request.Password),
                RegistrationDate = DateTime.UtcNow,
                Tier = UserTier.Normal,
                IpAddressHistory = ipAddress
            };

            int userId = CurrentRequest.UserRepository.CreateUser(user);
            user.Id = userId;

            // Log registration, including hashed IP
            CurrentRequest.LogManager.Information($"New user registered: {request.Username}", 
                additionalData: $"Hashed IP: {ipAddress}");

            return Task.FromResult(new AuthResponse
            {
                Success = true,
                Message = "Registration successful",
                User = user
            });
        }

        public Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            // Get user by username
            User? user = CurrentRequest.UserRepository.GetUserByUsername(request.Username);

            // Check if user exists
            if (user == null)
            {
                throw new OrangeUserException("Invalid username or password");
            }

            // Verify password
            if (!this.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new OrangeUserException("Invalid username or password");
            }

            CurrentRequest.LogManager.Information($"User logged in: {request.Username}");

            return Task.FromResult(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                User = user
            });
        }

        public bool IsBlacklisted(string ipAddress)
        {
            // Hash the IP address before checking the blacklist
            string hashedIpAddress = MapHive.Utilities.NetworkingUtility.HashIpAddress(ipAddress);
            return CurrentRequest.UserRepository.IsBlacklisted(hashedIpAddress);
        }

        public string HashPassword(string password)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

            StringBuilder builder = new();
            for (int i = 0; i < bytes.Length; i++)
            {
                _ = builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            string hashedPassword = this.HashPassword(password);
            return hashedPassword.Equals(storedHash);
        }
    }
}