using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using NUnit.Framework;
using Event.Models.DTOs;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class EmailServiceTests : ServiceTestBase
    {
        private IConfiguration _configuration = null!;
        private EmailService _emailService = null!;

        private const string Service = "EmailService";
        private const string TestEmail = "keshwarankeerthi@gmail.com";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string apiDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "Event.API"));
            string appSettingsPath = Path.Combine(apiDir, "appsettings.json");

            _configuration = new ConfigurationBuilder()
                .AddJsonFile(appSettingsPath, optional: false, reloadOnChange: false)
                .Build();

            var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ \"messageId\": \"12345\" }"),
                })
                .Verifiable();

            var httpClient = new HttpClient(handlerMock.Object);
            _emailService = new EmailService(_configuration, httpClient);
        }
        #endregion

        #region Build Email HTML Tests
        [Test]
        public async Task Test_BuildEmailHtmlAsync_FallbackTemplate_Success()
        {
            var dto = new EmailTemplateDto
            {
                TemplateName = "NonExistentTemplate.html",
                Placeholders = new Dictionary<string, string>
                {
                    { "purposeLabel", "verify registration" },
                    { "otp", "123456" },
                    { "year", "2026" }
                }
            };

            try
            {
                var html = await _emailService.BuildEmailHtmlAsync(dto);
                Assert.That(html, Is.Not.Null);
                Assert.That(html, Contains.Substring("123456"));
                Assert.That(html, Contains.Substring("verify registration"));
                LogTestDetail(Service, "BuildEmailHtmlAsync", "Build html body using fallback template", dto, html.Substring(0, Math.Min(100, html.Length)) + "...", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "BuildEmailHtmlAsync", "Build html body using fallback template", dto, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Send Email Tests
        [Test]
        public async Task Test_SendEmailAsync_Success()
        {
            string subject = "Unit Test Email Delivery";
            string body = "<h3>Test Email Content</h3><p>This is a real email sent during the automated test run.</p>";

            try
            {
                await _emailService.SendEmailAsync(TestEmail, subject, body);
                LogTestDetail(Service, "SendEmailAsync", "Send test email to Keerthi", new { TestEmail, subject }, "Sent", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SendEmailAsync", "Send test email to Keerthi", new { TestEmail, subject }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
