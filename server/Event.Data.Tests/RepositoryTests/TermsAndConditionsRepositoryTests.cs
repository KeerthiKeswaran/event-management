using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Event.Models;
using Event.Data.Repositories;

namespace Event.Data.Tests.RepositoryTests
{
    [TestFixture]
    public class TermsAndConditionsRepositoryTests : RepositoryTestBase
    {
        private TermsAndConditionsRepository? _repository;
        private const string Repo = "TermsAndConditionsRepository";

        #region Setup
        [SetUp]
        public void SetUp()
        {
            _repository = new TermsAndConditionsRepository(Context);
        }

        private TermsAndConditions CreateMockTerms(string version, bool isActive, string type = "General")
        {
            return new TermsAndConditions
            {
                Terms_Id = Guid.NewGuid().ToString("N"),
                Version = version,
                File_Path = $"/docs/policies/terms_{version}.md",
                Is_Active = isActive,
                Type = type,
                Created_At = DateTime.UtcNow
            };
        }
        #endregion

        #region Create Tests
        [Test]
        public async Task Test_AddAsync()
        {
            var terms = CreateMockTerms("v1.0-add", true);

            try
            {
                await _repository.AddAsync(terms);
                Assert.That(terms.Terms_Id, Is.Not.Null.And.Not.Empty);
                LogTestDetail(Repo, "AddAsync", "Create terms and conditions", terms, terms, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "AddAsync", "Create terms and conditions", terms, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Read Tests
        [Test]
        public async Task Test_GetByIdAsync()
        {
            var terms = CreateMockTerms("v1.0", true);
            await _repository.AddAsync(terms);

            try
            {
                var fetched = await _repository.GetByIdAsync(terms.Terms_Id);
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Version, Is.EqualTo("v1.0"));
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve terms by ID", terms.Terms_Id, fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetByIdAsync", "Retrieve terms by ID", terms.Terms_Id, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetActiveTermsAsync()
        {
            var activeTerms = CreateMockTerms("v1.0-act", true);
            await _repository.AddAsync(activeTerms);

            try
            {
                var fetchedActive = await _repository.GetActiveTermsAsync();
                Assert.That(fetchedActive, Is.Not.Null);
                LogTestDetail(Repo, "GetActiveTermsAsync", "Retrieve current active terms", null, fetchedActive, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetActiveTermsAsync", "Retrieve current active terms", null, null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetTermsByVersionAsync()
        {
            var terms = CreateMockTerms("v0.9-ver", false);
            await _repository.AddAsync(terms);

            try
            {
                var fetched = await _repository.GetTermsByVersionAsync("v0.9-ver");
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Is_Active, Is.False);
                LogTestDetail(Repo, "GetTermsByVersionAsync", "Retrieve terms by version string", "v0.9-ver", fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetTermsByVersionAsync", "Retrieve terms by version string", "v0.9-ver", null, false, ex.Message);
                throw;
            }
        }

        [Test]
        public async Task Test_GetActiveTermsByTypeAsync()
        {
            var terms = CreateMockTerms("v1.0-type", true, "Registration");
            await _repository.AddAsync(terms);

            try
            {
                var fetched = await _repository.GetActiveTermsByTypeAsync("Registration");
                Assert.That(fetched, Is.Not.Null);
                Assert.That(fetched!.Type, Is.EqualTo("Registration"));
                LogTestDetail(Repo, "GetActiveTermsByTypeAsync", "Retrieve active terms by type", "Registration", fetched, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "GetActiveTermsByTypeAsync", "Retrieve active terms by type", "Registration", null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Update Tests
        [Test]
        public async Task Test_UpdateAsync()
        {
            var terms = CreateMockTerms("v1.0-upd", false);
            await _repository.AddAsync(terms);

            try
            {
                terms.Is_Active = true;
                await _repository.UpdateAsync(terms);
                var updated = await _repository.GetByIdAsync(terms.Terms_Id);
                Assert.That(updated!.Is_Active, Is.True);
                LogTestDetail(Repo, "UpdateAsync", "Update terms active status", terms, updated, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "UpdateAsync", "Update terms active status", terms, null, false, ex.Message);
                throw;
            }
        }
        #endregion

        #region Delete Tests
        [Test]
        public async Task Test_DeleteAsync()
        {
            var terms = CreateMockTerms("v1.0-del", true);
            await _repository.AddAsync(terms);

            try
            {
                await _repository.DeleteAsync(terms);
                var deleted = await _repository.GetByIdAsync(terms.Terms_Id);
                Assert.That(deleted, Is.Null);
                LogTestDetail(Repo, "DeleteAsync", "Remove terms from database", terms, deleted, true);
            }
            catch (Exception ex)
            {
                LogTestDetail(Repo, "DeleteAsync", "Remove terms from database", terms, null, false, ex.Message);
                throw;
            }
        }
        #endregion
    }
}
