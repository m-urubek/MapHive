namespace MapHive.Services;

using System.Collections.Generic;
using MapHive.Models.Data.DbTableModels;
using MapHive.Models.PageModels;
using MapHive.Repositories;

public class DiscussionService(
    IDiscussionRepository _discussionRepository,
    IMapLocationRepository _mapRepository,
    IUserContextService _userContextService,
    IUsernamesService _usernamesService) : IDiscussionService
{
    public async Task<ThreadDisplayPageModel> GetThreadDisplayPageModelAsync(int threadId)
    {
        ThreadDisplayPageModel threadDisplayPageModel = await _discussionRepository.GetThreadByIdOrThrowAsync(id: threadId);

        (threadDisplayPageModel.AuthorUsername, threadDisplayPageModel.AuthorId) =
            _usernamesService.GetAnonymizedUser(
                username: threadDisplayPageModel.AuthorUsername,
                authorId: threadDisplayPageModel.AuthorId,
                isAnonymous: threadDisplayPageModel.IsAnonymous);

        IEnumerable<ThreadMessageExtended> messages = await _discussionRepository.GetMessagesByThreadIdAsync(threadId: threadId);

        foreach (ThreadMessageExtended message in messages)
        {
            (message.AuthorUsername, message.AuthorId) =
                _usernamesService.GetAnonymizedUser(
                    username: message.AuthorUsername,
                    authorId: message.AuthorId,
                    isAnonymous: threadDisplayPageModel.IsAnonymous);
        }

        return threadDisplayPageModel;
    }

    public async Task<ThreadCreatePageModel> GetThreadCreatePageModelAsync(int locationId)
    {
        LocationExtended location = await _mapRepository.GetLocationByIdAsync(id: locationId)
            ?? throw new KeyNotFoundException($"Location {locationId} not found");
        return new ThreadCreatePageModel
        {
            LocationName = location.Name,
            ThreadName = null,
            InitialMessage = null
        };
    }

    public async Task<int> CreateDiscussionThreadAsync(
        int locationId,
        string threadName,
        int? reviewId,
        bool isAnonymous,
        string initialMessage
    )
    {
        int threadId = await _discussionRepository.CreateDiscussionThreadAsync(
            locationId: locationId,
            accountId: _userContextService.AccountIdOrThrow,
            threadName: threadName,
            reviewId: reviewId,
            isAnonymous: isAnonymous);

        _ = await _discussionRepository.CreateMessageAsync(
            threadId: threadId,
            authorId: _userContextService.AccountIdOrThrow,
            messageText: initialMessage,
            isInitialMessage: true
        );
        return threadId;
    }

    public async Task DeleteMessageAsync(int messageId)
    {
        ThreadMessageExtended message = await _discussionRepository.GetMessageByIdOrThrowAsync(id: messageId);
        if (!_userContextService.IsAdminOrThrow && message.AuthorId != _userContextService.AccountIdOrThrow)
            throw new UnauthorizedAccessException();

        await _discussionRepository.DeleteMessageOrThrowAsync(id: messageId, deletedByAccountId: _userContextService.AccountIdOrThrow);
    }

    public async Task<List<ThreadInitialMessageDbModel>> GetInitialMessageThreadsPageModelByAccountIdAsync(int accountId)
    {
        List<ThreadInitialMessageDbModel> threads = await _discussionRepository.GetInitialMessageThreadsByAccountIdAsync(accountId: accountId);
        foreach (ThreadInitialMessageDbModel thread in threads)
        {
            if (thread.InitialMessageDeletedAt != null)
                thread.InitialMessageText = "";
        }
        return threads;
    }
}
