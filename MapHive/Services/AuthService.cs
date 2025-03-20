using MapHive.Models;
using MapHive.Models.Exceptions;
using MapHive.Repositories;
using System.Security.Cryptography;
using System.Text;

namespace MapHive.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly LogManager _logManager;

        public AuthService(IUserRepository userRepository, LogManager logManager)
        {
            this._userRepository = userRepository;
            this._logManager = logManager;
        }

        public Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress)
        {
            // Check if username already exists
            if (this._userRepository.CheckUsernameExists(request.Username))
            {
                throw new OrangeUserException("Username already exists");
            }

            // Check if IP is blacklisted
            if (this._userRepository.IsBlacklisted(ipAddress))
            {
                throw new WarningException($"Registration attempt from blacklisted IP. IP: {ipAddress}");
            }

            // Create the user
            User user = new()
            {
                Username = request.Username,
                PasswordHash = this.HashPassword(request.Password),
                RegistrationDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                Tier = UserTier.Normal
            };

            int userId = this._userRepository.CreateUser(user);
            user.Id = userId;

            this._logManager.Information($"New user registered: {request.Username}",
                additionalData: $"IP: {ipAddress}");

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
            User? user = this._userRepository.GetUserByUsername(request.Username);

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

            this._logManager.Information($"User logged in: {request.Username}");

            return Task.FromResult(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                User = user
            });
        }

        public bool IsBlacklisted(string ipAddress)
        {
            return this._userRepository.IsBlacklisted(ipAddress);
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