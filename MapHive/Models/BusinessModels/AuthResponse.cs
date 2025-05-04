using MapHive.Models.RepositoryModels;

namespace MapHive.Models.BusinessModels
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserGet User { get; set; } = default!;
    }
}