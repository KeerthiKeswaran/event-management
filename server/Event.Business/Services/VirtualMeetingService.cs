using System;
using System.Threading.Tasks;
using Event.Contracts.IServices;

namespace Event.Business.Services
{
    public class VirtualMeetingService : IVirtualMeetingService
    {
        #region GenerateMeetingRoomAsync

        public Task<(string RoomUrl, string RawPasscode)> GenerateMeetingRoomAsync(string eventTitle)
        {
            if (string.IsNullOrWhiteSpace(eventTitle))
            {
                throw new ArgumentException("Event title cannot be null or empty.", nameof(eventTitle));
            }

            // Clean event title to make it URL friendly
            string sanitizedTitle = eventTitle.Replace(" ", "-");
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                sanitizedTitle = sanitizedTitle.Replace(c.ToString(), "");
            }

            // Generate unique meeting room ID
            string uniqueRoomName = $"{sanitizedTitle}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
            string roomUrl = $"https://meet.jit.si/{uniqueRoomName}";

            // Generate a random plain-text passcode
            string rawPasscode = Guid.NewGuid().ToString("N").Substring(0, 12);

            return Task.FromResult((roomUrl, rawPasscode));
        }

        #endregion
    }
}
