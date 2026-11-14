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
using System.Threading.Tasks;
using Jube.Test.Infrastructure.ConnectionLeak;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using LinqToDB;
using LinqToDB.Async;
using Xunit;

namespace Jube.Test.ConnectionLeak
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class EngineHostConnectionLeakTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int HostPoolSize = 30;
        private readonly string isolated = PoolHeadroom.CreateIsolatedConnectionString(fx.ConnectionString, HostPoolSize);
        private ModelEngineHost? host;

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            if (host != null)
            {
                await host.DisposeAsync();
            }

            PoolHeadroom.ClearPool(isolated);
        }

        [Fact]
        public async Task AnEngineHostThatHasRunItsBackgroundTasksAndIsDisposedLeavesEveryPooledConnectionReturnedAsync()
        {
            var startedAt = DateTime.UtcNow.AddSeconds(-1);

            host = await ModelEngineHost.StartAsync(new Dictionary<string, string>
            {
                ["ConnectionString"] = isolated,
                ["ReportConnectionString"] = isolated
            });

            await WaitForLocalCacheInstanceAsync(startedAt);

            await host.DisposeAsync();
            host = null;

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "A started and disposed engine host");
        }

        private async Task WaitForLocalCacheInstanceAsync(DateTime startedAt)
        {
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                await using var dbContext = fx.GetDbContext();

                if (await dbContext.LocalCacheInstance.AnyAsync(w => w.CreatedDate >= startedAt))
                {
                    return;
                }

                await Task.Delay(200);
            }
        }
    }
}
