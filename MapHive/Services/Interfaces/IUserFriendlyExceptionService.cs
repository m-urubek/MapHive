namespace MapHive.Services
{
    public interface IUserFriendlyExceptionService
    {
        public string? Message { get; set; }
        public string? Type { get; set; }
        public void Clear();
    }
}
