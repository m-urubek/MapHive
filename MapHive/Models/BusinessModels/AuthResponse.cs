namespace MapHive.Models.BusinessModels
{
    using MapHive.Models.RepositoryModels;

    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public AccountGet User { get; set; } = default!;
    }
}