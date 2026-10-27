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
using Jube.Service.Query.CaseKeyJournal;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CaseKeyJournalPeerJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const string CaseKey = "AccountId";

        private static readonly string connectionString =
            Environment.GetEnvironmentVariable("JubeTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionString") ?? string.Empty;

        private readonly JournalJobsSeed seed = new(fx);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await seed.CleanupAsync(dbContext);
        }

        private static CaseKeyJournalPeerJob Job(int maximumRows = 100000, TimeSpan? refreshAfter = null,
            int windowDays = 90)
        {
            return new CaseKeyJournalPeerJob(connectionString, TestLog.NoOp, windowDays, maximumRows,
                refreshAfter ?? TimeSpan.Zero);
        }

        private async Task<(int Model, int WorkflowId, Guid Workflow)> SeedAsync(DbContext dbContext, int keys,
            int rowsPerKey = 3, Func<int, string?>? tier = null, DateTime? created = null)
        {
            var (model, workflowId, workflow) = await seed.WorkflowAsync(dbContext);
            await seed.XPathAsync(dbContext, workflowId, "Amount");
            await seed.XPathAsync(dbContext, workflowId, "Tier", segment: tier is not null);
            await seed.CaseAsync(dbContext, workflow, CaseKey);

            for (var key = 0; key < keys; key++)
            {
                for (var row = 0; row < rowsPerKey; row++)
                {
                    var payload = new JObject
                    {
                        [CaseKey] = $"K{key}", ["Amount"] = 100 + key + row, ["Tier"] = tier?.Invoke(key)
                    };

                    await seed.ArchiveAsync(dbContext, model, payload, created);
                }
            }

            return (model, workflowId, workflow);
        }

        [Fact]
        public async Task RebuildsThePeerDistributionOfEachMeasureFromTheArchiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 25);

            var built = await Job().RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            built.Should().Be(1);

            var peer = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            peer.CaseKey.Should().Be(CaseKey);
            peer.Field.Should().Be("Amount");
            peer.Segment.Should().BeNull();
            peer.KeyCount.Should().Be(25);
            peer.RowCount.Should().Be(75);
            peer.Median.Should().Be(113);
            peer.WindowDays.Should().Be(90);
            peer.ComputedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            JArray.Parse(peer.Bins).Sum(bin => bin.Value<int>("Count")).Should().Be(25);
        }

        [Fact]
        public async Task SegmentsGetTheirOwnDistributionAndSmallOnesGetNoneAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 30, tier: key => key < 24 ? "Gold" : "Silver");

            await Job().RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            var peers = await dbContext.CaseKeyJournalPeer.Where(p => p.CaseWorkflowGuid == workflow).ToListAsync();

            peers.Select(p => p.Segment).Should().BeEquivalentTo([null, "Gold"]);
            peers.Single(p => p.Segment == "Gold").KeyCount.Should().Be(24);
        }

        [Fact]
        public async Task FewerThanTwentyKeysBuildNothingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 19);

            (await Job().RebuildAsync(dbContext, workflowId, workflow, model, CaseKey)).Should().Be(0);
            (await dbContext.CaseKeyJournalPeer.CountAsync(p => p.CaseWorkflowGuid == workflow)).Should().Be(0);
        }

        [Fact]
        public async Task OnlyTheNewestRowsUpToTheCapAreReadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 30);

            await Job(maximumRows: 60).RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            var peer = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            peer.RowCount.Should().Be(60);
            peer.KeyCount.Should().Be(20);
        }

        [Fact]
        public async Task PagesThroughTheArchiveWhenTheSampleIsLargerThanOnePageAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 25, rowsPerKey: 2);

            var job = new CaseKeyJournalPeerJob(connectionString, TestLog.NoOp, 90, 100000, TimeSpan.Zero);

            (await job.RebuildAsync(dbContext, workflowId, workflow, model, CaseKey)).Should().Be(1);
            (await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow)).RowCount
                .Should().Be(50);
        }

        [Fact]
        public async Task TransactionsOutsideTheWindowAreLeftOutAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 25, created: DateTime.UtcNow.AddDays(-30));

            (await Job(windowDays: 7).RebuildAsync(dbContext, workflowId, workflow, model, CaseKey)).Should().Be(0);
            (await Job(windowDays: 60).RebuildAsync(dbContext, workflowId, workflow, model, CaseKey)).Should().Be(1);
        }

        [Fact]
        public async Task ARecentRebuildIsLeftAloneAndAnOldOneIsReplacedInPlaceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 25);

            await Job().RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            var first = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            (await Job(refreshAfter: TimeSpan.FromHours(1)).RebuildAsync(dbContext, workflowId, workflow, model,
                CaseKey)).Should().BeNull();

            await dbContext.CaseKeyJournalPeer.Where(p => p.CaseWorkflowGuid == workflow)
                .Set(p => p.ComputedDate, DateTime.UtcNow.AddHours(-3)).UpdateAsync();

            (await Job(refreshAfter: TimeSpan.FromHours(1)).RebuildAsync(dbContext, workflowId, workflow, model,
                CaseKey)).Should().Be(1);

            var second = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            second.Id.Should().Be(first.Id);
            second.ComputedDate.Should().BeAfter(DateTime.UtcNow.AddMinutes(-5));
        }

        [Fact]
        public async Task ADistributionThatNoLongerQualifiesIsSoftDeletedAndRevivedWhenItDoesAgainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (model, workflowId, workflow) = await SeedAsync(dbContext, 30);
            var repository = new Jube.Data.Repository.CaseKeyJournalPeerRepository(dbContext);

            await Job().RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            var original = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            original.Deleted.Should().Be(0);
            original.DeletedDate.Should().BeNull();

            await Job(maximumRows: 30).RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            var deleted = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            deleted.Id.Should().Be(original.Id);
            deleted.Deleted.Should().Be(1);
            deleted.DeletedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(5));
            (await repository.GetAsync(workflow, CaseKey, null!)).Should().BeEmpty();
            (await repository.NewestAsync(workflow, CaseKey)).Should().BeNull();

            await Job().RebuildAsync(dbContext, workflowId, workflow, model, CaseKey);

            var revived = await dbContext.CaseKeyJournalPeer.SingleAsync(p => p.CaseWorkflowGuid == workflow);

            revived.Id.Should().Be(original.Id);
            revived.Deleted.Should().Be(0);
            revived.DeletedDate.Should().BeNull();
            (await repository.GetAsync(workflow, CaseKey, null!)).Should().ContainSingle();
        }

        [Fact]
        public async Task TheRunWalksEveryWorkflowWithACaseKeyAndCountsWhatItDidAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, workflow) = await SeedAsync(dbContext, 25);

            var result = await Job(refreshAfter: TimeSpan.FromHours(1)).RunOnceAsync();

            result.Rebuilt.Should().BeGreaterThanOrEqualTo(1);
            result.Distributions.Should().BeGreaterThanOrEqualTo(1);
            (await dbContext.CaseKeyJournalPeer.CountAsync(p => p.CaseWorkflowGuid == workflow)).Should().Be(1);

            var again = await Job(refreshAfter: TimeSpan.FromHours(1)).RunOnceAsync();

            again.Fresh.Should().BeGreaterThanOrEqualTo(1);
        }
    }
}
