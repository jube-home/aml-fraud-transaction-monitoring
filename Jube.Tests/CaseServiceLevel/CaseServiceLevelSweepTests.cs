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
using Jube.Dto.Options;
using Jube.Engine.CaseServiceLevel;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.CaseServiceLevel
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseServiceLevelSweepTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly CaseRiskSeeder seeder = new(fx);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.UserNotification.Where(w => w.CaseId != null && seeder.CaseIds.Contains(w.CaseId.Value))
                .DeleteAsync();
            await dbContext.CaseServiceLevelBreach.Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static async Task TargetsAsync(DbContext dbContext, Guid workflow, int? firstView = null,
            int? ownership = null, int? resolution = null)
        {
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow)
                .Set(s => s.FirstViewTargetMinutes, firstView)
                .Set(s => s.OwnershipTargetMinutes, ownership)
                .Set(s => s.ResolutionTargetMinutes, resolution)
                .UpdateAsync();
        }

        private async Task<int> AgedCaseAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow, int ageMinutes,
            byte closedStatusId = 0, string? lockedUser = null)
        {
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, closedStatusId);

            await dbContext.Case.Where(w => w.Id == caseId)
                .Set(s => s.CreatedDate, DateTime.UtcNow.AddMinutes(-ageMinutes))
                .Set(s => s.LockedUser, lockedUser)
                .Set(s => s.Locked, (byte)(lockedUser == null ? 0 : 1))
                .UpdateAsync();

            return caseId;
        }

        private static Task<int> SweepAsync(DbContext dbContext) =>
            CaseServiceLevelSweep.RunAsync(dbContext, 1000, default);

        [Fact]
        public async Task AResolutionBreachIsRecordedOnceAndNotifiesTheHolderAtHighPriorityAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await TargetsAsync(dbContext, workflow.WorkflowGuid, resolution: 60);
            var caseId = await AgedCaseAsync(dbContext, workflow, 120, lockedUser: user);

            var first = await SweepAsync(dbContext);
            var second = await SweepAsync(dbContext);

            first.Should().BeGreaterThanOrEqualTo(1);
            second.Should().Be(0, "a breach is recorded once however many sweeps see it");

            (await dbContext.CaseServiceLevelBreach.Where(w => w.CaseId == caseId).ToListAsync())
                .Should().ContainSingle().Which.DimensionId.Should().Be(CaseServiceLevelBreachRepository.Resolution);

            var notifications = await dbContext.UserNotification.Where(w => w.CaseId == caseId).ToListAsync();
            notifications.Should().ContainSingle(n => n.RecipientUser == user);
            var mine = notifications.Single(n => n.RecipientUser == user);
            mine.KindId.Should().Be((short)NotificationKind.CaseServiceLevelBreached);
            mine.PriorityId.Should().Be((short)NotificationPriority.High);
            mine.ArgumentsJson.Should().Contain("\"dimension\"");
            mine.ReadDate.Should().BeNull();
        }

        [Fact]
        public async Task ACaseInsideItsTargetIsNotSweptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await TargetsAsync(dbContext, workflow.WorkflowGuid, resolution: 600);
            var caseId = await AgedCaseAsync(dbContext, workflow, 10);

            await SweepAsync(dbContext);

            (await dbContext.CaseServiceLevelBreach.AnyAsync(w => w.CaseId == caseId)).Should().BeFalse();
        }

        [Fact]
        public async Task AClosedCaseIsNotSweptAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await TargetsAsync(dbContext, workflow.WorkflowGuid, resolution: 60);
            var caseId = await AgedCaseAsync(dbContext, workflow, 600, 3);

            await SweepAsync(dbContext);

            (await dbContext.CaseServiceLevelBreach.AnyAsync(w => w.CaseId == caseId)).Should().BeFalse();
        }

        [Fact]
        public async Task AnUnheldCaseNotifiesTheSupervisorsOfItsWorkflowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, user);
            await TargetsAsync(dbContext, workflow.WorkflowGuid, resolution: 60);
            var caseId = await AgedCaseAsync(dbContext, workflow, 120);

            await SweepAsync(dbContext);

            (await dbContext.UserNotification.Where(w => w.CaseId == caseId).Select(s => s.RecipientUser)
                    .ToListAsync())
                .Should().Contain(user);
        }

        [Fact]
        public async Task AUserWithoutTheWorkflowRoleIsNotNotifiedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await TargetsAsync(dbContext, workflow.WorkflowGuid, resolution: 60);
            var caseId = await AgedCaseAsync(dbContext, workflow, 120);

            await SweepAsync(dbContext);

            (await dbContext.UserNotification.Where(w => w.CaseId == caseId).ToListAsync()).Should().BeEmpty();
        }

        [Fact]
        public async Task AFirstViewBreachIsRaisedUntilTheCaseHasBeenLookedAtAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await TargetsAsync(dbContext, workflow.WorkflowGuid, firstView: 30);
            var unseen = await AgedCaseAsync(dbContext, workflow, 60, lockedUser: user);
            var seen = await AgedCaseAsync(dbContext, workflow, 60, lockedUser: user);
            await seeder.EventAsync(dbContext, seen, 4, user);

            await SweepAsync(dbContext);

            (await dbContext.CaseServiceLevelBreach.Where(w => w.CaseId == unseen).Select(s => s.DimensionId)
                    .ToListAsync())
                .Should().Equal(CaseServiceLevelBreachRepository.FirstView);
            (await dbContext.CaseServiceLevelBreach.AnyAsync(w => w.CaseId == seen)).Should().BeFalse();
        }

        [Fact]
        public async Task AStatusDwellBreachIsRaisedWhenTheCaseHasStayedInAStatusTooLongAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var user = fx.Seed.UserWithPermission;
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await dbContext.CaseWorkflowStatus.Where(w => w.Guid == workflow.Neutral)
                .Set(s => s.MaxDwellMinutes, 30).UpdateAsync();
            var caseId = await AgedCaseAsync(dbContext, workflow, 5, lockedUser: user);
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(caseId, workflow.WorkflowGuid,
                workflow.Neutral, CaseTransitionEnterReason.Created, null, DateTime.UtcNow.AddMinutes(-90));

            await SweepAsync(dbContext);

            (await dbContext.CaseServiceLevelBreach.Where(w => w.CaseId == caseId).Select(s => s.DimensionId)
                    .ToListAsync())
                .Should().Equal(CaseServiceLevelBreachRepository.StatusDwell);
        }
    }
}
