using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Models.DTOs;
using Event.Contracts.IServices;
using Microsoft.Extensions.Configuration;

namespace Event.Business.Services
{
    public class IntentClassificationService : IIntentClassificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _intentModel;

        public IntentClassificationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var groqSection = configuration.GetSection("Groq");
            _apiKey = groqSection["ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is missing.");
            _baseUrl = groqSection["BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
            _intentModel = groqSection["IntentModel"] ?? "llama-3.1-8b-instant";
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<bool> IsValidEventIntentAsync(string userMessage)
        {
            string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (rootPath.Contains("bin"))
            {
                rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else if (rootPath.EndsWith("Event.API") || rootPath.EndsWith("Event.Business.Tests") || rootPath.EndsWith("Event.Business"))
            {
                rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            
            string promptPath = Path.Combine(rootPath, "Event.Business", "assets", "agents", "classifier-prompt.txt");
            string systemPrompt = await File.ReadAllTextAsync(promptPath);

            var requestBody = new GroqChatRequest
            {
                Model = _intentModel,
                Messages = new List<ChatMessageDto>
                {
                    new ChatMessageDto { Role = "system", Content = systemPrompt },
                    new ChatMessageDto { Role = "user", Content = userMessage }
                }
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(_baseUrl, jsonContent);
            
            if (!response.IsSuccessStatusCode)
            {
                // Fallback to true if API fails so we don't completely block the user
                return true; 
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var groqResponse = JsonSerializer.Deserialize<GroqChatResponse>(responseJson);
            
            var answer = groqResponse?.Choices?[0]?.Message?.Content?.Trim().ToUpper() ?? "";
            
            return !answer.Contains("INVALID");
        }
    }
}
