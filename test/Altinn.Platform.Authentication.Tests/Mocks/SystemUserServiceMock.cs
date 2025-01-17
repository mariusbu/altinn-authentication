﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

using Altinn.Platform.Authentication.Core.Models;
using Altinn.Platform.Authentication.Services.Interfaces;

namespace Altinn.Platform.Authentication.Tests.Mocks
{
    /// <summary>
    /// The service that supports the SystemUser CRUD APIcontroller
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SystemUserServiceMock : ISystemUserService
    {
        private readonly List<SystemUser> theMockList;

        /// <summary>
        /// The Constructor
        /// </summary>
        public SystemUserServiceMock()
        {
            theMockList = MockDataHelper();
        }

        /// <summary>
        /// Creates a new SystemUser
        /// The unique Id for the systemuser is handled by the db.
        /// But the calling client may send a guid for the request of creating a new system user
        /// to ensure that there is no mismatch if the same partyId creates several new SystemUsers at the same time
        /// </summary>
        /// <returns></returns>
        public Task<SystemUser> CreateSystemUser(SystemUserRequestDto request, string partyOrgNo)
        {
            SystemUser newSystemUser = new()
            {
                Id = Guid.NewGuid().ToString(),
                IntegrationTitle = request.IntegrationTitle,
                SystemId = request.SystemId,
                ReporteeOrgNo = partyOrgNo
            };
            theMockList.Add(newSystemUser);
            return Task.FromResult(newSystemUser);
        }

        /// <summary>
        /// Returns the list of SystemUsers this PartyID has registered, including "deleted" ones.
        /// </summary>
        /// <returns></returns>
        public Task<List<SystemUser>> GetListOfSystemUsersForParty(int partyId)
        {
            if (partyId < 1)
            {
                return Task.FromResult<List<SystemUser>>(null);
            }

            return Task.FromResult(theMockList);
        }

        /// <summary>
        /// Return a single SystemUser by PartyId and SystemUserId
        /// </summary>
        /// <returns></returns>
        public Task<SystemUser> GetSingleSystemUserById(Guid systemUserId)
        {
            SystemUser search = theMockList.Find(s => s.Id == systemUserId.ToString());

            return Task.FromResult(search);
        }

        /// <summary>
        /// Set the Delete flag on the identified SystemUser
        /// </summary>
        /// <returns></returns>
        public Task<bool> SetDeleteFlagOnSystemUser(Guid systemUserId)
        {
            SystemUser toBeDeleted = theMockList.Find(s => s.Id == systemUserId.ToString());
            toBeDeleted.IsDeleted = true;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Replaces the values for the existing system user with those from the update 
        /// </summary>
        /// <returns></returns>
        public Task<int> UpdateSystemUserById(SystemUserUpdateDto request)
        {
            int array = theMockList.FindIndex(su => su.Id == request.Id.ToString());
            theMockList[array].IntegrationTitle = request.IntegrationTitle;
            theMockList[array].SystemId = request.SystemId;
            return Task.FromResult(1);
        }

        /// <summary>
        /// Helper method during development, just some Mock data.
        /// </summary>
        /// <returns></returns>        
        private static List<SystemUser> MockDataHelper()
        {
            SystemUser systemUser1 = new()
            {
                Id = "37ce1792-3b35-4d50-a07d-636017aa7dbd",
                IntegrationTitle = "Vårt regnskapsystem",
                SystemId = "supplier_name_cool_system",
                PartyId = "orgno:91235123",
                IsDeleted = false,
                SupplierName = "Supplier1 Name",
                SupplierOrgNo = "123456789"
            };

            SystemUser systemUser2 = new()
            {
                Id = "37ce1792-3b35-4d50-a07d-636017aa7dbe",
                IntegrationTitle = "Vårt andre regnskapsystem",
                SystemId = "supplier2_product_name",
                PartyId = "orgno:91235124",
                IsDeleted = false,
                SupplierName = "Supplier2 Name",
                SupplierOrgNo = "123456789"
            };

            SystemUser systemUser3 = new()
            {
                Id = "37ce1792-3b35-4d50-a07d-636017aa7dbf",
                IntegrationTitle = "Et helt annet system",
                SystemId = "supplier3_product_name",
                PartyId = "orgno:91235125",
                IsDeleted = false,
                SupplierName = "Supplier3 Name",
                SupplierOrgNo = "123456789"
            };

            List<SystemUser> systemUserList = new()
        {
            systemUser1,
            systemUser2,
            systemUser3
        };
            return systemUserList;
        }

        public Task<SystemUser> CheckIfPartyHasIntegration(string clientId, string consumerId, string systemOrg, CancellationToken cancellationToken)
        {
            return Task.FromResult( new SystemUser 
            {
                Id = "37ce1792-3b35-4d50-a07d-636017aa7dbf",
                IntegrationTitle = "Et helt annet system",
                SystemId = "supplier3_product_name",
                PartyId = "orgno:" + systemOrg,
                IsDeleted = false,
                SupplierName = "Supplier3 Name",
                SupplierOrgNo = consumerId
            });
        }

        public Task<SystemUser> CreateSystemUser(string party, SystemUserRequestDto request)
        {
            throw new NotImplementedException();
        }
    }
}
