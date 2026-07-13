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

        public AiService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            var groqSection = configuration.GetSection("Groq");
            _apiKey = groqSection["ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey is missing.");
            _baseUrl = groqSection["BaseUrl"] ?? "https://api.groq.com/openai/v1/chat/completions";
            _descriptionModel = groqSection["DescriptionModel"] ?? "llama3-8b-8192";
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<string> GenerateEventDescriptionAsync(string keywords)
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

            var promptFilePath = Path.Combine(rootPath, "Event.Business", "assets", "agents", "desc-prompt.txt");
            string systemPrompt = "You are a professional copywriter who writes in pure HTML.";
            if (File.Exists(promptFilePath))
            {
                systemPrompt = await File.ReadAllTextAsync(promptFilePath);
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
