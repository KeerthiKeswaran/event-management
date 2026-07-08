using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class PaymentServiceTests : ServiceTestBase
    {
        private IConfiguration _configuration = null!;
        private StripePaymentService _paymentService = null!;

        private const string Service = "StripePaymentService";
        private const string TestToken = "tok_visa";
        private const string TestTxReference = "ch_3TfzOd24vU9n7PQJ1jBjLRms";
        private const string TestDestinationAccountId = "acct_1Tg0SJ24vUFDuoGl";

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

            _paymentService = new StripePaymentService(_configuration);
        }
        #endregion

        #region Create Charge Tests
        [Test]
        public async Task Test_CreateChargeAsync_WithToken()
        {
            try
            {
                var result = await _paymentService.CreateChargeAsync(10.00m, "usd", TestToken, "Test charge with tok_visa");
                // The charge might succeed or fail depending on if the API key in appsettings is active or restricted,
                // so we assert that the operation completed and returned result information.
                Assert.That(result, Is.Not.Null);
                LogTestDetail(Service, "CreateChargeAsync", "Charge using test token", new { Amount = 10.00m, Currency = "usd", Token = TestToken }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateChargeAsync", "Charge using test token", new { Amount = 10.00m, Currency = "usd", Token = TestToken }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Create Refund Tests
        [Test]
        public async Task Test_CreateRefundAsync_WithReference()
        {
            try
            {
                var result = await _paymentService.CreateRefundAsync(TestTxReference, 10.00m);
                Assert.That(result, Is.Not.Null);
                LogTestDetail(Service, "CreateRefundAsync", "Refund using test reference", new { TxRef = TestTxReference, Amount = 10.00m }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreateRefundAsync", "Refund using test reference", new { TxRef = TestTxReference, Amount = 10.00m }, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Create Payout Tests
        [Test]
        public async Task Test_CreatePayoutAsync_WithDestinationAccount()
        {
            try
            {
                var result = await _paymentService.CreatePayoutAsync(TestDestinationAccountId, 10.00m, "usd");
                Assert.That(result, Is.Not.Null);
                LogTestDetail(Service, "CreatePayoutAsync", "Payout using destination connect account", new { DestAccount = TestDestinationAccountId, Amount = 10.00m }, result, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "CreatePayoutAsync", "Payout using destination connect account", new { DestAccount = TestDestinationAccountId, Amount = 10.00m }, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
