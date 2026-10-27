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
using Jube.CaseRisk;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Service.Exceptions.Query.CaseAnalytics;
using Jube.Service.Query.CaseAnalytics;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseAnalyticsServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly string connectionString = Environment.GetEnvironmentVariable("JubeTestConnectionString")
                                                          ?? Environment.GetEnvironmentVariable("ConnectionString")
                                                          ?? string.Empty;

        private static readonly JubeDynamicEnvironment dynamicEnvironment = TestDynamicEnvironment.Create(
            new Dictionary<string, string>
            {
                ["ConnectionString"] = connectionString,
                ["ReportConnectionString"] = connectionString
            });

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseAnalyticsService> BuildAsync(DbContext dbContext, string? user) =>
            CaseAnalyticsService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext, params string[] users)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, users.Length == 0 ? [Me] : users);

            return workflow;
        }

        private static CaseAnalyticsRequestDto Request(CaseRiskSeeder.Seeded workflow) =>
            new() { CaseWorkflowGuid = workflow.WorkflowGuid };

        private async Task SetCreatedAsync(DbContext dbContext, int id, DateTime created)
        {
            await dbContext.Case.Where(w => w.Id == id).Set(s => s.CreatedDate, created).UpdateAsync();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => w.CaseId == id)
                .DeleteAsync();
        }

        private async Task<int> CaseAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow,
            CaseStatusTransitionRepository transitions, DateTime t0, string key, Guid? rule, int closedAfter,
            Guid status, string? closedUser = null)
        {
            var closed = closedAfter > 0 ? (byte)3 : (byte)0;

            var id = await seeder.SeedCaseAsync(dbContext, workflow, status, closed, key, rule,
                closedDate: closedAfter > 0 ? t0.AddMinutes(closedAfter) : null);
            await SetCreatedAsync(dbContext, id, t0);

            if (closedUser != null)
            {
                await dbContext.Case.Where(w => w.Id == id).Set(s => s.ClosedUser, closedUser).UpdateAsync();
            }

            await transitions.EnterAsync(id, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.Created, null, t0);

            if (closedAfter > 0)
            {
                await transitions.CloseAsync(id, t0.AddMinutes(closedAfter), closedUser);
            }

            return id;
        }

        [Fact]
        public async Task AStatusThatWasDeletedAndRecreatedWithTheSameGuidDoesNotBreakTheReportAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var live = await dbContext.CaseWorkflowStatus.FirstAsync(w => w.Guid == workflow.Neutral);
            var deleted = await dbContext.CaseWorkflowStatus.FirstAsync(w => w.Guid == workflow.Neutral);
            deleted.Id = 0;
            deleted.Name = "Retired copy";
            deleted.Deleted = 1;
            await dbContext.InsertWithInt32IdentityAsync(deleted);

            var t0 = DateTime.UtcNow.AddDays(-3).Date.AddHours(10);
            var transitions = new CaseStatusTransitionRepository(dbContext);
            await CaseAsync(dbContext, workflow, transitions, t0, "A", null, 0, workflow.Neutral);

            var report = await (await BuildAsync(dbContext, Me)).GetAsync(Request(workflow));

            report.StatusNames[workflow.Neutral].Should().Be(live.Name);
            report.StatusDwell.Should().ContainSingle(d => d.CaseWorkflowStatusGuid == workflow.Neutral);
        }

        [Fact]
        public async Task TheWholeReportIsBuiltFromTheCasesTheirEventsStaysAndAssessmentsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Set(s => s.FirstViewTargetMinutes, 30).Set(s => s.ResolutionTargetMinutes, 1440).UpdateAsync();

            var t0 = DateTime.UtcNow.AddDays(-3).Date.AddHours(10);
            var r1 = Guid.NewGuid();
            var r2 = Guid.NewGuid();
            var transitions = new CaseStatusTransitionRepository(dbContext);

            var a = await CaseAsync(dbContext, workflow, transitions, t0, "A", r1, 120, workflow.Confirmed, Me);
            var b = await CaseAsync(dbContext, workflow, transitions, t0, "B", r1, 300, workflow.NotConfirmed, Me);
            var c = await CaseAsync(dbContext, workflow, transitions, t0, "C", r2, 60, workflow.NotConfirmed, "bob");
            var d = await CaseAsync(dbContext, workflow, transitions, t0, "D", r2, 0, workflow.Neutral);
            var e = await CaseAsync(dbContext, workflow, transitions, t0, "E", null, 0, workflow.Neutral);

            await seeder.EventAsync(dbContext, a, 4, "alice", t0.AddMinutes(10));
            await seeder.EventAsync(dbContext, a, 6, "alice", t0.AddMinutes(20));
            await seeder.EventAsync(dbContext, b, 4, "alice", t0.AddMinutes(50));

            await seeder.AssessAsync(dbContext, a, 4, 80, 0.8);
            await seeder.AssessAsync(dbContext, b, 4, 20, 0.2);
            await seeder.AssessAsync(dbContext, c, 4, 10, 0.1);
            await seeder.AssessAsync(dbContext, d, 1, 90, 0.5);

            var service = await BuildAsync(dbContext, Me);
            var report = await service.GetAsync(Request(workflow));

            report.CasesAnalysed.Should().Be(5);
            report.Truncated.Should().BeFalse();

            var h = report.Headline;
            h.Opened.Should().Be(5);
            h.Closed.Should().Be(3);
            h.OpenNow.Should().Be(2);
            h.NetChange.Should().Be(2);
            h.UnassessedOpen.Should().Be(1);
            h.UrgentOpen.Should().Be(1);
            h.Decided.Should().Be(3);
            h.ConfirmedRate.Should().BeApproximately(1.0 / 3, 1e-9);
            h.MedianFirstViewMinutes.Should().Be(30);
            h.MedianResolutionMinutes.Should().Be(120);
            h.SlaBreachedOpen.Should().Be(2);
            h.ReopenRate.Should().Be(0);

            report.CycleTimes.FirstView.Count.Should().Be(2);
            report.CycleTimes.NeverViewed.Should().Be(3);
            report.CycleTimes.Resolution.Count.Should().Be(3);

            var ruleOne = report.Rules.Single(r => r.RuleGuid == r1);
            ruleOne.Cases.Should().Be(2);
            ruleOne.Precision.Should().Be(0.5);
            ruleOne.WastedMinutes.Should().Be(300);
            report.Rules.Single(r => r.RuleGuid == r2).Open.Should().Be(1);
            report.Rules.Single(r => r.RuleGuid == null).Cases.Should().Be(1);
            report.Rules[0].RuleGuid.Should().Be(r1);

            report.Ranking.Outcomes.Should().Be(3);
            report.Ranking.Sufficient.Should().BeFalse();
            report.Ranking.PriorityAuc.Should().Be(1);

            report.Aging.Open.Should().Be(2);
            report.Aging.Oldest.Select(o => o.CaseId).Should().BeEquivalentTo([d, e]);

            report.Flow.Sum(day => day.Opened).Should().Be(5);
            report.Flow.Sum(day => day.Closed).Should().Be(3);
            report.Flow[^1].Backlog.Should().Be(2);

            report.StatusNames.Should().ContainKey(workflow.Neutral);
            var dwell = report.StatusDwell.Single(s => s.CaseWorkflowStatusGuid == workflow.Neutral);
            dwell.Stays.Should().Be(5);
            dwell.CurrentlyIn.Should().Be(2);
            dwell.Completed.Count.Should().Be(3);
            dwell.Completed.Max.Should().Be(300);

            var view = report.ServiceLevels.Dimensions.Single(x => x.Dimension == ServiceLevelDimension.FirstView);
            view.Met.Should().Be(1);
            view.Breached.Should().Be(1 + 2);

            report.Rhythm.ArrivalsByWeekdayAndHour.Sum(x => x.Sum()).Should().Be(5);
            report.Rhythm.ArrivalsByWeekdayAndHour[((int)t0.DayOfWeek + 6) % 7][10].Should().Be(5);

            report.Handoffs.Distribution.Single(x => x.Label == "1").Cases.Should().Be(1);
            report.Workload.Mine.Closed.Should().Be(2);
            report.Workload.Team.Should().BeNull();
            report.MyDay.Days.Should().HaveCount(14);
            report.MyDay.Days.Sum(d => d.Closed).Should().Be(2);
            report.MyDay.Goal.Should().BeNull();
            e.Should().BePositive();
        }

        [Fact]
        public async Task ThePeriodIsValidatedAndTheDefaultIsTheLastThirtyDaysAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);

            var defaults = await service.GetAsync(Request(workflow));
            (defaults.To - defaults.From).TotalDays.Should().BeApproximately(30, 0.01);

            var now = DateTimeOffset.UtcNow;

            await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(new CaseAnalyticsRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid, From = now, To = now.AddDays(-1)
            }));
            await Assert.ThrowsAsync<DtoValidationException>(() => service.GetAsync(new CaseAnalyticsRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid, From = now.AddDays(-400), To = now
            }));
        }

        [Fact]
        public async Task CasesOutsideThePeriodAreLeftOutExceptThoseStillOpenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var old = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, "OLD",
                closedDate: DateTime.UtcNow.AddDays(-100));
            await dbContext.Case.Where(w => w.Id == old).Set(s => s.CreatedDate, DateTime.UtcNow.AddDays(-101))
                .UpdateAsync();
            var oldOpen = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "OLDOPEN");
            await dbContext.Case.Where(w => w.Id == oldOpen).Set(s => s.CreatedDate, DateTime.UtcNow.AddDays(-90))
                .UpdateAsync();

            var report = await (await BuildAsync(dbContext, Me)).GetAsync(Request(workflow));

            report.CasesAnalysed.Should().Be(1);
            report.Headline.Opened.Should().Be(0);
            report.Headline.OpenNow.Should().Be(1);
            report.Aging.Oldest.Single().CaseId.Should().Be(oldOpen);
        }

        [Fact]
        public async Task OnlyCasesTheCallerMaySeeAreCountedAndAWorkflowTheyLackIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var mine = await (await BuildAsync(dbContext, Me)).GetAsync(Request(workflow));
            mine.CasesAnalysed.Should().Be(1);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval).Result.GetAsync(Request(workflow)));
            await Assert.ThrowsAsync<NotFoundException>(() =>
                BuildAsync(dbContext, fx.Seed.UserTenantB).Result.GetAsync(Request(workflow)));
        }

        [Fact]
        public async Task ASignedOutUserIsRefusedAndOneWithoutThePermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, fx.Seed.UserNoTenant));

            var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
                BuildAsync(dbContext, fx.Seed.UserWithoutPermission).Result.GetAsync(Request(workflow)));
            ex.RequiredSpecifications.Should().Equal(1);
        }
    }
}
