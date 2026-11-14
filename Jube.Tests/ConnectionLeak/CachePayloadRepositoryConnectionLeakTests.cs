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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Resp;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ConnectionLeak;
using Jube.Test.Infrastructure.DatabaseFixture;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.ConnectionLeak
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class CachePayloadRepositoryConnectionLeakTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly ConnectionMultiplexer inertMultiplexer =
            ConnectionMultiplexer.Connect("127.0.0.1:59999,abortConnect=false,connectTimeout=100,connectRetry=0");

        private readonly string isolated = PoolHeadroom.CreateIsolatedConnectionString(fx.ConnectionString);

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task DisposeAsync()
        {
            PoolHeadroom.ClearPool(isolated);

            return Task.CompletedTask;
        }

        private Task<CachePayloadRepository> CreateAsync(bool fill, CancellationToken token)
        {
            return CachePayloadRepository.CreateAsync(inertMultiplexer, new FakeHybridResilientRespDatabase(),
                isolated, TestLog.NoOp, fill, true, 10_000_000, false, false, false, TimeSpan.FromHours(1), false,
                token);
        }

        [Fact]
        public async Task CreatingTheLocalCacheInstanceReturnsItsConnectionToThePoolAsync()
        {
            using var cancellation = new CancellationTokenSource();

            using var repository = await CreateAsync(false, cancellation.Token);

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "CachePayloadRepository.CreateLocalCacheInstanceAsync");

            await cancellation.CancelAsync();
        }

        [Fact]
        public async Task FillingPreferringTheReplicaReturnsItsConnectionToThePoolAsync()
        {
            using var cancellation = new CancellationTokenSource();

            using var repository = await CreateAsync(true, cancellation.Token);

            repository.FillTask.Should().NotBeNull("a repository created with fill and a local cache starts a fill");

            await repository.FillTask.WaitAsync(TimeSpan.FromSeconds(60));

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "CachePayloadRepository.FillPreferReplicaAsync");

            await cancellation.CancelAsync();
        }

        [Fact]
        public async Task OnceTheTokenIsCancelledTheRepositoryTouchesTheDatabaseNoMoreAsync()
        {
            using var cancellation = new CancellationTokenSource();

            using var repository = await CreateAsync(false, cancellation.Token);

            await cancellation.CancelAsync();

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "A CachePayloadRepository whose token has been cancelled");

            PoolHeadroom.ClearPool(isolated);

            var counts = new List<int>();

            for (var observation = 0; observation < 10; observation++)
            {
                await Task.Delay(200);

                counts.Add(await PoolHeadroom.CountServerConnectionsAsync(fx.ConnectionString, isolated));
            }

            counts.Should().OnlyContain(c => c == 0,
                "after cancellation and clearing the pool nothing may reconnect under the leak application name");
        }

        [Fact]
        public async Task DisposingTheRepositoryTwiceIsSafeAndStopsItsTimerAsync()
        {
            var repository = await CreateAsync(false, CancellationToken.None);

            repository.Dispose();
            repository.Dispose();

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "A disposed CachePayloadRepository");
        }
    }
}
