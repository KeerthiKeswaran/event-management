using System.Collections.Generic;
using System.Threading.Tasks;
using Event.Models.DTOs;

namespace Event.Contracts.IServices
{
    public interface IIntentClassificationService
    {
        Task<bool> IsValidEventIntentAsync(string userMessage);
    }

    public interface IAgentService
    {
        Task<ChatResponseDto> ProcessAgentRequestAsync(string userId, List<ChatMessageDto> messages, Action<string>? onProgress = null);
    }
}
