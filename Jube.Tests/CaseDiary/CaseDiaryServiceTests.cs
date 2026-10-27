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
using Jube.Dto.Query.CaseDiary;
using Jube.Engine.CaseRisk;
using Jube.Service.Exceptions.Query.CaseDiary;
using Jube.Service.Query.CaseDiary;
using Jube.Service.Reactivity;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.CaseDiary
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseDiaryServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseDiaryService> BuildAsync(DbContext dbContext, string? user) =>
            CaseDiaryService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus());

        private async Task<(CaseRiskSeeder.Seeded Workflow, int CaseId)> OpenCaseAsync(DbContext dbContext)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Me);

            return (workflow, await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral));
        }

        private static CaseDiarySetDto Set(int caseId, CaseDiaryType type, DateTimeOffset due) =>
            new() { CaseId = caseId, DiaryTypeId = (int)type, DueDate = due };

        [Fact]
        public async Task SettingADiaryRecordsItsTypeAndDateAskForAFreshRankingAndLogsWhoDidItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, caseId) = await OpenCaseAsync(dbContext);
            await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.RiskCheckedDate, DateTime.UtcNow).UpdateAsync();
            var due = DateTimeOffset.UtcNow.AddDays(3);

            var service = await BuildAsync(dbContext, Me);
            await service.SetAsync(Set(caseId, CaseDiaryType.RegulatoryDeadline, due));

            var after = await dbContext.Case.FirstAsync(w => w.Id == caseId);
            after.Diary.Should().Be(1);
            after.DiaryTypeId.Should().Be((byte)CaseDiaryType.RegulatoryDeadline);
            after.DiaryDate.Should().BeCloseTo(due.UtcDateTime, TimeSpan.FromSeconds(1));
            after.DiaryUser.Should().Be(Me);
            after.RiskCheckedDate.Should().BeNull();
            after.RiskRecheckTriggerId.Should().Be((byte)CaseRiskTrigger.DiaryLaneThreshold);
            after.Locked.Should().Be(0);

            var events = await dbContext.CaseEvent.Where(w => w.CaseId == caseId).OrderBy(e => e.Id).ToListAsync();
            events.Select(e => e.CaseEventTypeId).Should().Equal(CaseDiaryService.DiarySetEventTypeId,
                CaseDiaryService.DiaryDateChangeEventTypeId);
            events[0].After.Should().Be("Regulatory deadline");
            events[0].Before.Should().BeNull();
            events.Should().OnlyContain(e => e.CreatedUser == Me);
        }

        [Fact]
        public async Task ChangingOnlyTheTypeOrOnlyTheDateLogsOnlyWhatChangedAndSettingTheSameAgainLogsNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, caseId) = await OpenCaseAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);
            var due = DateTimeOffset.UtcNow.AddDays(3);

            await service.SetAsync(Set(caseId, CaseDiaryType.Review, due));
            await service.SetAsync(Set(caseId, CaseDiaryType.Review, due));
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(2);

            await service.SetAsync(Set(caseId, CaseDiaryType.PaymentHold, due));
            var typeOnly = await dbContext.CaseEvent.Where(w => w.CaseId == caseId).OrderByDescending(e => e.Id)
                .FirstAsync();
            typeOnly.CaseEventTypeId.Should().Be(CaseDiaryService.DiarySetEventTypeId);
            typeOnly.Before.Should().Be("Review");
            typeOnly.After.Should().Be("Payment hold");
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(3);

            await service.SetAsync(Set(caseId, CaseDiaryType.PaymentHold, due.AddDays(1)));
            var dateOnly = await dbContext.CaseEvent.Where(w => w.CaseId == caseId).OrderByDescending(e => e.Id)
                .FirstAsync();
            dateOnly.CaseEventTypeId.Should().Be(CaseDiaryService.DiaryDateChangeEventTypeId);
            dateOnly.Before.Should().NotBeNull();
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(4);
        }

        [Fact]
        public async Task ClearingTurnsTheDiaryOffReturnsTheTypeToReviewAndLogsWhatWasClearedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, caseId) = await OpenCaseAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);
            await service.SetAsync(Set(caseId, CaseDiaryType.PaymentHold, DateTimeOffset.UtcNow.AddDays(1)));
            await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.RiskCheckedDate, DateTime.UtcNow)
                .Set(s => s.RiskRecheckTriggerId, (byte?)null).UpdateAsync();

            await service.ClearAsync(new CaseDiaryClearDto { CaseId = caseId });

            var after = await dbContext.Case.FirstAsync(w => w.Id == caseId);
            after.Diary.Should().Be(0);
            after.DiaryTypeId.Should().Be((byte)CaseDiaryType.Review);
            after.RiskCheckedDate.Should().BeNull();
            after.RiskRecheckTriggerId.Should().Be((byte)CaseRiskTrigger.DiaryLaneThreshold);
            var cleared = await dbContext.CaseEvent.Where(w => w.CaseId == caseId).OrderByDescending(e => e.Id)
                .FirstAsync();
            cleared.CaseEventTypeId.Should().Be(CaseDiaryService.DiaryClearedEventTypeId);
            cleared.Before.Should().Be("Payment hold");
            cleared.After.Should().BeNull();

            (await Assert.ThrowsAsync<ConflictException>(() =>
                service.ClearAsync(new CaseDiaryClearDto { CaseId = caseId }))).Message.Should().Contain("no diary");
        }

        [Fact]
        public async Task OnlyAnOpenCaseTheCallerCanSeeAndIsNotHeldByAnotherCarriesATypedDiaryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, caseId) = await OpenCaseAsync(dbContext);
            var suspended = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 1);
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3);
            var held = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await dbContext.Case.Where(w => w.Id == held).Set(s => s.Locked, (byte?)1)
                .Set(s => s.LockedUser, "someone-else").UpdateAsync();
            var mine = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await dbContext.Case.Where(w => w.Id == mine).Set(s => s.Locked, (byte?)1).Set(s => s.LockedUser, Me)
                .UpdateAsync();
            var due = DateTimeOffset.UtcNow.AddDays(1);

            var service = await BuildAsync(dbContext, Me);
            (await Assert.ThrowsAsync<ConflictException>(() =>
                service.SetAsync(Set(suspended, CaseDiaryType.Review, due)))).Message.Should().Contain("open case");
            (await Assert.ThrowsAsync<ConflictException>(() =>
                service.SetAsync(Set(closed, CaseDiaryType.Review, due)))).Message.Should().Contain("open case");
            (await Assert.ThrowsAsync<ConflictException>(() => service.SetAsync(Set(held, CaseDiaryType.Review, due))))
                .Message.Should().Contain("another user");
            await Assert.ThrowsAsync<NotFoundException>(() =>
                service.SetAsync(Set(int.MaxValue, CaseDiaryType.Review, due)));
            await service.SetAsync(Set(mine, CaseDiaryType.Review, due));

            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                otherTenant.SetAsync(Set(caseId, CaseDiaryType.Review, due)));
            var ungranted = await BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            await Assert.ThrowsAsync<NotFoundException>(() =>
                ungranted.SetAsync(Set(caseId, CaseDiaryType.Review, due)));

            foreach (var id in new[] { suspended, closed, held })
            {
                (await dbContext.Case.FirstAsync(w => w.Id == id)).Diary.Should().NotBe(1);
                (await dbContext.CaseEvent.CountAsync(w => w.CaseId == id)).Should().Be(0);
            }
        }

        [Fact]
        public async Task TheRequestIsValidatedAndTheCallerMustHoldTheCasePermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, caseId) = await OpenCaseAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);
            var due = DateTimeOffset.UtcNow.AddDays(1);

            async Task<string> CodesAsync(CaseDiarySetDto model) => string.Join(",",
                (await Assert.ThrowsAsync<DtoValidationException>(() => service.SetAsync(model))).Result.Errors
                .Select(e => e.ErrorCode));

            (await CodesAsync(new CaseDiarySetDto { CaseId = 0, DiaryTypeId = 1, DueDate = due }))
                .Should().Contain("CaseIdInvalid");
            (await CodesAsync(new CaseDiarySetDto { CaseId = caseId, DiaryTypeId = 0, DueDate = due }))
                .Should().Contain("DiaryTypeInvalid");
            (await CodesAsync(new CaseDiarySetDto { CaseId = caseId, DiaryTypeId = 5, DueDate = due }))
                .Should().Contain("DiaryTypeInvalid");
            (await CodesAsync(new CaseDiarySetDto { CaseId = caseId, DiaryTypeId = 1 }))
                .Should().Contain("DueDateRequired");
            (await CodesAsync(Set(caseId, CaseDiaryType.Review, DateTimeOffset.UtcNow.AddYears(-2))))
                .Should().Contain("DueDateOutOfRange");
            (await CodesAsync(Set(caseId, CaseDiaryType.Review, DateTimeOffset.UtcNow.AddYears(11))))
                .Should().Contain("DueDateOutOfRange");
            await service.SetAsync(Set(caseId, CaseDiaryType.RegulatoryDeadline, DateTimeOffset.UtcNow.AddDays(-3)));

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, null));
            var nobody = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            await Assert.ThrowsAsync<ForbiddenException>(() => nobody.SetAsync(Set(caseId, CaseDiaryType.Review, due)));
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                nobody.ClearAsync(new CaseDiaryClearDto { CaseId = caseId }));
        }

        [Fact]
        public async Task ARegulatoryDeadlineAndAPaymentHoldRankTheCaseInTheirLanesAtTheNextSweepWithTheRightTriggerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (workflow, caseId) = await OpenCaseAsync(dbContext);
            var settings = new Jube.CaseRisk.CaseRiskSettings();
            var assessments = new CaseRiskAssessmentRepository(dbContext);

            await CaseRiskSweep.RunAsync(dbContext, settings, TimeSpan.FromMinutes(15), 1000, DateTime.UtcNow,
                TestLog.NoOp);
            (await assessments.GetLatestAsync(caseId)).LaneId.Should().Be((byte)CaseRiskLane.Standard);

            var service = await BuildAsync(dbContext, Me);
            await service.SetAsync(Set(caseId, CaseDiaryType.RegulatoryDeadline, DateTimeOffset.UtcNow.AddDays(20)));
            await CaseRiskSweep.RunAsync(dbContext, settings, TimeSpan.FromMinutes(15), 1000, DateTime.UtcNow,
                TestLog.NoOp);

            var regulatory = await assessments.GetLatestAsync(caseId);
            regulatory.LaneId.Should().Be((byte)CaseRiskLane.Mandatory);
            regulatory.TriggerId.Should().Be((byte)CaseRiskTrigger.DiaryLaneThreshold);
            regulatory.LaneReason.Should().Contain("regulatory");
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskRecheckTriggerId.Should().BeNull();

            await service.SetAsync(Set(caseId, CaseDiaryType.PaymentHold, DateTimeOffset.UtcNow.AddHours(4)));
            await CaseRiskSweep.RunAsync(dbContext, settings, TimeSpan.FromMinutes(15), 1000, DateTime.UtcNow,
                TestLog.NoOp);
            (await assessments.GetLatestAsync(caseId)).LaneId.Should().Be((byte)CaseRiskLane.LiveMoney);

            await service.ClearAsync(new CaseDiaryClearDto { CaseId = caseId });
            await CaseRiskSweep.RunAsync(dbContext, settings, TimeSpan.FromMinutes(15), 1000, DateTime.UtcNow,
                TestLog.NoOp);
            var cleared = await assessments.GetLatestAsync(caseId);
            cleared.LaneId.Should().Be((byte)CaseRiskLane.Standard);
            cleared.TriggerId.Should().Be((byte)CaseRiskTrigger.DiaryLaneThreshold);
            workflow.WorkflowGuid.Should().NotBeEmpty();
        }

        [Fact]
        public async Task OnlyCasesWhoseDiaryExpiryChangesTheirStatusAreHandedToTheExpiryAutomationAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var past = DateTime.UtcNow.AddHours(-1);
            var open = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "K1", null, 1, past);
            var suspendOpen = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 1, "K2", null, 1, past);
            var suspendClosed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 2, "K3", null, 1, past);
            var future = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 1, "K4", null, 1,
                DateTime.UtcNow.AddHours(5));

            var expired = (await new CaseRepository(dbContext).GetByExpiredAsync()).Select(c => c.Id).ToList();

            expired.Should().Contain([suspendOpen, suspendClosed]);
            expired.Should().NotContain(open).And.NotContain(future);
        }
    }
}
