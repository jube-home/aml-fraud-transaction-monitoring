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
using System.Threading.Tasks;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using Npgsql;
using Xunit;

namespace Jube.Test.Service.ModelScaffolding
{
    [Collection("Database")]
    public sealed class ModelSyncTests(ModelScaffoldFixture fixture) : IClassFixture<ModelScaffoldFixture>
    {
        [Fact]
        public void SharedModel_IsSynchronisedByTheRealEngine_AndListsItsCaller()
        {
            var model = fixture.Engine.FindActiveModel(fixture.Shared.ModelGuid);
            model = model.Required();

            Assert.NotNull(model);
            Assert.Equal(fixture.Shared.ModelGuid, model.Instance.Guid);
            Assert.Equal(fixture.Shared.ModelId, model.Instance.Id);
            Assert.Contains(fixture.Shared.UserName, model.Collections.Users);
            Assert.DoesNotContain(fixture.Shared.OtherUserName, model.Collections.Users);
            Assert.NotNull(fixture.SharedSync.SynchronisedDate);
            Assert.Equal(0, fixture.SharedSync.ErrorRowsAdded);
            Assert.Contains(fixture.SharedSync.ScheduleId, fixture.Shared.ScheduleIds);
        }

        [Fact]
        public async Task IsolatedCopies_SynchroniseAlongsideTheSharedModelAsync()
        {
            await using var one = await fixture.CreateIsolatedAsync();
            await using var two = await fixture.CreateIsolatedAsync();

            foreach (var scaffold in new[] { fixture.Shared, one, two })
            {
                var model = fixture.Engine.FindActiveModel(scaffold.ModelGuid);
                Assert.NotNull(model);
                Assert.Contains(scaffold.UserName, model.Required().Collections.Users);
            }

            Assert.NotEqual(one.ModelGuid, fixture.Shared.ModelGuid);
            Assert.DoesNotContain(one.UserName,
                fixture.Engine.FindActiveModel(two.ModelGuid).Required().Collections.Users);
        }

        [Fact]
        public async Task ChangingAnIsolatedCopy_ThenResynchronising_IsPickedUpByTheEngineAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();
            var renamed = scaffold.ModelName + "Renamed";

            await using (var connection = new NpgsqlConnection(ModelScaffold.ConnectionString))
            {
                await connection.OpenAsync();
                await using var update = new NpgsqlCommand(
                    "UPDATE \"EntityAnalysisModel\" SET \"Name\" = @name WHERE \"Id\" = @id", connection);
                update.Parameters.AddWithValue("name", renamed);
                update.Parameters.AddWithValue("id", scaffold.ModelId);
                await update.ExecuteNonQueryAsync();
            }

            var result = await ModelSync.SyncAsync(fixture.Engine, scaffold);

            Assert.Equal(renamed, fixture.Engine.FindActiveModel(scaffold.ModelGuid).Required().Instance.Name);
            Assert.True(result.Elapsed < TimeSpan.FromSeconds(60));
            Assert.True(scaffold.ScheduleIds.Count >= 2,
                "each SyncAsync inserts a schedule row and tracks it for clean-up");
        }

        [Fact]
        public async Task SyncAsync_TimesOutSayingWhatItWasWaitingOnAsync()
        {
            await using var scaffold = await ModelScaffold.CreateAsync(ModelScaffoldOptions.Isolated);

            var ex = await Assert.ThrowsAsync<ModelSyncTimeoutException>(() =>
                ModelSync.SyncAsync(fixture.Engine, scaffold, TimeSpan.Zero));

            Assert.Contains("did not report", ex.Message);
            Assert.Contains($"tenant {scaffold.TenantRegistryId}", ex.Message);
            Assert.Contains("Schedule row", ex.Message);
            Assert.Contains("does not hold model " + scaffold.ModelGuid, ex.Message);
            Assert.Contains("synchronisation error row(s)", ex.Message);
        }
    }
}