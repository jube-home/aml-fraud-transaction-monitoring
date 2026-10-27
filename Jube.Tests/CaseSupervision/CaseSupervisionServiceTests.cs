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
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Dto.Query.CaseSupervision;
using Jube.Service.Exceptions.Query.CaseSupervision;
using Jube.Service.Query.CaseSupervision;
using Jube.Service.Reactivity;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.CaseSupervision
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseSupervisionServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Supervisor => fx.Seed.UserWithPermission;
        private string Analyst => fx.Seed.UserWithPermissionNoApproval;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseSupervisionService> BuildAsync(DbContext dbContext, string? user) =>
            CaseSupervisionService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus());

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Supervisor, Analyst);

            return workflow;
        }

        private static Task LockAsync(DbContext dbContext, int caseId, string user, int minutesAgo) =>
            dbContext.Case.Where(w => w.Id == caseId).Set(s => s.Locked, (byte?)1).Set(s => s.LockedUser, user)
                .Set(s => s.LockedDate, DateTime.UtcNow.AddMinutes(-minutesAgo)).UpdateAsync();

        private static CaseAnalyticsRequestDto Request(CaseRiskSeeder.Seeded workflow) =>
            new() { CaseWorkflowGuid = workflow.WorkflowGuid };

        [Fact]
        public async Task TheWorkloadShowsEachAnalystsHoldsTakesPeeksAndClosuresAndTheCasesNobodyHoldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var mine = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var theirs = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await LockAsync(dbContext, mine, Supervisor, 180);
            await LockAsync(dbContext, theirs, Analyst, 30);

            foreach (var minutes in new[] { 120, 480 })
            {
                var closedId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3);
                await dbContext.Case.Where(w => w.Id == closedId)
                    .Set(s => s.CreatedDate, DateTime.UtcNow.AddMinutes(-minutes - 60))
                    .Set(s => s.ClosedDate, DateTime.UtcNow.AddMinutes(-60))
                    .Set(s => s.ClosedUser, Analyst).UpdateAsync();
            }

            await seeder.EventAsync(dbContext, theirs, 2, Analyst);
            await seeder.EventAsync(dbContext, theirs, 6, Analyst);
            await seeder.EventAsync(dbContext, mine, 16, Analyst);
            await seeder.EventAsync(dbContext, mine, 2, Analyst, DateTime.UtcNow.AddDays(-60));

            var service = await BuildAsync(dbContext, Supervisor);
            var report = await service.WorkloadAsync(Request(workflow));

            report.Held.Should().Be(2);
            report.Unassigned.Should().Be(1);
            var analyst = report.Analysts.Single(a => a.User == Analyst);
            analyst.Holding.Should().Be(1);
            analyst.OldestHoldMinutes.Should().BeApproximately(30, 2);
            analyst.Taken.Should().Be(2);
            analyst.Peeks.Should().Be(1);
            analyst.Closed.Should().Be(2);
            analyst.MedianResolutionMinutes.Should().BeApproximately(300, 1);
            var supervisor = report.Analysts.Single(a => a.User == Supervisor);
            supervisor.Holding.Should().Be(1);
            supervisor.OldestHoldMinutes.Should().BeApproximately(180, 2);
            supervisor.Closed.Should().Be(0);
            supervisor.MedianResolutionMinutes.Should().BeNull();
            report.Analysts.Select(a => a.User).Should().NotContain(fx.Seed.UserWithoutPermission)
                .And.NotContain(fx.Seed.UserTenantB);
            report.Analysts.Select(a => a.Holding).Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task AnAnalystWhoseRoleLacksTheCasePermissionIsNeverListedOrAssignedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var tenantId = await dbContext.UserInTenant.Where(w => w.User == Supervisor)
                .Select(s => s.TenantRegistryId).FirstAsync();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var roleGuid = Guid.NewGuid();
            var userName = $"{DatabaseFixture.Prefix}UserNoCase{suffix}";

            var roleId = await dbContext.InsertWithInt32IdentityAsync(new RoleRegistry
            {
                Guid = roleGuid, Name = $"{DatabaseFixture.Prefix}RoleNoCase{suffix}", Active = 1, Locked = 0,
                Deleted = 0, TenantRegistryId = tenantId, Version = 1, CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            });

            try
            {
                await dbContext.InsertAsync(new RoleRegistryPermission
                {
                    Guid = Guid.NewGuid(), PermissionSpecificationId = 2, RoleRegistryId = roleId, Active = 1,
                    Locked = 0, Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow,
                    CreatedUser = DatabaseFixture.Prefix
                });
                await dbContext.InsertAsync(new UserRegistry
                {
                    Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid, Name = userName,
                    Email = $"{userName}@example.invalid", Password = "x", Active = 1, PasswordLocked = 0,
                    Deleted = 0, Version = 1, CreatedDate = DateTime.UtcNow, CreatedUser = DatabaseFixture.Prefix
                });
                await dbContext.InsertAsync(new UserInTenant { User = userName, TenantRegistryId = tenantId });

                foreach (var status in new[] { workflow.Confirmed, workflow.NotConfirmed, workflow.Neutral })
                {
                    await dbContext.InsertAsync(new CaseWorkflowStatusRole
                    {
                        CaseWorkflowStatusGuid = status, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid,
                        CreatedUser = userName, CreatedDate = DateTime.UtcNow, Deleted = 0
                    });
                }

                await dbContext.InsertAsync(new CaseWorkflowRole
                {
                    CaseWorkflowGuid = workflow.WorkflowGuid, Guid = Guid.NewGuid(), RoleRegistryGuid = roleGuid,
                    CreatedUser = userName, CreatedDate = DateTime.UtcNow, Deleted = 0
                });

                var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
                var service = await BuildAsync(dbContext, Supervisor);

                (await service.WorkloadAsync(Request(workflow))).Analysts.Select(a => a.User)
                    .Should().NotContain(userName);
                (await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.ReassignAsync(new CaseReassignDto { CaseId = caseId, ToUser = userName })))
                    .Result.Errors.Should().Contain(e => e.ErrorCode == "AnalystNotEligible");
            }
            finally
            {
                await dbContext.GetTable<CaseWorkflowRole>().Where(w => w.RoleRegistryGuid == roleGuid).DeleteAsync();
                await dbContext.GetTable<CaseWorkflowStatusRole>().Where(w => w.RoleRegistryGuid == roleGuid)
                    .DeleteAsync();
                await dbContext.GetTable<UserInTenant>().Where(w => w.User == userName).DeleteAsync();
                await dbContext.UserRegistry.Where(w => w.Name == userName).DeleteAsync();
                await dbContext.RoleRegistryPermission.Where(w => w.RoleRegistryId == roleId).DeleteAsync();
                await dbContext.RoleRegistry.Where(w => w.Id == roleId).DeleteAsync();
            }
        }

        [Fact]
        public async Task OnlyAnalystsWhoCanSeeTheWorkflowAndHoldTheCasePermissionAreListedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Supervisor, fx.Seed.UserWithoutPermission);

            var service = await BuildAsync(dbContext, Supervisor);
            var report = await service.WorkloadAsync(Request(workflow));

            report.Analysts.Select(a => a.User).Should().Contain(Supervisor)
                .And.NotContain(Analyst)
                .And.NotContain(fx.Seed.UserWithoutPermission);
        }

        [Fact]
        public async Task SupervisionNeedsThePermissionAndAWorkflowTheCallersRoleIsGrantedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var ungranted = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await LockAsync(dbContext, caseId, Analyst, 500);

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, fx.Seed.UserNoTenant));

            var nobody = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await Assert.ThrowsAsync<ForbiddenException>(() => nobody.WorkloadAsync(Request(workflow)));
            await Assert.ThrowsAsync<ForbiddenException>(() => nobody.StuckLocksAsync(workflow.WorkflowGuid));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                nobody.BreakLockAsync(new CaseBreakLockDto { CaseId = caseId }));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                nobody.ReassignAsync(new CaseReassignDto { CaseId = caseId, ToUser = Analyst }));

            var service = await BuildAsync(dbContext, Supervisor);
            await Assert.ThrowsAsync<NotFoundException>(() => service.WorkloadAsync(Request(ungranted)));
            await Assert.ThrowsAsync<NotFoundException>(() => service.StuckLocksAsync(ungranted.WorkflowGuid));
            await Assert.ThrowsAsync<NotFoundException>(() => service.WorkloadAsync(Request(new CaseRiskSeeder.Seeded(
                Guid.NewGuid(), Guid.Empty, Guid.Empty, Guid.Empty))));

            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await Assert.ThrowsAsync<NotFoundException>(() => otherTenant.WorkloadAsync(Request(workflow)));
            await Assert.ThrowsAsync<NotFoundException>(() =>
                otherTenant.BreakLockAsync(new CaseBreakLockDto { CaseId = caseId }));

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).LockedUser.Should().Be(Analyst);
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task StuckLocksAreOpenCasesLockedAndIdleLongEnoughLongestFirstWithTheirRiskAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var oldest = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var older = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var recent = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var active = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var free = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3);
            var suspended = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 2);

            await LockAsync(dbContext, oldest, Analyst, 900);
            await LockAsync(dbContext, older, Supervisor, 300);
            await LockAsync(dbContext, recent, Analyst, 60);
            await LockAsync(dbContext, active, Analyst, 600);
            await LockAsync(dbContext, closed, Analyst, 900);
            await LockAsync(dbContext, suspended, Analyst, 900);
            await seeder.EventAsync(dbContext, active, 9, Analyst, DateTime.UtcNow.AddMinutes(-20));
            await seeder.AssessAsync(dbContext, older, 3, 71);
            await seeder.AssessAsync(dbContext, older, 2, 88);

            var service = await BuildAsync(dbContext, Supervisor);
            var stuck = await service.StuckLocksAsync(workflow.WorkflowGuid);

            stuck.Select(s => s.CaseId).Should().Equal(oldest, older);
            stuck[0].LockedUser.Should().Be(Analyst);
            stuck[0].IdleMinutes.Should().BeApproximately(900, 2);
            stuck[0].StatusName.Should().StartWith(DatabaseFixture.Prefix);
            stuck[0].Priority.Should().Be(0);
            stuck[1].Lane.Should().Be(2);
            stuck[1].Priority.Should().Be(88);
            free.Should().BeGreaterThan(0);

            (await service.StuckLocksAsync(workflow.WorkflowGuid, 30)).Select(s => s.CaseId)
                .Should().Equal(oldest, older, recent);
            (await service.StuckLocksAsync(workflow.WorkflowGuid, 1000)).Should().BeEmpty();
        }

        [Fact]
        public async Task BreakingALockReleasesItAndRecordsWhoAndWhyAndOnlyForAnOpenLockedCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var held = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var free = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3);
            await LockAsync(dbContext, held, Analyst, 500);
            await LockAsync(dbContext, closed, Analyst, 500);

            var service = await BuildAsync(dbContext, Supervisor);
            await service.BreakLockAsync(new CaseBreakLockDto { CaseId = held, Reason = "  Analyst on leave  " });

            var after = await dbContext.Case.FirstAsync(w => w.Id == held);
            after.Locked.Should().Be(0);
            after.LockedUser.Should().BeNull();
            after.LockedDate.Should().BeNull();
            var broken = (await dbContext.CaseEvent.Where(w => w.CaseId == held).ToListAsync()).Should().ContainSingle()
                .Which;
            broken.CaseEventTypeId.Should().Be(CaseSupervisionService.BreakLockEventTypeId);
            broken.CreatedUser.Should().Be(Supervisor);
            broken.Before.Should().Be(Analyst);
            broken.After.Should().Be("Analyst on leave");

            await Assert.ThrowsAsync<ConflictException>(() => service.BreakLockAsync(new CaseBreakLockDto { CaseId = held }));
            (await Assert.ThrowsAsync<ConflictException>(() => service.BreakLockAsync(new CaseBreakLockDto { CaseId = free })))
                .Message.Should().Contain("not locked");
            await Assert.ThrowsAsync<ConflictException>(() => service.BreakLockAsync(new CaseBreakLockDto { CaseId = closed }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.BreakLockAsync(new CaseBreakLockDto { CaseId = int.MaxValue }));

            var bad = await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.BreakLockAsync(new CaseBreakLockDto { CaseId = held, Reason = new string('x', 501) }));
            bad.Result.Errors.Should().Contain(e => e.ErrorCode == "ReasonMaximumLength");
            (await Assert.ThrowsAsync<DtoValidationException>(() => service.BreakLockAsync(new CaseBreakLockDto())))
                .Result.Errors.Should().Contain(e => e.ErrorCode == "CaseIdInvalid");

            (await dbContext.Case.FirstAsync(w => w.Id == closed)).LockedUser.Should().Be(Analyst);
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == free)).Should().Be(0);
        }

        [Fact]
        public async Task ReassigningLocksTheCaseToAnAnalystWhoCanSeeItAndRecordsTheHandoverAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var free = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var held = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await LockAsync(dbContext, held, Supervisor, 500);

            var service = await BuildAsync(dbContext, Supervisor);
            await service.ReassignAsync(new CaseReassignDto { CaseId = free, ToUser = " " + Analyst + " " });
            await service.ReassignAsync(new CaseReassignDto { CaseId = held, ToUser = Analyst });

            foreach (var id in new[] { free, held })
            {
                var after = await dbContext.Case.FirstAsync(w => w.Id == id);
                after.Locked.Should().Be(1);
                after.LockedUser.Should().Be(Analyst);
                after.LockedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
                (await new CaseRepository(dbContext, Analyst).GetByIdActiveOnlyAsync(id)).Should().NotBeNull();
            }

            var freeEvent = (await dbContext.CaseEvent.Where(w => w.CaseId == free).ToListAsync()).Should()
                .ContainSingle().Which;
            freeEvent.CaseEventTypeId.Should().Be(CaseSupervisionService.AllocateLockEventTypeId);
            freeEvent.CreatedUser.Should().Be(Supervisor);
            freeEvent.Before.Should().BeNull();
            freeEvent.After.Should().Be(Analyst);
            (await dbContext.CaseEvent.FirstAsync(w => w.CaseId == held)).Before.Should().Be(Supervisor);
        }

        [Fact]
        public async Task ACaseCannotBeHandedToSomeoneWhoCouldNotSeeItOrWhenItIsNotOpenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Supervisor);
            await seeder.GrantAsync(dbContext, workflow, fx.Seed.UserWithoutPermission, fx.Seed.UserTenantB);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3);

            var service = await BuildAsync(dbContext, Supervisor);

            foreach (var target in new[]
                     {
                         Analyst, fx.Seed.UserWithoutPermission, fx.Seed.UserTenantB, fx.Seed.UserNoTenant,
                         fx.Seed.UnknownUser
                     })
            {
                var refused = await Assert.ThrowsAsync<DtoValidationException>(() =>
                    service.ReassignAsync(new CaseReassignDto { CaseId = caseId, ToUser = target }));

                refused.Result.Errors.Should().Contain(e => e.ErrorCode == "AnalystNotEligible");
            }

            await seeder.GrantAsync(dbContext, workflow, Analyst);
            var analystRole = await dbContext.UserRegistry.Where(u => u.Name == Analyst)
                .Select(u => u.RoleRegistryGuid).FirstAsync();
            await dbContext.GetTable<CaseWorkflowStatusRole>()
                .Where(w => w.CaseWorkflowStatusGuid == workflow.Neutral && w.RoleRegistryGuid == analystRole)
                .DeleteAsync();

            (await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.ReassignAsync(new CaseReassignDto { CaseId = caseId, ToUser = Analyst })))
                .Result.Errors.Should().Contain(e => e.ErrorCode == "AnalystNotEligible");

            await Assert.ThrowsAsync<ConflictException>(() =>
                service.ReassignAsync(new CaseReassignDto { CaseId = closed, ToUser = Supervisor }));
            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.ReassignAsync(new CaseReassignDto { CaseId = int.MaxValue, ToUser = Supervisor }));
            (await Assert.ThrowsAsync<DtoValidationException>(() =>
                service.ReassignAsync(new CaseReassignDto { CaseId = caseId })))
                .Result.Errors.Should().Contain(e => e.ErrorCode == "ToUserNotEmpty");

            var unchanged = await dbContext.Case.FirstAsync(w => w.Id == caseId);
            unchanged.Locked.Should().Be(0);
            unchanged.LockedUser.Should().BeNull();
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }
    }
}
