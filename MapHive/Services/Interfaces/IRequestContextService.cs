namespace MapHive.Services
{
    public interface IRequestContextService
    {
        public bool IsInRequest { get; }
        public string RequestPath { get; }
        public string IpAddress { get; }
        /// <summary>Returns null if not in request</summary>
        public bool IsRequestAjax { get; }
        public string? Referer { get; }
    }
}
