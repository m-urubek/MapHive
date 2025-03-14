using MapHive.Models;
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

        public Task<AuthResponse> RegisterAsync(RegisterRequest request, string ipAddress, string macAddress)
        {
            // Check if username already exists
            if (this._userRepository.CheckUsernameExists(request.Username))
            {
                return Task.FromResult(new AuthResponse
                {
                    Success = false,
                    Message = "Username already exists"
                });
            }

            // Check if MAC address already exists (one account per MAC address)
            // Allow multiple accounts if the MAC address is associated with an admin account or is the local development MAC
            if (this._userRepository.CheckMacAddressExists(macAddress) &&
                !this._userRepository.HasAdminAccount(macAddress))
            {
                return Task.FromResult(new AuthResponse
                {
                    Success = false,
                    Message = "An account already exists for this device"
                });
            }

            // Check if IP or MAC is blacklisted
            if (this._userRepository.IsBlacklisted(ipAddress, macAddress))
            {
                this._logManager.Warning("Registration attempt from blacklisted IP or MAC",
                    additionalData: $"IP: {ipAddress}, MAC: {macAddress}");

                return Task.FromResult(new AuthResponse
                {
                    Success = false,
                    Message = "Registration is not allowed from this device or network"
                });
            }

            // Create the user
            User user = new()
            {
                Username = request.Username,
                PasswordHash = this.HashPassword(request.Password),
                RegistrationDate = DateTime.UtcNow,
                IpAddress = ipAddress,
                MacAddress = macAddress,
                IsTrusted = false,
                IsAdmin = false
            };

            int userId = this._userRepository.CreateUser(user);
            user.Id = userId;

            this._logManager.Information($"New user registered: {request.Username}",
                additionalData: $"IP: {ipAddress}, MAC: {macAddress}");

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
                return Task.FromResult(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            // Verify password
            if (!this.VerifyPassword(request.Password, user.PasswordHash))
            {
                this._logManager.Warning($"Failed login attempt for user: {request.Username}");

                return Task.FromResult(new AuthResponse
                {
                    Success = false,
                    Message = "Invalid username or password"
                });
            }

            this._logManager.Information($"User logged in: {request.Username}");

            return Task.FromResult(new AuthResponse
            {
                Success = true,
                Message = "Login successful",
                User = user
            });
        }

        public bool IsBlacklisted(string ipAddress, string macAddress)
        {
            return this._userRepository.IsBlacklisted(ipAddress, macAddress);
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