using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class QrCodeServiceTests : ServiceTestBase
    {
        private QrCodeService _qrCodeService = null!;

        private const string Service = "QrCodeService";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _qrCodeService = new QrCodeService();
        }
        #endregion

        #region Generate QR Code Tests
        [Test]
        public async Task Test_GenerateQrCodeAsync_Success()
        {
            string testText = "booking_secret_hash_123456";

            try
            {
                var bytes = await _qrCodeService.GenerateQrCodeAsync(testText);
                Assert.That(bytes, Is.Not.Null);
                Assert.That(bytes.Length, Is.GreaterThan(0));
                LogTestDetail(Service, "GenerateQrCodeAsync", "Generate QR Code bytes from secret text", testText, $"Bytes generated: {bytes.Length}", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GenerateQrCodeAsync", "Generate QR Code bytes from secret text", testText, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public void Test_GenerateQrCodeAsync_EmptyText_ThrowsArgumentException()
        {
            try
            {
                Assert.ThrowsAsync<ArgumentException>(async () =>
                    await _qrCodeService.GenerateQrCodeAsync(string.Empty)
                );
                LogTestDetail(Service, "GenerateQrCodeAsync", "Generate QR with empty text throws exception", string.Empty, "ArgumentException", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GenerateQrCodeAsync", "Generate QR with empty text throws exception", string.Empty, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
