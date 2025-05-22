namespace MapHive.Models.BusinessModels;

using MapHive.Models.Enums;

public class UserLogin
{
    public required int Id { get; set; }
    public required string Username { get; set; }
    public required AccountTier Tier { get; set; }
}