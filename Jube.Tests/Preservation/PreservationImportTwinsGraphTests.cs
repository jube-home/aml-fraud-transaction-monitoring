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
using Jube.Service.Query.Graph.Jobs;
using Jube.Service.Query.Graph.Jobs.Models;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Service.Query.Graph;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.Preservation
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class PreservationImportTwinsGraphTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly GraphJobsSeed seed = new(fx);
        private readonly List<Guid> deadRegistryGuids = [];

        public Task InitializeAsync() => Task.CompletedTask;

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await dbContext.GraphRegistryRole.Where(r => deadRegistryGuids.Contains(r.GraphRegistryGuid)).DeleteAsync();
            await dbContext.GraphRegistry.Where(r => deadRegistryGuids.Contains(r.Guid)).DeleteAsync();
            await seed.CleanupAsync(dbContext);
        }

        private async Task<List<(byte Restricted, float Weight)>> DeviceEdgesAsync(DbContext dbContext) =>
            (await dbContext.GraphEdge.Where(e => e.GraphRegistryEdgeGuid == seed.DeviceDefinition)
                .OrderBy(e => e.Id).ToListAsync()).Select(e => (e.Restricted, e.Weight)).ToList();

        [Fact]
        public async Task TheHubJobRestrictsAndWeighsEdgesFromTheLiveDefinitionAndNeverItsSoftDeletedTwinAsync()
        {
            await using var dbContext = fx.GetDbContext();
            await seed.SetupAsync(dbContext);
            var now = DateTime.UtcNow;

            await seed.TransactionAsync(dbContext, now.AddDays(-3), "A1", null, null, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-2), "A2", null, null, "D1");
            await seed.TransactionAsync(dbContext, now.AddDays(-1), "A3", null, null, "D1");

            var definitions = (await new GraphRegistryEdgeRepository(dbContext, seed.TenantRegistryId)
                    .GetActiveByGraphRegistryGuidAsync(seed.Registry))
                .Select(d => new GraphBuildDefinition(d.Guid, d.KindId!.Value, d.HalfLifeDays ?? 90, d.KindPrior ?? 1,
                    d.Restricted ?? 0)).ToList();

            await new GraphBuildJob(fx.ConnectionString, TestLog.NoOp, 5000, 50, 365, 1000, false)
                .BuildTenantAsync(dbContext, seed.TenantRegistryId, definitions);

            var hubs = new GraphHubJob(fx.ConnectionString, TestLog.NoOp, 0.999, 50);
            await hubs.RunTenantAsync(dbContext, seed.TenantRegistryId);
            var control = await DeviceEdgesAsync(dbContext);
            control.Should().NotBeEmpty();
            control.Should().OnlyContain(e => e.Restricted == 0);

            var live = await dbContext.GraphRegistryEdge.FirstAsync(d => d.Guid == seed.DeviceDefinition);

            await dbContext.InsertWithInt32IdentityAsync(new GraphRegistryEdge
            {
                GraphRegistryId = live.GraphRegistryId,
                Guid = live.Guid,
                Name = "Dead twin",
                KindId = live.KindId,
                FromKey = live.FromKey,
                ToKey = live.ToKey,
                HalfLifeDays = live.HalfLifeDays,
                KindPrior = 50,
                MinimumAmount = 0,
                Hard = 0,
                Restricted = 1,
                Active = 0,
                Locked = 0,
                Deleted = 1,
                CreatedUser = seed.Me,
                CreatedDate = DateTime.UtcNow
            });

            await dbContext.GraphRegistryEdge.Where(d => d.Id == live.Id).Set(d => d.Name, live.Name).UpdateAsync();

            await hubs.RunTenantAsync(dbContext, seed.TenantRegistryId);
            var after = await DeviceEdgesAsync(dbContext);

            after.Should().OnlyContain(e => e.Restricted == 0,
                "the dead twin is restricted and must not be the definition the sync reads");
            after.Select(e => e.Weight).Should().BeEquivalentTo(control.Select(e => e.Weight),
                options => options.Using<float>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-4f))
                    .WhenTypeIs<float>(),
                "the dead twin carries a kind prior of 50, which would inflate every weight");
        }

        private async Task<Guid> DeadRegistryAsync(DbContext dbContext)
        {
            var guid = Guid.NewGuid();
            deadRegistryGuids.Add(guid);

            var role = await dbContext.UserRegistry.Where(u => u.Name == seed.Me).Select(u => u.RoleRegistryGuid)
                .FirstAsync();

            await dbContext.InsertWithInt32IdentityAsync(new GraphRegistry
            {
                Guid = guid,
                TenantRegistryId = await dbContext.UserInTenant.Where(w => w.User == seed.Me)
                    .Select(w => w.TenantRegistryId).FirstAsync(),
                Name = $"{DatabaseFixture.Prefix}DeadGraph{Guid.NewGuid():N}"[..40],
                Active = 1,
                Locked = 0,
                Deleted = 1,
                ContextKindId = 1,
                DefaultHops = 2,
                DefaultWindowDays = 30,
                RenderCap = 150,
                HubPolicyId = 1,
                CreatedUser = seed.Me,
                CreatedDate = DateTime.UtcNow
            });

            await dbContext.InsertAsync(new GraphRegistryRole
            {
                GraphRegistryGuid = guid,
                Guid = Guid.NewGuid(),
                RoleRegistryGuid = role,
                CreatedUser = seed.Me,
                CreatedDate = DateTime.UtcNow,
                Deleted = 0,
                Version = 1
            });

            return guid;
        }

        [Fact]
        public async Task ASoftDeletedGraphRegistryIsNeverGrantedOrReturnedAsActiveAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var guid = await DeadRegistryAsync(dbContext);
            var repository = new GraphRegistryRepository(dbContext, seed.Me);

            (await repository.IsGrantedAsync(guid)).Should().BeFalse();
            (await repository.GetByGuidActiveOnlyAsync(guid)).Should().BeNull();
            (await repository.ExistsByGuidAsync(guid)).Should().BeFalse();
        }
    }
}
