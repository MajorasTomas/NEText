using NEText.Api.Dtos;
using NEText.Api.Entities;

namespace NEText.Api.Services;

public interface IConversationService
{
    Task<(ConversationDto Conversation, bool Created)> StartAsync(Guid myId, Guid otherUserId);

    Task<List<ConversationDto>> GetMyConversationsAsync(Guid myId, ConversationType? type);

    Task<ConversationDto> GetAsync(Guid myId, Guid conversationId);

    Task MarkAsReadAsync(Guid myId, Guid conversationId);

    Task<List<MessageDto>> GetMessagesAsync(Guid myId, Guid conversationId, MessageListQuery options);

    Task<MessageDto> SendMessageAsync(Guid myId, Guid conversationId, string text);

    Task<MessageDto> EditMessageAsync(Guid myId, Guid messageId, string text);

    Task DeleteMessageAsync(Guid myId, Guid messageId);
}
