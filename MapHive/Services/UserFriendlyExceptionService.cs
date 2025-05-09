namespace MapHive.Services
{
    public class UserFriendlyExceptionService : IUserFriendlyExceptionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserFriendlyExceptionService(IHttpContextAccessor httpContextAccessor)
        {
            this._httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => this._httpContextAccessor.HttpContext?.Session ?? throw new Exception("Not in request");

        public string? Message
        {
            get => this.Session.GetString("UserFriendlyExceptionMessage");
            set => this.Session.SetString("UserFriendlyExceptionMessage", value ?? string.Empty);
        }
        public string? Type
        {
            get => this.Session.GetString("UserFriendlyExceptionType");
            set => this.Session.SetString("UserFriendlyExceptionType", value ?? string.Empty);
        }

        public void Clear()
        {
            this.Session.Remove("UserFriendlyExceptionMessage");
            this.Session.Remove("UserFriendlyExceptionType");
        }
    }
}
