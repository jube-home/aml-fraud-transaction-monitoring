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
using Jube.Dto.Query.Graph;
using Jube.Service.Exceptions.Query.Graph;
using Jube.Service.Query.Graph;
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Query.Graph.Jobs.Models;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.Graph
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);
        private readonly GraphJobsSeed seed = new(fx);
        private Guid anchor;
        private long anchorArchiveId;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.CleanupAsync(dbContext);
        }

        [Fact]
        public async Task ReturnsTheRingAroundTheSeedWithEvidenceAndExplodeAgreesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);

            var one = await service.GetNeighbourhoodAsync(Request(1));
            one.Seed.Should().NotBeNull();
            one.Seed!.Value.Should().Be("A1");
            one.Nodes.Select(n => n.Value).Should().BeEquivalentTo(["A1", "B1", "D1"]);
            var pays = one.Edges.Single(e => e.KindId == 2);
            pays.ArchiveIds.Should().Contain(anchorArchiveId);
            pays.WindowCount.Should().Be(2);
            pays.DefinitionName.Should().Be("Pays");

            var two = await service.GetNeighbourhoodAsync(Request(2));
            two.Nodes.Select(n => n.Value).Should().BeEquivalentTo(["A1", "B1", "D1", "A2", "A3"]);
            two.Nodes.Single(n => n.Value == "A3").Depth.Should().Be(2);
            two.Nodes.Single(n => n.Value == "A3").Cases.Confirmed.Should().Be(1);
            two.Nodes.Single(n => n.Value == "A1").Cases.Open.Should().Be(1);
            two.Hops.Should().Be(2);

            var explode = await service.GetExplodeAsync(new GraphExplodeRequestDto
            {
                RegistryGuid = seed.Registry, NodeId = one.Seed.Id
            });
            explode.Lanes.Select(l => l.Node.Value).Should().BeEquivalentTo(["B1", "D1"]);
            explode.Lanes.Single(l => l.Node.Value == "B1").Count.Should().Be(2);
            explode.Lanes.Single(l => l.Node.Value == "B1").Total.Should().Be(400);

            var transactions = await service.GetEdgeTransactionsAsync(new GraphEdgeTransactionsRequestDto { EdgeId = pays.Id });
            transactions.ArchiveIds.Should().BeEquivalentTo(pays.ArchiveIds,
                $"edge first {pays.FirstSeen:O} events {string.Join(",", transactions.Transactions.Select(t => t.OccurredDate.ToString("O")))}");

            var facts = await service.GetNodeAsync(new GraphNodeRequestDto { Key = seed.AccountKey, Value = "A1" });
            facts.Definitions.Should().HaveCount(2);
            facts.Sentences.Should().NotBeEmpty();
        }

        [Fact]
        public async Task RenderingAKeyValueWithNoNetworkIsNotFoundRatherThanAServerErrorAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var unknown = Request(2);
            unknown.Value = "NO-SUCH-VALUE";

            var render = () => service.RenderAsync(new GraphRenderRequestDto { Query = unknown, LayoutId = 1 });

            await render.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RendersDeterministicallyWithPositionsAndCapturesAHashedPairOfFilesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var render = new GraphRenderRequestDto { Query = Request(2), LayoutId = 1, MeasureId = 1 };

            var first = await service.RenderAsync(render);
            var second = await service.RenderAsync(render);
            first.Svg.Should().Be(second.Svg);
            first.Svg.Should().Contain($"data-node=\"{first.Graph.Seed!.Id}\"");
            first.Svg.Should().Contain("data-edge=");
            first.Positions.Should().HaveCount(first.Graph.Nodes.Count);
            first.LayoutId.Should().Be(1);

            render.Anchors = first.Positions;
            var anchored = await service.RenderAsync(render);
            anchored.Positions.Should().HaveCount(first.Positions.Count);

            foreach (var layout in new[] { 2, 3, 4 })
            {
                (await service.RenderAsync(new GraphRenderRequestDto { Query = Request(2), LayoutId = layout })).Svg
                    .Should().Contain("<svg");
            }

            var strip = await service.GetTimeStripSvgAsync(Request(2), 12, false);
            strip.Should().Contain("data-bin=");

            var caseId = await dbContext.Case.Where(c => c.CaseKey == seed.AccountKey && c.CaseKeyValue == "A1")
                .Select(c => c.Id).FirstAsync();
            var capture = await service.CaptureAsync(new GraphCaptureRequestDto { CaseId = caseId, Render = render });
            capture.Nodes.Should().Be(first.Graph.Nodes.Count);
            var svgFile = await dbContext.GetTable<Data.Poco.CaseFile>().FirstAsync(f => f.Id == capture.SvgCaseFileId);
            svgFile.Hash.Should().Be(capture.Hash);
            Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(svgFile.Object)).Should().Be(capture.Hash);
            svgFile.CaseNoteId.Should().Be(capture.CaseNoteId);
            var definitionFile = await dbContext.GetTable<Data.Poco.CaseFile>().FirstAsync(f => f.Id == capture.DefinitionCaseFileId);
            System.Text.Encoding.UTF8.GetString(definitionFile.Object).Should().Contain(capture.Hash!);
            (await dbContext.GetTable<Data.Poco.CaseNote>().FirstAsync(n => n.Id == capture.CaseNoteId)).KindId.Should().Be(4);
            await dbContext.GetTable<Data.Poco.CaseFile>().Where(f => f.CaseNoteId == capture.CaseNoteId).DeleteAsync();
            await dbContext.GetTable<Data.Poco.CaseNote>().Where(n => n.Id == capture.CaseNoteId).DeleteAsync();
        }

        [Fact]
        public async Task SavesListsOverwritesAndRemovesTheCallersOwnExplorationsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var name = $"{DatabaseFixture.Prefix}Exploration{Guid.NewGuid():N}"[..40];

            var saved = await service.SaveExplorationAsync(new GraphExplorationDto
                { Name = name, GraphRegistryGuid = seed.Registry, Address = "gv=relevance&gh=2" });
            saved.Id.Should().BePositive();
            var overwritten = await service.SaveExplorationAsync(new GraphExplorationDto
                { Name = name, GraphRegistryGuid = seed.Registry, Address = "gv=hops&gh=3" });
            overwritten.Id.Should().Be(saved.Id);
            (await service.GetExplorationsAsync()).Should().ContainSingle(e => e.Id == saved.Id).Which.Address.Should()
                .Be("gv=hops&gh=3");

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermissionNoApproval);
            (await other.GetExplorationsAsync()).Should().NotContain(e => e.Id == saved.Id);
            var foreign = () => other.DeleteExplorationAsync(saved.Id);
            await foreign.Should().ThrowAsync<NotFoundException>();

            var invalid = () => service.SaveExplorationAsync(new GraphExplorationDto
                { Name = "", GraphRegistryGuid = seed.Registry, Address = "x" });
            await invalid.Should().ThrowAsync<ArgumentException>();

            await service.DeleteExplorationAsync(saved.Id);
            (await service.GetExplorationsAsync()).Should().NotContain(e => e.Id == saved.Id);
            await dbContext.GraphExploration.Where(e => e.Id == saved.Id).DeleteAsync();
        }

        [Fact]
        public async Task CollapsesBeyondTheCapAndHidesRestrictedEdgesWithoutAGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext, restrictDevice: true);
            var service = await BuildServiceAsync(dbContext, seed.Me);

            var capped = await service.GetNeighbourhoodAsync(Request(2, cap: 10));
            capped.Nodes.Should().NotContain(n => n.Value == "D1");
            capped.Definitions.Single(d => d.Name == "Shares device").Visible.Should().BeFalse();

            var wide = await service.GetNeighbourhoodAsync(Request(1, cap: 10));
            wide.Edges.Should().OnlyContain(e => !e.Restricted);
        }

        [Fact]
        public async Task RefusesBadArgumentsUnknownRegistriesAndMissingPermissionAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);

            var hops = () => service.GetNeighbourhoodAsync(Request(9));
            await hops.Should().ThrowAsync<ArgumentException>();
            var registry = () => service.GetNeighbourhoodAsync(new GraphNeighbourhoodRequestDto
                { RegistryGuid = Guid.NewGuid(), Key = seed.AccountKey, Value = "A1" });
            await registry.Should().ThrowAsync<NotFoundException>();
            var missingKey = () => service.GetNeighbourhoodAsync(new GraphNeighbourhoodRequestDto { RegistryGuid = seed.Registry });
            await missingKey.Should().ThrowAsync<ArgumentException>();

            var other = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            var forbidden = () => other.GetNeighbourhoodAsync(Request(1));
            await forbidden.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task CaseNodesAreHiddenFromAViewerWithoutTheCaseWorkflowRolesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            await new GraphBuildJob(fx.ConnectionString, TestLog.NoOp, 5000, 50, 365, 1000, false)
                .BuildCaseNodesAsync(dbContext, seed.TenantRegistryId);
            var service = await BuildServiceAsync(dbContext, seed.Me);

            var permitted = await service.GetNeighbourhoodAsync(Request(1));
            var caseNode = permitted.Nodes.First(n => n.Key == GraphCaseEdge.NodeKey);
            permitted.Edges.Should().Contain(e => e.FromNodeId == caseNode.Id || e.ToNodeId == caseNode.Id);

            await dbContext.CaseWorkflowRole.Where(r => r.CaseWorkflowGuid == seed.Workflow).DeleteAsync();

            var hidden = await service.GetNeighbourhoodAsync(Request(1));
            hidden.Nodes.Should().NotContain(n => n.Key == GraphCaseEdge.NodeKey);
            hidden.Edges.Should().NotContain(e => e.FromNodeId == caseNode.Id || e.ToNodeId == caseNode.Id);
            hidden.Nodes.Select(n => n.Value).Should().Contain("A1");
            var byId = () => service.GetNodeAsync(new GraphNodeRequestDto { NodeId = caseNode.Id });
            await byId.Should().ThrowAsync<NotFoundException>();
            var byKey = () => service.GetNodeAsync(new GraphNodeRequestDto
                { Key = GraphCaseEdge.NodeKey, Value = caseNode.Value });
            await byKey.Should().ThrowAsync<NotFoundException>();
            var facts = await service.GetNodeAsync(new GraphNodeRequestDto { Key = seed.AccountKey, Value = "A1" });
            facts.Definitions.Should().NotContain(d => d.Name == GraphCaseEdge.Name);
        }

        [Fact]
        public async Task APathWhoseOnlyRouteRunsThroughAHiddenCaseNodeIsNotReturnedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (caseDefinition, caseNode, a1, a3) = await SeedCaseNodeAsync(dbContext);
            await InsertEdgeAsync(dbContext, caseDefinition, 5, caseNode, a3);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var request = new GraphPathsRequestDto
            {
                RegistryGuid = seed.Registry, FromNodeId = a1, ToNodeId = a3, Edges = caseDefinition.ToString(), MaxHops = 2
            };

            var permitted = await service.GetPathsAsync(request);
            permitted.Paths.Should().ContainSingle(p => p.NodeIds.Contains(caseNode));

            await dbContext.CaseWorkflowRole.Where(r => r.CaseWorkflowGuid == seed.Workflow).DeleteAsync();

            var hidden = await service.GetPathsAsync(request);
            hidden.Paths.Should().BeEmpty();
            hidden.Nodes.Should().NotContain(n => n.Id == caseNode);
            hidden.Edges.Should().NotContain(e => e.FromNodeId == caseNode || e.ToNodeId == caseNode);
        }

        [Fact]
        public async Task FlowThroughAHiddenCaseNodeIsLeftOutOfTheTranchesAndTheTotalsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, caseNode, a1, _) = await SeedCaseNodeAsync(dbContext);
            var edge = await InsertEdgeAsync(dbContext, seed.PaysDefinition, 2, a1, caseNode);
            await dbContext.InsertAsync(new Data.Poco.GraphEdgeEvent
            {
                TenantRegistryId = seed.TenantRegistryId, GraphEdgeId = edge, ArchiveId = 910000000 + a1,
                EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(), ArchiveKeyVersion = 1,
                OccurredDate = DateTime.UtcNow.AddDays(-3), Amount = 200, Retracted = 0
            });
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var request = new GraphFlowRequestDto { RegistryGuid = seed.Registry, NodeId = a1 };

            var permitted = await service.GetFlowAsync(request);
            permitted.Tranches.Should().Contain(t => t.NodeId == caseNode);

            await dbContext.CaseWorkflowRole.Where(r => r.CaseWorkflowGuid == seed.Workflow).DeleteAsync();

            var hidden = await service.GetFlowAsync(request);
            hidden.Tranches.Should().NotContain(t => t.NodeIds.Contains(caseNode));
            hidden.Tranches.Count.Should().Be(permitted.Tranches.Count - 1);
            hidden.Graph.Nodes.Should().NotContain(n => n.Id == caseNode);
            hidden.PassThroughNodeIds.Should().NotContain(caseNode);
            hidden.Sentences.Should().NotBeEquivalentTo(permitted.Sentences);
        }

        [Fact]
        public async Task ExplodeLanesOmitAHiddenCaseNodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, caseNode, a1, _) = await SeedCaseNodeAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var request = new GraphExplodeRequestDto { RegistryGuid = seed.Registry, NodeId = a1 };

            (await service.GetExplodeAsync(request)).Lanes.Should().Contain(l => l.Node.Id == caseNode);

            await dbContext.CaseWorkflowRole.Where(r => r.CaseWorkflowGuid == seed.Workflow).DeleteAsync();

            var hidden = await service.GetExplodeAsync(request);
            hidden.Lanes.Should().NotContain(l => l.Node.Id == caseNode);
            hidden.Lanes.Should().NotBeEmpty();
        }

        private async Task<(Guid CaseDefinition, long CaseNode, long A1, long A3)> SeedCaseNodeAsync(DbContext dbContext)
        {
            await SeedRingAsync(dbContext);
            await new GraphBuildJob(fx.ConnectionString, TestLog.NoOp, 5000, 50, 365, 1000, false)
                .BuildCaseNodesAsync(dbContext, seed.TenantRegistryId);
            var registryId = await dbContext.GraphRegistry.Where(r => r.Guid == seed.Registry).Select(r => r.Id).FirstAsync();
            var caseDefinition = await dbContext.GraphRegistryEdge
                .Where(d => d.GraphRegistryId == registryId && d.KindId == GraphCaseEdge.KindId).Select(d => d.Guid).FirstAsync();
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            var a3 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A3");
            var caseNode = await dbContext.GraphEdge
                .Where(e => e.GraphRegistryEdgeGuid == caseDefinition && e.FromNodeId == a1.Id).Select(e => e.ToNodeId).FirstAsync();

            return (caseDefinition, caseNode, a1.Id, a3.Id);
        }

        private async Task<long> InsertEdgeAsync(DbContext dbContext, Guid definition, byte kind, long from, long to)
        {
            var now = DateTime.UtcNow;

            return await dbContext.InsertWithInt64IdentityAsync(new Data.Poco.GraphEdge
            {
                TenantRegistryId = seed.TenantRegistryId, GraphRegistryEdgeGuid = definition, KindId = kind, FromNodeId = from,
                ToNodeId = to, Count = 1, AmountSum = 0, FirstSeen = now.AddDays(-3), LastSeen = now.AddDays(-1),
                DecayedCount = 1, DecayedAmount = 0, DecayedAt = now.AddDays(-1), Specificity = 1, Confidence = 1,
                Weight = 1f, FromHub = 0, ToHub = 0, Restricted = 0
            });
        }

        [Fact]
        public async Task CaseCountsOnANodeFollowTheViewersCaseWorkflowRolesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var keys = new[] { seed.AccountKey, seed.AccountKey };
            var values = new[] { "A1", "A3" };

            var permitted = await new Data.Query.GetGraphNodeCasesQuery.GetGraphNodeCasesQuery(dbContext)
                .ExecuteAsync(seed.TenantRegistryId, seed.Me, keys, values);
            permitted.Should().HaveCount(2);
            permitted.Single(c => c.CaseKeyValue == "A1").Open.Should().Be(1);
            permitted.Single(c => c.CaseKeyValue == "A3").Confirmed.Should().Be(1);

            var outsider = await new Data.Query.GetGraphNodeCasesQuery.GetGraphNodeCasesQuery(dbContext)
                .ExecuteAsync(seed.TenantRegistryId, fx.Seed.UserWithoutPermission, keys, values);
            outsider.Should().BeEmpty();

            var none = await new Data.Query.GetGraphNodeCasesQuery.GetGraphNodeCasesQuery(dbContext)
                .ExecuteAsync(seed.TenantRegistryId, "nobody-" + Guid.NewGuid(), keys, values);
            none.Should().BeEmpty();
        }

        [Fact]
        public async Task RefusesMoreExpandedNodesThanTheCapAndIgnoresForeignNodeIdsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var request = Request(1);

            request.Expanded = string.Join(",", Enumerable.Range(1, GraphService.MaximumExpanded + 1));
            var tooMany = () => service.GetNeighbourhoodAsync(request);
            await tooMany.Should().ThrowAsync<ArgumentException>();

            request.Expanded = "9223372036854775807,-5,abc";
            var result = await service.GetNeighbourhoodAsync(request);
            result.Nodes.Select(n => n.Value).Should().BeEquivalentTo(["A1", "B1", "D1"]);
        }

        [Fact]
        public async Task NodeFactsAndEdgeEvidenceHideRestrictedEdgesFromAViewerWithoutAGrantAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext, restrictDevice: true);
            var service = await BuildServiceAsync(dbContext, seed.Me);

            var facts = await service.GetNodeAsync(new GraphNodeRequestDto { Key = seed.AccountKey, Value = "A1" });
            facts.Definitions.Should().ContainSingle(d => d.Guid == seed.PaysDefinition);
            facts.Definitions.Should().NotContain(d => d.Guid == seed.DeviceDefinition);

            var restricted = await dbContext.GraphEdge.FirstAsync(e =>
                e.TenantRegistryId == seed.TenantRegistryId && e.GraphRegistryEdgeGuid == seed.DeviceDefinition);
            var evidence = () => service.GetEdgeTransactionsAsync(new GraphEdgeTransactionsRequestDto { EdgeId = restricted.Id });
            await evidence.Should().ThrowAsync<NotFoundException>();
        }

        [Fact]
        public async Task RefusesAFlowWhoseFanOutAndDepthWouldExplodeAndAnOversizedPinnedPayloadAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var service = await BuildServiceAsync(dbContext, seed.Me);
            var node = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");

            var flow = () => service.GetFlowAsync(new GraphFlowRequestDto
                { RegistryGuid = seed.Registry, NodeId = node.Id, FanOut = 50, Depth = 6 });
            await flow.Should().ThrowAsync<ArgumentException>();

            var pinned = () => service.SaveExplorationAsync(new GraphExplorationDto
            {
                Name = "pinned", GraphRegistryGuid = seed.Registry, Address = "/graph",
                Pinned = new string('x', GraphService.MaximumPinnedLength + 1)
            });
            await pinned.Should().ThrowAsync<ArgumentException>();
        }

        [Fact]
        public async Task TheTraversalPlanUsesTheEdgeIndexesAndNeverScansTheEdgeTableAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedRingAsync(dbContext);
            var node = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            await dbContext.ExecuteAsync("SET enable_seqscan = off");
            var plan = await dbContext.QueryToListAsync<string>(
                "EXPLAIN (FORMAT TEXT) " + NeighbourhoodSql, new DataParameter("tenant", seed.TenantRegistryId),
                new DataParameter("seeds", new[] { node.Id }), new DataParameter("defs", new[] { seed.PaysDefinition, seed.DeviceDefinition }),
                new DataParameter("from", DateTime.UtcNow.AddDays(-30)), new DataParameter("to", DateTime.UtcNow));
            await dbContext.ExecuteAsync("RESET enable_seqscan");
            var text = string.Join("\n", plan);
            text.Should().Contain("WorkTable Scan");
            text.Should().NotContain("Seq Scan on \"GraphEdge\"");
            text.Should().MatchRegex("Index (Only )?Scan using \"IX_GraphEdge_(From|To)");
        }

        private const string NeighbourhoodSql = """
            WITH RECURSIVE frontier("NodeId", "Depth", "Path") AS (
                SELECT s."Id", 0, ARRAY[s."Id"] FROM unnest(@seeds::bigint[]) AS s("Id")
                UNION ALL
                SELECT n."NodeId", f."Depth" + 1, f."Path" || n."NodeId" FROM frontier f
                JOIN LATERAL (
                    (SELECT e."ToNodeId" AS "NodeId", e."Weight" FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."FromNodeId" = f."NodeId" AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[])
                        AND e."ToHub" = 0 AND e."LastSeen" >= @from AND e."FirstSeen" <= @to ORDER BY e."Weight" DESC LIMIT 25)
                    UNION ALL
                    (SELECT e."FromNodeId", e."Weight" FROM "GraphEdge" e
                      WHERE e."TenantRegistryId" = @tenant AND e."ToNodeId" = f."NodeId" AND e."GraphRegistryEdgeGuid" = ANY(@defs::uuid[])
                        AND e."FromHub" = 0 AND e."LastSeen" >= @from AND e."FirstSeen" <= @to ORDER BY e."Weight" DESC LIMIT 25)
                ) n ON NOT n."NodeId" = ANY(f."Path") WHERE f."Depth" < 2)
            SELECT count(*) FROM frontier
            """;

        private GraphNeighbourhoodRequestDto Request(int hops, int? cap = null) => new()
        {
            RegistryGuid = seed.Registry, Key = seed.AccountKey, Value = "A1", Hops = hops, Cap = cap
        };

        private async Task SeedRingAsync(DbContext dbContext, bool restrictDevice = false)
        {
            await seed.SetupAsync(dbContext, restrictDevice: restrictDevice);
            var now = DateTime.UtcNow;
            await seed.TransactionAsync(dbContext, now.AddDays(-5), "A1", "B1", 100, "D1");
            anchor = await seed.TransactionAsync(dbContext, now.AddDays(-4), "A1", "B1", 300, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-3), "A2", null, null, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-3), "A3", null, null, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-2), "A3", "B2", 50, null);
            anchorArchiveId = await seed.ArchiveIdAsync(dbContext, anchor);
            await seed.CaseAsync(dbContext, anchor, "A1", false);
            await seed.CaseAsync(dbContext, anchor, "A3", true);
            var definitions = (await new Data.Repository.GraphRegistryEdgeRepository(dbContext, seed.TenantRegistryId)
                    .GetActiveByGraphRegistryGuidAsync(seed.Registry))
                .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90, d.KindPrior ?? 1,
                    d.Restricted ?? 0)).ToList();
            await new GraphBuildJob(fx.ConnectionString, TestLog.NoOp, 5000, 50, 365, 1000, false)
                .BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
        }

        private static Task<GraphService> BuildServiceAsync(DbContext dbContext, string? userName)
        {
            return GraphService.CreateAsync(dbContext, userName, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestLog.NoOp);
        }
    }
}
