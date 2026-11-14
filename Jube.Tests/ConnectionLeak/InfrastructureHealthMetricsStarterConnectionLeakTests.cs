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
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.BackgroundTasks.TaskStarters;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
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
    public sealed class InfrastructureHealthMetricsStarterConnectionLeakTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int StarterPoolSize = 30;

        private readonly string isolated =
            PoolHeadroom.CreateIsolatedConnectionString(fx.ConnectionString, StarterPoolSize);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            PoolHeadroom.ClearPool(isolated);

            return Task.CompletedTask;
        }

        [Fact]
        public async Task AStartedAndCancelledStarterDisposesItsSamplersAndLeavesEveryPooledConnectionReturnedAsync()
        {
            var startedAt = DateTime.UtcNow.AddSeconds(-1);
            var cancellation = new CancellationTokenProvider();
            var log = new TestLog(false);

            var context = new Jube.Engine.BackgroundTasks.Context.Context();
            context.Services.Log = log;
            context.Services.TaskCoordinator = new TaskCoordinator(cancellation, log);
            context.Services.DynamicEnvironment = ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
            {
                ["ConnectionString"] = isolated,
                ["ReportConnectionString"] = isolated
            });

            var run = new InfrastructureHealthMetricsStarter(context).StartAsync();

            await WaitForFirstCycleAsync(startedAt);

            cancellation.Cancel();

            await run.WaitAsync(TimeSpan.FromSeconds(60));

            run.IsCompletedSuccessfully.Should().BeTrue("the starter must exit cleanly once cancelled");

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "A started and cancelled InfrastructureHealthMetricsStarter");
        }

        private async Task WaitForFirstCycleAsync(DateTime startedAt)
        {
            var instance = Dns.GetHostName();
            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (DateTime.UtcNow < deadline)
            {
                await using var dbContext = fx.GetDbContext();

                if (await dbContext.PostgresMetric.AnyAsync(w => w.Instance == instance && w.CreatedDate >= startedAt))
                {
                    return;
                }

                await Task.Delay(200);
            }
        }
    }
}
