using System;
using System.IO;
using System.Threading.Tasks;
using Event.Contracts.IRepositories;
using Event.Contracts.IServices;
using Event.Models.DTOs;

namespace Event.Business.Services
{
    public class PolicyService : IPolicyService
    {
        private readonly ITermsAndConditionsRepository _termsRepository;

        public PolicyService(ITermsAndConditionsRepository termsRepository)
        {
            _termsRepository = termsRepository;
        }

        public async Task<PolicyResponse?> GetPolicyByTypeAsync(string type)
        {
            #region GetPolicyByTypeAsync
            // 1. Fetch the active policy of the specified type from the database
            var policy = await _termsRepository.GetActiveTermsByTypeAsync(type);
            if (policy == null)
            {
                return null;
            }

            // 2. Construct and return the policy response DTO with only metadata
            return new PolicyResponse
            {
                TermsId = policy.Terms_Id,
                Version = policy.Version,
                FilePath = policy.File_Path
            };
            #endregion
        }
    }
}
