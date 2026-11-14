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
using Jube.Data.Query.GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery;
using Jube.Data.Query.GetGraphRecallTenantsQuery;
using Jube.Data.Query.GetRuleParseEnvironmentQuery;
using Jube.Data.Repository;
using Jube.Data.Repository.Models;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation;
using Jube.Engine.EntityAnalysisModelInvoke.Simulation.Models;
using Jube.Engine.GraphRecall;
using Jube.GraphRecallProtocol;
using Jube.Parser;
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Query.Graph.Jobs.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.Graph;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Engine.GraphRecall
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphRecallLoaderTests(DatabaseFixture fx, ITestOutputHelper output) : IAsyncLifetime
    {
        private readonly GraphJobsSeed seed = new(fx);
        private readonly List<int> recalls = [];
        private readonly List<int> xpaths = [];
        private string tag = string.Empty;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            if (tag.Length > 0)
            {
                await dbContext.GraphNodeTag.Where(t => t.Tag == tag).DeleteAsync();
            }

            var nodeIds = dbContext.GraphNode.Where(n => n.Key == seed.AccountKey || n.Key == seed.DeviceKey || n.Key == seed.BeneficiaryKey).Select(n => n.Id);
            var communityIds = dbContext.GraphNode.Where(n => nodeIds.Contains(n.Id) && n.CommunityId != null).Select(n => n.CommunityId!.Value);
            await dbContext.GraphCommunity.Where(c => communityIds.Contains(c.Id)).DeleteAsync();
            await dbContext.EntityApproval.Where(a => a.EntityId != null && recalls.Contains(a.EntityId.Value)
                                                      && a.EntityApprovalKindId == (int)EntityApprovalKind.EntityAnalysisModelGraphRecall).DeleteAsync();
            await dbContext.EntityAnalysisModelGraphRecall.Where(r => recalls.Contains(r.Id)).DeleteAsync();
            await dbContext.EntityAnalysisModelRequestXpath.Where(x => xpaths.Contains(x.Id)).DeleteAsync();
            await seed.CleanupAsync(dbContext);
        }

        [Fact]
        public async Task TheLoaderBuildsTheSeededTenantSnapshotAndAConfiguredEntityRuleCompilesAndEvaluatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            var a3 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A3");
            var b1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.BeneficiaryKey && n.Value == "B1");
            tag = $"{DatabaseFixture.Prefix}Mule{Guid.NewGuid():N}"[..24];

            await new GraphNodeTagRepository(dbContext).ReplaceTagAsync(seed.TenantRegistryId, tag,
            [
                new GraphNodeTag { GraphNodeId = a3.Id, Distance = 0, Within1 = 1, Within2 = 1, Within3 = 1 },
                new GraphNodeTag { GraphNodeId = a1.Id, Distance = 2, Within1 = 0, Within2 = 1, Within3 = 1 }
            ]);

            var snapshot = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId,
                new GraphRecallLoaderOptions(false, 2000000, 4000000, 4), TestLog.NoOp);

            snapshot.TenantRegistryId.Should().Be(seed.TenantRegistryId);
            snapshot.NodeKeys.Should().Contain(seed.AccountKey);
            snapshot.Tags.Should().Contain(tag);
            snapshot.Definitions.Should().Contain(d => d.RegistryGuid == seed.Registry && d.FromNodeKey == seed.AccountKey && d.ToNodeKey == seed.BeneficiaryKey);
            snapshot.Connects(seed.AccountKey, seed.BeneficiaryKey, seed.Registry).Should().BeTrue();
            snapshot.TryGetNode(seed.AccountKey, "A3", out var confirmed).Should().BeTrue();
            confirmed.DistanceToConfirmed.Should().Be(0);
            snapshot.TryGetNode(seed.AccountKey, "A1", out var near).Should().BeTrue();
            near.Id.Should().Be(a1.Id);
            near.DistanceToConfirmed.Should().Be(2);
            near.ConfirmedWithin2.Should().Be(1);
            near.PropagationScore.Should().BeGreaterThan(0);
            near.CommunityId.Should().NotBe(GraphRecallNode.Unknown);
            near.TryGetTag(tag, out var nearTag).Should().BeTrue();
            nearTag.Distance.Should().Be(2);
            snapshot.TryGetNode(seed.AccountKey, "Nobody", out _).Should().BeFalse();
            snapshot.EdgeCount.Should().BeGreaterThan(0);
            snapshot.TryGetEdge(a1.Id, b1.Id, seed.Registry, out var edge).Should().BeTrue();
            edge.Count.Should().Be(2);
            edge.AmountSum.Should().Be(400);
            edge.KindId.Should().Be(2);
            snapshot.TryGetEdge(a1.Id, b1.Id, null, out _).Should().BeTrue();
            snapshot.TryGetEdge(a1.Id, b1.Id, Guid.NewGuid(), out _).Should().BeFalse();

            var repository = new EntityAnalysisModelGraphRecallRepository(dbContext, seed.TenantRegistryId);
            var rows = (await repository.GetApprovedByEntityAnalysisModelIdAsync(seed.ModelId)).ToList();
            rows.Select(r => r.Name).Should().Equal("Account", "Beneficiary", "Device");
            var plan = GraphRecallPlan.Build(rows, 4);
            plan.Names.Should().Equal("Account", "Beneficiary", "Device");
            plan.Entities[0].CounterpartIndex.Should().Be(1);
            plan.Entities[2].ResponsePayload.Should().BeTrue();
            plan.Entities[2].ReportTable.Should().BeFalse();

            var graph = new PooledDictionary<string, GraphEntity>();
            GraphRecallEvaluator.Evaluate(plan, snapshot, ["A1", "B1", "D1"], DateTime.UtcNow, graph);
            graph["Account"].Known.Should().BeTrue();
            graph["Account"].Value.Should().Be("A1");
            graph["Account"].DistanceToConfirmed.Should().Be(2);
            graph["Account"].Tag(tag).Distance.Should().Be(2);
            graph["Account"].NearestTag.Should().Be(tag);
            graph["Account"].Edge.Exists.Should().BeTrue();
            graph["Account"].Edge.Count.Should().Be(2);
            graph["Account"].Edge.Kind.Should().Be("MoneyFlow");
            graph["Beneficiary"].DistanceToConfirmed.Should().Be(1);
            graph["Beneficiary"].RelaxedDistanceToConfirmed.Should().Be(1);
            graph["Beneficiary"].BridgesToTagged.Should().BeTrue();
            graph["Device"].Known.Should().BeTrue();
            graph["Device"].ReportTable.Should().BeFalse();

            var environment = await new GetRuleParseEnvironmentQuery(dbContext, seed.TenantRegistryId)
                .ExecuteAsync(seed.ModelId, RuleParse.ActivationRule);
            environment.Graph.Should().Equal("Account", "Beneficiary", "Device");

            var fields = (await new GetEntityAnalysisModelFieldByEntityAnalysisModelIdParseTypeIdQuery(dbContext,
                    seed.TenantRegistryId).ExecuteAsync(seed.ModelId, RuleParse.ActivationRule, false))
                .Where(f => f.Group == "Graph").ToList();

            fields.Should().HaveCount(3 * GraphEntityMembers.Scalars.Count);
            var distance = fields.Single(f => f.Name == "Graph.Account.DistanceToConfirmed");
            distance.ValueJsonPath.Should().Be("graph.Account.distanceToConfirmed");
            distance.ValueSqlPath.Should().Be("(\"Json\"-> 'graph' -> 'Account' ->> 'distanceToConfirmed')::int");
            distance.DataTypeId.Should().Be(2);
            distance.JQueryBuilderDataType.Should().Be("integer");
            distance.ProcessingTypeId.Should().Be(8);

            var parsed = RuleParse.Execute(
                $"If Graph.Account.Tag(\"{tag}\").Distance <= 2 And Graph.Beneficiary.DistanceToConfirmed <= 1 Then\n   Return True\nEnd If",
                RuleParse.ActivationRule, environment, TestLog.NoOp,
                RuleParse.EngineReferences(RuleParse.ActivationRule), engineWrap: true);
            parsed.Compiled.Should().BeTrue(parsed.Message);

            (await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, Inputs(graph))).Value.Should().Be(true);

            var far = new PooledDictionary<string, GraphEntity>();
            GraphRecallEvaluator.Evaluate(plan, snapshot, ["S", "F0", null], DateTime.UtcNow, far);
            far["Account"].DistanceToConfirmed.Should().Be(5);
            far["Account"].Tag(tag).Known.Should().BeFalse();
            (await RuleRunner.RunAsync(parsed, RuleParse.ActivationRule, false, Inputs(far))).Value.Should().Be(false);
        }

        [Fact]
        public async Task TheLoaderReportsTruncationWhenTheNodeCapIsReachedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);

            var full = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId,
                new GraphRecallLoaderOptions(false, 2000000, 4000000, 4), TestLog.NoOp);

            full.Truncated.Should().BeFalse();
            full.EdgesTruncated.Should().BeFalse();
            full.NodeCount.Should().BeGreaterThan(2);

            var exact = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId,
                new GraphRecallLoaderOptions(false, full.NodeCount, 4000000, 4), TestLog.NoOp);

            exact.Truncated.Should().BeFalse();

            var capped = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId,
                new GraphRecallLoaderOptions(false, 2, 4000000, 4), TestLog.NoOp);

            capped.NodeCount.Should().Be(2);
            capped.Truncated.Should().BeTrue();
        }

        [Fact]
        public async Task ASnapshotHoldsOnlyItsOwnTenantsNodesAndATenantWithNothingLoadsEmptyAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var options = new GraphRecallLoaderOptions(false, 2000000, 4000000, 4);

            var snapshot = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId, options,
                TestLog.NoOp);

            snapshot.NodeCount.Should().BeGreaterThan(0);
            snapshot.TenantRegistryId.Should().Be(seed.TenantRegistryId);

            foreach (var nodeKey in snapshot.NodeKeys)
            {
                var values = await dbContext.GraphNode.Where(n => n.TenantRegistryId == seed.TenantRegistryId && n.Key == nodeKey)
                    .Select(n => n.Value).ToListAsync();

                foreach (var value in values)
                {
                    snapshot.TryGetNode(nodeKey, value, out var node).Should().BeTrue("node {0}={1} is in the database for the tenant but not in the snapshot", nodeKey, value);
                    (await dbContext.GraphNode.FirstAsync(n => n.Id == node.Id)).TenantRegistryId.Should()
                        .Be(seed.TenantRegistryId);
                }
            }

            var nobody = await GraphRecallSnapshotLoader.LoadAsync(dbContext, int.MaxValue, options, TestLog.NoOp);

            nobody.NodeCount.Should().Be(0);
            nobody.EdgeCount.Should().Be(0);
            nobody.Definitions.Should().BeEmpty();
            nobody.TryGetNode(seed.AccountKey, "A1", out _).Should().BeFalse();
        }

        [Fact]
        public async Task TheEdgeCapStopsEdgesWithoutTruncatingNodesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);

            var capped = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId,
                new GraphRecallLoaderOptions(false, 2000000, 1, 4), TestLog.NoOp);

            capped.EdgeCount.Should().Be(1);
            capped.EdgesTruncated.Should().BeTrue();
            capped.Truncated.Should().BeFalse();
        }

        [Fact]
        public async Task EdgesOfADeactivatedRegistryAreNotLoadedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var options = new GraphRecallLoaderOptions(false, 2000000, 4000000, 4);

            (await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId, options, TestLog.NoOp))
                .EdgeCount.Should().BeGreaterThan(0);

            await dbContext.GraphRegistry.Where(r => r.Guid == seed.Registry).Set(r => r.Active, (byte?)0)
                .UpdateAsync();

            var after = await GraphRecallSnapshotLoader.LoadAsync(dbContext, seed.TenantRegistryId, options,
                TestLog.NoOp);

            after.EdgeCount.Should().Be(0);
            after.Definitions.Should().BeEmpty();
        }

        [Fact]
        public async Task TheLoaderReportsLoadTimeAndBytesPerNodeForEveryActiveTenantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var tenants = await new GetGraphRecallTenantsQuery(dbContext).ExecuteAsync();

            foreach (var tenant in tenants)
            {
                var before = GC.GetTotalMemory(true);

                var snapshot = await GraphRecallSnapshotLoader.LoadAsync(dbContext, tenant.TenantRegistryId,
                    new GraphRecallLoaderOptions(false, 2000000, 4000000, 4), TestLog.NoOp);

                var resident = GC.GetTotalMemory(true) - before;

                output.WriteLine(
                    $"tenant {tenant.TenantRegistryId}: {snapshot.NodeCount} nodes, {snapshot.EdgeCount} edges, {snapshot.Definitions.Count} definitions, {snapshot.Tags.Count} tags, {snapshot.LoadTime.TotalMilliseconds:F1} ms, {snapshot.Bytes} bytes allocated ({GraphRecallSnapshotLoader.BytesPerNode(snapshot):F0} per node), {resident} bytes resident ({(snapshot.NodeCount == 0 ? 0 : resident / snapshot.NodeCount)} per node)");

                snapshot.LoadTime.Should().BeGreaterThan(TimeSpan.Zero);
                snapshot.Truncated.Should().BeFalse();
            }
        }

        private static RuleRunInputs Inputs(PooledDictionary<string, GraphEntity> graph)
        {
            var inputs = new RuleRunInputs();

            foreach (var (key, value) in graph)
            {
                inputs.Graph[key] = value;
            }

            return inputs;
        }

        private async Task SeedAndRunAsync(DbContext dbContext)
        {
            await seed.SetupAsync(dbContext);

            foreach (var name in new[] { seed.AccountKey, seed.BeneficiaryKey, seed.DeviceKey })
            {
                xpaths.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelRequestXpath
                {
                    EntityAnalysisModelId = seed.ModelId, Name = name, DataTypeId = 1, XPath = $"$.{name}", Active = 1,
                    ReportTable = 1, Deleted = 0, Version = 1, Guid = Guid.NewGuid(), CreatedUser = seed.Me,
                    CreatedDate = DateTime.UtcNow
                }));
            }

            var repository = new EntityAnalysisModelGraphRecallRepository(dbContext, seed.TenantRegistryId);

            foreach (var (name, field, counterpart, responsePayload, reportTable) in new[]
                     {
                         ("Account", seed.AccountKey, "Beneficiary", (byte)0, (byte)1),
                         ("Beneficiary", seed.BeneficiaryKey, "Account", (byte)0, (byte)1),
                         ("Device", seed.DeviceKey, null, 1, 0)
                     })
            {
                var recall = await repository.InsertAsync(new EntityAnalysisModelGraphRecall
                {
                    EntityAnalysisModelId = seed.ModelId, Name = name, RequestXPathName = field,
                    CounterpartName = counterpart, GraphRegistryGuid = seed.Registry, ResponsePayload = responsePayload,
                    ReportTable = reportTable, Active = 1, Locked = 0, Deleted = 0, CreatedUser = seed.Me
                });
                recalls.Add(recall.Id);
                await EntityApprovalSeed.ApproveAsync(dbContext, EntityApprovalKind.EntityAnalysisModelGraphRecall, recall.Id, 1, seed.TenantRegistryId);
            }

            var now = DateTime.UtcNow;
            await seed.TransactionAsync(dbContext, now.AddDays(-5), "A1", "B1", 100, "D1");
            var anchor = await seed.TransactionAsync(dbContext, now.AddDays(-4), "A1", "B1", 300, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-3), "A2", "B1", 80, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-3), "A3", "B1", 120, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-2), "A3", "B2", 50, "D2");
            await seed.TransactionAsync(dbContext, now.AddDays(-2), "A1", null, null, "D2");

            for (var i = 0; i < 6; i++)
            {
                await seed.TransactionAsync(dbContext, now.AddDays(-6).AddHours(i), "S", $"F{i}", 100, null);
            }

            await seed.CaseAsync(dbContext, anchor, "A1", false);
            await seed.CaseAsync(dbContext, anchor, "A3", true);
            var definitions = (await new GraphRegistryEdgeRepository(dbContext, seed.TenantRegistryId)
                    .GetActiveByGraphRegistryGuidAsync(seed.Registry))
                .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90, d.KindPrior ?? 1,
                    d.Restricted ?? 0)).ToList();
            var connectionString = fx.ConnectionString;
            await new GraphBuildJob(connectionString, TestLog.NoOp, 5000, 50, 365, 1000, false)
                .BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            await new GraphHubJob(connectionString, TestLog.NoOp, 0.999, 50).RunTenantAsync(dbContext, seed.TenantRegistryId);
            await new GraphCommunityJob(connectionString, TestLog.NoOp, 1.0, 1_000_000).RunTenantAsync(dbContext, seed.TenantRegistryId);
            await new GraphDistanceJob(connectionString, TestLog.NoOp, 4, 1_000_000).RunTenantAsync(dbContext, seed.TenantRegistryId);
        }
    }
}
