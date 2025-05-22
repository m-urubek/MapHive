namespace MapHive.Services;

public interface IUsernamesService
{
    public (string username, int? authorId) GetAnonymizedUser(string username, int? authorId, bool isAnonymous);
}
