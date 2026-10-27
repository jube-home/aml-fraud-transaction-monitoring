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
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Dto.Options;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseRiskAssessmentRepositoryTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<int> caseIds = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.Case.Where(w => caseIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedCaseAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var id = await dbContext.InsertWithInt32IdentityAsync(new Jube.Data.Poco.Case
            {
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                CaseWorkflowGuid = Guid.NewGuid(),
                CaseWorkflowStatusGuid = Guid.NewGuid(),
                CreatedDate = DateTime.UtcNow,
                Locked = 0,
                ClosedStatusId = 0,
                CaseKey = "AccountId",
                CaseKeyValue = $"{DatabaseFixture.Prefix}Risk{Guid.NewGuid():N}",
                Diary = 0,
                Rating = 0,
                LastClosedStatus = 0,
                Json = "{}"
            });
            caseIds.Add(id);

            return id;
        }

        private static CaseRiskAssessment Assessment(int caseId, int priority = 50) => new()
        {
            CaseId = caseId,
            TriggerId = (byte)CaseRiskTrigger.Initial,
            MethodId = (byte)CaseRiskMethod.ColdStart,
            BaseRate = 0.08,
            LikelihoodProbability = 0.56,
            LikelihoodBandId = (byte)CaseLikelihoodBand.Likely,
            EvidenceCount = 0,
            ContributionsJson = "[{\"family\":\"Model\",\"label\":\"StructNet\",\"logOdds\":1.15}]",
            ImpactId = (byte)CaseImpactBand.High,
            ImpactComponentsJson = "{\"typologySeverity\":3,\"breadthUplift\":1}",
            LaneId = (byte)CaseRiskLane.Standard,
            LaneReason = "No regulatory diary within two business days.",
            ExpectedHarm = 16.8,
            Priority = priority
        };

        [Fact]
        public async Task SequencesCountUpFromOnePerCaseAndAreIndependentBetweenCasesAsync()
        {
            var first = await SeedCaseAsync();
            var second = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseRiskAssessmentRepository(dbContext);

            (await repository.InsertAsync(Assessment(first))).Sequence.Should().Be(1);
            (await repository.InsertAsync(Assessment(first))).Sequence.Should().Be(2);
            (await repository.InsertAsync(Assessment(second))).Sequence.Should().Be(1);
            (await repository.InsertAsync(Assessment(first))).Sequence.Should().Be(3);
        }

        [Fact]
        public async Task InsertStampsTheDateAndEveryFieldRoundTripsIncludingTheJsonAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseRiskAssessmentRepository(dbContext);
            var before = DateTime.UtcNow.AddSeconds(-5);

            var saved = await repository.InsertAsync(Assessment(caseId, 62));
            var read = await repository.GetLatestAsync(caseId);

            read.Id.Should().Be(saved.Id).And.BeGreaterThan(0);
            read.CreatedDate.Should().BeAfter(before);
            read.TriggerId.Should().Be((byte)CaseRiskTrigger.Initial);
            read.MethodId.Should().Be((byte)CaseRiskMethod.ColdStart);
            read.MethodVersion.Should().BeNull();
            read.BaseRate.Should().Be(0.08);
            read.LikelihoodProbability.Should().Be(0.56);
            read.LikelihoodBandId.Should().Be((byte)CaseLikelihoodBand.Likely);
            read.ImpactId.Should().Be((byte)CaseImpactBand.High);
            read.LaneId.Should().Be((byte)CaseRiskLane.Standard);
            read.LaneReason.Should().Contain("two business days");
            read.NextDeadlineDate.Should().BeNull();
            read.DeadlineBreached.Should().Be(0);
            read.ExpectedHarm.Should().Be(16.8);
            read.Priority.Should().Be(62);
            read.ContributionsJson.Should().Contain("StructNet");
            read.ImpactComponentsJson.Should().Contain("typologySeverity");
        }

        [Fact]
        public async Task LatestIsTheHighestSequenceAndACaseWithNoAssessmentHasNoneAsync()
        {
            var assessed = await SeedCaseAsync();
            var bare = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseRiskAssessmentRepository(dbContext);

            await repository.InsertAsync(Assessment(assessed, 10));
            await repository.InsertAsync(Assessment(assessed, 90));

            (await repository.GetLatestAsync(assessed)).Priority.Should().Be(90);
            (await repository.GetLatestAsync(bare)).Should().BeNull();
        }

        [Fact]
        public async Task LatestForManyCasesReturnsOneCurrentRowPerAssessedCaseAsync()
        {
            var one = await SeedCaseAsync();
            var two = await SeedCaseAsync();
            var bare = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseRiskAssessmentRepository(dbContext);

            await repository.InsertAsync(Assessment(one, 10));
            await repository.InsertAsync(Assessment(one, 20));
            await repository.InsertAsync(Assessment(two, 30));

            var latest = await repository.GetLatestForCasesAsync([one, two, bare]);

            latest.Keys.Should().BeEquivalentTo([one, two]);
            latest[one].Priority.Should().Be(20);
            latest[one].Sequence.Should().Be(2);
            latest[two].Priority.Should().Be(30);
        }

        [Fact]
        public async Task HistoryIsNewestFirstAndHonoursTheLimitAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var repository = new CaseRiskAssessmentRepository(dbContext);

            foreach (var priority in new[] { 1, 2, 3, 4 })
            {
                await repository.InsertAsync(Assessment(caseId, priority));
            }

            var history = await repository.ListHistoryAsync(caseId, 3);

            history.Select(h => h.Priority).Should().Equal(4, 3, 2);
            history.Select(h => h.Sequence).Should().Equal(4, 3, 2);
        }

        [Fact]
        public async Task ConcurrentWritersForOneCaseEachGetADistinctSequenceAsync()
        {
            var caseId = await SeedCaseAsync();

            var sequences = await Task.WhenAll(Enumerable.Range(0, 8).Select(async _ =>
            {
                await using var dbContext = fx.GetDbContext();

                return (await new CaseRiskAssessmentRepository(dbContext).InsertAsync(Assessment(caseId))).Sequence;
            }));

            sequences.OrderBy(s => s).Should().Equal(Enumerable.Range(1, 8));
        }

        [Fact]
        public async Task AssessmentsAreRemovedWithTheirCaseAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            await new CaseRiskAssessmentRepository(dbContext).InsertAsync(Assessment(caseId));

            await dbContext.Case.Where(w => w.Id == caseId).DeleteAsync();

            (await dbContext.CaseRiskAssessment.CountAsync(w => w.CaseId == caseId)).Should().Be(0);
        }

        [Fact]
        public void TheRepositoryOffersNoWayToChangeOrRemoveAnAssessment()
        {
            var verbs = typeof(CaseRiskAssessmentRepository).GetMethods(BindingFlags.Public | BindingFlags.Instance
                                                                         | BindingFlags.DeclaredOnly)
                .Select(m => m.Name).ToList();

            verbs.Should().NotContain(n => n.StartsWith("Update") || n.StartsWith("Delete")
                                                                  || n.StartsWith("Remove")
                                                                  || n.StartsWith("Upsert"));
        }

        [Fact]
        public async Task TheDatabaseRefusesTwoAssessmentsWithTheSameSequenceForACaseAsync()
        {
            var caseId = await SeedCaseAsync();
            await using var dbContext = fx.GetDbContext();
            var saved = await new CaseRiskAssessmentRepository(dbContext).InsertAsync(Assessment(caseId));

            var duplicate = Assessment(caseId);
            duplicate.Sequence = saved.Sequence;
            duplicate.CreatedDate = DateTime.UtcNow;

            Exception? thrown = null;

            try
            {
                await dbContext.InsertWithInt64IdentityAsync(duplicate);
            }
            catch (Exception ex)
            {
                thrown = ex;
            }

            thrown.Should().NotBeNull();
            thrown!.ToString().Should().Contain("23505");
        }
    }
}
