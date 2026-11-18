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
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Query.Graph.Jobs.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Service.Query.Graph
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphBuildJobTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly GraphJobsSeed seed = new(fx);

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.CleanupAsync(dbContext);
        }

        [Fact]
        public async Task BuildsNodesEdgesAndEventsFromArchiveKeysAndIsIdempotentAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext);
            var now = DateTime.UtcNow;
            await seed.TransactionAsync(dbContext, now.AddDays(-5), "A1", "B1", 100, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-4), "A1", "B1", 300, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-3), "A2", "B2", 50, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-2), "A3", null, null, "D1");
            var job = Job();
            var definitions = await DefinitionsAsync(dbContext);

            var first = await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            first.Events.Should().Be(7);

            var nodes = await dbContext.GraphNode.Where(n => n.Key == seed.AccountKey || n.Key == seed.DeviceKey || n.Key == seed.BeneficiaryKey).ToListAsync();
            nodes.Select(n => n.Value).Should().BeEquivalentTo(["A1", "A2", "A3", "B1", "B2", "D1"]);
            var nodeIds = nodes.Select(n => n.Id).ToList();
            var edges = await dbContext.GraphEdge.Where(e => nodeIds.Contains(e.FromNodeId)).ToListAsync();
            edges.Should().HaveCount(5);
            var pays = edges.Single(e => e.GraphRegistryEdgeGuid == seed.PaysDefinition && e.Count == 2);
            pays.AmountSum.Should().Be(400);
            pays.KindId.Should().Be(2);
            pays.Weight.Should().BeGreaterThan(0);
            pays.DecayedCount.Should().BeApproximately(1 + Math.Pow(2, -1d / 90), 1e-9);
            var events = await dbContext.GraphEdgeEvent.Where(e => e.GraphEdgeId == pays.Id).ToListAsync();
            events.Should().HaveCount(2);
            events.Select(e => e.Amount).Should().BeEquivalentTo([100d, 300d]);

            var second = await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            second.Events.Should().Be(0);
            (await dbContext.GraphEdge.CountAsync(e => nodeIds.Contains(e.FromNodeId))).Should().Be(5);
        }

        [Fact]
        public async Task AnEdgeDefinitionAddedLaterIsBuiltFromTheTransactionsAlreadyTakenUpByAnotherAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext);
            var now = DateTime.UtcNow;
            await seed.TransactionAsync(dbContext, now.AddDays(-5), "A1", "B1", 100, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-4), "A1", "B1", 300, "D1");
            var job = Job();
            var definitions = await DefinitionsAsync(dbContext);
            var deviceOnly = definitions.Where(d => d.Guid == seed.DeviceDefinition).ToList();

            var first = await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, deviceOnly);
            first.Events.Should().Be(2);
            (await dbContext.GraphEdge.CountAsync(e => e.GraphRegistryEdgeGuid == seed.PaysDefinition)).Should().Be(0);

            var second = await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            second.Events.Should().Be(2);

            var pays = await dbContext.GraphEdge.SingleAsync(e => e.GraphRegistryEdgeGuid == seed.PaysDefinition);
            pays.Count.Should().Be(2);
            pays.AmountSum.Should().Be(400);
            (await dbContext.GraphEdge.SingleAsync(e => e.GraphRegistryEdgeGuid == seed.DeviceDefinition)).Count.Should().Be(2);

            var third = await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            third.Events.Should().Be(0);
        }

        [Fact]
        public async Task ABuildThatFindsNothingNewStillRecordsWhenItRanAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext);
            await seed.TransactionAsync(dbContext, DateTime.UtcNow.AddDays(-5), "A1", "B1", 100, "D1");
            var job = Job();
            var definitions = await DefinitionsAsync(dbContext);
            var guids = definitions.Select(d => d.Guid).ToList();
            await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            var longAgo = DateTime.UtcNow.AddDays(-3);
            await dbContext.GraphBuildState.Where(s => guids.Contains(s.GraphRegistryEdgeGuid))
                .Set(s => s.LastRunDate, longAgo).UpdateAsync();

            var idle = await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);

            idle.Events.Should().Be(0);
            var states = await dbContext.GraphBuildState.Where(s => guids.Contains(s.GraphRegistryEdgeGuid)).ToListAsync();
            states.Should().HaveCount(definitions.Count);
            states.Should().OnlyContain(s => s.LastRunDate > DateTime.UtcNow.AddMinutes(-5));
        }

        [Fact]
        public async Task RetractsAndRebuildsAReprocessedTransactionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext);
            var entry = await seed.TransactionAsync(dbContext, DateTime.UtcNow.AddDays(-1), "A1", "B1", 100, "D1");
            var job = Job();
            var definitions = await DefinitionsAsync(dbContext);
            await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);

            await dbContext.ArchiveKey.Where(k => k.EntityAnalysisModelInstanceEntryGuid == entry && k.Key == seed.BeneficiaryKey)
                .Set(k => k.KeyValueString, "B9").Set(k => k.Version, 2).UpdateAsync();
            var retracted = await job.ReprocessTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            retracted.Should().Be(1);

            var b1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.BeneficiaryKey && n.Value == "B1");
            var b9 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.BeneficiaryKey && n.Value == "B9");
            (await dbContext.GraphEdge.FirstAsync(e => e.ToNodeId == b1.Id)).Count.Should().Be(0);
            (await dbContext.GraphEdge.FirstAsync(e => e.ToNodeId == b9.Id)).Count.Should().Be(1);
            (await dbContext.GraphEdgeEvent.CountAsync(e => e.EntityAnalysisModelInstanceEntryGuid == entry && e.Retracted == 0))
                .Should().Be(2);
        }

        [Fact]
        public async Task FlagsAHubAboveTheDefinitionThresholdAndReweightsEdgesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext, deviceHubThreshold: 3);
            var now = DateTime.UtcNow;

            for (var i = 1; i <= 5; i++)
            {
                await seed.TransactionAsync(dbContext, now.AddDays(-i), $"A{i}", null, null, "HUB");
            }

            await seed.TransactionAsync(dbContext, now.AddDays(-1), "A1", null, null, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-1), "A2", null, null, "D1");
            var definitions = await DefinitionsAsync(dbContext);
            await Job().BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);

            var hubs = new GraphHubJob(fx.ConnectionString, TestLog.NoOp, 0.999, 50);
            await hubs.RunTenantAsync(dbContext, seed.TenantRegistryId);

            var hub = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.DeviceKey && n.Value == "HUB");
            hub.Hub.Should().Be(1);
            hub.Degree.Should().Be(5);
            hub.HubByEdgeGuids.Should().Contain(seed.DeviceDefinition);
            var shared = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.DeviceKey && n.Value == "D1");
            shared.Hub.Should().Be(0);
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            a1.HubNeighbourCount.Should().Be(1);
            var toHub = await dbContext.GraphEdge.Where(e => e.ToNodeId == hub.Id).ToListAsync();
            toHub.Should().OnlyContain(e => e.ToHub == 1);
            toHub.Should().OnlyContain(e => Math.Abs(e.Specificity - 1.0 / Math.Log(2 + 5)) < 1e-5);
        }

        private GraphBuildJob Job() => new(fx.ConnectionString, TestLog.NoOp, 5000, 50, 365,
            1000, false);

        private async Task<System.Collections.Generic.List<GraphBuildDefinition>> DefinitionsAsync(
            Data.Context.DbContext dbContext)
        {
            return (await new Data.Repository.GraphRegistryEdgeRepository(dbContext, seed.TenantRegistryId)
                    .GetActiveByGraphRegistryGuidAsync(seed.Registry))
                .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90, d.KindPrior ?? 1,
                    d.Restricted ?? 0))
                .ToList();
        }
    }
}
