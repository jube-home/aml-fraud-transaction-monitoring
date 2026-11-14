/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
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
using FluentAssertions;
using Jube.Data.Context;
using Jube.Dto.Repository.GraphRegistry;
using Jube.Service.Exceptions.Repository.GraphRegistry;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using GraphRegistryService = Jube.Service.Repository.GraphRegistry.GraphRegistryService;

namespace Jube.Test.Service.Repository.GraphRegistry
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphRegistryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int AuthorPermission = 50;
        private const int ViewPermission = 49;
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);
        private readonly List<int> createdRegistryIds = [];
        private readonly List<int> createdRoleRegistryIds = [];
        private readonly List<string> createdUserNames = [];
        private string authorA = string.Empty;
        private string authorB = string.Empty;
        private string viewerA = string.Empty;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenantAId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(w => w.TenantRegistryId).FirstAsync();
            var tenantBId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserTenantB)
                .Select(w => w.TenantRegistryId).FirstAsync();
            authorA = await SeedActorAsync(dbContext, tenantAId, "AuthorA", [AuthorPermission]);
            authorB = await SeedActorAsync(dbContext, tenantBId, "AuthorB", [AuthorPermission]);
            viewerA = await SeedActorAsync(dbContext, tenantAId, "ViewerA", [ViewPermission]);
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guids = dbContext.GraphRegistry.Where(s => createdRegistryIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GraphRegistryRole.Where(w => guids.Contains(w.GraphRegistryGuid)).DeleteAsync();
            await dbContext.GraphRegistryVersion.Where(w => createdRegistryIds.Contains(w.GraphRegistryId)).DeleteAsync();
            await dbContext.GraphRegistry.Where(w => createdRegistryIds.Contains(w.Id)).DeleteAsync();
            await CleanupActorsAsync(dbContext);
        }

        [Fact]
        public async Task CreatesReadsUpdatesAndSoftDeletesARegistryWritingAVersionRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var created = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);
            created.Guid.Should().NotBeEmpty();
            created.Version.Should().Be(1);
            created.DefaultHops.Should().Be(2);

            var fetched = await service.GetByIdAsync(created.Id);
            fetched.Should().NotBeNull();
            fetched!.Name.Should().Be(created.Name);
            (await service.GetByGuidAsync(created.Guid))!.Id.Should().Be(created.Id);

            fetched.DefaultHops = 3;
            fetched.RenderCap = 120;
            var updated = await service.UpdateAsync(fetched);
            updated.Version.Should().Be(2);
            updated.DefaultHops.Should().Be(3);
            var versions = await dbContext.GraphRegistryVersion.Where(v => v.GraphRegistryId == created.Id).ToListAsync();
            versions.Should().ContainSingle(v => v.Version == 1 && v.DefaultHops == 2);

            await service.DeleteAsync(created.Id);
            (await service.GetByIdAsync(created.Id)).Should().BeNull();
            (await dbContext.GraphRegistry.FirstAsync(r => r.Id == created.Id)).Deleted.Should().Be(1);
        }

        [Fact]
        public async Task RefusesInvalidDefaultsAndDuplicateNamesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, authorA);
            var first = await service.InsertAsync(ValidDto());
            createdRegistryIds.Add(first.Id);

            var duplicate = ValidDto(first.Name);
            var act = () => service.InsertAsync(duplicate);
            (await act.Should().ThrowAsync<DtoValidationException>()).Which.Result.Errors
                .Should().Contain(e => e.ErrorCode == "NameDuplicate");

            var invalid = ValidDto();
            invalid.DefaultHops = 7;
            invalid.RenderCap = 5;
            invalid.HubPolicyId = 9;
            var validation = await service.ValidateAsync(invalid);
            validation.Errors.Select(e => e.ErrorCode).Should()
                .Contain(["DefaultHopsOutOfRange", "RenderCapOutOfRange", "HubPolicyInvalid"]);
        }

        [Fact]
        public async Task IsTenantScopedAndPermissionGatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var serviceA = await BuildServiceAsync(dbContext, authorA);
            var created = await serviceA.InsertAsync(ValidDto());
            createdRegistryIds.Add(created.Id);

            var serviceB = await BuildServiceAsync(dbContext, authorB);
            (await serviceB.GetByIdAsync(created.Id)).Should().BeNull();
            (await serviceB.GetAsync()).Should().NotContain(r => r.Id == created.Id);

            var viewer = await BuildServiceAsync(dbContext, viewerA);
            (await viewer.GetByIdAsync(created.Id)).Should().NotBeNull();
            var act = () => viewer.InsertAsync(ValidDto());
            await act.Should().ThrowAsync<ForbiddenException>();

            var noPermission = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var read = () => noPermission.GetByIdAsync(created.Id);
            await read.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task TheDirectoryListsOnlyGrantedActiveRegistriesShownInTheDirectoryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var author = await BuildServiceAsync(dbContext, authorA);
            var shown = await author.InsertAsync(ValidDto());
            var caseOnly = ValidDto();
            caseOnly.ContextKindId = 2;
            var hidden = await author.InsertAsync(caseOnly);
            createdRegistryIds.AddRange([shown.Id, hidden.Id]);

            var viewer = await BuildServiceAsync(dbContext, viewerA);
            (await viewer.GetByShowInDirectoryAsync()).Should().NotContain(r => r.Id == shown.Id);

            await GrantAsync(dbContext, shown.Guid, viewerA);
            await GrantAsync(dbContext, hidden.Guid, viewerA);
            var directory = await viewer.GetByShowInDirectoryAsync();
            directory.Should().Contain(r => r.Id == shown.Id);
            directory.Should().NotContain(r => r.Id == hidden.Id);
            (await viewer.GetByGuidActiveOnlyAsync(hidden.Guid)).Should().NotBeNull();
        }

        private static Task<GraphRegistryService> BuildServiceAsync(DbContext dbContext, string? userName,
            ILog? log = null, IServiceChangeBus? serviceChangeBus = null)
        {
            return GraphRegistryService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), TestLog.NoOp);
        }

        private static async Task GrantAsync(DbContext dbContext, Guid graphRegistryGuid, string userName)
        {
            var roleRegistryGuid = await dbContext.UserRegistry
                .Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();
            await dbContext.InsertAsync(new Data.Poco.GraphRegistryRole
            {
                GraphRegistryGuid = graphRegistryGuid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = roleRegistryGuid,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0
            });
        }

        private static GraphRegistryDto ValidDto(string? name = null) => new()
        {
            Name = name ?? $"{DatabaseFixture.Prefix}Graph{Guid.NewGuid():N}"[..40],
            Active = true,
            Locked = false,
            ContextKindId = 1,
            DefaultLayoutId = 1,
            DefaultHops = 2,
            DefaultWindowDays = 30,
            RenderCap = 150,
            HubPolicyId = 1
        };

        private async Task<string> SeedActorAsync(DbContext dbContext, int tenantRegistryId, string label,
            int[] specs)
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var roleId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RoleRegistry
            {
                Guid = roleGuid,
                Name = $"{DatabaseFixture.Prefix}RoleGraph{label}{suffix}",
                Active = 1,
                Locked = 0,
                Deleted = 0,
                TenantRegistryId = tenantRegistryId,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });
            createdRoleRegistryIds.Add(roleId);

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

            var userName = $"{DatabaseFixture.Prefix}UserGraph{label}{suffix}";
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
            await dbContext.InsertAsync(new Data.Poco.UserInTenant { User = userName, TenantRegistryId = tenantRegistryId });
            createdUserNames.Add(userName);

            return userName;
        }

        private async Task CleanupActorsAsync(DbContext dbContext)
        {
            await dbContext.UserInTenant.Where(w => createdUserNames.Contains(w.User)).DeleteAsync();
            await dbContext.UserRegistry.Where(w => createdUserNames.Contains(w.Name)).DeleteAsync();
            await dbContext.RoleRegistryPermission
                .Where(w => w.RoleRegistryId != null && createdRoleRegistryIds.Contains(w.RoleRegistryId.Value))
                .DeleteAsync();
            await dbContext.RoleRegistry.Where(w => createdRoleRegistryIds.Contains(w.Id)).DeleteAsync();
        }
    }
}
