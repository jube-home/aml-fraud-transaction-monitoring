/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Repository.VisualisationRegistryDatasource
{
    using VisualisationRegistryDatasourceService =
        global::Jube.Service.Repository.VisualisationRegistryDatasource.VisualisationRegistryDatasourceService;

    public abstract class VisualisationRegistryDatasourceServiceTestBase(DatabaseFixture fx) : IAsyncLifetime
    {
        protected const int DatasourcePermissionSpec = 33;

        protected static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        protected readonly List<int> createdDatasourceIds = [];
        protected readonly List<DbContext> openContexts = [];
        protected readonly List<int> createdParameterIds = [];
        protected readonly List<int> createdRoleIds = [];
        protected readonly List<int> createdTenantIds = [];
        protected readonly List<string> createdUserNames = [];
        protected readonly List<int> createdVisualisationRegistryIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceSeries>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasourceVersion>()
                .Where(w => createdDatasourceIds.Contains(w.VisualisationRegistryDatasourceId ?? 0)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryDatasource>()
                .Where(w => createdDatasourceIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryParameter>()
                .Where(w => createdParameterIds.Contains(w.Id)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.VisualisationRegistryVersion>()
                .Where(w => createdVisualisationRegistryIds.Contains(w.VisualisationRegistryId)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.VisualisationRegistry>()
                .Where(w => createdVisualisationRegistryIds.Contains(w.Id)).DeleteAsync();

            foreach (var userName in createdUserNames)
            {
                await dbContext.GetTable<Data.Poco.UserInTenant>().Where(w => w.User == userName).DeleteAsync();
                await dbContext.GetTable<Data.Poco.UserRegistry>().Where(w => w.Name == userName).DeleteAsync();
            }

            await dbContext.GetTable<Data.Poco.RoleRegistryPermission>()
                .Where(w => createdRoleIds.Contains(w.RoleRegistryId ?? 0)).DeleteAsync();

            await dbContext.GetTable<Data.Poco.RoleRegistry>().Where(w => createdRoleIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.TenantRegistry>().Where(w => createdTenantIds.Contains(w.Id))
                .DeleteAsync();

            foreach (var openContext in openContexts)
            {
                await openContext.DisposeAsync();
            }
        }

        protected static Task<VisualisationRegistryDatasourceService> BuildServiceAsync(DbContext dbContext,
            string? userName)
        {
            var realConnectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                       ?? Environment.GetEnvironmentVariable("ConnectionString")
                                       ?? string.Empty;

            var dynamicEnvironment = TestDynamicEnvironment.Create(new Dictionary<string, string>
            {
                ["ReportConnectionString"] = realConnectionString
            });

            return VisualisationRegistryDatasourceService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
        }

        protected async Task<(int TenantRegistryId, string UserName)> CreateTenantWithPermissionAsync(
            DbContext dbContext, string label, int[] specs)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var tenantId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Tenant{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdTenantIds.Add(tenantId);

            var roleGuid = Guid.NewGuid();

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}Role{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleIds.Add(roleId);

            foreach (var spec in specs)
            {
                await dbContext.InsertAsync(new Data.Poco.RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(),
                    PermissionSpecificationId = spec,
                    RoleRegistryId = roleId,
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                    Version = 1,
                    CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
            }

            var userName = $"{DatabaseFixture.Prefix}User{label}{suffix}";

            await dbContext.InsertAsync(new Data.Poco.UserRegistry
            {
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleGuid,
                Name = userName,
                Email = $"{userName}@example.invalid",
                Password = "not-used-by-permission-checks",
                Active = 1,
                PasswordLocked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            createdUserNames.Add(userName);

            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantId });

            return (tenantId, userName);
        }

        protected async Task<int> CreateRegistryAsync(DbContext dbContext, int tenantRegistryId, string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Registry{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow
            });

            createdVisualisationRegistryIds.Add(id);

            return id;
        }

        protected async Task AddParameterAsync(DbContext dbContext, int registryId, string name, int dataTypeId,
            string defaultValue, string createdUser)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.VisualisationRegistryParameter
            {
                VisualisationRegistryId = registryId,
                Name = name,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                DataTypeId = dataTypeId,
                DefaultValue = defaultValue,
                Required = 0,
                CreatedUser = createdUser,
                CreatedDate = DateTime.UtcNow
            });

            createdParameterIds.Add(id);
        }

        protected DbContext OpenContext()
        {
            var dbContext = fx.GetDbContext();
            openContexts.Add(dbContext);

            return dbContext;
        }

        protected async Task<(DbContext DbContext, VisualisationRegistryDatasourceService Service, int RegistryId)>
            ArrangeAsync(string label)
        {
            var dbContext = OpenContext();

            var (tenantId, userName) =
                await CreateTenantWithPermissionAsync(dbContext, label, [DatasourcePermissionSpec]);

            var registryId = await CreateRegistryAsync(dbContext, tenantId, userName);
            var service = await BuildServiceAsync(dbContext, userName);

            return (dbContext, service, registryId);
        }
    }
}
