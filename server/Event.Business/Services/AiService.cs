using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Contracts.IServices;
using Microsoft.Extensions.Configuration;

namespace Event.Business.Services
{
    public class AiService : IAiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _baseUrl;
        private readonly string _descriptionModel;
        private readonly IFileStorageService _fileStorageService;

        public AiService(HttpClient httpClient, IConfiguration configuration, IFileStorageService fileStorageService)
        {
            _httpClient = httpClient;
            var groqSection = configuration.GetSection("Groq");
            _apiKey = groqSection["ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is missing.");
            _baseUrl = groqSection["BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
            _descriptionModel = groqSection["DescriptionModel"] ?? "llama3-8b-8192";
            _fileStorageService = fileStorageService;
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> GenerateEventDescriptionAsync(string keywords)
        {
            string systemPrompt = "You are a professional copywriter who writes in pure HTML.";
            try
            {
                systemPrompt = await _fileStorageService.ReadTextAsync("agents/desc-prompt.txt");
            }
            catch
            {
                // Fallback to default if file is missing in storage
            }

            var requestBody = new
            {
                model = _descriptionModel,
                messages = new[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Keywords: {keywords}" }
                },
                temperature = 0.7
            };

            var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(_baseUrl, jsonContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorMsg = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to generate description from Groq: {response.StatusCode} - {errorMsg}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var resultHtml = doc.RootElement
                                .GetProperty("choices")[0]
                                .GetProperty("message")
                                .GetProperty("content")
                                .GetString();

            if (resultHtml == null) return string.Empty;

            // Strip potential markdown wrappers just in case
            if (resultHtml.StartsWith("```html"))
            {
                resultHtml = resultHtml.Substring(7);
                if (resultHtml.EndsWith("```"))
                {
                    resultHtml = resultHtml.Substring(0, resultHtml.Length - 3);
                }
            }

            return resultHtml.Trim();
        }
    }
}
