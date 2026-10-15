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
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;
using Xunit.Abstractions;

namespace Jube.Test.Exhaustive
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RecallTimelineTests(ModelScaffoldFixture fixture, ITestOutputHelper output)
        : IClassFixture<ModelScaffoldFixture>
    {
        [Fact]
        public async Task PromotedModelBecomesRecallableWhileTheEngineIsRunningAsync()
        {
            await using var scaffold = await fixture.CreateIsolatedAsync();

            await using var db = DataConnectionDbContext.GetResilientDbContextDataConnection(
                ModelScaffold.ConnectionString, TestLog.NoOp);

            var seed = await PromotedModelSeed.InsertAsync(db, scaffold);
            var instanceGuid = seed.InstanceGuid;

            var clock = Stopwatch.StartNew();
            var deadline = TimeSpan.FromSeconds(30);
            long? loadedAt = null;
            long? recallAt = null;
            var lastRecall = "";

            while (clock.Elapsed < deadline && recallAt == null)
            {
                var model = fixture.Engine.FindActiveModel(scaffold.ModelGuid);

                if (model is { Snapshot.ExhaustiveModels.Count: > 0 })
                {
                    loadedAt ??= clock.ElapsedMilliseconds;

                    var response = await fixture.Api.SendAsync("POST",
                        "/api/Invoke/ExhaustiveSearchInstance/" + instanceGuid,
                        scaffold.UserName, Encoding.UTF8.GetBytes("{\"Field0\":1,\"Field1\":1}"), "application/json");

                    lastRecall = $"{response.Status}:{response.Body}";

                    if (response.Status == 200 && response.Body != "0")
                    {
                        recallAt = clock.ElapsedMilliseconds;
                    }
                }

                await Task.Delay(50);
            }

            output.WriteLine($"TIMELINE loadedAtMs={loadedAt} recallWorksAtMs={recallAt} lastRecall={lastRecall}");
            Assert.NotNull(recallAt);

            await seed.DeleteAsync();
        }
    }
}