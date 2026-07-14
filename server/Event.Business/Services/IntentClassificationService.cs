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
        private readonly IFileStorageService _fileStorageService;

        public IntentClassificationService(HttpClient httpClient, IConfiguration configuration, IFileStorageService fileStorageService)
        {
            _httpClient = httpClient;
            var groqSection = configuration.GetSection("Groq");
            _apiKey = groqSection["ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is missing.");
            _baseUrl = groqSection["BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
            _intentModel = groqSection["IntentModel"] ?? "llama-3.1-8b-instant";
            
            _fileStorageService = fileStorageService;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<bool> IsValidEventIntentAsync(string userMessage)
        {
            string systemPrompt = await _fileStorageService.ReadTextAsync("agents/classifier-prompt.txt");

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
