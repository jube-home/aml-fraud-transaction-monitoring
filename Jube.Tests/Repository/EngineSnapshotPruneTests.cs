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
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;

namespace Jube.Test.Repository
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EngineSnapshotPruneTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private readonly List<string> instances = [];
        private readonly List<int> modelIds = [];
        private int tenantRegistryId;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            tenantRegistryId = await dbContext.UserInTenant
                .Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId)
                .FirstAsync();
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await dbContext.EntityAnalysisModelEngineSnapshot
                .Where(w => instances.Contains(w.Instance)).DeleteAsync();
            await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                .Where(w => instances.Contains(w.Instance)).DeleteAsync();
            await dbContext.GetTable<EntityAnalysisModelVersion>()
                .Where(w => modelIds.Contains(w.EntityAnalysisModelId)).DeleteAsync();
            await dbContext.EntityAnalysisModel
                .Where(w => modelIds.Contains(w.Id)).DeleteAsync();
        }

        private async Task<int> SeedModelAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = new EntityAnalysisModel
            {
                Name = $"{DatabaseFixture.Prefix}Prune{Guid.NewGuid():N}"[..40],
                Guid = Guid.NewGuid(),
                TenantRegistryId = tenantRegistryId,
                Active = 1,
                Locked = 0,
                Deleted = 0,
                Version = 1,
                CreatedDate = DateTime.UtcNow,
                CreatedUser = DatabaseFixture.Prefix
            };

            model.Id = await dbContext.InsertWithInt32IdentityAsync(model);
            modelIds.Add(model.Id);
            return model.Id;
        }

        private async Task SeedNodeAsync(string instance, DateTime heartbeat)
        {
            await using var dbContext = fx.GetDbContext();

            instances.Add(instance);
            await dbContext.InsertAsync(new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                Instance = instance,
                HeartbeatDate = heartbeat,
                TenantRegistryId = tenantRegistryId
            });
        }

        [Fact]
        public async Task ASnapshotOfAnInstanceThatStoppedReportingIsRemovedButALiveOneIsKeptAsync()
        {
            var modelId = await SeedModelAsync();
            var now = DateTime.UtcNow;
            var live = $"{DatabaseFixture.Prefix}Live{Guid.NewGuid():N}";
            var dead = $"{DatabaseFixture.Prefix}Dead{Guid.NewGuid():N}";
            await SeedNodeAsync(live, now);
            await SeedNodeAsync(dead, now.AddMinutes(-10));

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelEngineSnapshotRepository(dbContext);
            await repository.UpsertAsync(live, tenantRegistryId, modelId, "{}");
            await repository.UpsertAsync(dead, tenantRegistryId, modelId, "{}");

            await repository.DeleteStaleAsync(tenantRegistryId, "ZzNotThisInstance", now);

            var remaining = await dbContext.EntityAnalysisModelEngineSnapshot
                .Where(w => w.EntityAnalysisModelId == modelId)
                .Select(s => s.Instance)
                .ToListAsync();

            remaining.Should().Contain(live).And.NotContain(dead);
        }

        [Fact]
        public async Task TheInstanceDoingThePruneIsNeverRemovedEvenWithoutAFreshHeartbeatAsync()
        {
            var modelId = await SeedModelAsync();
            var now = DateTime.UtcNow;
            var self = $"{DatabaseFixture.Prefix}Self{Guid.NewGuid():N}";

            await using var dbContext = fx.GetDbContext();
            var repository = new EntityAnalysisModelEngineSnapshotRepository(dbContext);
            instances.Add(self);
            await repository.UpsertAsync(self, tenantRegistryId, modelId, "{}");

            await repository.DeleteStaleAsync(tenantRegistryId, self, now);

            var remaining = await dbContext.EntityAnalysisModelEngineSnapshot
                .Where(w => w.EntityAnalysisModelId == modelId)
                .Select(s => s.Instance)
                .ToListAsync();

            remaining.Should().ContainSingle().Which.Should().Be(self);
        }

        [Fact]
        public async Task ASyncRemovesTheNodeRowOfAnInstanceThatStoppedReportingButKeepsLiveOnesAsync()
        {
            var now = DateTime.UtcNow;
            var self = $"{DatabaseFixture.Prefix}NodeSelf{Guid.NewGuid():N}";
            var live = $"{DatabaseFixture.Prefix}NodeLive{Guid.NewGuid():N}";
            var dead = $"{DatabaseFixture.Prefix}NodeDead{Guid.NewGuid():N}";
            await SeedNodeAsync(self, now.AddMinutes(-10));
            await SeedNodeAsync(live, now.AddMinutes(-1));
            await SeedNodeAsync(dead, now.AddMinutes(-10));

            await using var dbContext = fx.GetDbContext();
            await new EntityAnalysisModelSynchronisationNodeStatusEntryRepository(dbContext)
                .DeleteSilentAsync(tenantRegistryId, self, now);

            var remaining = await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                .Where(w => instances.Contains(w.Instance))
                .Select(s => s.Instance)
                .ToListAsync();

            remaining.Should().Contain([self, live]).And.NotContain(dead);
        }
    }
}