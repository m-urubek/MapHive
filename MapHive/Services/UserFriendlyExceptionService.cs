namespace MapHive.Services;

public class UserFriendlyExceptionService(IHttpContextAccessor httpContextAccessor) : IUserFriendlyExceptionService
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    private ISession Session => _httpContextAccessor.HttpContext?.Session ?? throw new Exception("Not in request");

    public string? Message
    {
        get => Session.GetString(key: "UserFriendlyExceptionMessage");
        set => Session.SetString(key: "UserFriendlyExceptionMessage", value: value ?? string.Empty);
    }
    public string? Type
    {
        get => Session.GetString(key: "UserFriendlyExceptionType");
        set => Session.SetString(key: "UserFriendlyExceptionType", value: value ?? string.Empty);
    }

    public void Clear()
    {
        Session.Remove(key: "UserFriendlyExceptionMessage");
        Session.Remove(key: "UserFriendlyExceptionType");
    }
}
