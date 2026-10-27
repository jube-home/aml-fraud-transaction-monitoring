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
using Jube.Service.Exceptions.Query.CaseSummary;
using Jube.Service.Query.CaseById;
using Jube.Service.Query.CaseSummary;
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
    public sealed class CaseSummaryServiceTests(DatabaseFixture fx) : IAsyncLifetime
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

        private static Task<CaseSummaryService> BuildAsync(DbContext dbContext, string? user) =>
            CaseSummaryService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                dynamicEnvironment, TestLog.NoOp);

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext, params string[] users)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, workflow, users.Length == 0 ? [Me] : users);

            return workflow;
        }

        [Fact]
        public async Task APeekReturnsTheWholePictureOfACaseAndRecordsTheLookWithoutTakingTheLockAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var rule = Guid.NewGuid();
            await seeder.SeedClosedAsync(dbContext, workflow, 3, 7, rule);
            await seeder.SeedClosedAsync(dbContext, workflow, 0, 2, null, "ACC-1");
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "ACC-1",
                ruleGuid: rule);
            await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "ACC-1");

            await new CaseRiskAssessmentRepository(dbContext).InsertAsync(new CaseRiskAssessment
            {
                CaseId = caseId, TriggerId = 1, MethodId = 1, BaseRate = 0.08, LikelihoodProbability = 0.3,
                LikelihoodBandId = 2, EvidenceCount = 12,
                ContributionsJson = "[{\"family\":\"Rule\",\"label\":\"Structuring 9d\",\"source\":\"Closed cases\"," +
                                    "\"confirmed\":3,\"closed\":10,\"precision\":0.28,\"logLikelihoodRatio\":1.6," +
                                    "\"decay\":1.0,\"weight\":1.0,\"logOdds\":1.12,\"sharesInputs\":false}]",
                ImpactId = 4,
                ImpactComponentsJson = "{\"typologySeverity\":3,\"moneyBand\":2,\"value\":45000," +
                                       "\"exposureUplift\":0,\"breadthUplift\":1,\"floor\":1}",
                LaneId = 4, LaneReason = "No deadline applies.", ExpectedHarm = 9, Priority = 40
            });
            await new CaseStatusTransitionRepository(dbContext).EnterAsync(caseId, workflow.WorkflowGuid,
                workflow.Neutral, CaseTransitionEnterReason.Created, null, DateTime.UtcNow.AddHours(-3));
            await seeder.EventAsync(dbContext, caseId, 9, "alice");
            await dbContext.CaseNote.InsertAsync(() => new CaseNote
            {
                CaseId = caseId, Note = new string('x', 600), CreatedUser = "alice", CreatedDate = DateTime.UtcNow,
                CaseKey = "AccountId", CaseKeyValue = "ACC-1"
            });

            var service = await BuildAsync(dbContext, Me);
            var summary = await service.GetAsync(caseId);

            summary.Case.CaseId.Should().Be(caseId);
            summary.Case.Risk.Priority.Should().Be(40);
            summary.BaseRate.Should().Be(0.08);
            var contribution = summary.Contributions.Single();
            contribution.Family.Should().Be("Rule");
            contribution.Label.Should().Be("Structuring 9d");
            contribution.Closed.Should().Be(10);
            contribution.LogOdds.Should().Be(1.12);
            summary.Impact!.TypologySeverity.Should().Be(3);
            summary.Impact.MoneyBand.Should().Be(2);
            summary.Impact.Value.Should().Be(45000);
            summary.Impact.BreadthUplift.Should().Be(1);
            summary.RiskHistory.Should().ContainSingle().Which.Priority.Should().Be(40);
            var stay = summary.Stays.Single();
            stay.StatusName.Should().StartWith(DatabaseFixture.Prefix);
            stay.Minutes.Should().BeApproximately(180, 2);
            stay.ExitedDate.Should().BeNull();
            summary.RecentEvents.Should().Contain(e => e.EventTypeId == 9 && e.EventType == "Status Change");
            summary.Notes.Single().Note!.Length.Should().Be(400);
            summary.KeyHistory.Closed.Should().Be(2);
            summary.KeyHistory.Confirmed.Should().Be(0);
            summary.KeyHistory.OtherOpenCases.Should().Be(1);
            summary.Since.FirstLook.Should().BeTrue();
            summary.Since.Changes.Should().BeEmpty();

            var stored = await dbContext.Case.FirstAsync(w => w.Id == caseId);
            stored.Locked.Should().Be(0);
            stored.LockedUser.Should().BeNull();
            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId && w.CaseEventTypeId == 16
                                                       && w.CreatedUser == Me)).Should().Be(1);
            (await new CaseViewRepository(dbContext, Me).GetAsync(caseId)).Should().NotBeNull();
        }

        [Fact]
        public async Task ASecondPeekShowsOnlyWhatOthersChangedSinceTheFirstAndHowThePriorityMovedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            await seeder.AssessAsync(dbContext, caseId, 4, 20);
            var service = await BuildAsync(dbContext, Me);

            await service.GetAsync(caseId);

            await seeder.EventAsync(dbContext, caseId, 9, "alice");
            await seeder.EventAsync(dbContext, caseId, 11, Me);
            await seeder.EventAsync(dbContext, caseId, 4, "bob");
            await seeder.AssessAsync(dbContext, caseId, 4, 55);

            var second = await service.GetAsync(caseId);

            second.Since.FirstLook.Should().BeFalse();
            second.Since.LastViewedDate.Should().NotBeNull();
            second.Since.PriorityThen.Should().Be(20);
            second.Since.PriorityNow.Should().Be(55);
            second.Since.Changes.Select(c => c.EventTypeId).Should().Equal(9);
            second.Since.Changes[0].User.Should().Be("alice");
            second.RecentEvents.Where(e => e.New).Select(e => e.EventTypeId).Should().Equal(9);
            second.Case.Attention.Needed.Should().BeTrue();

            var third = await service.GetAsync(caseId);

            third.Since.Changes.Should().BeEmpty();
            third.Since.PriorityThen.Should().Be(55);
            third.Case.Attention.Needed.Should().BeFalse();
        }

        [Fact]
        public async Task TheWaterfallChartsTheEvidenceWithoutRecordingALookAndRefusesWhatTheCallerCannotSeeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);
            var unassessed = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            await new CaseRiskAssessmentRepository(dbContext).InsertAsync(new CaseRiskAssessment
            {
                CaseId = caseId, TriggerId = 1, MethodId = 1, BaseRate = 0.08, LikelihoodProbability = 0.3,
                LikelihoodBandId = 2, EvidenceCount = 2,
                ContributionsJson = "[{\"family\":\"Rule\",\"label\":\"Structuring 9d\",\"logOdds\":1.12}," +
                                    "{\"family\":\"KeyHistory\",\"label\":\"Repeat key\",\"logOdds\":-0.4}]",
                ImpactId = 4, LaneId = 4, LaneReason = "No deadline applies.", ExpectedHarm = 9, Priority = 40
            });

            var service = await BuildAsync(dbContext, Me);
            var svg = await service.WaterfallAsync(caseId);

            svg.Should().StartWith("<svg").And.Contain("Structuring 9d").And.Contain("Repeat key")
                .And.Contain("Base rate").And.Contain("Likelihood");
            (await service.WaterfallAsync(caseId, true)).Should().NotBe(svg);

            await Assert.ThrowsAsync<ArgumentException>(() => service.WaterfallAsync(unassessed));

            var ungranted = await BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            await Assert.ThrowsAsync<NotFoundException>(() => ungranted.WaterfallAsync(caseId));
            var tenantB = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await Assert.ThrowsAsync<NotFoundException>(() => tenantB.WaterfallAsync(caseId));

            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
            (await dbContext.CaseView.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task ACaseTheCallerCannotSeeIsNotFoundAndNothingIsRecordedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var ungranted = await BuildAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            await Assert.ThrowsAsync<NotFoundException>(() => ungranted.GetAsync(caseId));

            var tenantB = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            await Assert.ThrowsAsync<NotFoundException>(() => tenantB.GetAsync(caseId));

            var service = await BuildAsync(dbContext, Me);
            await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(int.MaxValue));

            (await dbContext.CaseEvent.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
            (await dbContext.CaseView.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public async Task ASignedOutUserIsRefusedAndOneWithoutThePermissionIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, Me, fx.Seed.UserWithoutPermission);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, null));
            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildAsync(dbContext, fx.Seed.UserNoTenant));

            var service = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            var ex = await Assert.ThrowsAsync<ForbiddenException>(() => service.GetAsync(caseId));
            ex.RequiredSpecifications.Should().Equal(1);
        }

        [Fact]
        public async Task OpeningACaseInFullAlsoCountsAsLookingAtItAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var caseId = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var service = await CaseByIdService.CreateAsync(dbContext, Me, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
            await service.GetAsync(caseId);

            (await new CaseViewRepository(dbContext, Me).GetAsync(caseId)).Should().NotBeNull();
        }
    }
}
