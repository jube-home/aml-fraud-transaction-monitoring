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
using FluentAssertions;
using Jube.Engine.BackgroundTasks.TaskStarters;
using Jube.Engine.BackgroundTasks.TaskStarters.Models;
using Jube.TaskCancellation;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ConnectionLeak;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;

namespace Jube.Test.ConnectionLeak
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class TaggingStarterConnectionLeakTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private const int TagCount = 20;
        private readonly string isolated = PoolHeadroom.CreateIsolatedConnectionString(fx.ConnectionString, 4);

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
        public async Task ProcessingManyTagsOpensAContextPerTagAndNeverHoldsAPooledConnectionAsync()
        {
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

            for (var i = 0; i < TagCount; i++)
            {
                context.ConcurrentQueues.PendingTagging.Enqueue(new TagMessage
                {
                    Tag = ["leak-guard"],
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    UserName = "leak-guard"
                });
            }

            var run = new TaggingStarter(context).StartAsync();

            var deadline = DateTime.UtcNow.AddSeconds(30);

            while (!context.ConcurrentQueues.PendingTagging.IsEmpty && DateTime.UtcNow < deadline)
            {
                await Task.Delay(100);
            }

            context.ConcurrentQueues.PendingTagging.IsEmpty.Should().BeTrue("the starter must drain the queue");

            cancellation.Cancel();

            try
            {
                await run.WaitAsync(TimeSpan.FromSeconds(30));
            }
            catch (OperationCanceledException)
            {
            }

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated, "TaggingStarter");
        }
    }
}
