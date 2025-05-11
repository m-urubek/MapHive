namespace MapHive.Services
{
    /// <summary>
    /// Provides access to the current user's context within a request scope.
    /// </summary>
    public interface IUserContextService
    {
        /// <summary>
        /// Gets the current authenticated user's ID, or null if not authenticated.
        /// </summary>
        int UserId { get; }

        /// <summary>
        /// Gets the current authenticated user's username, or null if not authenticated.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Gets a value indicating whether the current user is authenticated.
        /// </summary>
        bool IsAuthenticated { get; }
    }
}
