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
using Jube.Dto.Query.Graph;
using Jube.Service.Query.Graph;
using Jube.Service.Query.Graph.Resolution;
using Jube.Service.Reactivity;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using LinqToDB.Async;
using Xunit;
using CaseExceptions = Jube.Service.Exceptions.Repository.Case;

namespace Jube.Test.Service.Query.GraphEntityResolution
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GraphEntityResolutionTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly string tag = "Er" + Guid.NewGuid().ToString("N")[..8];
        private readonly Guid definition = Guid.NewGuid();
        private readonly List<long> nodeIds = [];
        private int registryId;
        private int tenant;

        private string User => fx.Seed.UserWithPermission;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            tenant = (await UserInTenantRepository.GetTenantRegistryIdAsync(dbContext, User))!.Value;

            registryId = await dbContext.InsertWithInt32IdentityAsync(new GraphRegistry
            {
                Guid = Guid.NewGuid(), TenantRegistryId = tenant, Name = tag, Active = 1, Locked = 0, Deleted = 0,
                CreatedUser = User, CreatedDate = DateTime.UtcNow, Version = 1
            });

            await dbContext.InsertAsync(new GraphRegistryEdge
            {
                GraphRegistryId = registryId, Guid = definition, Name = tag + "Hard", KindId = 1, FromKey = "A", ToKey = "B",
                FromNodeKey = tag + "Account", ToNodeKey = tag + "NationalId", Hard = 1, Restricted = 0, Active = 1,
                Locked = 0, Deleted = 0, KindPrior = 1, Version = 1
            });
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GraphEntityMatch.Where(w => nodeIds.Contains(w.NodeIdA) || nodeIds.Contains(w.NodeIdB)).DeleteAsync();
            await dbContext.GraphEdge.Where(w => w.GraphRegistryEdgeGuid == definition).DeleteAsync();
            await dbContext.GraphNode.Where(w => nodeIds.Contains(w.Id)).DeleteAsync();
            await dbContext.GraphRegistryEdge.Where(w => w.Guid == definition).DeleteAsync();
            await dbContext.GraphRegistry.Where(w => w.Id == registryId).DeleteAsync();
        }

        private async Task<long> NodeAsync(DbContext dbContext, string key, string value, byte hub = 0)
        {
            var id = await dbContext.InsertWithInt64IdentityAsync(new GraphNode
                { TenantRegistryId = tenant, Key = key, Value = value, Hub = hub });
            nodeIds.Add(id);

            return id;
        }

        private async Task LinkAsync(DbContext dbContext, long from, long to)
        {
            await dbContext.InsertAsync(new GraphEdge
            {
                TenantRegistryId = tenant, GraphRegistryEdgeGuid = definition, KindId = 1, FromNodeId = from, ToNodeId = to,
                Count = 1, FirstSeen = DateTime.UtcNow, LastSeen = DateTime.UtcNow, DecayedCount = 1,
                DecayedAt = DateTime.UtcNow, Specificity = 1, Confidence = 1, Weight = 1
            });
        }

        private GraphEntityResolutionJob Job(int maximum = 10, bool auto = false, params string[] fuzzy) =>
            new(fx.ConnectionString, TestLog.NoOp,
                new GraphEntityResolutionOptions(maximum, auto, fuzzy, 0.2));

        private static Task<GraphEntityMatchService> ServiceAsync(DbContext dbContext, string user) =>
            GraphEntityMatchService.CreateAsync(dbContext, user, TestLog.NoOp, new NullServiceChangeBus());

        private async Task<(long A, long B, long Id)> TwoAccountsAsync(DbContext dbContext, string suffix = "1")
        {
            var identifier = await NodeAsync(dbContext, tag + "NationalId", "ID-" + suffix);
            var a = await NodeAsync(dbContext, tag + "Account", "A" + suffix);
            var b = await NodeAsync(dbContext, tag + "Account", "B" + suffix);
            await LinkAsync(dbContext, a, identifier);
            await LinkAsync(dbContext, b, identifier);

            await Job().RunOnceAsync();

            var match = await dbContext.GraphEntityMatch.SingleAsync(w => w.NodeIdA == Math.Min(a, b)
                                                                          && w.NodeIdB == Math.Max(a, b));

            return (a, b, match.Id);
        }

        [Fact]
        public async Task TwoNodesSharingAHardIdentifierBecomeAPendingCandidateWithEvidenceAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b, id) = await TwoAccountsAsync(dbContext);

            var match = await dbContext.GraphEntityMatch.SingleAsync(w => w.Id == id);
            (match.SourceId, match.DecisionId, match.Posterior).Should()
                .Be(((short)GraphEntityMatchSource.HardIdentifier, (short)GraphEntityMatchDecision.Pending,
                    GraphEntityResolutionJob.HardPosterior));
            match.Evidence.Should().Contain("ID-1").And.Contain(tag + "NationalId").And.Contain("\"records\"");
            (match.NodeIdA, match.NodeIdB).Should().Be((Math.Min(a, b), Math.Max(a, b)));

            await Job().RunOnceAsync();
            (await dbContext.GraphEntityMatch.CountAsync(w => w.NodeIdA == match.NodeIdA)).Should().Be(1);
        }

        [Fact]
        public async Task AHubAnOversizedClusterAndNodesOfDifferentKeysAreNotCandidatesAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var hub = await NodeAsync(dbContext, tag + "NationalId", "HUB", hub: 1);
            var big = await NodeAsync(dbContext, tag + "NationalId", "BIG");
            var mixed = await NodeAsync(dbContext, tag + "NationalId", "MIX");
            var h1 = await NodeAsync(dbContext, tag + "Account", "H1");
            var h2 = await NodeAsync(dbContext, tag + "Account", "H2");
            await LinkAsync(dbContext, h1, hub);
            await LinkAsync(dbContext, h2, hub);

            var crowd = new List<long>();

            for (var i = 0; i < 3; i++)
            {
                crowd.Add(await NodeAsync(dbContext, tag + "Account", "C" + i));
                await LinkAsync(dbContext, crowd[i], big);
            }

            var account = await NodeAsync(dbContext, tag + "Account", "M1");
            var other = await NodeAsync(dbContext, tag + "Card", "M2");
            await LinkAsync(dbContext, account, mixed);
            await LinkAsync(dbContext, other, mixed);

            await Job(maximum: 2).RunOnceAsync();

            (await dbContext.GraphEntityMatch.CountAsync(w => nodeIds.Contains(w.NodeIdA))).Should().Be(0);
        }

        [Fact]
        public async Task ConfirmingMergesTheNodesIntoOneResolvedEntityAndUndoTakesItAwayAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (a, b, id) = await TwoAccountsAsync(dbContext);
            var service = await ServiceAsync(dbContext, User);

            var confirmed = await service.DecideAsync(new GraphEntityMatchDecideDto
                { Id = id, DecisionId = 2, Note = " Same person. " });

            (confirmed.DecisionId, confirmed.DecidedUser, confirmed.DecisionNote).Should().Be((2, User, "Same person."));
            confirmed.ResolvedEntityId.Should().Be(Math.Min(a, b));
            var resolved = await dbContext.GraphNode.Where(w => w.Id == a || w.Id == b).Select(s => s.ResolvedEntityId).ToListAsync();
            resolved.Should().OnlyContain(r => r == Math.Min(a, b));

            var undone = await service.DecideAsync(new GraphEntityMatchDecideDto { Id = id, DecisionId = 1 });
            (undone.DecisionId, undone.ResolvedEntityId, undone.DecidedUser).Should().Be((1, null, null));
            (await dbContext.GraphNode.Where(w => w.Id == a || w.Id == b).Select(s => s.ResolvedEntityId).ToListAsync())
                .Should().OnlyContain(r => r == null);
        }

        [Fact]
        public async Task ConfirmedPairsThatChainAreOneEntityAndRejectionKeepsThemApartAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var identifier = await NodeAsync(dbContext, tag + "NationalId", "CHAIN");
            var one = await NodeAsync(dbContext, tag + "Account", "X1");
            var two = await NodeAsync(dbContext, tag + "Account", "X2");
            var three = await NodeAsync(dbContext, tag + "Account", "X3");

            foreach (var node in new[] { one, two, three })
            {
                await LinkAsync(dbContext, node, identifier);
            }

            await Job().RunOnceAsync();
            var service = await ServiceAsync(dbContext, User);
            var matches = (await service.ListAsync(1, 500)).Where(m => nodeIds.Contains(m.NodeIdA)).ToList();
            matches.Should().HaveCount(3);

            foreach (var match in matches.Where(m => new[] { m.NodeIdA, m.NodeIdB }.All(n => n != three)))
            {
                await service.DecideAsync(new GraphEntityMatchDecideDto { Id = match.Id, DecisionId = 2 });
            }

            foreach (var match in matches.Where(m => new[] { m.NodeIdA, m.NodeIdB }.Contains(three)))
            {
                await service.DecideAsync(new GraphEntityMatchDecideDto { Id = match.Id, DecisionId = 3 });
            }

            var resolved = await dbContext.GraphNode.Where(w => w.Id == one || w.Id == two || w.Id == three)
                .ToDictionaryAsync(k => k.Id, v => v.ResolvedEntityId);
            resolved[one].Should().Be(Math.Min(one, two));
            resolved[two].Should().Be(Math.Min(one, two));
            resolved[three].Should().BeNull();
        }

        [Fact]
        public async Task HardCandidatesAreConfirmedAutomaticallyOnlyWhenSwitchedOnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var identifier = await NodeAsync(dbContext, tag + "NationalId", "AUTO");
            var a = await NodeAsync(dbContext, tag + "Account", "AU1");
            var b = await NodeAsync(dbContext, tag + "Account", "AU2");
            await LinkAsync(dbContext, a, identifier);
            await LinkAsync(dbContext, b, identifier);

            var result = await Job(auto: true).RunOnceAsync();

            result.AutoConfirmed.Should().BeGreaterThan(0);
            var match = await dbContext.GraphEntityMatch.SingleAsync(w => w.NodeIdA == Math.Min(a, b));
            (match.DecisionId, match.DecidedUser).Should().Be(((short)2, "System"));
            (await dbContext.GraphNode.Where(w => w.Id == a).Select(s => s.ResolvedEntityId).SingleAsync())
                .Should().Be(Math.Min(a, b));
        }

        [Fact]
        public async Task FuzzyCandidatesComeOnlyFromTheConfiguredKeysAndNeverAutoConfirmAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var name = tag + "Name";
            var one = await NodeAsync(dbContext, name, "John A. Smith");
            var two = await NodeAsync(dbContext, name, "JOHN A SMITH");
            var three = await NodeAsync(dbContext, name, "Maria Garcia");
            var other = await NodeAsync(dbContext, tag + "Other", "John A Smith");

            await Job(auto: true).RunOnceAsync();
            (await dbContext.GraphEntityMatch.CountAsync(w => nodeIds.Contains(w.NodeIdA))).Should().Be(0);

            await Job(auto: true, fuzzy: name).RunOnceAsync();

            var match = await dbContext.GraphEntityMatch.SingleAsync(w => nodeIds.Contains(w.NodeIdA));
            (match.NodeIdA, match.NodeIdB, match.SourceId, match.DecisionId)
                .Should().Be((Math.Min(one, two), Math.Max(one, two), (short)2, (short)1));
            match.Evidence.Should().Contain("Exact");
            match.Posterior.Should().BeInRange(0.2, 0.99);
            three.Should().NotBe(other);
        }

        [Fact]
        public async Task PermissionTenantUnknownAndRepeatedDecisionsAreRefusedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var (_, _, id) = await TwoAccountsAsync(dbContext, "P");
            var service = await ServiceAsync(dbContext, User);

            var without = await ServiceAsync(dbContext, fx.Seed.UserWithoutPermission);
            await ((Func<Task>)(() => without.ListAsync(null, null))).Should().ThrowAsync<CaseExceptions.ForbiddenException>();
            await ((Func<Task>)(() => without.DecideAsync(new GraphEntityMatchDecideDto { Id = id, DecisionId = 2 })))
                .Should().ThrowAsync<CaseExceptions.ForbiddenException>();

            var other = await ServiceAsync(dbContext, fx.Seed.UserTenantB);
            await ((Func<Task>)(() => other.DecideAsync(new GraphEntityMatchDecideDto { Id = id, DecisionId = 2 })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
            (await other.ListAsync(null, 500)).Select(m => m.Id).Should().NotContain(id);

            await ((Func<Task>)(() => service.DecideAsync(new GraphEntityMatchDecideDto { Id = 0, DecisionId = 2 })))
                .Should().ThrowAsync<CaseExceptions.NotFoundException>();
            await ((Func<Task>)(() => service.DecideAsync(new GraphEntityMatchDecideDto { Id = id, DecisionId = 9 })))
                .Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.DecideAsync(new GraphEntityMatchDecideDto
                { Id = id, DecisionId = 2, Note = new string('x', 501) }))).Should().ThrowAsync<ArgumentException>();
            await ((Func<Task>)(() => service.DecideAsync(new GraphEntityMatchDecideDto { Id = id, DecisionId = 1 })))
                .Should().ThrowAsync<CaseExceptions.ConflictException>();
            await ((Func<Task>)(() => service.ListAsync(7, null))).Should().ThrowAsync<ArgumentException>();

            (await service.ListForNodeAsync(nodeIds.First())).Should().NotBeNull();
        }
    }
}
