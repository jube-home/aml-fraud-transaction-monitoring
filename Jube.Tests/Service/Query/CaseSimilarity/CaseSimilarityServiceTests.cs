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
using Jube.CaseRisk.Similarity;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query.ReplaceCaseSimilarityComponentsQuery;
using Jube.Dto.Options;
using Jube.Service.Exceptions.Query.CaseSimilarity;
using Jube.Service.Query.CaseSimilarity;
using Jube.Service.Reactivity;
using Jube.Test.CaseRisk;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.CaseSimilarity
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseSimilarityServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private static readonly SimilarityComponent common = new(CaseSimilarityKind.Rule, "Common");
        private static readonly SimilarityComponent rare = new(CaseSimilarityKind.Rule, "Rare");
        private static readonly SimilarityComponent device = new(CaseSimilarityKind.Entity, "Device=0A1B2C");
        private static readonly SimilarityComponent amount = new(CaseSimilarityKind.Measure, "Amount:sum=+e3.5");

        private readonly CaseRiskSeeder seeder = new(fx);

        private string Me => fx.Seed.UserWithPermission;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.CaseSimilarityComponent.Where(w => seeder.WorkflowGuids.Contains(w.CaseWorkflowGuid))
                .DeleteAsync();
            await dbContext.CaseSimilarityComponentFrequency
                .Where(w => seeder.WorkflowGuids.Contains(w.CaseWorkflowGuid)).DeleteAsync();
            await seeder.CleanAsync(dbContext);
        }

        private static Task<CaseSimilarityService> BuildAsync(DbContext dbContext, string? user) =>
            CaseSimilarityService.CreateAsync(dbContext, user, TestLog.NoOp, localizers, new NullServiceChangeBus());

        private static Task VectorAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow, int caseId,
            params SimilarityComponent[] components)
        {
            return new ReplaceCaseSimilarityComponentsQuery(dbContext).ExecuteAsync(caseId, workflow.WorkflowGuid,
                [.. components.Select(c => (short)c.Kind)], [.. components.Select(c => c.Value)], DateTime.UtcNow);
        }

        private async Task<CaseRiskSeeder.Seeded> WorkflowAsync(DbContext dbContext, int background = 20)
        {
            var workflow = await seeder.SeedWorkflowAsync(dbContext);

            await seeder.GrantAsync(dbContext, workflow, Me);

            for (var i = 0; i < background; i++)
            {
                var id = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral, caseKeyValue: "BG" + i);

                await VectorAsync(dbContext, workflow, id, common,
                    new SimilarityComponent(CaseSimilarityKind.Entity, "Device=BG" + i));
            }

            return workflow;
        }

        private async Task<int> CaseAsync(DbContext dbContext, CaseRiskSeeder.Seeded workflow, Guid status,
            byte closedStatusId, string account, params SimilarityComponent[] components)
        {
            var id = await seeder.SeedCaseAsync(dbContext, workflow, status, closedStatusId, account);

            await VectorAsync(dbContext, workflow, id, components);

            return id;
        }

        [Fact]
        public async Task SimilarCasesAreRankedCountedByOutcomeAndExplainedByTheFactsTheyShareAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var target = await CaseAsync(dbContext, workflow, workflow.Neutral, 0, "TARGET", common, rare, device,
                amount);

            var twin = await CaseAsync(dbContext, workflow, workflow.Confirmed, 3, "TWIN", common, rare, device,
                amount);

            for (var i = 0; i < 4; i++)
            {
                await CaseAsync(dbContext, workflow, workflow.Confirmed, 3, "C" + i, common, rare, amount);
            }

            await CaseAsync(dbContext, workflow, workflow.NotConfirmed, 3, "N", common, rare, amount);
            var open = await CaseAsync(dbContext, workflow, workflow.Neutral, 0, "OPEN", common, rare, amount);

            var result = await (await BuildAsync(dbContext, Me)).GetAsync(target);

            result.Built.Should().BeTrue();
            result.CheckedDate.Should().NotBeNull();
            result.Similar.Should().Be(7);
            result.Closed.Should().Be(6);
            result.Confirmed.Should().Be(5);
            result.Open.Should().Be(1);
            result.EnoughToJudge.Should().BeTrue();
            result.Neighbours.Should().HaveCount(7);

            var first = result.Neighbours[0];

            first.CaseId.Should().Be(twin);
            first.CaseKeyValue.Should().Be("TWIN");
            first.BandId.Should().Be((byte)CaseSimilarityBand.Strong);
            first.Score.Should().Be(1);
            first.Linked.Should().BeTrue();
            first.OutcomeId.Should().Be(1);
            first.ClosedStatusId.Should().Be(3);
            first.ClosedDate.Should().NotBeNull();
            first.Reasons.Select(r => (r.KindId, r.Name)).Should().Equal(
                ((byte)CaseSimilarityKind.Entity, "Device"), ((byte)CaseSimilarityKind.Rule, "Rare"),
                ((byte)CaseSimilarityKind.Measure, "Amount"));

            var still = result.Neighbours.Single(n => n.CaseId == open);

            still.Linked.Should().BeFalse();
            still.ClosedDate.Should().BeNull();
            still.Reasons.Should().NotContain(r => r.Name == "Device");
            result.Neighbours.Should().BeInDescendingOrder(n => n.Score);
            result.Neighbours.SelectMany(n => n.Reasons).Should().NotContain(r => r.Name.Contains("0A1B2C"));
        }

        [Fact]
        public async Task AFactEveryCaseCarriesNeverMakesTwoCasesSimilarAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var target = await CaseAsync(dbContext, workflow, workflow.Neutral, 0, "TARGET", common);

            var result = await (await BuildAsync(dbContext, Me)).GetAsync(target);

            result.Built.Should().BeTrue();
            result.Similar.Should().Be(0);
            result.Neighbours.Should().BeEmpty();
        }

        [Fact]
        public async Task TooFewClosedSimilarCasesAreCountedButNotEnoughToJudgeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var target = await CaseAsync(dbContext, workflow, workflow.Neutral, 0, "TARGET", common, rare, amount);

            await CaseAsync(dbContext, workflow, workflow.Confirmed, 3, "C", common, rare, amount);
            await CaseAsync(dbContext, workflow, workflow.NotConfirmed, 3, "N", common, rare, amount);

            var result = await (await BuildAsync(dbContext, Me)).GetAsync(target);

            result.Closed.Should().Be(2);
            result.Confirmed.Should().Be(1);
            result.EnoughToJudge.Should().BeFalse();
        }

        [Fact]
        public async Task ACaseTheSweepHasNotReachedSaysSoAndListsNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, 0);
            var target = await seeder.SeedCaseAsync(dbContext, workflow, workflow.Neutral);

            var result = await (await BuildAsync(dbContext, Me)).GetAsync(target);

            result.Built.Should().BeFalse();
            result.CheckedDate.Should().BeNull();
            result.Neighbours.Should().BeEmpty();
        }

        [Fact]
        public async Task SimilarCasesInAStatusOrWorkflowTheCallerCannotSeeAreNeitherListedNorCountedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext);
            var target = await CaseAsync(dbContext, workflow, workflow.Neutral, 0, "TARGET", common, rare, amount);
            var seen = await CaseAsync(dbContext, workflow, workflow.Confirmed, 3, "SEEN", common, rare, amount);
            var hidden = await CaseAsync(dbContext, workflow, workflow.NotConfirmed, 3, "HIDDEN", common, rare,
                amount);

            await dbContext.GetTable<CaseWorkflowStatusRole>()
                .Where(r => r.CaseWorkflowStatusGuid == workflow.NotConfirmed).DeleteAsync();

            var other = await seeder.SeedWorkflowAsync(dbContext);
            await seeder.GrantAsync(dbContext, other, Me);
            var elsewhere = await CaseAsync(dbContext, other, other.Confirmed, 3, "ELSEWHERE", common, rare, amount);
            await CaseAsync(dbContext, other, other.Confirmed, 3, "ELSEWHERE2", common, rare, amount);

            var result = await (await BuildAsync(dbContext, Me)).GetAsync(target);

            result.Neighbours.Select(n => n.CaseId).Should().Equal(seen);
            result.Neighbours.Select(n => n.CaseId).Should().NotContain([hidden, elsewhere]);
            result.Similar.Should().Be(1);
            result.Closed.Should().Be(1);
        }

        [Fact]
        public async Task ReadingNeedsThePermissionAndTheCaseMustBeVisibleToTheCallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var workflow = await WorkflowAsync(dbContext, 0);
            var target = await CaseAsync(dbContext, workflow, workflow.Neutral, 0, "TARGET", rare);

            var without = await BuildAsync(dbContext, fx.Seed.UserWithoutPermission);
            var otherTenant = await BuildAsync(dbContext, fx.Seed.UserTenantB);
            var mine = await BuildAsync(dbContext, Me);
            var unauthenticated = BuildAsync(dbContext, null);

            await ((Func<Task>)(() => without.GetAsync(target))).Should().ThrowAsync<ForbiddenException>();
            await ((Func<Task>)(() => otherTenant.GetAsync(target))).Should().ThrowAsync<NotFoundException>();
            await ((Func<Task>)(() => mine.GetAsync(int.MaxValue))).Should().ThrowAsync<NotFoundException>();
            await ((Func<Task>)(() => unauthenticated)).Should().ThrowAsync<NotAuthenticatedException>();
        }
    }
}
