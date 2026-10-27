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
using Jube.Dto.Options;
using Jube.Dto.Query.CaseAnalytics;
using Jube.Dto.Query.CaseSearch;
using Jube.Engine.CaseRisk;
using Jube.Service.Query.CaseAnalytics;
using Jube.Service.Query.CaseSearch;
using Jube.Service.Reactivity;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;
using JubeDynamicEnvironment = Jube.DynamicEnvironment.DynamicEnvironment;

namespace Jube.Test.CaseQa
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseQaSamplingTests(DatabaseFixture fx) : IAsyncLifetime
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

        private static readonly TimeSpan interval = TimeSpan.FromMinutes(15);
        private readonly CaseRiskSeeder seeder = new(fx);

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseStatusTransition>().Where(w => seeder.CaseIds.Contains(w.CaseId)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext, double? rate)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, Me);
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Set(s => s.RiskQaSampleRate, rate).UpdateAsync();

            return workflow;
        }

        private static Task SweepAsync(DbContext dbContext) =>
            CaseRiskSweep.RunAsync(dbContext, new CaseRiskSettings(), interval, 1000, DateTime.UtcNow, TestLog.NoOp);

        private static Task<Jube.Data.Poco.Case> ReadAsync(DbContext dbContext, int id) =>
            dbContext.Case.FirstAsync(w => w.Id == id);

        private static Task<CaseSearchService> SearchAsync(DbContext dbContext, string user) =>
            CaseSearchService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        [Fact]
        public async Task AnEligibleCaseIsDecidedOnceAndAPickedOneIsRecordedWithAnEventAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, 0.5);
            var ids = new List<int>();

            for (var i = 0; i < 60; i++)
            {
                ids.Add(await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: $"K{i}"));
            }

            await SweepAsync(dbContext);

            var cases = await dbContext.Case.Where(w => ids.Contains(w.Id)).ToListAsync();
            var picked = cases.Where(c => c.QaSampleStatusId == 2).ToList();
            var declined = cases.Where(c => c.QaSampleStatusId == 1).ToList();

            (picked.Count + declined.Count).Should().Be(60);
            picked.Count.Should().BeInRange(15, 45);
            picked.Should().OnlyContain(c => c.QaSampleDate != null);
            declined.Should().OnlyContain(c => c.QaSampleDate == null);

            foreach (var c in picked)
            {
                QaSampler.Picked(workflow.WorkflowGuid, c.Id, 0.5).Should().BeTrue();
            }

            var events = await dbContext.CaseEvent.Where(w => ids.Contains(w.CaseId!.Value)
                                                              && w.CaseEventTypeId == 20).ToListAsync();
            events.Select(e => e.CaseId!.Value).Should().BeEquivalentTo(picked.Select(c => c.Id));
            events.Should().OnlyContain(e => e.CreatedUser == "Administrator" && e.After == "0.5");

            var before = cases.ToDictionary(c => c.Id, c => c.QaSampleStatusId);
            await dbContext.Case.Where(w => ids.Contains(w.Id)).Set(s => s.RiskCheckedDate, (DateTime?)null).UpdateAsync();
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Set(s => s.RiskQaSampleRate, 0.01).UpdateAsync();
            await SweepAsync(dbContext);

            var again = await dbContext.Case.Where(w => ids.Contains(w.Id)).ToListAsync();
            again.ToDictionary(c => c.Id, c => c.QaSampleStatusId).Should().BeEquivalentTo(before);
            (await dbContext.CaseEvent.CountAsync(w => ids.Contains(w.CaseId!.Value) && w.CaseEventTypeId == 20))
                .Should().Be(picked.Count);
        }

        [Fact]
        public async Task OnlyLowPriorityStandardCasesAreEverDecidedAndANilRateLeavesThemUndecidedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, 0.5);
            var low = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "LOW");
            var severe = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "SEVERE");
            var deadline = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "DUE",
                diary: 1, diaryDate: DateTime.UtcNow.AddHours(6));
            var regulatory = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "REG",
                diary: 1, diaryDate: DateTime.UtcNow.AddDays(20));
            await dbContext.Case.Where(w => w.Id == regulatory).Set(s => s.DiaryTypeId, (byte)3).UpdateAsync();
            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Set(s => s.TypologySeverityId, (byte)5).UpdateAsync();
            var allSevere = new[] { low, severe, deadline, regulatory };

            await SweepAsync(dbContext);

            foreach (var id in allSevere)
            {
                (await ReadAsync(dbContext, id)).QaSampleStatusId.Should().Be(0, "a severe or urgent case is not low priority");
            }

            var calm = await WorkflowAsync(dbContext, 0);
            var calmCase = await seeder.SeedCaseAsync(dbContext, calm, calm.Neutral);
            var unset = await WorkflowAsync(dbContext, null);
            var unsetCase = await seeder.SeedCaseAsync(dbContext, unset, unset.Neutral);

            await SweepAsync(dbContext);

            (await ReadAsync(dbContext, calmCase)).QaSampleStatusId.Should().Be(0);
            var fallback = (await ReadAsync(dbContext, unsetCase)).QaSampleStatusId;
            (QaSampler.Picked(unset.WorkflowGuid, unsetCase, new CaseRiskSettings().QaSampleRate)
                ? fallback == 2
                : fallback == 1).Should().BeTrue("the environment default of five percent applies when unset");
        }

        [Fact]
        public async Task APickedCaseIsOrderedAheadOfOrdinaryStandardCasesButBehindUrgentOnesAndHasItsOwnViewAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, 0);
            var sampled = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "S");
            var ordinary = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "O");
            var urgent = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "U");
            var near = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "N");
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, "C");
            await seeder.AssessAsync(dbContext, sampled, 4, 90);
            await seeder.AssessAsync(dbContext, ordinary, 4, 95);
            await seeder.AssessAsync(dbContext, urgent, 2, 5);
            await seeder.AssessAsync(dbContext, near, 3, 10);
            var sampledNear = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "SN");
            await seeder.AssessAsync(dbContext, sampledNear, 3, 60);
            await dbContext.Case.Where(w => w.Id == sampled || w.Id == closed || w.Id == sampledNear)
                .Set(s => s.QaSampleStatusId, (byte)2).Set(s => s.QaSampleDate, DateTime.UtcNow).UpdateAsync();

            var service = await SearchAsync(dbContext, Me);
            var page = await service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid, View = CaseSearchView.All, Take = 50
            });

            page.Rows.Select(r => r.CaseId).Should().Equal(urgent, sampledNear, near, sampled, ordinary);
            page.Rows.Single(r => r.CaseId == sampled).QaSample.Should().BeTrue();
            page.Rows.Where(r => r.CaseId != sampled && r.CaseId != sampledNear).Should().OnlyContain(r => !r.QaSample);

            var view = await service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid, View = CaseSearchView.QaSamples, Take = 50
            });

            view.Rows.Select(r => r.CaseId).Should().BeEquivalentTo([sampled, sampledNear]);

            var counts = await service.CountsAsync(workflow.WorkflowGuid);
            counts.Single(c => c.Kind == "View" && c.Key == "7").Count.Should().Be(2);

            var sorted = await service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid, View = CaseSearchView.All, Sort = CaseSearchSort.Priority,
                Take = 50
            });

            sorted.Rows.Select(r => r.CaseId).Should().Equal(ordinary, sampled, sampledNear, near, urgent);

            (await dbContext.Case.FirstAsync(w => w.Id == closed)).QaSampleStatusId.Should().Be(2);
            (await service.GetRowAsync(workflow.WorkflowGuid, closed))!.QaSample.Should().BeFalse();
            (await service.GetRowAsync(workflow.WorkflowGuid, sampled))!.QaSample.Should().BeTrue();
        }

        [Fact]
        public async Task TheAnalyticsReportCountsPickedCasesAndTheirOutcomesSeparatelyFromEveryoneElseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, 0);

            var ids = new List<int>();

            foreach (var (status, sampled) in new[]
                     {
                         (workflow.Confirmed, true), (workflow.NotConfirmed, true), (workflow.NotConfirmed, true),
                         (workflow.Confirmed, false), (workflow.Confirmed, false)
                     })
            {
                var id = await seeder.SeedCaseAsync(dbContext, workflow, status, 3);

                if (sampled)
                {
                    ids.Add(id);
                }
            }

            await dbContext.Case.Where(w => ids.Contains(w.Id)).Set(s => s.QaSampleStatusId, (byte)2).UpdateAsync();
            var open = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await dbContext.Case.Where(w => w.Id == open).Set(s => s.QaSampleStatusId, (byte)2).UpdateAsync();

            var service = await CaseAnalyticsService.CreateAsync(dbContext, Me, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), dynamicEnvironment, TestLog.NoOp);
            var report = (await service.GetAsync(new CaseAnalyticsRequestDto { CaseWorkflowGuid = workflow.WorkflowGuid }))
                .QaSample;

            report.Picked.Should().Be(4);
            report.Open.Should().Be(1);
            report.Closed.Should().Be(3);
            report.Decided.Should().Be(3);
            report.Confirmed.Should().Be(1);
            report.ConfirmedRate.Should().BeApproximately(1 / 3.0, 1e-9);
            report.OverallConfirmedRate.Should().BeApproximately(3 / 5.0, 1e-9);
            report.Enough.Should().BeFalse();
        }
    }
}
