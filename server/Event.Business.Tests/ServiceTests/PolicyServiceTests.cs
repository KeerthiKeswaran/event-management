using System;
using System.IO;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Event.Models;
using Event.Contracts.IRepositories;
using Event.Business.Services;

namespace Event.Business.Tests.ServiceTests
{
    [TestFixture]
    public class PolicyServiceTests : ServiceTestBase
    {
        private Mock<ITermsAndConditionsRepository> _termsRepositoryMock = null!;
        private PolicyService _policyService = null!;
        private const string Service = "PolicyService";

        [SetUp]
        public void SetUp()
        {
            _termsRepositoryMock = new Mock<ITermsAndConditionsRepository>();
            _policyService = new PolicyService(_termsRepositoryMock.Object);
        }

        [Test]
        public async Task Test_GetPolicyByTypeAsync_Success()
        {
            var mockTerms = new TermsAndConditions
            {
                Terms_Id = "10000",
                Version = "v1.0",
                Type = "EventCreation",
                File_Path = "assets/policies/10002.md",
                Is_Active = true
            };

            _termsRepositoryMock
                .Setup(r => r.GetActiveTermsByTypeAsync("EventCreation"))
                .ReturnsAsync(mockTerms);

            try
            {
                var response = await _policyService.GetPolicyByTypeAsync("EventCreation");
                Assert.That(response, Is.Not.Null);
                Assert.That(response.TermsId, Is.EqualTo("10000"));
                Assert.That(response.Version, Is.EqualTo("v1.0"));
                Assert.That(response.FilePath, Is.EqualTo("assets/policies/10002.md"));
                LogTestDetail(Service, "GetPolicyByTypeAsync", "Retrieve active policy by type successfully", "EventCreation", response, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetPolicyByTypeAsync", "Retrieve active policy by type successfully", "EventCreation", null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetPolicyByTypeAsync_NotFound_ReturnsNull()
        {
            _termsRepositoryMock
                .Setup(r => r.GetActiveTermsByTypeAsync("NonExistent"))
                .ReturnsAsync((TermsAndConditions?)null);

            try
            {
                var response = await _policyService.GetPolicyByTypeAsync("NonExistent");
                Assert.That(response, Is.Null);
                LogTestDetail(Service, "GetPolicyByTypeAsync", "Retrieve non-existent policy by type returns null", "NonExistent", response, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Service, "GetPolicyByTypeAsync", "Retrieve non-existent policy by type returns null", "NonExistent", null, false, ex.Message);
                throw;
            }
        }
    }
}
