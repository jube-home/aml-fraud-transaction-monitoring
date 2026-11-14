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
using Jube.Data.Repository;
using Jube.Dto.Query.Graph;
using Jube.Dto.Repository.CaseTag;
using Jube.Service.Repository.CaseTag;
using Jube.Engine.CaseRisk;
using Jube.Service.Exceptions.Query.Graph;
using Jube.Service.Query.Graph;
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Query.Graph.Jobs.Models;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.Query.Graph
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphAnalyticsTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);
        private readonly GraphJobsSeed seed = new(fx);
        private int confirmedCaseId;

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var nodeIds = dbContext.GraphNode.Where(n => n.Key == seed.AccountKey || n.Key == seed.DeviceKey || n.Key == seed.BeneficiaryKey).Select(n => n.Id);
            var patternIds = dbContext.GraphPattern.Where(p => nodeIds.Contains(p.AnchorNodeId)).Select(p => p.Id);
            await dbContext.GraphPatternNode.Where(m => patternIds.Contains(m.GraphPatternId)).DeleteAsync();
            await dbContext.GraphPattern.Where(p => nodeIds.Contains(p.AnchorNodeId)).DeleteAsync();
            var communityIds = dbContext.GraphNode.Where(n => nodeIds.Contains(n.Id) && n.CommunityId != null).Select(n => n.CommunityId!.Value);
            await dbContext.GraphCommunity.Where(c => communityIds.Contains(c.Id)).DeleteAsync();
            await seed.CleanupAsync(dbContext);
        }

        [Fact]
        public async Task CommunityMembersOmitAHiddenCaseNodeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var connectionString = fx.ConnectionString;
            await new GraphBuildJob(connectionString, TestLog.NoOp, 5000, 50, 365, 1000, false)
                .BuildCaseNodesAsync(dbContext, seed.TenantRegistryId);
            await new GraphCommunityJob(connectionString, TestLog.NoOp, 1.0, 1_000_000).RunTenantAsync(dbContext, seed.TenantRegistryId);
            var caseNode = await dbContext.GraphNode.FirstAsync(n =>
                n.TenantRegistryId == seed.TenantRegistryId && n.Key == "Case" && n.Value == confirmedCaseId.ToString());
            caseNode.CommunityId.Should().NotBeNull();
            var analytics = await GraphAnalyticsService.CreateAsync(dbContext, seed.Me, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);

            (await analytics.GetCommunityAsync(caseNode.CommunityId!.Value)).Members.Should().Contain(m => m.Id == caseNode.Id);

            await dbContext.CaseWorkflowRole.Where(r => r.CaseWorkflowGuid == seed.Workflow).DeleteAsync();

            var hidden = await analytics.GetCommunityAsync(caseNode.CommunityId!.Value);
            hidden.Members.Should().NotContain(m => m.Key == "Case");
            hidden.Members.Should().NotBeEmpty();
        }

        [Fact]
        public async Task RestrictedEdgesNeverFeedTheCachedDegreesCommunitiesOrPageRankAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext, restrictDevice: true);
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            var d1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.DeviceKey && n.Value == "D1");

            a1.Degree.Should().Be(1);
            a1.DegreeIdentity.Should().Be(0);
            d1.Degree.Should().Be(0);
            d1.CommunityId.Should().BeNull();
            d1.PageRank.Should().BeNull();
            d1.ComponentId.Should().BeNull();
        }

        [Fact]
        public async Task MarkingADefinitionRestrictedRestrictsItsExistingEdgesAtOnceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var repository = new GraphRegistryEdgeRepository(dbContext, seed.Me);
            var definition = await repository.GetByGuidAsync(seed.DeviceDefinition);
            var before = await dbContext.GraphEdge.Where(e => e.GraphRegistryEdgeGuid == seed.DeviceDefinition).ToListAsync();
            before.Should().NotBeEmpty();
            before.Should().OnlyContain(e => e.Restricted == 0);

            definition.Restricted = 1;
            await repository.UpdateAsync(definition);

            var after = await dbContext.GraphEdge.Where(e => e.GraphRegistryEdgeGuid == seed.DeviceDefinition).ToListAsync();
            after.Should().OnlyContain(e => e.Restricted == 1);
        }

        [Fact]
        public async Task CasingAPatternIntoAnUnknownCaseIsNotFoundAndLeavesThePatternUndecidedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var source = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "S");
            var analytics = await GraphAnalyticsService.CreateAsync(dbContext, seed.Me, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
            var fan = (await analytics.GetPatternsAsync(new GraphPatternListRequestDto { KindId = 1 }))
                .First(p => p.Anchor != null && p.Anchor.Id == source.Id);

            var cased = () => analytics.DecidePatternAsync(new GraphPatternDecideRequestDto
                { Id = fan.Id, StateId = 4, CaseId = int.MaxValue });
            await cased.Should().ThrowAsync<NotFoundException>();
            (await analytics.GetPatternAsync(fan.Id)).StateId.Should().Be(fan.StateId);
        }

        [Fact]
        public async Task TheAnalyticsCyclesWriteCommunitiesDistancesAndPatternsTheServicesReadBackAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            var a3 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A3");
            var source = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "S");

            a3.DistanceToConfirmed.Should().Be(0);
            a1.DistanceToConfirmed.Should().Be(2);
            a1.ConfirmedWithin2.Should().Be(1);
            a1.PropagationScore.Should().BeGreaterThan(0);
            a1.CommunityId.Should().NotBeNull();
            a1.ComponentId.Should().NotBeNull();
            a1.CoreNumber.Should().NotBeNull();
            a1.PageRank.Should().BeGreaterThan(0);
            a1.ComputedDate.Should().NotBeNull();

            var analytics = await GraphAnalyticsService.CreateAsync(dbContext, seed.Me, TestLog.NoOp, localizers,
                new NullServiceChangeBus(), TestLog.NoOp);
            var community = await analytics.GetCommunityAsync(a1.CommunityId!.Value);
            community.Size.Should().BeGreaterThanOrEqualTo(GraphCommunityJob.MinimumCommunitySize);
            community.ConfirmedCount.Should().Be(1);
            community.Members.Should().Contain(m => m.Id == a1.Id);
            (await analytics.GetCommunitiesAsync(50)).Should().Contain(c => c.Id == community.Id);

            var patterns = await analytics.GetPatternsAsync(new GraphPatternListRequestDto { KindId = 1 });
            var fan = patterns.Should().Contain(p => p.Anchor != null && p.Anchor.Id == source.Id).Subject;
            fan.Kind.Should().Be("FanOut");
            fan.MemberCount.Should().BeGreaterThanOrEqualTo(6);
            fan.ArchiveIds.Should().HaveCount(6);
            var detail = await analytics.GetPatternAsync(fan.Id);
            detail.Members.Should().Contain(m => m.Role == "Destination");
            detail.Sentence.Should().Contain("FanOut");

            var decided = await analytics.DecidePatternAsync(new GraphPatternDecideRequestDto { Id = fan.Id, StateId = 2 });
            decided.StateId.Should().Be(2);
            decided.DecidedUser.Should().Be(seed.Me);
            var badState = () => analytics.DecidePatternAsync(new GraphPatternDecideRequestDto { Id = fan.Id, StateId = 4 });
            await badState.Should().ThrowAsync<ArgumentException>();

            var facts = await (await ServiceAsync(dbContext)).GetNodeAsync(new GraphNodeRequestDto { NodeId = source.Id });
            facts.Patterns.Should().Contain(p => p.Id == fan.Id);
            facts.Community.Should().NotBeNull();

            var hubs = await analytics.GetHubsAsync();
            hubs.Should().NotContain(h => h.Id == a1.Id);
            var overridden = await analytics.OverrideHubAsync(new GraphHubOverrideRequestDto { NodeId = a1.Id, OverrideId = 2 });
            overridden.Hub.Should().BeTrue();
            (await analytics.GetHubsAsync()).Should().Contain(h => h.Id == a1.Id);
            (await analytics.OverrideHubAsync(new GraphHubOverrideRequestDto { NodeId = a1.Id, OverrideId = 0 })).Hub.Should().BeTrue();
            (await analytics.OverrideHubAsync(new GraphHubOverrideRequestDto { NodeId = a1.Id, OverrideId = 1 })).Hub.Should().BeFalse();

            var other = await GraphAnalyticsService.CreateAsync(dbContext, fx.Seed.UserWithoutPermission, TestLog.NoOp,
                localizers, new NullServiceChangeBus(), TestLog.NoOp);
            var forbidden = () => other.GetPatternsAsync(new GraphPatternListRequestDto());
            await forbidden.Should().ThrowAsync<ForbiddenException>();
        }

        [Fact]
        public async Task PathsFlowAndRelevanceRankingFollowTheSeededChainAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var service = await ServiceAsync(dbContext);
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            var a3 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A3");
            var source = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "S");

            var paths = await service.GetPathsAsync(new GraphPathsRequestDto
                { RegistryGuid = seed.Registry, FromNodeId = a1.Id, ToNodeId = a3.Id, MaxHops = 4, K = 5 });
            paths.Paths.Should().NotBeEmpty();
            paths.Paths[0].Hops.Should().Be(2);
            paths.Paths[0].NodeIds.First().Should().Be(a1.Id);
            paths.Paths[0].NodeIds.Last().Should().Be(a3.Id);
            paths.Paths[0].EdgeIds.Should().HaveCount(2);
            paths.Nodes.Should().Contain(n => n.Value == "D1");
            paths.Sentences.Should().NotBeEmpty();
            var same = () => service.GetPathsAsync(new GraphPathsRequestDto { RegistryGuid = seed.Registry, FromNodeId = a1.Id, ToNodeId = a1.Id });
            await same.Should().ThrowAsync<ArgumentException>();

            var flow = await service.GetFlowAsync(new GraphFlowRequestDto
                { RegistryGuid = seed.Registry, NodeId = source.Id, Forward = true, Depth = 3, HopWindowHours = 240 });
            flow.Forward.Should().BeTrue();
            flow.Tranches.Should().HaveCount(6);
            flow.Tranches.Should().OnlyContain(t => t.Depth == 1 && t.ArchiveIds.Count == 1);
            flow.Graph.Seed!.Id.Should().Be(source.Id);
            flow.Graph.Nodes.Should().HaveCount(7);
            flow.Graph.Edges.Should().HaveCount(6);
            flow.Sentences.Should().ContainSingle().Which.Should().StartWith("6 tranche(s) reach 6 node(s)");
            var rendered = await service.RenderAsync(new GraphRenderRequestDto
            {
                Flow = new GraphFlowRequestDto { RegistryGuid = seed.Registry, NodeId = source.Id, Depth = 3, HopWindowHours = 240 }
            });
            rendered.LayoutId.Should().Be(3);
            rendered.Svg.Should().Contain($"data-node=\"{source.Id}\"");

            var ranked = await service.GetNeighbourhoodAsync(new GraphNeighbourhoodRequestDto
                { RegistryGuid = seed.Registry, Key = seed.AccountKey, Value = "A1", Hops = 2, Relevance = true });
            ranked.Relevance.Should().BeTrue();
            ranked.Nodes.Should().Contain(n => n.Value == "A3").Which.Interest.Should().BeGreaterThan(0);
            ranked.Nodes.Where(n => !n.Seed).Select(n => n.Interest ?? -1).Should().BeInDescendingOrder();
            ranked.Nodes.Single(n => n.Value == "A3").Interest.Should()
                .BeGreaterThanOrEqualTo(ranked.Nodes.Single(n => n.Value == "A2").Interest ?? 0);
        }

        [Fact]
        public async Task TheNetworkEvidenceFamilyReportsTheCommunityRateAndStaysSilentWithoutAGraphAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var openCaseId = await dbContext.Case.Where(c => c.CaseKey == seed.AccountKey && c.CaseKeyValue == "A1")
                .Select(c => c.Id).FirstAsync();

            var items = await CaseNetworkEvidence.BuildAsync(dbContext, seed.TenantRegistryId, seed.Workflow, openCaseId,
                seed.AccountKey, "A1", DateTime.UtcNow, 90);
            items.Should().ContainSingle(i => i.Label.StartsWith("Community"));
            items.Should().OnlyContain(i => i.Family == CaseNetworkEvidence.Family && i.DependenceGroup == CaseNetworkEvidence.Family);
            items.Single(i => i.Label.StartsWith("Community")).Confirmed.Should().Be(1);
            items.Single(i => i.Label.StartsWith("Community")).Closed.Should().Be(1);

            var none = await CaseNetworkEvidence.BuildAsync(dbContext, seed.TenantRegistryId, seed.Workflow, openCaseId,
                seed.AccountKey, "NoSuchNode", DateTime.UtcNow, 90);
            none.Should().BeEmpty();
            confirmedCaseId.Should().BePositive();
        }

        [Fact]
        public async Task CaseNodesCaseTagsAndTheQueueFastPathFeedProximityToTagAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await SeedAndRunAsync(dbContext);
            var connectionString = fx.ConnectionString;
            var job = new GraphBuildJob(connectionString, TestLog.NoOp, 5000, 50, 365, 1000, false);
            var now = DateTime.UtcNow;

            var late = await seed.TransactionAsync(dbContext, now.AddHours(-1), "A2", "B9", 70, null);
            var before = await dbContext.GraphEdgeEvent.CountAsync(e => e.EntityAnalysisModelInstanceEntryGuid == late);
            before.Should().Be(0);
            Jube.Engine.Graph.GraphBuildQueue.Enqueue([late, late]);
            var queued = await new GraphBuildJob(Environment.GetEnvironmentVariable("JubeTestConnectionString") ?? connectionString,
                TestLog.NoOp, 5000, 50, 365, 1000, false).ApplyQueuedAsync(100);
            queued.Entries.Should().Be(2);
            queued.Events.Should().Be(1);
            (await dbContext.GraphEdgeEvent.CountAsync(e => e.EntityAnalysisModelInstanceEntryGuid == late)).Should().Be(1);
            var definitions = (await new GraphRegistryEdgeRepository(dbContext, seed.TenantRegistryId)
                    .GetActiveByGraphRegistryGuidAsync(seed.Registry))
                .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90, d.KindPrior ?? 1,
                    d.Restricted ?? 0)).ToList();
            await job.BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);
            (await dbContext.GraphEdgeEvent.CountAsync(e => e.EntityAnalysisModelInstanceEntryGuid == late)).Should().Be(1);

            (await job.BuildCaseNodesAsync(dbContext, seed.TenantRegistryId)).Should().BeGreaterThanOrEqualTo(1);
            var openCaseId = await dbContext.Case.Where(c => c.CaseKey == seed.AccountKey && c.CaseKeyValue == "A1")
                .Select(c => c.Id).FirstAsync();
            var caseNode = await dbContext.GraphNode.FirstOrDefaultAsync(n => n.Key == "Case" && n.Value == openCaseId.ToString());
            caseNode.Should().NotBeNull();
            var a1 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A1");
            (await dbContext.GraphEdge.CountAsync(e => e.FromNodeId == a1.Id && e.ToNodeId == caseNode!.Id && e.KindId == 5)).Should().Be(1);

            await seed.ModelTagAsync(dbContext, "Mule");
            await seed.ModelTagAsync(dbContext, "Victim");
            var tags = await CaseTagService.CreateAsync(dbContext, seed.Me, TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);
            (await tags.GetChoicesAsync(openCaseId)).Should().BeEquivalentTo(["Mule", "Victim"]);
            var merged = await tags.MergeAsync(new CaseTagMergeRequestDto { CaseId = openCaseId, Tags = ["Mule"] });
            merged.Select(t => t.Name).Should().BeEquivalentTo(["Mule"]);
            var unknown = () => tags.MergeAsync(new CaseTagMergeRequestDto { CaseId = openCaseId, Tags = ["Nope"] });
            await unknown.Should().ThrowAsync<ArgumentException>();
            var a3Entry = await dbContext.Archive.Where(a => a.EntryKeyValue == "A3").Select(a => a.EntityAnalysisModelInstanceEntryGuid).FirstAsync();
            await seed.ArchiveTagAsync(dbContext, a3Entry, "Victim");

            await new GraphDistanceJob(connectionString, TestLog.NoOp, 4, 1_000_000).RunTenantAsync(dbContext, seed.TenantRegistryId);
            var a1Tags = await dbContext.GraphNodeTag.Where(t => t.GraphNodeId == a1.Id).ToListAsync();
            a1Tags.Single(t => t.Tag == "Mule").Distance.Should().Be(1);
            a1Tags.Single(t => t.Tag == "Victim").Distance.Should().Be(1);
            var a2 = await dbContext.GraphNode.FirstAsync(n => n.Key == seed.AccountKey && n.Value == "A2");
            (await dbContext.GraphNodeTag.SingleAsync(t => t.GraphNodeId == a2.Id && t.Tag == "Mule")).Distance.Should().Be(3);
            (await dbContext.GraphNodeTag.SingleAsync(t => t.GraphNodeId == caseNode!.Id && t.Tag == "Mule")).Distance.Should().Be(0);
            (await dbContext.GraphRegistryEdge.CountAsync(d => d.KindId == 5 && d.Name == "In case" && d.Locked == 1)).Should().BeGreaterThanOrEqualTo(1);

            var service = await ServiceAsync(dbContext);
            var facts = await service.GetNodeAsync(new GraphNodeRequestDto { NodeId = a1.Id });
            facts.Node.NearestTag.Should().BeOneOf("Mule", "Victim");
            facts.Node.Tags.Should().HaveCount(2);
            facts.Sentences.Should().Contain(s => s.Contains("tagged Mule"));
            var neighbourhood = await service.GetNeighbourhoodAsync(new GraphNeighbourhoodRequestDto
                { RegistryGuid = seed.Registry, Key = seed.AccountKey, Value = "A1", Hops = 1 });
            neighbourhood.Definitions.Should().Contain(d => d.KindId == 5 && d.Name == "In case");
            neighbourhood.Nodes.Should().Contain(n => n.Key == "Case" && n.Value == openCaseId.ToString());
            (await tags.MergeAsync(new CaseTagMergeRequestDto { CaseId = openCaseId, Tags = [] })).Should().BeEmpty();
            (await tags.GetByCaseIdAsync(openCaseId)).Should().BeEmpty();
            var other = await CaseTagService.CreateAsync(dbContext, fx.Seed.UserWithoutPermission, TestLog.NoOp, localizers, new NullServiceChangeBus(), TestLog.NoOp);
            var forbidden = () => other.GetByCaseIdAsync(openCaseId);
            await forbidden.Should().ThrowAsync<Jube.Service.Exceptions.Repository.CaseTag.ForbiddenException>();
        }

        private async Task SeedAndRunAsync(DbContext dbContext, bool restrictDevice = false)
        {
            await seed.SetupAsync(dbContext, restrictDevice: restrictDevice);
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
            confirmedCaseId = await seed.CaseAsync(dbContext, anchor, "A3", true);
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
            await new GraphPatternJob(connectionString, TestLog.NoOp, 30, 1_000_000, 10000).RunTenantAsync(dbContext,
                seed.TenantRegistryId, now);
        }

        private Task<GraphService> ServiceAsync(DbContext dbContext)
        {
            return GraphService.CreateAsync(dbContext, seed.Me, TestLog.NoOp, localizers, new NullServiceChangeBus(),
                TestLog.NoOp);
        }
    }
}
