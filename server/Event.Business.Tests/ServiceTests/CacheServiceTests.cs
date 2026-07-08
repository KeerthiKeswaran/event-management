using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using NUnit.Framework;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class CacheServiceTests : ServiceTestBase
    {
        private Mock<IDistributedCache> _cacheMock = null!;
        private CacheService _cacheService = null!;
        private const string Service = "CacheService";

        [SetUp]
        public void SetUp()
        {
            _cacheMock = new Mock<IDistributedCache>();
            _cacheService = new CacheService(_cacheMock.Object);
        }

        #region Test_SetAsync_Success
        [Test]
        public async Task Test_SetAsync_Success()
        {
            // Arrange
            string key = "test-key";
            var value = new { Name = "Event 1" };
            byte[] expectedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value));

            _cacheMock.Setup(c => c.SetAsync(
                key,
                It.Is<byte[]>(b => Encoding.UTF8.GetString(b) == JsonSerializer.Serialize(value)),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            try
            {
                // Act
                await _cacheService.SetAsync(key, value, TimeSpan.FromMinutes(10));

                // Assert
                _cacheMock.Verify(c => c.SetAsync(key, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
                LogTestDetail(Service, "SetAsync", "Set key-value pair in cache successfully", new { Key = key, Value = value }, "Success", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SetAsync", "Set key-value pair in cache successfully", new { Key = key, Value = value }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_SetAsync_EmptyKey_DoesNothing
        [Test]
        public async Task Test_SetAsync_EmptyKey_DoesNothing()
        {
            // Arrange
            string key = "";
            var value = "some-value";

            try
            {
                // Act
                await _cacheService.SetAsync(key, value);

                // Assert
                _cacheMock.Verify(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
                LogTestDetail(Service, "SetAsync", "Set with empty key does nothing", new { Key = key, Value = value }, "Success (No action)", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "SetAsync", "Set with empty key does nothing", new { Key = key, Value = value }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetAsync_Success
        [Test]
        public async Task Test_GetAsync_Success()
        {
            // Arrange
            string key = "get-key";
            var expectedObj = new { Id = 123 };
            byte[] cachedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(expectedObj));

            _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(cachedBytes);

            try
            {
                // Act
                var result = await _cacheService.GetAsync<dynamic>(key);

                // Assert
                Assert.That(result, Is.Not.Null);
                LogTestDetail(Service, "GetAsync", "Retrieve value from cache successfully", new { Key = key }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAsync", "Retrieve value from cache successfully", new { Key = key }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetAsync_EmptyKey_ReturnsDefault
        [Test]
        public async Task Test_GetAsync_EmptyKey_ReturnsDefault()
        {
            try
            {
                // Act
                var result = await _cacheService.GetAsync<string>("");

                // Assert
                Assert.That(result, Is.Null);
                LogTestDetail(Service, "GetAsync", "Retrieve with empty key returns default", new { Key = "" }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAsync", "Retrieve with empty key returns default", new { Key = "" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetAsync_EmptyJson_ReturnsDefault
        [Test]
        public async Task Test_GetAsync_EmptyJson_ReturnsDefault()
        {
            // Arrange
            string key = "empty-json-key";
            _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync((byte[]?)null);

            try
            {
                // Act
                var result = await _cacheService.GetAsync<string>(key);

                // Assert
                Assert.That(result, Is.Null);
                LogTestDetail(Service, "GetAsync", "Retrieve with empty JSON returns default", new { Key = key }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAsync", "Retrieve with empty JSON returns default", new { Key = key }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_GetAsync_InvalidJson_ReturnsDefault
        [Test]
        public async Task Test_GetAsync_InvalidJson_ReturnsDefault()
        {
            // Arrange
            string key = "invalid-json-key";
            byte[] invalidBytes = Encoding.UTF8.GetBytes("{invalid-json}");
            _cacheMock.Setup(c => c.GetAsync(key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(invalidBytes);

            try
            {
                // Act
                var result = await _cacheService.GetAsync<dynamic>(key);

                // Assert
                Assert.That(result, Is.Null);
                LogTestDetail(Service, "GetAsync", "Deserialization error returns default", new { Key = key }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetAsync", "Deserialization error returns default", new { Key = key }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RemoveAsync_Success
        [Test]
        public async Task Test_RemoveAsync_Success()
        {
            // Arrange
            string key = "remove-key";
            _cacheMock.Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            try
            {
                // Act
                await _cacheService.RemoveAsync(key);

                // Assert
                _cacheMock.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
                LogTestDetail(Service, "RemoveAsync", "Remove key from cache successfully", new { Key = key }, "Success", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RemoveAsync", "Remove key from cache successfully", new { Key = key }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Test_RemoveAsync_EmptyKey_DoesNothing
        [Test]
        public async Task Test_RemoveAsync_EmptyKey_DoesNothing()
        {
            try
            {
                // Act
                await _cacheService.RemoveAsync("");

                // Assert
                _cacheMock.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
                LogTestDetail(Service, "RemoveAsync", "Remove with empty key does nothing", new { Key = "" }, "Success (No action)", true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "RemoveAsync", "Remove with empty key does nothing", new { Key = "" }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
