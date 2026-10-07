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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Service.Exceptions.Query.EntityVersionHistory;
using Jube.Service.Query.CaseWorkflowStatusVersionHistory;
using Jube.Service.Query.TenantRegistryVersionHistory;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.EntityVersionHistory
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EntityVersionHistoryQueryServiceTests(DatabaseFixture fx)
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        [Fact]
        public async Task TenantRegistry_IdDateRangeLatestAndCompareBehaveCorrectlyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var repository = new TenantRegistryRepository(dbContext, fx.Seed.UserWithPermission);
            var service = await TenantRegistryVersionHistoryService.CreateAsync(dbContext, fx.Seed.LandlordUser,
                TestLog.NoOp, localizers);

            var tenant = await repository.InsertAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Tenant{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0
            });

            tenant.Name = $"{DatabaseFixture.Prefix}TenantRenamedOnce{Guid.NewGuid():N}"[..40];
            await repository.UpdateAsync(tenant);

            tenant.Name = $"{DatabaseFixture.Prefix}TenantRenamedTwice{Guid.NewGuid():N}"[..40];
            await repository.UpdateAsync(tenant);

            var versions = await dbContext.GetTable<TenantRegistryVersion>()
                .Where(v => v.TenantRegistryId == tenant.Id).OrderBy(v => v.Id).ToListAsync();

            versions.Should().HaveCount(2);
            var firstVersionId = versions[0].Id;
            var secondVersionId = versions[1].Id;

            var earlyDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var midDate = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var lateDate = new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            await dbContext.GetTable<TenantRegistryVersion>().Where(v => v.Id == firstVersionId)
                .Set(v => v.CreatedDate, earlyDate).UpdateAsync();
            await dbContext.GetTable<TenantRegistryVersion>().Where(v => v.Id == secondVersionId)
                .Set(v => v.CreatedDate, lateDate).UpdateAsync();

            (await service.GetByIdAsync(firstVersionId)).Should().NotBeNull();
            (await service.GetByIdAsync(-1)).Should().BeNull();

            var byParent = await service.GetByParentIdAsync(tenant.Id);
            byParent.Should().HaveCount(2);
            byParent[0].Id.Should().Be(secondVersionId, "results should be ordered most-recent-id first");

            var inRange = await service.GetByDateRangeAsync(tenant.Id, midDate, lateDate.AddDays(1));
            inRange.Should().ContainSingle().Which.Id.Should().Be(secondVersionId);

            var noneInRange = await service.GetByDateRangeAsync(tenant.Id, new DateTime(2019, 1, 1),
                new DateTime(2019, 6, 1));
            noneInRange.Should().BeEmpty();

            (await service.GetByIdAndDateRangeAsync(firstVersionId, earlyDate.AddDays(-1), earlyDate.AddDays(1)))
                .Should().NotBeNull();
            (await service.GetByIdAndDateRangeAsync(firstVersionId, midDate, lateDate))
                .Should().BeNull("the row exists but falls outside the requested window");

            var latest = (await service.GetLatestAsync(tenant.Id)).Required();
            latest.Id.Should().Be(secondVersionId);

            var changes = await service.CompareAsync(firstVersionId, secondVersionId);
            changes.Should().Contain(c => c.PropertyName == "Name");
            changes.Should().NotContain(c => c.PropertyName == "TenantRegistryId");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task TenantRegistry_NullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                TenantRegistryVersionHistoryService.CreateAsync(dbContext, userName, log, localizers));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
        }

        [Fact]
        public async Task TenantRegistry_UnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                TenantRegistryVersionHistoryService.CreateAsync(dbContext, fx.Seed.UnknownUser, TestLog.NoOp,
                    localizers));
        }

        [Fact]
        public async Task TenantRegistry_NonLandlordUserIsForbiddenRegardlessOfParentIdAsync()
        {
            await using var landlordDb = fx.GetDbContext();
            var landlordRepository = new TenantRegistryRepository(landlordDb, fx.Seed.LandlordUser);
            var tenant = await landlordRepository.InsertAsync(new TenantRegistry
            {
                Name = $"{DatabaseFixture.Prefix}Tenant{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Landlord = 0
            });

            await using var ownDb = fx.GetDbContext();
            var ownTenantService = await TenantRegistryVersionHistoryService.CreateAsync(ownDb,
                fx.Seed.UserWithPermission, TestLog.NoOp, localizers);
            await Assert.ThrowsAsync<ForbiddenException>(() => ownTenantService.GetByParentIdAsync(tenant.Id));

            await using var otherDb = fx.GetDbContext();
            var otherTenantService = await TenantRegistryVersionHistoryService.CreateAsync(otherDb,
                fx.Seed.UserTenantB, TestLog.NoOp, localizers);
            await Assert.ThrowsAsync<ForbiddenException>(() => otherTenantService.GetByParentIdAsync(tenant.Id));
        }

        [Fact]
        public async Task CaseWorkflowStatus_ParentScopingNeverLeaksAnotherParentsVersionsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = fx.Seed.UserWithPermission;

            var modelRepository = new EntityAnalysisModelRepository(dbContext, userName);
            var modelId = (await modelRepository.InsertAsync(new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            })).Id;

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });

            var statusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            var service = await CaseWorkflowStatusVersionHistoryService.CreateAsync(dbContext, userName, TestLog.NoOp,
                localizers);

            var statusA = await statusRepository.InsertAsync(new CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}StatusA{Guid.NewGuid():N}"[..40],
                CaseWorkflowId = workflowId,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            statusA.Name = $"{DatabaseFixture.Prefix}StatusARenamed{Guid.NewGuid():N}"[..40];
            await statusRepository.UpdateAsync(statusA);

            var statusB = await statusRepository.InsertAsync(new CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}StatusB{Guid.NewGuid():N}"[..40],
                CaseWorkflowId = workflowId,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            statusB.Name = $"{DatabaseFixture.Prefix}StatusBRenamedOnce{Guid.NewGuid():N}"[..40];
            await statusRepository.UpdateAsync(statusB);
            statusB.Name = $"{DatabaseFixture.Prefix}StatusBRenamedTwice{Guid.NewGuid():N}"[..40];
            await statusRepository.UpdateAsync(statusB);

            var versionsForA = await service.GetByParentIdAsync(statusA.Id);
            var versionsForB = await service.GetByParentIdAsync(statusB.Id);

            versionsForA.Should().ContainSingle();
            versionsForB.Should().HaveCount(2);
            versionsForA.Should().NotContain(v => versionsForB.Select(b => b.Id).Contains(v.Id));

            var latestForB = await service.GetLatestAsync(statusB.Id);
            latestForB.Should().NotBeNull();
            latestForB!.Id.Should().Be(versionsForB.Max(v => v.Id));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CaseWorkflowStatus_NullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowStatusVersionHistoryService.CreateAsync(dbContext, userName, log, localizers));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
        }

        [Fact]
        public async Task CaseWorkflowStatus_UnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseWorkflowStatusVersionHistoryService.CreateAsync(dbContext, fx.Seed.UnknownUser, TestLog.NoOp,
                    localizers));
        }

        [Fact]
        public async Task CaseWorkflowStatus_UserWithoutPermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = fx.Seed.UserWithPermission;

            var modelRepository = new EntityAnalysisModelRepository(dbContext, userName);
            var modelId = (await modelRepository.InsertAsync(new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            })).Id;

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });

            var statusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            var status = await statusRepository.InsertAsync(new CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                CaseWorkflowId = workflowId,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });

            var deniedService = await CaseWorkflowStatusVersionHistoryService.CreateAsync(dbContext,
                fx.Seed.UserWithoutPermission, TestLog.NoOp, localizers);

            await Assert.ThrowsAsync<ForbiddenException>(() => deniedService.GetByParentIdAsync(status.Id));
            await Assert.ThrowsAsync<ForbiddenException>(() => deniedService.GetByIdAsync(status.Id));
        }

        [Fact]
        public async Task CaseWorkflowStatus_UserInTenantBCannotReadTenantARowsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var userName = fx.Seed.UserWithPermission;

            var modelRepository = new EntityAnalysisModelRepository(dbContext, userName);
            var modelId = (await modelRepository.InsertAsync(new Jube.Data.Poco.EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            })).Id;

            var workflowId = await dbContext.InsertWithInt32IdentityAsync(new CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                EntityAnalysisModelId = modelId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                EnableVisualisation = 0,
                Version = 1,
                CreatedUser = userName,
                CreatedDate = DateTime.UtcNow
            });

            var statusRepository = new CaseWorkflowStatusRepository(dbContext, userName);
            var status = await statusRepository.InsertAsync(new CaseWorkflowStatus
            {
                Name = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40],
                CaseWorkflowId = workflowId,
                Guid = Guid.NewGuid(),
                Active = 1,
                Locked = 0,
                Deleted = 0
            });
            status.Name = $"{DatabaseFixture.Prefix}StatusRenamed{Guid.NewGuid():N}"[..40];
            await statusRepository.UpdateAsync(status);

            var versions = await dbContext.GetTable<CaseWorkflowStatusVersion>()
                .Where(v => v.CaseWorkflowStatusId == status.Id).OrderBy(v => v.Id).ToListAsync();
            versions.Should().ContainSingle();
            var versionId = versions[0].Id;

            var otherTenantService = await CaseWorkflowStatusVersionHistoryService.CreateAsync(dbContext,
                fx.Seed.UserTenantB, TestLog.NoOp, localizers);

            (await otherTenantService.GetByIdAsync(versionId)).Should().BeNull();
            (await otherTenantService.GetByParentIdAsync(status.Id)).Should().BeEmpty();
            (await otherTenantService.GetByDateRangeAsync(status.Id, DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(1))).Should().BeEmpty();
            (await otherTenantService.GetLatestAsync(status.Id)).Should().BeNull();

            var changes = await otherTenantService.CompareAsync(versionId, versionId);
            changes.Should().BeEmpty();
        }
    }
}