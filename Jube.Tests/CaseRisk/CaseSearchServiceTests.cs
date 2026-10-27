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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseSearch;
using Jube.Service.Exceptions.Query.CaseSearch;
using Jube.Service.Query.CaseSearch;
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
    public sealed class CaseSearchServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
        private string Other => fx.Seed.UserWithPermissionNoApproval;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GetTable<CaseSearchFilter>()
                .Where(w => seeder.WorkflowGuids.Contains(w.CaseWorkflow.Guid)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseSearchService> BuildAsync(DbContext dbContext, string? user) =>
            CaseSearchService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext, params string[] users)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, users.Length == 0 ? [Me] : users);

            return workflow;
        }

        private static CaseSearchRequestDto Request(CaseRiskSeeder.Seeded workflow, CaseSearchView? view = null,
            CaseSearchSort sort = CaseSearchSort.Triage, int take = 50, int skip = 0) => new()
        {
            CaseWorkflowGuid = workflow.WorkflowGuid,
            View = view ?? CaseSearchView.All,
            Sort = sort,
            Take = take,
            Skip = skip
        };

        private static List<int> Ids(CaseSearchPageDto page) => page.Rows.Select(r => r.CaseId).ToList();

        [Fact]
        public async Task ASignedOutOrTenantlessUserIsRefusedAndOneWithoutThePermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, fx.Seed.UserNoTenant));

            var service = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            var forbidden = await Assert.ThrowsAsync<ForbiddenException>(() => service.RunAsync(Request(workflow)));
            forbidden.RequiredSpecifications.Should().Equal(1);
            await Assert.ThrowsAsync<ForbiddenException>(() => service.CountsAsync(workflow.WorkflowGuid));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.RiskMatrixAsync(Request(workflow)));
            await Assert.ThrowsAsync<ForbiddenException>(() => service.SkimAsync(Request(workflow)));
        }

        [Fact]
        public async Task TheTriageOrderPutsLiveMoneyFirstThenReturnedCasesThenLaneAndPriorityAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var neutral = workflow.Neutral;

            var standardLow = await seeder.SeedCaseAsync(dbContext, workflow, neutral, caseKeyValue: "A");
            var standardHigh = await seeder.SeedCaseAsync(dbContext, workflow, neutral, caseKeyValue: "B");
            var liveMoney = await seeder.SeedCaseAsync(dbContext, workflow, neutral, caseKeyValue: "C");
            var deadline = await seeder.SeedCaseAsync(dbContext, workflow, neutral, caseKeyValue: "D");
            var unassessed = await seeder.SeedCaseAsync(dbContext, workflow, neutral, caseKeyValue: "E");
            var returned = await seeder.SeedCaseAsync(dbContext, workflow, neutral, caseKeyValue: "F");

            await seeder.AssessAsync(dbContext, standardLow, 4, 40);
            await seeder.AssessAsync(dbContext, standardHigh, 4, 80);
            await seeder.AssessAsync(dbContext, liveMoney, 1, 5);
            await seeder.AssessAsync(dbContext, deadline, 3, 20);
            await seeder.AssessAsync(dbContext, returned, 4, 10);

            await new CaseViewRepository(dbContext, Me).UpsertAsync(returned, 0, 1);
            await dbContext.GetTable<CaseStatusTransition>().Where(w => w.CaseId == returned)
                .DeleteAsync();
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(returned, workflow.WorkflowGuid, neutral,
                CaseTransitionEnterReason.Reopened, "someone", DateTime.UtcNow.AddMinutes(1));

            var service = await BuildAsync(dbContext, Me);
            var page = await service.RunAsync(Request(workflow));

            Ids(page).Should().Equal(liveMoney, returned, deadline, standardHigh, standardLow, unassessed);
            page.Total.Should().Be(6);
        }

        [Theory]
        [InlineData(CaseSearchSort.Priority)]
        [InlineData(CaseSearchSort.Age)]
        [InlineData(CaseSearchSort.Due)]
        [InlineData(CaseSearchSort.Value)]
        [InlineData(CaseSearchSort.Likelihood)]
        [InlineData(CaseSearchSort.Newest)]
        public async Task EverySortOrdersTheListByItsOwnKeyAsync(CaseSearchSort sort)
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var now = DateTime.UtcNow;

            var first = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var second = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");
            var third = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "C");

            await dbContext.Case.Where(w => w.Id == first).Set(s => s.CreatedDate, now.AddHours(-3)).UpdateAsync();
            await dbContext.Case.Where(w => w.Id == second).Set(s => s.CreatedDate, now.AddHours(-2)).UpdateAsync();
            await dbContext.Case.Where(w => w.Id == third).Set(s => s.CreatedDate, now.AddHours(-1)).UpdateAsync();

            await seeder.AssessAsync(dbContext, first, 4, 10, 0.1, deadline: now.AddDays(3), value: 100);
            await seeder.AssessAsync(dbContext, second, 4, 90, 0.9, deadline: now.AddDays(1), value: 900);
            await seeder.AssessAsync(dbContext, third, 4, 50, 0.5, deadline: now.AddDays(2), value: 500);

            var service = await BuildAsync(dbContext, Me);
            var page = await service.RunAsync(Request(workflow, sort: sort));

            var expected = sort switch
            {
                CaseSearchSort.Priority or CaseSearchSort.Value or CaseSearchSort.Likelihood =>
                    new List<int> { second, third, first },
                CaseSearchSort.Age => [first, second, third],
                CaseSearchSort.Due => [second, third, first],
                _ => [third, second, first]
            };

            Ids(page).Should().Equal(expected);
        }

        [Fact]
        public async Task PagingKeepsTheTotalAndNeverRepeatsOrSkipsACaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);

            for (var i = 0; i < 7; i++)
            {
                var id = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "P" + i);
                await seeder.AssessAsync(dbContext, id, 4, 10 + i);
            }

            var service = await BuildAsync(dbContext, Me);
            var seen = new List<int>();

            for (var skip = 0; skip < 7; skip += 3)
            {
                var page = await service.RunAsync(Request(workflow, take: 3, skip: skip));
                page.Total.Should().Be(7);
                page.Take.Should().Be(3);
                seen.AddRange(Ids(page));
            }

            seen.Should().HaveCount(7).And.OnlyHaveUniqueItems();

            await Assert.ThrowsAsync<DtoValidationException>(() => service.RunAsync(Request(workflow, take: 0)));
            await Assert.ThrowsAsync<DtoValidationException>(() => service.RunAsync(Request(workflow, take: 201)));
        }

        [Fact]
        public async Task ARowSaysWhatItsDiaryIsForAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var due = DateTime.UtcNow.AddDays(2);
            var regulatory = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "R",
                diary: 1, diaryDate: due);
            var plain = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "P");
            await dbContext.Case.Where(w => w.Id == regulatory).Set(s => s.DiaryTypeId, (byte)3).UpdateAsync();

            var rows = (await (await BuildAsync(dbContext, Me)).RunAsync(Request(workflow))).Rows;

            var typed = rows.Single(r => r.CaseId == regulatory);
            typed.Diary.Should().BeTrue();
            typed.DiaryTypeId.Should().Be(3);
            typed.DiaryType.Should().Be("Regulatory deadline");
            typed.DiaryDate.Should().NotBeNull();
            var untouched = rows.Single(r => r.CaseId == plain);
            untouched.Diary.Should().BeFalse();
            untouched.DiaryTypeId.Should().Be(1);
            untouched.DiaryType.Should().Be("Review");
        }

        [Fact]
        public async Task ARowCarriesTheRiskTheTopDriverTheValueTheStatusAndTheLockAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var deadline = DateTime.UtcNow.AddHours(5);

            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "ACC-9");
            await seeder.AssessAsync(dbContext, caseId, 3, 62, 0.56, 3, 4, deadline, "Structuring 9d", 45000);
            await dbContext.Case.Where(w => w.Id == caseId).Set(s => s.Locked, (byte)1)
                .Set(s => s.LockedUser, Me).UpdateAsync();

            var service = await BuildAsync(dbContext, Me);
            var row = (await service.RunAsync(Request(workflow))).Rows.Single();

            row.CaseKey.Should().Be("AccountId");
            row.CaseKeyValue.Should().Be("ACC-9");
            row.CaseWorkflowGuid.Should().Be(workflow.WorkflowGuid);
            row.StatusName.Should().StartWith(DatabaseFixture.Prefix);
            row.ForeColor.Should().Be("#112233");
            row.BackColor.Should().Be("#445566");
            row.Locked.Should().BeTrue();
            row.LockedUser.Should().Be(Me);
            row.Mine.Should().BeTrue();
            row.Value.Should().Be(45000);
            row.Risk.Assessed.Should().BeTrue();
            row.Risk.Lane.Should().Be(3);
            row.Risk.LaneName.Should().Be("Deadline near");
            row.Risk.Likelihood.Should().Be(0.56);
            row.Risk.LikelihoodBandName.Should().Be("Likely");
            row.Risk.Impact.Should().Be(4);
            row.Risk.ImpactName.Should().Be("High");
            row.Risk.Priority.Should().Be(62);
            row.Risk.TopDriver.Should().Be("Structuring 9d");
            row.Risk.TopDriverFamily.Should().Be("Rule");
            row.Risk.NextDeadline.Should().BeCloseTo(deadline, TimeSpan.FromSeconds(1));
            row.StatusEnteredDate.Should().BeNull();
        }

        [Fact]
        public async Task AnUnassessedCaseSaysSoAndIsNeverMistakenForALaneOneCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var row = (await (await BuildAsync(dbContext, Me)).RunAsync(Request(workflow))).Rows.Single();

            row.Risk.Assessed.Should().BeFalse();
            row.Risk.Lane.Should().Be((int)CaseRiskLane.Standard);
            row.Risk.Priority.Should().Be(0);
        }

        [Fact]
        public async Task OnlyTheLatestAssessmentOfACaseDecidesItsLaneAndItsPlaceInTheUrgentViewAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var cooled = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var hot = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");

            await seeder.AssessAsync(dbContext, cooled, 1, 90);
            await seeder.AssessAsync(dbContext, cooled, 4, 20);
            await seeder.AssessAsync(dbContext, hot, 4, 20);
            await seeder.AssessAsync(dbContext, hot, 2, 70);

            var service = await BuildAsync(dbContext, Me);
            var urgent = await service.RunAsync(Request(workflow, CaseSearchView.Urgent));

            Ids(urgent).Should().Equal(hot);
            urgent.Rows[0].Risk.Sequence.Should().Be(2);
            (await service.RunAsync(Request(workflow))).Rows.Single(r => r.CaseId == cooled).Risk.Lane.Should().Be(4);
        }

        private async Task<int> ViewedCaseAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow, string key,
            DateTime? viewedAt = null)
        {
            var id = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: key);
            await new CaseViewRepository(dbContext, Me).UpsertAsync(id, 0, 0);

            if (viewedAt.HasValue)
            {
                await dbContext.CaseView.Where(w => w.CaseId == id && w.User == Me)
                    .Set(s => s.LastViewedDate, viewedAt.Value).UpdateAsync();
            }

            return id;
        }

        private static async Task<List<int>> ReasonsAsync(CaseSearchService service, CaseRiskSeeder.Seeded workflow,
            int caseId)
        {
            var row = (await service.RunAsync(Request(workflow))).Rows.Single(r => r.CaseId == caseId);

            return row.Attention.Reasons.Select(r => r.Code).ToList();
        }

        [Fact]
        public async Task AttentionNeedsTheCallerToHaveLookedOrHoldTheCaseAndNamesEachReasonAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);
            var transitions = new CaseStatusTransitionRepository(dbContext);
            var before = DateTime.UtcNow.AddHours(-2);
            var after = DateTime.UtcNow.AddHours(-1);

            var neverSeen = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "N");
            await transitions.EnterAsync(neverSeen, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.Reopened, "x", after);

            var reopened = await ViewedCaseAsync(dbContext, workflow, "R", before);
            await transitions.EnterAsync(reopened, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.Reopened, "x", after);

            var escalated = await ViewedCaseAsync(dbContext, workflow, "E", before);
            await transitions.EnterAsync(escalated, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.Escalated, null, after);

            var movedByOther = await ViewedCaseAsync(dbContext, workflow, "O", before);
            await transitions.EnterAsync(movedByOther, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.StatusChanged, "someone-else", after);

            var movedByMe = await ViewedCaseAsync(dbContext, workflow, "M", before);
            await transitions.EnterAsync(movedByMe, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.StatusChanged, Me, after);

            var diaryDue = await ViewedCaseAsync(dbContext, workflow, "D", after);
            await dbContext.Case.Where(w => w.Id == diaryDue).Set(s => s.Diary, (byte)1)
                .Set(s => s.DiaryDate, DateTime.UtcNow.AddMinutes(-5)).UpdateAsync();

            var diaryFuture = await ViewedCaseAsync(dbContext, workflow, "F", after);
            await dbContext.Case.Where(w => w.Id == diaryFuture).Set(s => s.Diary, (byte)1)
                .Set(s => s.DiaryDate, DateTime.UtcNow.AddDays(3)).UpdateAsync();

            var diaryHeld = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "H",
                diary: 1, diaryDate: DateTime.UtcNow.AddMinutes(-5));
            await dbContext.Case.Where(w => w.Id == diaryHeld).Set(s => s.Locked, (byte)1)
                .Set(s => s.LockedUser, Me).UpdateAsync();

            var riskRose = await ViewedCaseAsync(dbContext, workflow, "K", after);
            await seeder.AssessAsync(dbContext, riskRose, 4, 20);
            await new CaseViewRepository(dbContext, Me).UpsertAsync(riskRose, 0, 1);
            await seeder.AssessAsync(dbContext, riskRose, 4, 45);

            var riskSlight = await ViewedCaseAsync(dbContext, workflow, "L", after);
            await seeder.AssessAsync(dbContext, riskSlight, 4, 20);
            await new CaseViewRepository(dbContext, Me).UpsertAsync(riskSlight, 0, 1);
            await seeder.AssessAsync(dbContext, riskSlight, 4, 25);

            var theirEvents = await ViewedCaseAsync(dbContext, workflow, "T", after);
            await seeder.EventAsync(dbContext, theirEvents, 9, "someone-else");
            await seeder.EventAsync(dbContext, theirEvents, 11, "someone-else");

            var myEvents = await ViewedCaseAsync(dbContext, workflow, "Y", after);
            await seeder.EventAsync(dbContext, myEvents, 9, Me);

            var onlyLookedAt = await ViewedCaseAsync(dbContext, workflow, "Z", after);
            await seeder.EventAsync(dbContext, onlyLookedAt, 4, "someone-else");
            await seeder.EventAsync(dbContext, onlyLookedAt, 2, "someone-else");

            var closed = await ViewedCaseAsync(dbContext, workflow, "C", before);
            await dbContext.Case.Where(w => w.Id == closed).Set(s => s.ClosedStatusId, (byte)3).UpdateAsync();

            (await ReasonsAsync(service, workflow, neverSeen)).Should().BeEmpty();
            (await ReasonsAsync(service, workflow, reopened)).Should().Equal((int)CaseAttentionReason.Reopened);
            (await ReasonsAsync(service, workflow, escalated)).Should().Equal((int)CaseAttentionReason.Escalated);
            (await ReasonsAsync(service, workflow, movedByOther)).Should()
                .Equal((int)CaseAttentionReason.MovedByOther);
            (await ReasonsAsync(service, workflow, movedByMe)).Should().BeEmpty();
            (await ReasonsAsync(service, workflow, diaryDue)).Should().Equal((int)CaseAttentionReason.DiaryDue);
            (await ReasonsAsync(service, workflow, diaryFuture)).Should().BeEmpty();
            (await ReasonsAsync(service, workflow, diaryHeld)).Should().Equal((int)CaseAttentionReason.DiaryDue);
            (await ReasonsAsync(service, workflow, riskRose)).Should().Equal((int)CaseAttentionReason.RiskRose);
            (await ReasonsAsync(service, workflow, riskSlight)).Should().BeEmpty();
            (await ReasonsAsync(service, workflow, theirEvents)).Should().Equal((int)CaseAttentionReason.NewActivity);
            (await ReasonsAsync(service, workflow, myEvents)).Should().BeEmpty();
            (await ReasonsAsync(service, workflow, onlyLookedAt)).Should().BeEmpty();

            var rows = (await service.RunAsync(Request(workflow))).Rows;
            rows.Should().NotContain(r => r.CaseId == closed);

            var rise = rows.Single(r => r.CaseId == riskRose).Attention;
            rise.Needed.Should().BeTrue();
            rise.PriorityRise.Should().Be(25);
            rise.Reasons.Single().Text.Should().Contain("25");
            rows.Single(r => r.CaseId == theirEvents).Attention.ChangesSince.Should().Be(2);
        }

        [Fact]
        public async Task TheReturnedViewHoldsExactlyTheCasesTheListFlagsAsNeedingTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);
            var transitions = new CaseStatusTransitionRepository(dbContext);
            var before = DateTime.UtcNow.AddHours(-2);

            var reopened = await ViewedCaseAsync(dbContext, workflow, "R", before);
            await transitions.EnterAsync(reopened, workflow.WorkflowGuid, workflow.Neutral,
                CaseTransitionEnterReason.Reopened, "x", DateTime.UtcNow.AddHours(-1));

            var diaryDue = await ViewedCaseAsync(dbContext, workflow, "D", DateTime.UtcNow);
            await dbContext.Case.Where(w => w.Id == diaryDue).Set(s => s.Diary, (byte)1)
                .Set(s => s.DiaryDate, DateTime.UtcNow.AddMinutes(-5)).UpdateAsync();

            var risen = await ViewedCaseAsync(dbContext, workflow, "K", DateTime.UtcNow);
            await seeder.AssessAsync(dbContext, risen, 4, 20);
            await new CaseViewRepository(dbContext, Me).UpsertAsync(risen, 0, 1);
            await seeder.AssessAsync(dbContext, risen, 4, 50);

            var events = await ViewedCaseAsync(dbContext, workflow, "T", DateTime.UtcNow);
            await seeder.EventAsync(dbContext, events, 7, "someone-else");

            await ViewedCaseAsync(dbContext, workflow, "Quiet", DateTime.UtcNow);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "Unseen");

            var all = await service.RunAsync(Request(workflow));
            var flagged = all.Rows.Where(r => r.Attention.Needed).Select(r => r.CaseId).OrderBy(i => i).ToList();
            var returned = await service.RunAsync(Request(workflow, CaseSearchView.Returned));

            flagged.Should().Equal(new[] { reopened, diaryDue, risen, events }.OrderBy(i => i));
            Ids(returned).OrderBy(i => i).Should().Equal(flagged);

            var counts = await service.CountsAsync(workflow.WorkflowGuid);
            counts.Single(c => c.Kind == "View" && c.Key == ((int)CaseSearchView.Returned).ToString()).Count
                .Should().Be(flagged.Count);
        }

        [Fact]
        public async Task TheMineFreeAndAllViewsFollowTheLockAndTheClosedStatusAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, Other);
            var service = await BuildAsync(dbContext, Me);

            var mine = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "A");
            var theirs = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "B");
            var free = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "C");
            var suspendOpen = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 1, "D");
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, "E");
            var bypass = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 4, "F");

            await dbContext.Case.Where(w => w.Id == mine).Set(s => s.Locked, (byte)1).Set(s => s.LockedUser, Me)
                .UpdateAsync();
            await dbContext.Case.Where(w => w.Id == theirs).Set(s => s.Locked, (byte)1).Set(s => s.LockedUser, Other)
                .UpdateAsync();

            Ids(await service.RunAsync(Request(workflow, CaseSearchView.Mine))).Should().Equal(mine);
            Ids(await service.RunAsync(Request(workflow, CaseSearchView.Free))).OrderBy(i => i)
                .Should().Equal(new[] { free, suspendOpen }.OrderBy(i => i));
            Ids(await service.RunAsync(Request(workflow, CaseSearchView.All))).OrderBy(i => i)
                .Should().Equal(new[] { mine, theirs, free, suspendOpen }.OrderBy(i => i));

            var otherService = await BuildAsync(dbContext, Other);
            Ids(await otherService.RunAsync(Request(workflow, CaseSearchView.Mine))).Should().Equal(theirs);
            closed.Should().BePositive();
            bypass.Should().BePositive();
        }

        [Fact]
        public async Task TheOverdueViewAndEachRowShowTheServiceLevelsTheWorkflowAndStatusSetAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var now = DateTime.UtcNow;

            await dbContext.CaseWorkflow.Where(w => w.Guid == workflow.WorkflowGuid)
                .Set(s => s.FirstViewTargetMinutes, 30).Set(s => s.OwnershipTargetMinutes, 120)
                .Set(s => s.ResolutionTargetMinutes, 1440).UpdateAsync();
            await dbContext.CaseWorkflowStatus.Where(w => w.Guid == workflow.Neutral)
                .Set(s => s.MaxDwellMinutes, 60).UpdateAsync();

            var fresh = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "Fresh");
            var unviewed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "Unviewed");
            var viewedInTime = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral,
                caseKeyValue: "Viewed");
            var dwelling = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "Dwell");

            foreach (var (id, created) in new[]
                     {
                         (fresh, now.AddMinutes(-5)), (unviewed, now.AddMinutes(-45)),
                         (viewedInTime, now.AddMinutes(-45)), (dwelling, now.AddMinutes(-10))
                     })
            {
                await dbContext.Case.Where(w => w.Id == id).Set(s => s.CreatedDate, created).UpdateAsync();
            }

            await seeder.EventAsync(dbContext, viewedInTime, 4, "someone", now.AddMinutes(-40));
            await seeder.EventAsync(dbContext, viewedInTime, 6, "someone", now.AddMinutes(-39));
            await seeder.EventAsync(dbContext, dwelling, 4, "someone", now.AddMinutes(-9));
            await seeder.EventAsync(dbContext, dwelling, 6, "someone", now.AddMinutes(-9));
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(dwelling, workflow.WorkflowGuid,
                workflow.Neutral, CaseTransitionEnterReason.Created, null, now.AddMinutes(-90));
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(fresh, workflow.WorkflowGuid,
                workflow.Neutral, CaseTransitionEnterReason.Created, null, now.AddMinutes(-5));

            var service = await BuildAsync(dbContext, Me);
            var rows = (await service.RunAsync(Request(workflow))).Rows;

            rows.Single(r => r.CaseId == fresh).ServiceLevel.State.Should().Be(ServiceLevelStateOnTrack);
            var late = rows.Single(r => r.CaseId == unviewed).ServiceLevel;
            late.State.Should().Be(4);
            late.Worst!.DimensionName.Should().Be("First view");
            late.Worst.TargetMinutes.Should().Be(30);

            var met = rows.Single(r => r.CaseId == viewedInTime);
            met.FirstViewedBy.Should().Be("someone");
            met.FirstOwnedDate.Should().NotBeNull();
            met.ServiceLevel.Measures.Single(m => m.DimensionName == "First view").StateName.Should().Be("Met");

            rows.Single(r => r.CaseId == dwelling).ServiceLevel.Measures
                .Single(m => m.DimensionName == "Time in status").StateName.Should().Be("Breached");
            rows.Single(r => r.CaseId == dwelling).StatusEnteredDate.Should().NotBeNull();

            Ids(await service.RunAsync(Request(workflow, CaseSearchView.Overdue))).OrderBy(i => i)
                .Should().Equal(new[] { unviewed, dwelling }.OrderBy(i => i));
        }

        private const int ServiceLevelStateOnTrack = 2;

        [Fact]
        public async Task CountsListEveryViewTheWorkflowFiltersAndTheSavedSearchesAndFlagAFailingOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, Other);
            var service = await BuildAsync(dbContext, Me);

            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "WANTED");
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "WANTED");
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "OTHER");

            var filters = new CaseSearchFilterRepository(dbContext, Me,
                (await dbContext.UserInTenant.Where(u => u.User == Me).Select(u => u.TenantRegistryId).FirstAsync()));
            var workflowId = (await filters.GetAccessibleCaseWorkflowIdAsync(workflow.WorkflowGuid))!.Value;

            string Filter(string value) => "{\"condition\":\"AND\",\"rules\":[{\"id\":\"CaseKeyValue\"," +
                                           "\"field\":\"CaseKeyValue\",\"type\":\"string\"," +
                                           "\"operator\":\"equal\",\"value\":\"" + value + "\"}]}";

            await filters.InsertAsync(new CaseSearchFilter
            {
                CaseWorkflowId = workflowId, Name = "Wanted only", FilterJson = Filter("WANTED"),
                SelectJson = "{\"condition\":\"AND\",\"rules\":[]}"
            });
            await filters.InsertAsync(new CaseSearchFilter
            {
                CaseWorkflowId = workflowId, Name = "Broken", FilterJson = "{\"condition\":\"AND\",\"rules\":[" +
                    "{\"id\":\"NoSuchField\",\"field\":\"x\",\"type\":\"string\"," +
                    "\"operator\":\"equal\",\"value\":\"1\"}]}",
                SelectJson = "{\"condition\":\"AND\",\"rules\":[]}"
            });

            var counts = await service.CountsAsync(workflow.WorkflowGuid);

            counts.Where(c => c.Kind == "View").Select(c => c.Name).Should().Equal("My cases", "Returned to me",
                "Free for all", "Urgent", "Overdue", "All open", "Quality review samples");
            counts.Single(c => c.Name == "All open").Count.Should().Be(3);
            counts.Single(c => c.Name == "Free for all").Count.Should().Be(3);
            counts.Single(c => c.Name == "My cases").Count.Should().Be(0);
            var own = counts.Single(c => c.Name == "Wanted only");
            own.Kind.Should().Be("Own");
            own.Mine.Should().BeTrue();
            own.Count.Should().Be(2);
            counts.Single(c => c.Name == "Broken").Failed.Should().BeTrue();
        }

        [Fact]
        public async Task ASavedSearchRunsThroughTheSameCompilerAndNeverShowsMoreThanTheCallerMaySeeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            var service = await BuildAsync(dbContext, Me);

            var wanted = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "WANTED");
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "OTHER");

            var tenant = await dbContext.UserInTenant.Where(u => u.User == Me).Select(u => u.TenantRegistryId)
                .FirstAsync();
            var filters = new CaseSearchFilterRepository(dbContext, Me, tenant);
            var saved = await filters.InsertAsync(new CaseSearchFilter
            {
                CaseWorkflowId = (await filters.GetAccessibleCaseWorkflowIdAsync(workflow.WorkflowGuid))!.Value,
                Name = "Wanted",
                FilterJson = "{\"condition\":\"AND\",\"rules\":[{\"id\":\"CaseKeyValue\"," +
                             "\"field\":\"CaseKeyValue\",\"type\":\"string\",\"operator\":\"equal\"," +
                             "\"value\":\"WANTED\"}]}",
                SelectJson = "{\"condition\":\"AND\",\"rules\":[]}"
            });

            var page = await service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                CaseSearchFilterGuid = saved.Guid
            });

            Ids(page).Should().Equal(wanted);

            var stranger = await BuildAsync(dbContext, Other);
            await Assert.ThrowsAsync<NotFoundException>(() => stranger.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                CaseSearchFilterGuid = saved.Guid
            }));
        }

        [Fact]
        public async Task ASavedSearchCanFilterOnTheTypeOfTheDiaryAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            var service = await BuildAsync(dbContext, Me);
            var due = DateTime.UtcNow.AddDays(3);
            var regulatory = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "R",
                diary: 1, diaryDate: due);
            var review = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "V",
                diary: 1, diaryDate: due);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "N");
            await dbContext.Case.Where(w => w.Id == regulatory).Set(s => s.DiaryTypeId, (byte)3).UpdateAsync();

            var tenant = await dbContext.UserInTenant.Where(u => u.User == Me).Select(u => u.TenantRegistryId)
                .FirstAsync();
            var filters = new CaseSearchFilterRepository(dbContext, Me, tenant);
            var saved = await filters.InsertAsync(new CaseSearchFilter
            {
                CaseWorkflowId = (await filters.GetAccessibleCaseWorkflowIdAsync(workflow.WorkflowGuid))!.Value,
                Name = "Regulatory",
                FilterJson = "{\"condition\":\"AND\",\"rules\":[{\"id\":\"DiaryTypeId\"," +
                             "\"field\":\"DiaryTypeId\",\"type\":\"integer\",\"operator\":\"equal\"," +
                             "\"value\":3}]}",
                SelectJson = "{\"condition\":\"AND\",\"rules\":[]}"
            });

            var page = await service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                CaseSearchFilterGuid = saved.Guid
            });

            Ids(page).Should().Equal(regulatory);
            review.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task AWorkflowTheCallersRoleIsNotGrantedAndAnotherTenantsWorkflowAreNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var notGranted = await BuildAsync(dbContext, Other);
            await Assert.ThrowsAsync<NotFoundException>(() => notGranted.RunAsync(Request(workflow)));
            await Assert.ThrowsAsync<NotFoundException>(() => notGranted.CountsAsync(workflow.WorkflowGuid));
            await Assert.ThrowsAsync<NotFoundException>(() => notGranted.SkimAsync(Request(workflow)));

            var tenantB = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await Assert.ThrowsAsync<NotFoundException>(() => tenantB.RunAsync(Request(workflow)));
            await Assert.ThrowsAsync<NotFoundException>(() => tenantB.RunAsync(Request(
                new CaseRiskSeeder.Seeded(Guid.NewGuid(), Guid.Empty, Guid.Empty, Guid.Empty))));
        }

        [Fact]
        public async Task OnlyOneWayOfChoosingTheCasesIsAllowedAndAnUnknownSavedSearchOrSessionIsNotFoundAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            var service = await BuildAsync(dbContext, Me);

            await Assert.ThrowsAsync<DtoValidationException>(() => service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                View = CaseSearchView.Mine,
                CaseSearchFilterGuid = Guid.NewGuid()
            }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                CaseSearchFilterGuid = Guid.NewGuid()
            }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                SessionGuid = Guid.NewGuid()
            }));
            await Assert.ThrowsAsync<NotFoundException>(() => service.RunAsync(new CaseSearchRequestDto
            {
                CaseWorkflowGuid = workflow.WorkflowGuid,
                CaseWorkflowFilterGuid = Guid.NewGuid()
            }));
        }

        [Fact]
        public async Task TheRiskMatrixCountsCasesByLikelihoodAndImpactAndUnassessedCasesUnderZeroAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);

            foreach (var (band, impact) in new[] { (3, 4), (3, 4), (3, 4), (1, 2), (4, 5) })
            {
                var id = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
                await seeder.AssessAsync(dbContext, id, 4, 10, likelihoodBand: band, impact: impact);
            }

            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 3);
            await seeder.AssessAsync(dbContext, closed, 4, 10, likelihoodBand: 2, impact: 2);

            var matrix = await service.RiskMatrixAsync(Request(workflow));

            matrix.Total.Should().Be(6);
            matrix.Cells.Single(c => c.LikelihoodBand == 3 && c.Impact == 4).Cases.Should().Be(3);
            matrix.Cells.Single(c => c.LikelihoodBand == 1 && c.Impact == 2).Cases.Should().Be(1);
            matrix.Cells.Single(c => c.LikelihoodBand == 4 && c.Impact == 5).Cases.Should().Be(1);
            matrix.Cells.Single(c => c.LikelihoodBand == 0 && c.Impact == 0).Cases.Should().Be(1);
            matrix.Cells.Should().NotContain(c => c.LikelihoodBand == 2);
        }

        [Fact]
        public async Task SkimLocksTheTopCaseRecordsTheLookAndOffersTheNextOnesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, Other);
            var service = await BuildAsync(dbContext, Me);

            var top = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "TOP");
            var second = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "SECOND");
            var lockedByOther = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "L");
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 3, "CLOSED");
            await seeder.AssessAsync(dbContext, top, 1, 50);
            await seeder.AssessAsync(dbContext, second, 4, 30);
            await seeder.AssessAsync(dbContext, lockedByOther, 1, 99);
            await seeder.AssessAsync(dbContext, closed, 1, 99);
            await dbContext.Case.Where(w => w.Id == lockedByOther).Set(s => s.Locked, (byte)1)
                .Set(s => s.LockedUser, Other).UpdateAsync();

            var skim = await service.SkimAsync(Request(workflow));

            skim.Case!.CaseId.Should().Be(top);
            skim.Case.Locked.Should().BeTrue();
            skim.Case.Mine.Should().BeTrue();
            skim.Case.LockedUser.Should().Be(Me);
            skim.NextCaseIds.Should().Equal(second);
            skim.Remaining.Should().Be(2);

            var stored = await dbContext.Case.FirstAsync(w => w.Id == top);
            stored.Locked.Should().Be(1);
            stored.LockedUser.Should().Be(Me);
            stored.LockedDate.Should().NotBeNull();

            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == top && w.CaseEventTypeId == 2
                                                       && w.CreatedUser == Me)).Should().Be(1);
            (await new CaseViewRepository(dbContext, Me).GetAsync(top)).Should().NotBeNull();

            var again = await service.SkimAsync(Request(workflow));
            again.Case!.CaseId.Should().Be(top);

            await dbContext.Case.Where(w => w.Id == top).Set(s => s.LockedUser, Other).UpdateAsync();
            var next = await service.SkimAsync(Request(workflow));
            next.Case!.CaseId.Should().Be(second);
            next.Remaining.Should().Be(1);

            await dbContext.Case.Where(w => w.Id == second).Set(s => s.LockedUser, Other).UpdateAsync();
            var none = await service.SkimAsync(Request(workflow));
            none.Case.Should().BeNull();
            none.Remaining.Should().Be(0);
        }

        [Fact]
        public async Task SkimTakesAReturnedCaseBeforeAHigherPriorityStandardOneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var service = await BuildAsync(dbContext, Me);

            var standard = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "STD");
            await seeder.AssessAsync(dbContext, standard, 4, 95);
            var returned = await ViewedCaseAsync(dbContext, workflow, "RET", DateTime.UtcNow.AddHours(-2));
            await seeder.AssessAsync(dbContext, returned, 4, 5);
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(returned, workflow.WorkflowGuid,
                workflow.Neutral, CaseTransitionEnterReason.Reopened, "x", DateTime.UtcNow.AddHours(-1));

            (await service.SkimAsync(Request(workflow))).Case!.CaseId.Should().Be(returned);
        }

        [Fact]
        public async Task TwoPeopleSkimmingAtOnceNeverTakeTheSameCaseAsync()
        {
            await using var setup = fx.GetDbContext();
            var workflow = await WorkflowAsync(setup, Me, Other);

            for (var i = 0; i < 6; i++)
            {
                var id = await seeder.SeedCaseAsync(setup, workflow, workflow.Neutral, caseKeyValue: "S" + i);
                await seeder.AssessAsync(setup, id, 4, 10 + i);
            }

            async Task<List<int>> SkimmerAsync(string user)
            {
                var taken = new List<int>();
                await using var dbContext = fx.GetDbContext();
                var service = await BuildAsync(dbContext, user);

                for (var i = 0; i < 3; i++)
                {
                    var skim = await service.SkimAsync(Request(workflow));

                    if (skim.Case != null)
                    {
                        taken.Add(skim.Case.CaseId);
                        await dbContext.Case.Where(w => w.Id == skim.Case.CaseId)
                            .Set(s => s.ClosedStatusId, (byte)3).UpdateAsync();
                    }
                }

                return taken;
            }

            var results = await Task.WhenAll(SkimmerAsync(Me), SkimmerAsync(Other));
            var all = results.SelectMany(r => r).ToList();

            all.Should().OnlyHaveUniqueItems();
            all.Should().HaveCount(6);
        }

        [Fact]
        public async Task AnotherUsersLookAtACaseNeverCreatesAttentionOrDuplicatesTheRowForTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, Other);
            var service = await BuildAsync(dbContext, Me);
            var otherService = await BuildAsync(dbContext, Other);

            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "SHARED");
            await new CaseViewRepository(dbContext, Other).UpsertAsync(caseId, 0, 0);
            await new CaseViewRepository(dbContext, Me).UpsertAsync(caseId, 0, 0);
            await dbContext.CaseView.Where(w => w.CaseId == caseId && w.User == Other)
                .Set(s => s.LastViewedDate, DateTime.UtcNow.AddHours(-3)).UpdateAsync();
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(caseId, workflow.WorkflowGuid,
                workflow.Neutral, CaseTransitionEnterReason.Reopened, "x", DateTime.UtcNow.AddHours(-2));

            var mine = await service.RunAsync(Request(workflow));
            var theirs = await otherService.RunAsync(Request(workflow));

            mine.Rows.Should().ContainSingle();
            mine.Rows[0].Attention.Needed.Should().BeFalse();
            theirs.Rows.Should().ContainSingle();
            theirs.Rows[0].Attention.Needed.Should().BeTrue();
            theirs.Rows[0].Attention.Reasons.Single().Code.Should().Be((int)CaseAttentionReason.Reopened);
        }

        [Fact]
        public async Task TheWorkflowChoicesAreOnlyTheActiveOnesTheCallersRoleIsGrantedInTheirTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var granted = await WorkflowAsync(dbContext, Me);
            var notGranted = await seeder.SeedWorkflowAsync(dbContext);
            var inactive = await WorkflowAsync(dbContext, Me);
            await dbContext.CaseWorkflow.Where(w => w.Guid == inactive.WorkflowGuid).Set(s => s.Active, (byte)0)
                .UpdateAsync();

            var mine = await (await BuildAsync(dbContext, Me)).WorkflowsAsync();

            mine.Select(w => w.Guid).Should().Contain(granted.WorkflowGuid);
            mine.Select(w => w.Guid).Should().NotContain([notGranted.WorkflowGuid, inactive.WorkflowGuid]);
            mine.Single(w => w.Guid == granted.WorkflowGuid).Name.Should().StartWith(DatabaseFixture.Prefix);
            mine.Single(w => w.Guid == granted.WorkflowGuid).ModelName.Should().StartWith(DatabaseFixture.Prefix);

            (await (await BuildAsync(dbContext, fx.Seed.UserTenantB)).WorkflowsAsync()).Select(w => w.Guid)
                .Should().NotContain(granted.WorkflowGuid);
            await Assert.ThrowsAsync<ForbiddenException>(() =>
                BuildAsync(dbContext, fx.Seed.UserWithoutPermission).Result.WorkflowsAsync());
        }
    }
}
