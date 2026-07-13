using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Event.Models.DTOs;
using Microsoft.Extensions.Caching.Distributed;

namespace Event.Business.Services
{
    public class ChatHistoryService : IChatHistoryService
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _cacheOptions;

        public ChatHistoryService(IDistributedCache cache)
        {
            _cache = cache;
            // 24 hours TTL as requested
            _cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            };
        }

        private string GetUserKey(int userId) => $"user:{userId}:chatsessions";

        public async Task<List<ChatSessionSummaryDto>> GetChatSessionsAsync(int userId)
        {
            var key = GetUserKey(userId);
            var json = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(json))
                return new List<ChatSessionSummaryDto>();

            var sessions = JsonSerializer.Deserialize<List<ChatSessionDto>>(json);
            if (sessions == null)
                return new List<ChatSessionSummaryDto>();

            // Return summaries sorted by most recently updated
            return sessions
                .OrderByDescending(s => s.UpdatedAt)
                .Select(s => new ChatSessionSummaryDto
                {
                    SessionId = s.SessionId,
                    Title = s.Title,
                    UpdatedAt = s.UpdatedAt
                }).ToList();
        }

        public async Task<ChatSessionDto?> GetChatSessionAsync(int userId, string sessionId)
        {
            var key = GetUserKey(userId);
            var json = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(json))
                return null;

            var sessions = JsonSerializer.Deserialize<List<ChatSessionDto>>(json);
            return sessions?.FirstOrDefault(s => s.SessionId == sessionId);
        }

        public async Task SaveChatSessionAsync(int userId, ChatSessionDto session)
        {
            var key = GetUserKey(userId);
            var json = await _cache.GetStringAsync(key);
            
            List<ChatSessionDto> sessions = new List<ChatSessionDto>();
            if (!string.IsNullOrEmpty(json))
            {
                sessions = JsonSerializer.Deserialize<List<ChatSessionDto>>(json) ?? new List<ChatSessionDto>();
            }

            // Remove existing if present
            var existing = sessions.FirstOrDefault(s => s.SessionId == session.SessionId);
            if (existing != null)
            {
                sessions.Remove(existing);
            }

            session.UpdatedAt = DateTime.UtcNow;
            
            // Generate title from first message if empty
            if (string.IsNullOrWhiteSpace(session.Title) && session.Messages.Any())
            {
                var firstUserMsg = session.Messages.FirstOrDefault(m => m.Role == "user")?.Content;
                if (!string.IsNullOrEmpty(firstUserMsg))
                {
                    session.Title = firstUserMsg.Length > 30 ? firstUserMsg.Substring(0, 30) + "..." : firstUserMsg;
                }
                else
                {
                    session.Title = "New Chat";
                }
            }

            sessions.Add(session);

            // Save back to cache, sliding expiration for the entire list
            var updatedJson = JsonSerializer.Serialize(sessions);
            await _cache.SetStringAsync(key, updatedJson, _cacheOptions);
        }

        public async Task DeleteChatSessionAsync(int userId, string sessionId)
        {
            var key = GetUserKey(userId);
            var json = await _cache.GetStringAsync(key);
            
            if (string.IsNullOrEmpty(json))
                return;

            var sessions = JsonSerializer.Deserialize<List<ChatSessionDto>>(json);
            if (sessions != null)
            {
                var existing = sessions.FirstOrDefault(s => s.SessionId == sessionId);
                if (existing != null)
                {
                    sessions.Remove(existing);
                    var updatedJson = JsonSerializer.Serialize(sessions);
                    await _cache.SetStringAsync(key, updatedJson, _cacheOptions);
                }
            }
        }
    }
}
