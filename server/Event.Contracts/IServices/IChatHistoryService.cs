using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IChatHistoryService
    {
        Task<List<ChatSessionSummaryDto>> GetChatSessionsAsync(int userId);
        Task<ChatSessionDto?> GetChatSessionAsync(int userId, string sessionId);
        Task SaveChatSessionAsync(int userId, ChatSessionDto session);
        Task DeleteChatSessionAsync(int userId, string sessionId);
    }
}
