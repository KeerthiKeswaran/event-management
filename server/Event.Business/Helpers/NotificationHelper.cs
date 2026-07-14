using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Models;

namespace Event.Business.Helpers
{
    public static class NotificationHelper
    {
        public static async Task SendAndSaveNotificationAsync(
            INotificationRepository notificationRepository,
            IEmailService emailService,
            string recipientEmail,
            string subject,
            string htmlBody)
        {
            // 1. Create a Notification record to get the auto-generated ID
            var notification = new Notification
            {
                Recipient_Email = recipientEmail,
                MessageUrl = "placeholder",
                Status = "Pending",
                Retry_Count = 0,
                Created_At = DateTime.UtcNow
            };

            await notificationRepository.AddAsync(notification);

            try
            {
                // 2. Build the JSON filepath relative to the workspace
                string rootPath = Directory.GetCurrentDirectory().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (rootPath.Contains("bin") || rootPath.EndsWith("Tests"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                else if (rootPath.EndsWith("Event.API"))
                {
                    rootPath = Path.GetFullPath(Path.Combine(rootPath, "..")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }

                string notificationDir = Path.Combine(rootPath, "Event.Business", "assets", "notifications");
                if (!Directory.Exists(notificationDir))
                {
                    Directory.CreateDirectory(notificationDir);
                }

                string jsonFilePath = Path.Combine(notificationDir, $"{notification.Notification_Id}.json");

                // 3. Serialize and save Subject & Body to the JSON file
                var payload = new
                {
                    Subject = subject,
                    Body = htmlBody
                };
                string jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(jsonFilePath, jsonContent);

                // 4. Update the MessageUrl
                notification.MessageUrl = $"/assets/notifications/{notification.Notification_Id}.json";
            }
            catch (Exception)
            {
                // Ignore local file write errors so the email can still be sent
                notification.MessageUrl = "failed_to_save.json";
            }

            try
            {
                // 5. Trigger the email send directly
                await emailService.SendEmailAsync(recipientEmail, subject, htmlBody);
                notification.Status = "Sent";
                notification.Sent_At = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                notification.Status = "Failed";
                notification.ErrorMessage = ex.Message;
            }

            await notificationRepository.UpdateAsync(notification);
        }
    }
}
