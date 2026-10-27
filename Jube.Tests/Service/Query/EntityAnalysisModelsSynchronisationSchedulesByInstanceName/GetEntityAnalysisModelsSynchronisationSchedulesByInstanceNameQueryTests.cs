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
using Jube.Data.Query.GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery;
using Jube.Data.Repository;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using Xunit;
using ModelPoco = Jube.Data.Poco.EntityAnalysisModel;

namespace Jube.Test.Service.Query.EntityAnalysisModelsSynchronisationSchedulesByInstanceName
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryTests(DatabaseFixture fx)
        : IAsyncLifetime
    {
        private readonly List<int> scheduleIds = [];
        private readonly List<int> nodeIds = [];
        private string instance = string.Empty;
        private int tenantRegistryId;
        private DateTime now;

        public async Task InitializeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            tenantRegistryId = await dbContext.UserInTenant.Where(w => w.User == fx.Seed.UserWithPermission)
                .Select(s => s.TenantRegistryId).FirstAsync();

            instance = $"{DatabaseFixture.Prefix}Node{Guid.NewGuid():N}"[..40];
            now = DateTime.UtcNow;

            nodeIds.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelSynchronisationNodeStatusEntry
            {
                Instance = instance,
                TenantRegistryId = tenantRegistryId,
                HeartbeatDate = now,
                SynchronisedDate = now.AddMinutes(-1)
            }));
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in scheduleIds)
            {
                await dbContext.EntityAnalysisModelSynchronisationSchedule.Where(w => w.Id == id).DeleteAsync();
            }

            foreach (var id in nodeIds)
            {
                await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry.Where(w => w.Id == id)
                    .DeleteAsync();
            }
        }

        private async Task<int> NewModelAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var model = await new EntityAnalysisModelRepository(dbContext, fx.Seed.UserWithPermission).InsertAsync(
                new ModelPoco
                {
                    Name = $"{DatabaseFixture.Prefix}Sm{Guid.NewGuid():N}"[..40], Guid = Guid.NewGuid(), Active = 1,
                    Locked = 0, Deleted = 0
                });

            return model.Id;
        }

        private async Task ScheduleAsync(DateTime when, int? modelId = null)
        {
            await using var dbContext = fx.GetDbContext();

            scheduleIds.Add(await dbContext.InsertWithInt32IdentityAsync(new EntityAnalysisModelSynchronisationSchedule
            {
                CreatedDate = now,
                ScheduleDate = when,
                TenantRegistryId = tenantRegistryId,
                CreatedUser = fx.Seed.UserWithPermission,
                EntityAnalysisModelId = modelId
            }));
        }

        private async Task<GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQueryDto> OursAsync()
        {
            await using var dbContext = fx.GetDbContext();

            var all = await new GetEntityAnalysisModelsSynchronisationSchedulesByInstanceNameQuery(dbContext)
                .ExecuteAsync(instance);

            return all.Single(s => s.TenantRegistryId == tenantRegistryId);
        }

        [Fact]
        public async Task AWholeTenantRowThatIsDueIsPendingForEveryModelAsync()
        {
            await ScheduleAsync(now.AddSeconds(-30));

            var result = await OursAsync();

            result.SynchronisationPending.Should().BeTrue();
            result.EntityAnalysisModelIds.Should().BeNull("a row with no model asks for every model in the tenant");
        }

        [Fact]
        public async Task ModelRowsThatAreDueNameOnlyThoseModelsAndEachOnceAsync()
        {
            var first = await NewModelAsync();
            var second = await NewModelAsync();
            await ScheduleAsync(now.AddSeconds(-30), first);
            await ScheduleAsync(now.AddSeconds(-20), second);
            await ScheduleAsync(now.AddSeconds(-10), first);

            var result = await OursAsync();

            result.SynchronisationPending.Should().BeTrue();
            result.EntityAnalysisModelIds.Should().BeEquivalentTo(new[] { first, second });
        }

        [Fact]
        public async Task AWholeTenantRowAlongsideAModelRowWidensTheSyncToEveryModelAsync()
        {
            var model = await NewModelAsync();
            await ScheduleAsync(now.AddSeconds(-30), model);
            await ScheduleAsync(now.AddSeconds(-20));

            var result = await OursAsync();

            result.SynchronisationPending.Should().BeTrue();
            result.EntityAnalysisModelIds.Should().BeNull(
                "a model row cannot hide a whole tenant row that has not been honoured yet");
        }

        [Fact]
        public async Task ARowOlderThanTheNodesLastSynchronisationIsNotPendingAsync()
        {
            var model = await NewModelAsync();
            await ScheduleAsync(now.AddMinutes(-2), model);

            var result = await OursAsync();

            result.SynchronisationPending.Should().BeFalse();
            result.EntityAnalysisModelIds.Should().BeNull();
        }

        [Fact]
        public async Task ARowThatIsNotYetDueIsNotPendingAsync()
        {
            var model = await NewModelAsync();
            await ScheduleAsync(now.AddMinutes(10), model);

            var result = await OursAsync();

            result.SynchronisationPending.Should().BeFalse();
        }

        [Fact]
        public async Task ANodeThatHasNeverSynchronisedHonoursEveryRowThatIsDueAsync()
        {
            await using (var dbContext = fx.GetDbContext())
            {
                await dbContext.EntityAnalysisModelSynchronisationNodeStatusEntry
                    .Where(w => w.Instance == instance).DeleteAsync();
            }

            var model = await NewModelAsync();
            await ScheduleAsync(now.AddMinutes(-5), model);

            var result = await OursAsync();

            result.SynchronisationPending.Should().BeTrue();
            result.EntityAnalysisModelIds.Should().Equal(model);
        }
    }
}
