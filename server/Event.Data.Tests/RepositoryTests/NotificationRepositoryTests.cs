using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class NotificationRepositoryTests : RepositoryTestBase
    {
        private NotificationRepository? _repository;
        private const string Repo = "NotificationRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new NotificationRepository(Context);
        }

        private Notification CreateMockNotification(string recipient, string status, int retryCount = 0)
        {
            return new Notification
            {
                Recipient_Email = recipient,
                MessageUrl = "Event.Business/assets/notifications/test_notification.json",
                Status = status,
                Retry_Count = retryCount,
                Created_At = DateTime.UtcNow
            };
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var notification = CreateMockNotification("user1@example.com", "Pending");

            try
            {
                await _repository.AddAsync(notification);
                Assert.That(notification.Notification_Id, Is.GreaterThan(0));
                LogTestDetail(Repo, "AddAsync", "Create notification", notification, notification, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create notification", notification, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var notification = CreateMockNotification("user1@example.com", "Pending");
            await _repository.AddAsync(notification);

            try
            {
                var fetched = await _repository.GetByIdAsync(notification.Notification_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Recipient_Email, Is.EqualTo("user1@example.com"));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve notification by ID", notification.Notification_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve notification by ID", notification.Notification_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPendingNotificationsAsync()
        {
            var pending1 = CreateMockNotification("user1@example.com", "Pending");
            var pending2 = CreateMockNotification("user2@example.com", "Pending");
            await _repository.AddAsync(pending1);
            await _repository.AddAsync(pending2);

            try
            {
                var pendingBatch = await _repository.GetPendingNotificationsAsync(10);
                Assert.That(pendingBatch.Count(), Is.GreaterThanOrEqualTo(2));
                LogTestDetail(Repo, "GetPendingNotificationsAsync", "Retrieve pending notifications batch", 10, pendingBatch.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetPendingNotificationsAsync", "Retrieve pending notifications batch", 10, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetFailedNotificationsForRetryAsync()
        {
            var failed1 = CreateMockNotification("user3@example.com", "Failed", 1);
            var failedMax = CreateMockNotification("user4@example.com", "Failed", 3);
            await _repository.AddAsync(failed1);
            await _repository.AddAsync(failedMax);

            try
            {
                var retryBatch = await _repository.GetFailedNotificationsForRetryAsync(3, 10);
                Assert.That(retryBatch.Any(n => n.Notification_Id == failed1.Notification_Id), Is.True);
                Assert.That(retryBatch.Any(n => n.Notification_Id == failedMax.Notification_Id), Is.False);
                LogTestDetail(Repo, "GetFailedNotificationsForRetryAsync", "Retrieve failed notifications under max retries limit", new { MaxRetries = 3, Limit = 10 }, retryBatch.Count(), true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetFailedNotificationsForRetryAsync", "Retrieve failed notifications under max retries limit", new { MaxRetries = 3, Limit = 10 }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var notification = CreateMockNotification("user1@example.com", "Pending");
            await _repository.AddAsync(notification);

            try
            {
                notification.Status = "Sent";
                notification.Sent_At = DateTime.UtcNow;
                await _repository.UpdateAsync(notification);
                var updated = await _repository.GetByIdAsync(notification.Notification_Id);
                Assert.That(updated!.Status, Is.EqualTo("Sent"));
                LogTestDetail(Repo, "UpdateAsync", "Update notification status to Sent", notification, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update notification status to Sent", notification, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var notification = CreateMockNotification("user1@example.com", "Pending");
            await _repository.AddAsync(notification);

            try
            {
                await _repository.DeleteAsync(notification);
                var deleted = await _repository.GetByIdAsync(notification.Notification_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove notification from database", notification, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove notification from database", notification, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
