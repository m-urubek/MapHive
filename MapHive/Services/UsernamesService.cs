namespace MapHive.Services;

public class UsernamesService(IUserContextService _userContextService) : IUsernamesService
{
    public (string username, int? authorId) GetAnonymizedUser(string username, int? authorId, bool isAnonymous)
    {
        return isAnonymous
            ? _userContextService.IsAuthenticatedAndAdmin ||
            (_userContextService.IsAuthenticated && _userContextService.AccountIdOrThrow == authorId)
                ? (username + " (anonymous)", authorId)
                : ("Anonymous", null)
            : (username, authorId);
    }
}
