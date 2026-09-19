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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent.ServiceToolCatalogue;
using Jube.Service.Exceptions.Query.CaseByCaseKeyValue;
using Jube.Service.Query.CaseByCaseKeyValue;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.EntityAnalysisModel.CapturingBus;
using Jube.Test.Service.Query.CaseByCaseKeyValue.Models;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using log4net;

namespace Jube.Test.Service.Query.CaseByCaseKeyValue
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseByCaseKeyValueServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdCaseIds = [];
        private readonly List<int> createdCaseWorkflowIds = [];
        private readonly List<int> createdCaseWorkflowStatusIds = [];
        private readonly List<int> createdModelIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.Case.Where(w => createdCaseIds.Contains(w.Id)).DeleteAsync();
            var caseWorkflowStatusGuids = dbContext.CaseWorkflowStatus
                .Where(s => createdCaseWorkflowStatusIds.Contains(s.Id)).Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowStatusRole>()
                .Where(w => caseWorkflowStatusGuids.Contains(w.CaseWorkflowStatusGuid)).DeleteAsync();
            var caseWorkflowGuids = dbContext.CaseWorkflow.Where(s => createdCaseWorkflowIds.Contains(s.Id))
                .Select(s => s.Guid);
            await dbContext.GetTable<Data.Poco.CaseWorkflowRole>()
                .Where(w => caseWorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await dbContext.CaseWorkflowStatus.Where(w => createdCaseWorkflowStatusIds.Contains(w.Id)).DeleteAsync();
            await dbContext.CaseWorkflow.Where(w => createdCaseWorkflowIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GetTable<Data.Poco.EntityAnalysisModelVersion>()
                .Where(w => createdModelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel.Where(w => createdModelIds.Contains(w.Id)).DeleteAsync();
        }

        private static Task<CaseByCaseKeyValueService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return CaseByCaseKeyValueService.CreateAsync(dbContext, userName, log ?? TestLog.NoOp, localizers,
                serviceChangeBus ?? new NullServiceChangeBus(), auditLog ?? TestLog.NoOp);
        }

        private Task<Guid> RoleGuidOfAsync(DbContext dbContext, string userName) =>
            dbContext.UserRegistry.Where(u => u.Name == userName).Select(u => u.RoleRegistryGuid).FirstAsync();

        private async Task<Fixture> CreateCaseAsync(DbContext dbContext, string ownerUser,
            IEnumerable<string> workflowRoleUsers, IEnumerable<string> statusRoleUsers,
            string? caseKey = null, string? caseKeyValue = null, byte workflowDeleted = 0, byte statusDeleted = 0,
            byte workflowRoleDeleted = 0, byte statusRoleDeleted = 0)
        {
            var saved = await new EntityAnalysisModelRepository(dbContext, ownerUser).InsertAsync(
                new Data.Poco.EntityAnalysisModel
                {
                    Name = $"{DatabaseFixture.Prefix}Model{Guid.NewGuid():N}"[..40],
                    Guid = Guid.NewGuid(),
                    Active = 1,
                    Locked = 0,
                    Deleted = 0,
                }).ConfigureAwait(false);
            createdModelIds.Add(saved.Id);

            var caseWorkflowGuid = Guid.NewGuid();
            var caseWorkflowId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflow
            {
                Name = $"{DatabaseFixture.Prefix}Workflow{Guid.NewGuid():N}"[..40],
                Guid = caseWorkflowGuid,
                EntityAnalysisModelId = saved.Id,
                Active = 1,
                Locked = 0,
                Deleted = workflowDeleted,
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowIds.Add(caseWorkflowId);

            foreach (var user in workflowRoleUsers)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowRole
                {
                    CaseWorkflowGuid = caseWorkflowGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = await RoleGuidOfAsync(dbContext, user),
                    CreatedUser = ownerUser,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = workflowRoleDeleted,
                });
            }

            var statusName = $"{DatabaseFixture.Prefix}Status{Guid.NewGuid():N}"[..40];
            var caseWorkflowStatusGuid = Guid.NewGuid();
            var caseWorkflowStatusId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.CaseWorkflowStatus
            {
                Name = statusName,
                Guid = caseWorkflowStatusGuid,
                CaseWorkflowId = caseWorkflowId,
                Active = 1,
                Locked = 0,
                Deleted = statusDeleted,
                Priority = 1,
                ForeColor = "#112233",
                BackColor = "#445566",
                CreatedUser = ownerUser,
                CreatedDate = DateTime.UtcNow,
            });
            createdCaseWorkflowStatusIds.Add(caseWorkflowStatusId);

            foreach (var user in statusRoleUsers)
            {
                await dbContext.InsertAsync(new Data.Poco.CaseWorkflowStatusRole
                {
                    CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                    Guid = Guid.NewGuid(),
                    RoleRegistryGuid = await RoleGuidOfAsync(dbContext, user),
                    CreatedUser = ownerUser,
                    CreatedDate = DateTime.UtcNow,
                    Deleted = statusRoleDeleted,
                });
            }

            var key = caseKey ?? "account";
            var value = caseKeyValue ?? $"acc-{Guid.NewGuid():N}"[..20];
            var caseId = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = caseWorkflowGuid,
                CaseWorkflowStatusGuid = caseWorkflowStatusGuid,
                CaseKey = key,
                CaseKeyValue = value,
                Json = "{}",
                Rating = 7,
                Locked = 1,
                LockedUser = "locker",
                LockedDate = DateTime.UtcNow.AddMinutes(-5),
                Diary = 1,
                DiaryUser = "diarist",
                DiaryDate = DateTime.UtcNow.AddDays(1),
                ClosedStatusId = 2,
                ClosedUser = "closer",
                ClosedDate = DateTime.UtcNow.AddMinutes(-3),
                LastClosedStatus = 3,
                ClosedStatusMigrationDate = DateTime.UtcNow.AddMinutes(-1),
                CreatedDate = DateTime.UtcNow.AddHours(-1),
            }).ConfigureAwait(false);
            createdCaseIds.Add(caseId);

            return new Fixture(caseWorkflowGuid, statusName, caseId, key, value);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CreateWithNullOrBlankUserNameThrowsNotAuthenticatedAsync(string? userName)
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();

            var ex = await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                CaseByCaseKeyValueService.CreateAsync(dbContext, userName, log, localizers,
                    new NullServiceChangeBus(), TestLog.NoOp));

            ex.Code.Should().Be("NotAuthenticated");
            log.Entries.Should().Contain(e => e.Level == "WARN");
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task CreateWithUserHavingNoTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UserNoTenant));
        }

        [Fact]
        public async Task CreateWithUnknownUserThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task GetAsyncThrowsForbiddenWhenPermissionMissingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("account", "x"));
            ex.Code.Should().Be("PermissionDenied");
            ex.RequiredSpecifications.Should().BeEquivalentTo([1]);
        }

        [Fact]
        public async Task ForbiddenUserCannotSeeExistingMatchingCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission,
                [fx.Seed.UserWithPermission, fx.Seed.UserWithoutPermission],
                [fx.Seed.UserWithPermission, fx.Seed.UserWithoutPermission]);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(c.CaseKey, c.CaseKeyValue));
        }

        [Fact]
        public async Task GetAsyncMapsEveryFieldOfTheSeededCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission]);
            var poco = await dbContext.Case.FirstAsync(w => w.Id == c.CaseId);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync(c.CaseKey, c.CaseKeyValue);

            var dto = result.Should().ContainSingle().Subject;
            dto.Id.Should().Be(c.CaseId);
            dto.EntityAnalysisModelInstanceEntryGuid.Should().Be(poco.EntityAnalysisModelInstanceEntryGuid);
            dto.CaseWorkflowGuid.Should().Be(c.CaseWorkflowGuid);
            dto.CaseWorkflowStatusName.Should().Be(c.StatusName);
            dto.CaseKey.Should().Be(c.CaseKey);
            dto.CaseKeyValue.Should().Be(c.CaseKeyValue);
            dto.Locked.Should().BeTrue();
            dto.LockedUser.Should().Be("locker");
            dto.Diary.Should().BeTrue();
            dto.DiaryUser.Should().Be("diarist");
            dto.ClosedStatusId.Should().Be(2);
            dto.ClosedUser.Should().Be("closer");
            dto.Rating.Should().Be(7);
            dto.LastClosedStatus.Should().Be(3);
            dto.ForeColor.Should().Be("#112233");
            dto.BackColor.Should().Be("#445566");
            var tolerance = TimeSpan.FromMilliseconds(5);
            dto.DiaryDate.Should().BeCloseTo(poco.DiaryDate.Required(), tolerance);
            dto.CreatedDate.Should().BeCloseTo(poco.CreatedDate.Required(), tolerance);
            dto.LockedDate.Should().BeCloseTo(poco.LockedDate.Required(), tolerance);
            dto.ClosedDate.Should().BeCloseTo(poco.ClosedDate.Required(), tolerance);
            dto.ClosedStatusMigrationDate.Should().BeCloseTo(poco.ClosedStatusMigrationDate.Required(), tolerance);
        }

        [Fact]
        public async Task GetAsyncMapsNullableColumnsToLegacyDefaultsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission]);
            await dbContext.Case.Where(w => w.Id == c.CaseId)
                .Set(w => w.Locked, (byte?)null).Set(w => w.LockedUser, (string?)null)
                .Set(w => w.Diary, (byte?)null).Set(w => w.DiaryUser, (string?)null)
                .Set(w => w.ClosedUser, (string?)null).Set(w => w.ClosedStatusId, (byte?)null)
                .Set(w => w.Rating, (byte?)null).Set(w => w.DiaryDate, (DateTime?)null)
                .UpdateAsync();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var dto = (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().ContainSingle().Subject;

            dto.Locked.Should().BeFalse();
            dto.LockedUser.Should().BeEmpty();
            dto.Diary.Should().BeFalse();
            dto.DiaryUser.Should().BeEmpty();
            dto.ClosedUser.Should().BeEmpty();
            dto.ClosedStatusId.Should().Be(0);
            dto.Rating.Should().Be(0);
            dto.DiaryDate.Should().Be(default);
        }

        [Fact]
        public async Task GetAsyncReturnsMultipleMatchesNewestIdFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var value = $"multi-{Guid.NewGuid():N}"[..20];
            var first = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], caseKeyValue: value);
            var second = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], caseKeyValue: value);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync("account", value);

            result.Select(d => d.Id).Should().Equal(second.CaseId, first.CaseId);
        }

        [Fact]
        public async Task GetAsyncReturnsEmptyListWhenNothingMatchesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            var result = await service.GetAsync("account", $"absent-{Guid.NewGuid():N}");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAsyncRequiresBothKeyAndValueToMatchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], caseKey: "account");
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync("otherkey", c.CaseKeyValue)).Should().BeEmpty();
            (await service.GetAsync(c.CaseKey, c.CaseKeyValue + "x")).Should().BeEmpty();
            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().ContainSingle();
        }

        [Fact]
        public async Task TenantBCaseIsInvisibleToTenantAAndViceVersaAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var value = $"tenant-{Guid.NewGuid():N}"[..20];
            var caseA = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], caseKeyValue: value);
            var caseB = await CreateCaseAsync(dbContext, fx.Seed.UserTenantB, [fx.Seed.UserTenantB],
                [fx.Seed.UserTenantB], caseKeyValue: value);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync("account", value)).Select(d => d.Id).Should().Equal(caseA.CaseId);
            (await serviceB.GetAsync("account", value)).Select(d => d.Id).Should().Equal(caseB.CaseId);
        }

        [Fact]
        public async Task TenantBCaseIsInvisibleToTenantAEvenWhenTenantARoleIsGrantedOnTheWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserTenantB,
                [fx.Seed.UserTenantB, fx.Seed.UserWithPermission],
                [fx.Seed.UserTenantB, fx.Seed.UserWithPermission]);
            var serviceA = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceB = await BuildServiceAsync(dbContext, fx.Seed.UserTenantB);

            (await serviceA.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
            (await serviceB.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().ContainSingle();
        }

        [Fact]
        public async Task CaseIsHiddenWhenCallersRoleHasNoWorkflowRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission,
                [fx.Seed.UserWithPermissionNoApproveByReview], [fx.Seed.UserWithPermission]);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsHiddenWhenCallersRoleHasNoStatusRoleAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermissionNoApproveByReview]);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsVisibleToEachRoleGrantedOnBothWorkflowAndStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission,
                [fx.Seed.UserWithPermission, fx.Seed.UserWithPermissionNoApproveByReview],
                [fx.Seed.UserWithPermission, fx.Seed.UserWithPermissionNoApproveByReview]);
            var serviceOne = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);
            var serviceTwo = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproveByReview);

            (await serviceOne.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().ContainSingle();
            (await serviceTwo.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().ContainSingle();
        }

        [Fact]
        public async Task CaseIsHiddenWhenWorkflowIsSoftDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], workflowDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsHiddenWhenStatusIsSoftDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], statusDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsHiddenWhenWorkflowRoleIsSoftDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], workflowRoleDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task CaseIsHiddenWhenStatusRoleIsSoftDeletedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var c = await CreateCaseAsync(dbContext, fx.Seed.UserWithPermission, [fx.Seed.UserWithPermission],
                [fx.Seed.UserWithPermission], statusRoleDeleted: 1);
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            (await service.GetAsync(c.CaseKey, c.CaseKeyValue)).Should().BeEmpty();
        }

        [Fact]
        public async Task PreCancelledTokenThrowsOperationCanceledAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            using var cts = new CancellationTokenSource();
            await cts.CancelAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.GetAsync("account", "x", cts.Token));
        }

        [Fact]
        public async Task PermissionDeniedLogsWarnNotErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission, log);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("account", "x"));

            log.Entries.Should().Contain(e => e.Level == "WARN" && e.Message.Contains(fx.Seed.UserWithoutPermission));
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task FailureLogsErrorWithExceptionAttachedAndStillPropagatesAsync()
        {
            var dbContext = fx.GetDbContext();
            var log = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, log);

            await dbContext.DisposeAsync();

            await Assert.ThrowsAnyAsync<Exception>(() => service.GetAsync("account", "x"));

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR").Subject.Exception.Should().NotBeNull();
        }

        [Fact]
        public async Task AuditLogGetsExactlyOneLineAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var auditLog = new TestLog();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission, auditLog: auditLog);

            await service.GetAsync("account", $"absent-{Guid.NewGuid():N}");

            auditLog.Entries.Should().ContainSingle();
            auditLog.Entries[0].Message.Should().Contain("op=Get");
        }

        [Fact]
        public async Task ReadsAndFailuresPublishNothingAsync()
        {
            var serviceChangeBus = new CapturingBus();
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission,
                serviceChangeBus: serviceChangeBus);
            var forbiddenService = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission,
                serviceChangeBus: serviceChangeBus);

            await service.GetAsync("account", "x");
            await Assert.ThrowsAsync<ForbiddenException>(() => forbiddenService.GetAsync("account", "x"));

            serviceChangeBus.Published.Should().BeEmpty();
        }

        [Fact]
        public void CatalogueRegistersUniquePascalCaseNoUnderscoreNames()
        {
            var names = ServiceToolCatalogue.All.Select(t => t.Name).ToList();

            names.Should().OnlyHaveUniqueItems();
            names.Should().OnlyContain(n => !n.Contains('_'));
            names.Should().Contain("CaseByCaseKeyValueGet");
        }

        [Fact]
        public async Task PermissionDeniedMessageResolvesForFrenchCultureAsync()
        {
            var original = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo("fr");

                await using var dbContext = fx.GetDbContext();
                var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

                var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync("account", "x"));

                var frenchLocalizer = localizers.Create(typeof(CaseByCaseKeyValueResources));
                ex.Message.Should().Be(frenchLocalizer[CaseByCaseKeyValueResources.PermissionDenied].Value);
            }
            finally
            {
                CultureInfo.CurrentUICulture = original;
            }
        }
    }
}