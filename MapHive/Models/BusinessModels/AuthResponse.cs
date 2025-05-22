namespace MapHive.Models.BusinessModels;

using MapHive.Models.Data.DbTableModels;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public AccountAtomic User { get; set; } = default!;
}