using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class VirtualMeetingServiceTests : ServiceTestBase
    {
        private VirtualMeetingService _meetingService = null!;

        private const string Service = "VirtualMeetingService";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _meetingService = new VirtualMeetingService();
        }
        #endregion

        #region Test_GenerateMeetingRoomAsync_Success
        [Test]
        public async Task Test_GenerateMeetingRoomAsync_Success()
        {
            string title = "My Awesome Dev Conference";

            try
            {
                var result = await _meetingService.GenerateMeetingRoomAsync(title);
                Assert.That(result.RoomUrl, Is.Not.Null);
                Assert.That(result.RoomUrl, Contains.Substring("meet.jit.si"));
                Assert.That(result.RawPasscode, Is.Not.Null);
                Assert.That(result.RawPasscode.Length, Is.EqualTo(12));
                LogTestDetail(Service, "GenerateMeetingRoomAsync", "Generate virtual Jitsi meeting link", title, new { RoomUrl = result.RoomUrl, Passcode = result.RawPasscode }, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GenerateMeetingRoomAsync", "Generate virtual Jitsi meeting link", title, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GenerateMeetingRoomAsync_EmptyTitle_ThrowsArgumentException
        [Test]
        public void Test_GenerateMeetingRoomAsync_EmptyTitle_ThrowsArgumentException()
        {
            try
            {
                Assert.ThrowsAsync<ArgumentException>(async () =>
                    await _meetingService.GenerateMeetingRoomAsync(null!)
                );
                LogTestDetail(Service, "GenerateMeetingRoomAsync", "Empty event title throws exception", null, "ArgumentException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GenerateMeetingRoomAsync", "Empty event title throws exception", null, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
