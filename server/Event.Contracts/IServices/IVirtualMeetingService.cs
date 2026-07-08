using System.Threading.Tasks;

namespace Event.Contracts.IServices
{
    public interface IVirtualMeetingService
    {
        Task<(string RoomUrl, string RawPasscode)> GenerateMeetingRoomAsync(string eventTitle);
    }
}
