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
using Jube.HttpHeaders;
using Jube.Migrations.Branches.GitHubIssueBranch139;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.ConnectionLeak;
using Jube.Test.Infrastructure.DatabaseFixture;
using Jube.Test.Infrastructure.ModelScaffolding;
using Xunit;

namespace Jube.Test.ConnectionLeak
{
    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class ShortLivedContextConnectionLeakTests(DatabaseFixture fx) : IAsyncLifetime
    {
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
        public async Task ReadingTheHttpHeadersManyTimesNeverHoldsAPooledConnectionAsync()
        {
            for (var i = 0; i < 20; i++)
            {
                var headers = new HttpHeadersFromDatabase(isolated, TestLog.NoOp);

                headers.HeaderValues.Should().NotBeNull();
            }

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated, "HttpHeadersFromDatabase");
        }

        [Fact]
        public async Task TheRenameLatestCountMigrationNeverHoldsAPooledConnectionAsync()
        {
            var cacheService = TestCacheService.Create(out _);

            var environment = ModelEngineHost.CreateEnvironment(new Dictionary<string, string>
            {
                ["ConnectionString"] = isolated
            });

            try
            {
                for (var i = 0; i < 10; i++)
                {
                    new RenameLatestCountInRedisToReferenceDateLatest(cacheService, environment, TestLog.NoOp).Up();
                }
            }
            finally
            {
                cacheService.CachePayloadRepository.Dispose();
            }

            await PoolHeadroom.AssertEveryConnectionIsBackInThePoolAsync(isolated,
                "RenameLatestCountInRedisToReferenceDateLatest");
        }
    }
}
