using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody);
        Task<string> BuildEmailHtmlAsync(Event.Models.DTOs.EmailTemplateDto dto);
    }
}
