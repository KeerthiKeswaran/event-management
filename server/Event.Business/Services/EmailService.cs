using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class EmailService : IEmailService
    {
        #region Fields

        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly IFileStorageService _storageService;

        #endregion

        #region Constructor

        public EmailService(IConfiguration configuration, IFileStorageService storageService) : this(configuration, new HttpClient(), storageService)
        {
        }

        public EmailService(IConfiguration configuration, HttpClient httpClient, IFileStorageService storageService)
        {
            _storageService = storageService;
            // 1. Retrieve the Brevo config section and validate settings
            var section  = configuration.GetSection("Brevo");
            _apiKey      = section["ApiKey"]      ?? throw new InvalidOperationException("Brevo:ApiKey is not configured.");
            _senderEmail = section["SenderEmail"] ?? throw new InvalidOperationException("Brevo:SenderEmail is not configured.");
            _senderName  = section["SenderName"]  ?? "GetMyEvents";

            // 2. Initialize the HttpClient with target Brevo API headers
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("api-key", _apiKey);
        }

        #endregion

        #region SendEmailAsync

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            string? base64Logo = null;
            try
            {
                var logoBytes = await _storageService.ReadBytesAsync("logo.png");
                if (logoBytes != null && logoBytes.Length > 0)
                    base64Logo = Convert.ToBase64String(logoBytes);
            }
            catch
            {
                // Logo not found in storage — email sends without logo
            }

            object payload;
            if (base64Logo != null)
            {
                payload = new
                {
                    sender = new { name = _senderName, email = _senderEmail },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    htmlContent = htmlBody,
                    attachment = new[]
                    {
                        new { content = base64Logo, name = "logo.png" }
                    }
                };
            }
            else
            {
                // If logo is missing, replace cid: reference with a fallback URL or remove it to prevent Brevo 400 error
                htmlBody = htmlBody.Replace("cid:logo.png", "https://getmyevents.blob.core.windows.net/assets/logo.png"); // Or similar fallback

                payload = new
                {
                    sender = new { name = _senderName, email = _senderEmail },
                    to = new[] { new { email = toEmail } },
                    subject = subject,
                    htmlContent = htmlBody
                };
            }

            // 2. Serialize payload to JSON and create string content representation
            string jsonPayload = JsonSerializer.Serialize(payload);
            using var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // 3. Post transaction request to Brevo SMTP endpoint
            var response = await _httpClient.PostAsync("https://api.brevo.com/v3/smtp/email", content);

            // 4. Validate success status code or throw descriptive exception on failure
            if (!response.IsSuccessStatusCode)
            {
                string errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to send email via Brevo. Status: {response.StatusCode}, Details: {errorResponse}");
            }
        }

        #endregion

        #region BuildEmailHtmlAsync

        public async Task<string> BuildEmailHtmlAsync(Event.Models.DTOs.EmailTemplateDto dto)
        {
            string templatePath = $"templates/{dto.TemplateName}";
            string htmlContent = await _storageService.ReadTextAsync(templatePath);

            // 2. Load HTML body from Blob or fallback to inline defaults
            if (string.IsNullOrEmpty(htmlContent))
            {
                htmlContent = GetDefaultFallbackTemplate();
            }

            // 3. Replace placeholders with dynamic content values
            foreach (var placeholder in dto.Placeholders)
            {
                htmlContent = htmlContent.Replace($"{{{placeholder.Key}}}", placeholder.Value);
            }

            return htmlContent;
        }

        #endregion

        #region GetDefaultFallbackTemplate

        private string GetDefaultFallbackTemplate()
        {
            return @"<!DOCTYPE html>
                    <html>
                    <head>
                      <meta charset=""utf-8"" />
                      <style>
                        body { font-family: 'Segoe UI', Arial, sans-serif; background-color: #ffffff; color: #000000; margin: 0; padding: 0; }
                        .container { max-width: 480px; margin: 40px auto; background-color: #ffffff; border: 3px solid #000000; border-radius: 0px; overflow: hidden; }
                        .header { background-color: #8B0000; padding: 32px; text-align: center; border-bottom: 3px solid #000000; }
                        .header h1 { color: #ffffff; margin: 0; font-size: 24px; font-weight: 800; text-transform: uppercase; letter-spacing: 2px; }
                        .body { padding: 40px; }
                        .body p { color: #000000; font-size: 16px; line-height: 1.6; margin: 0 0 20px; }
                        .otp-box { background-color: #000000; border: 2px solid #8B0000; text-align: center; padding: 24px; margin: 30px 0; }
                        .otp-code { font-size: 38px; font-weight: 900; letter-spacing: 12px; color: #ffffff; }
                        .note { font-size: 13px; color: #000000; margin-top: 20px; border-left: 3px solid #8B0000; padding-left: 12px; }
                        .footer { background-color: #000000; color: #ffffff; padding: 20px; text-align: center; font-size: 12px; border-top: 3px solid #000000; letter-spacing: 1px; }
                      </style>
                    </head>
                    <body>
                      <div class=""container"">
                        <div class=""header""><h1>🎟 GetMyEvents</h1></div>
                        <div class=""body"">
                          <p>Hello,</p>
                          <p>Use the one-time passcode below to <strong>{purposeLabel}</strong>:</p>
                          <div class=""otp-box""><div class=""otp-code"">{otp}</div></div>
                          <p class=""note"">This OTP is valid for 10 minutes and can only be used once. Do not share it with anyone.</p>
                          <p class=""note"">If you did not request this, please ignore this email.</p>
                        </div>
                        <div class=""footer"">&copy; {year} GetMyEvents. All rights reserved.</div>
                      </div>
                    </body>
                    </html>";
        }

        #endregion
    }
}
