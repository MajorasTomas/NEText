using NEText.Api.Dtos;

namespace NEText.Api.Services;

// Conversations and their messages. "myId" = the logged-in user.
// Anything in a conversation I'm not part of behaves as if it doesn't exist (404).
public interface IConversationService
{
    // Returns the existing 1:1 conversation with that user, or creates one (only with friends).
    // Created = true if a new conversation was made (controller answers 201 instead of 200).
    Task<(ConversationDto Conversation, bool Created)> StartAsync(Guid myId, Guid otherUserId);

    // My conversations, most recent activity first.
    Task<List<ConversationDto>> GetMyConversationsAsync(Guid myId);

    Task<ConversationDto> GetAsync(Guid myId, Guid conversationId);

    // Everything sent until now counts as read for me.
    Task MarkAsReadAsync(Guid myId, Guid conversationId);

    // Newest first, at most "limit". Pass the id of the oldest message you have as "before" to get older ones.
    // includeDeleted = false leaves out deleted messages (by default they're returned as placeholders).
    Task<List<MessageDto>> GetMessagesAsync(Guid myId, Guid conversationId, Guid? before, int limit, bool includeDeleted);

    // 403 if the other user is no longer a friend (history stays readable, no new messages).
    Task<MessageDto> SendMessageAsync(Guid myId, Guid conversationId, string text);

    // Own messages only (403 otherwise), 409 if deleted.
    Task<MessageDto> EditMessageAsync(Guid myId, Guid messageId, string text);

    // Own messages only. Soft delete: the message stays, shown as deleted. Deleting twice is fine.
    Task DeleteMessageAsync(Guid myId, Guid messageId);
}
