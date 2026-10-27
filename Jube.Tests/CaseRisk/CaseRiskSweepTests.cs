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
using Jube.CaseRisk;
using Jube.Data.Context;
using Jube.Data.Query.GetCaseKeyOutcomeHistoryQuery;
using Jube.Data.Query.GetCaseRiskCandidatesQuery;
using Jube.Data.Query.GetCaseWorkflowOutcomeCountsQuery;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Engine.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.CaseRisk
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseRiskSweepTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly CaseRiskSettings settings = new();
        private static readonly TimeSpan interval = TimeSpan.FromMinutes(15);
        private readonly CaseRiskSeeder seeder = new(fx);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseRiskSweepResult> SweepAsync(DbContext dbContext, DateTime now) =>
            CaseRiskSweep.RunAsync(dbContext, settings, interval, 1000, now, TestLog.NoOp);

        [Fact]
        public async Task OutcomeCountsOnlyUseClosedCasesInStatusesMarkedConfirmedOrNotConfirmedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var other = await seeder.SeedWorkflowAsync(dbContext);
            var ruleGuid = Guid.NewGuid();

            await seeder.SeedClosedAsync(dbContext, workflow, 3, 5, ruleGuid);
            await seeder.SeedClosedAsync(dbContext, workflow, 1, 0);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 4);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 2);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 3);
            await seeder.SeedClosedAsync(dbContext, other, 7, 7);

            var counts = await new GetCaseWorkflowOutcomeCountsQuery(dbContext).ExecuteAsync(workflow.WorkflowGuid);

            counts.Closed.Should().Be(9);
            counts.Confirmed.Should().Be(4);
            var rule = counts.Rules.Single(r => r.RuleGuid == ruleGuid);
            rule.Closed.Should().Be(8);
            rule.Confirmed.Should().Be(3);
        }

        [Fact]
        public async Task KeyHistoryCountsEarlierClosedOutcomesOnTheSameKeyAndNeverTheCaseItselfAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.SeedClosedAsync(dbContext, workflow, 1, 3, null, "ACC-1");
            await seeder.SeedClosedAsync(dbContext, workflow, 5, 5, null, "ACC-2");
            var self = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3, "ACC-1");

            var history = await new GetCaseKeyOutcomeHistoryQuery(dbContext)
                .ExecuteAsync(workflow.WorkflowGuid, "AccountId", "ACC-1", self);
            var none = await new GetCaseKeyOutcomeHistoryQuery(dbContext)
                .ExecuteAsync(workflow.WorkflowGuid, "AccountId", "NEVER", self);

            history.Closed.Should().Be(4);
            history.Confirmed.Should().Be(1);
            history.NewestClosedDate.Should().NotBeNull();
            none.Closed.Should().Be(0);
        }

        [Fact]
        public async Task AnOpenCaseWithNoHistoryGetsAColdStartAssessmentFromThePriorsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var result = await SweepAsync(dbContext, DateTime.UtcNow);

            result.Written.Should().BeGreaterThanOrEqualTo(1);
            var assessment = await new CaseRiskAssessmentRepository(dbContext).GetLatestAsync(caseId);
            assessment.Sequence.Should().Be(1);
            assessment.TriggerId.Should().Be((byte)CaseRiskTrigger.Initial);
            assessment.MethodId.Should().Be((byte)CaseRiskMethod.ColdStart);
            assessment.BaseRate.Should().BeApproximately(0.08, 1e-9);
            assessment.LikelihoodProbability.Should().BeApproximately(0.08, 1e-9);
            assessment.LikelihoodBandId.Should().Be((byte)CaseLikelihoodBand.Unlikely);
            assessment.EvidenceCount.Should().Be(0);
            assessment.ImpactId.Should().Be((byte)CaseImpactBand.Moderate);
            assessment.LaneId.Should().Be((byte)CaseRiskLane.Standard);
            assessment.Priority.Should().Be(13);
            JArray.Parse(assessment.ContributionsJson).Should().BeEmpty();
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskCheckedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task AWorkflowsOwnRankingSettingsDecideItsCasesAndOnlyItsCasesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tuned = await seeder.SeedWorkflowAsync(dbContext);
            var plain = await seeder.SeedWorkflowAsync(dbContext);
            await dbContext.CaseWorkflow.Where(w => w.Guid == tuned.WorkflowGuid)
                .Set(s => s.RiskBaseRatePrior, 0.4)
                .Set(s => s.RiskImpactWeights, "1,3,10,30,200")
                .UpdateAsync();
            var tunedCase = await seeder.SeedCaseAsync(dbContext, tuned, tuned.Neutral);
            var plainCase = await seeder.SeedCaseAsync(dbContext, plain, plain.Neutral);

            await SweepAsync(dbContext, DateTime.UtcNow);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            var tunedAssessment = await repository.GetLatestAsync(tunedCase);
            var plainAssessment = await repository.GetLatestAsync(plainCase);

            tunedAssessment.BaseRate.Should().BeApproximately(0.4, 1e-9);
            tunedAssessment.LikelihoodBandId.Should().Be((byte)CaseLikelihoodBand.Possible);
            plainAssessment.BaseRate.Should().BeApproximately(0.08, 1e-9);
            plainAssessment.LikelihoodBandId.Should().Be((byte)CaseLikelihoodBand.Unlikely);
            tunedAssessment.Priority.Should().BeGreaterThan(plainAssessment.Priority);
        }

        [Fact]
        public async Task ChangingAWorkflowsRankingSettingsReassessesItsOpenCasesUnderTheModelChangedTriggerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var other = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var otherCase = await seeder.SeedCaseAsync(dbContext, other, other.Neutral);
            var closedId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed);

            await SweepAsync(dbContext, DateTime.UtcNow);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            (await repository.GetLatestAsync(caseId)).LikelihoodProbability.Should().BeApproximately(0.08, 1e-9);

            var workflows = new CaseWorkflowRepository(dbContext, fx.Seed.UserWithPermission);
            var existing = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow.WorkflowGuid);

            existing.Name = existing.Name + "x";
            await workflows.UpdateAsync(existing);

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskCheckedDate.Should().NotBeNull();

            existing = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow.WorkflowGuid);
            existing.RiskBaseRatePrior = 0.4;
            await workflows.UpdateAsync(existing);

            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskCheckedDate.Should().BeNull();
            (await dbContext.Case.FirstAsync(w => w.Id == otherCase)).RiskCheckedDate.Should().NotBeNull();

            await SweepAsync(dbContext, DateTime.UtcNow);

            var after = await repository.GetLatestAsync(caseId);
            after.Sequence.Should().Be(2);
            after.TriggerId.Should().Be((byte)CaseRiskTrigger.ModelChanged);
            after.BaseRate.Should().BeApproximately(0.4, 1e-9);
            (await repository.GetLatestAsync(otherCase)).Sequence.Should().Be(1);
            closedId.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ChangingTheTypologySeverityOrTheValueFieldReassessesTooAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await SweepAsync(dbContext, DateTime.UtcNow);

            var workflows = new CaseWorkflowRepository(dbContext, fx.Seed.UserWithPermission);
            var existing = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow.WorkflowGuid);
            existing.TypologySeverityId = 5;
            await workflows.UpdateAsync(existing);
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskCheckedDate.Should().BeNull();

            await SweepAsync(dbContext, DateTime.UtcNow);
            existing = await dbContext.CaseWorkflow.FirstAsync(w => w.Guid == workflow.WorkflowGuid);
            existing.ValueField = "Amount";
            await workflows.UpdateAsync(existing);
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskCheckedDate.Should().BeNull();

            await SweepAsync(dbContext, DateTime.UtcNow);
            (await new CaseRiskAssessmentRepository(dbContext).GetLatestAsync(caseId)).ImpactId.Should()
                .Be((byte)CaseImpactBand.Severe);
        }

        [Fact]
        public async Task ARuleThatHasBeenRightBeforeRaisesTheLikelihoodAndOneThatHasNotLowersItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var good = Guid.NewGuid();
            var noisy = Guid.NewGuid();
            await seeder.SeedClosedAsync(dbContext, workflow, 40, 10, good, "H1");
            await seeder.SeedClosedAsync(dbContext, workflow, 1, 49, noisy, "H2");
            var fromGood = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "N1", good);
            var fromNoisy = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "N2", noisy);

            await SweepAsync(dbContext, DateTime.UtcNow);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            var goodAssessment = await repository.GetLatestAsync(fromGood);
            var noisyAssessment = await repository.GetLatestAsync(fromNoisy);

            goodAssessment.LikelihoodProbability.Should().BeGreaterThan(noisyAssessment.LikelihoodProbability);
            goodAssessment.LikelihoodProbability.Should().BeGreaterThan(goodAssessment.BaseRate!.Value);
            noisyAssessment.LikelihoodProbability.Should().BeLessThan(noisyAssessment.BaseRate!.Value);
            goodAssessment.EvidenceCount.Should().Be(100);

            var contribution = JArray.Parse(goodAssessment.ContributionsJson).Single();
            contribution["family"]!.Value<string>().Should().Be("Rule");
            contribution["closed"]!.Value<int>().Should().Be(50);
            contribution["confirmed"]!.Value<int>().Should().Be(40);
            contribution["logOdds"]!.Value<double>().Should().BePositive();
        }

        [Fact]
        public async Task AKeyWhoseEarlierCasesWereAllFalsePositivesLowersTheLikelihoodAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.SeedClosedAsync(dbContext, workflow, 20, 20, null, "OTHER");
            await seeder.SeedClosedAsync(dbContext, workflow, 0, 6, null, "REPEAT");
            var repeat = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "REPEAT");
            var fresh = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "FRESH");

            await SweepAsync(dbContext, DateTime.UtcNow);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            var repeatAssessment = await repository.GetLatestAsync(repeat);
            var freshAssessment = await repository.GetLatestAsync(fresh);

            repeatAssessment.LikelihoodProbability.Should().BeLessThan(freshAssessment.LikelihoodProbability);
            JArray.Parse(repeatAssessment.ContributionsJson).Single()["family"]!.Value<string>()
                .Should().Be("KeyHistory");
        }

        [Fact]
        public async Task TheMoneyInThePayloadAndTheWorkflowTypologyDecideTheImpactAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext, 2, "Amount");
            var small = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "S",
                json: "{\"Payload\":{\"Amount\":150}}");
            var large = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "L",
                json: "{\"payload\":{\"amount\":\"25000000\"}}");
            var none = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "N");

            await SweepAsync(dbContext, DateTime.UtcNow);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            (await repository.GetLatestAsync(small)).ImpactId.Should().Be((byte)CaseImpactBand.Low);
            (await repository.GetLatestAsync(large)).ImpactId.Should().Be((byte)CaseImpactBand.Severe);
            (await repository.GetLatestAsync(none)).ImpactId.Should().Be((byte)CaseImpactBand.Low);

            var components = JObject.Parse((await repository.GetLatestAsync(large)).ImpactComponentsJson);
            components["moneyBand"]!.Value<int>().Should().Be(5);
            components["value"]!.Value<double>().Should().Be(25_000_000);
        }

        [Fact]
        public async Task ADiaryWithinTwoDaysPutsTheCaseInTheDeadlineLaneAndAnOverdueOneIsMarkedBreachedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var now = DateTime.UtcNow;
            var soon = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "A", diary: 1,
                diaryDate: now.AddDays(1));
            var overdue = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "B", diary: 1,
                diaryDate: now.AddDays(-1));
            var distant = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "C", diary: 1,
                diaryDate: now.AddDays(10));
            var noDiary = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "D", diary: 0,
                diaryDate: now.AddDays(-5));

            await SweepAsync(dbContext, now);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            var soonAssessment = await repository.GetLatestAsync(soon);
            soonAssessment.LaneId.Should().Be((byte)CaseRiskLane.DeadlineNear);
            soonAssessment.DeadlineBreached.Should().Be(0);
            soonAssessment.NextDeadlineDate.Should().NotBeNull();
            (await repository.GetLatestAsync(overdue)).DeadlineBreached.Should().Be(1);
            (await repository.GetLatestAsync(overdue)).LaneId.Should().Be((byte)CaseRiskLane.DeadlineNear);
            (await repository.GetLatestAsync(distant)).LaneId.Should().Be((byte)CaseRiskLane.Standard);
            (await repository.GetLatestAsync(noDiary)).LaneId.Should().Be((byte)CaseRiskLane.Standard);
        }

        [Fact]
        public async Task ClosedAndSuspendBypassCasesAreNeverAssessedAndRecentlyCheckedOnesAreSkippedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var now = DateTime.UtcNow;
            var closed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Confirmed, 3);
            var bypass = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 4);
            var suspendClosed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 2);
            var recent = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral,
                riskCheckedDate: now.AddMinutes(-5));
            var stale = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 1,
                riskCheckedDate: now.AddMinutes(-60));

            await SweepAsync(dbContext, now);

            var repository = new CaseRiskAssessmentRepository(dbContext);
            (await repository.GetLatestAsync(closed)).Should().BeNull();
            (await repository.GetLatestAsync(bypass)).Should().BeNull();
            (await repository.GetLatestAsync(suspendClosed)).Should().BeNull();
            (await repository.GetLatestAsync(recent)).Should().BeNull();
            (await repository.GetLatestAsync(stale)).Should().NotBeNull();
        }

        [Fact]
        public async Task AnUnchangedCaseIsCheckedAgainWithoutWritingAnotherAssessmentButAChangeIsRecordedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var now = DateTime.UtcNow;
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "C", diary: 1,
                diaryDate: now.AddDays(5));
            var repository = new CaseRiskAssessmentRepository(dbContext);

            await SweepAsync(dbContext, now);
            var again = await SweepAsync(dbContext, now.AddMinutes(20));

            again.Examined.Should().BeGreaterThanOrEqualTo(1);
            (await dbContext.CaseRiskAssessment.CountAsync(w => w.CaseId == caseId)).Should().Be(1);
            (await dbContext.Case.FirstAsync(w => w.Id == caseId)).RiskCheckedDate.Should()
                .BeCloseTo(now.AddMinutes(20), TimeSpan.FromSeconds(1));

            await SweepAsync(dbContext, now.AddDays(4).AddHours(1));

            var history = await repository.ListHistoryAsync(caseId, 10);
            history.Select(h => h.Sequence).Should().Equal(2, 1);
            history[0].LaneId.Should().Be((byte)CaseRiskLane.DeadlineNear);
            history[0].TriggerId.Should().Be((byte)CaseRiskTrigger.ScheduledSweep);
            history[1].LaneId.Should().Be((byte)CaseRiskLane.Standard);
        }

        [Fact]
        public async Task ACaseThatWasChangedAndSoHasNoCheckDateIsPickedUpFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            var now = DateTime.UtcNow;
            var old = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral,
                riskCheckedDate: now.AddHours(-3));
            var changed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var first = await new GetCaseRiskCandidatesQuery(dbContext)
                .ExecuteAsync(now - interval, 1000);

            var ours = first.Where(c => c.CaseId == old || c.CaseId == changed).Select(c => c.CaseId).ToList();
            ours.Should().Equal(changed, old);
        }

        [Fact]
        public async Task ABatchLimitLeavesTheRestForTheNextSweepAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            for (var i = 0; i < 3; i++)
            {
                await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, 0, "B" + i,
                    riskCheckedDate: DateTime.UtcNow.AddYears(-30).AddDays(i));
            }

            var result = await CaseRiskSweep.RunAsync(dbContext, settings, interval, 2, DateTime.UtcNow,
                TestLog.NoOp);

            result.Examined.Should().Be(2);
        }

        [Fact]
        public void TheDeploymentDefaultsAreTheSpecDefaults()
        {
            var read = CaseRiskSettingsReader.Read(
                TestDynamicEnvironment.Create(new System.Collections.Generic.Dictionary<string, string>()));

            read.Should().BeEquivalentTo(new CaseRiskSettings());
        }
    }
}
