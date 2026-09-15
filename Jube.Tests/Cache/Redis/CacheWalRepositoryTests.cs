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
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.ResilientRedisConnection;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheWalRepositoryTests
    {
        [Fact]
        public async Task InsertThenGetWalSizeReflectsTheStoredEntryAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheWalRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, Guid.NewGuid(), "node-a", [1, 2, 3]);
            var size = await repository.GetWalSizeAsync(1, modelGuid, "node-a");

            size.Should().Be(1);
        }

        [Fact]
        public async Task InsertUsesDocumentedWalKeyFormatAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheWalRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();

            await repository.InsertAsync(4, modelGuid, entryGuid, "node-a", [9, 9]);

            var raw = await fake.HashGetAsync($"Wal:4:{modelGuid:N}:node-a", $"{entryGuid:N}");
            raw.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task FlushWalRemovesOnlyTheSpecifiedEntriesAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheWalRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var keep = Guid.NewGuid();
            var flush = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, keep, "node-a", [1]);
            await repository.InsertAsync(1, modelGuid, flush, "node-a", [2]);
            await repository.FlushWalAsync(1, modelGuid, "node-a", [flush]);

            var size = await repository.GetWalSizeAsync(1, modelGuid, "node-a");
            size.Should().Be(1);
        }

        [Fact]
        public async Task InsertSwallowsAndLogsOnFailureAsync()
        {
            var log = new TestLog();
            var fake = new FakeHybridResilientRedisDatabase
            {
                ThrowOnMethod = nameof(IHybridResilientRedisDatabase.HashSetAsync)
            };
            var repository = new CacheWalRepository(fake, log);

            var act = async () => await repository.InsertAsync(1, Guid.NewGuid(), Guid.NewGuid(), "node-a", [1]);

            await act.Should().NotThrowAsync();
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task FlushWalSwallowsAndLogsOnFailureAsync()
        {
            var log = new TestLog();
            var fake = new FakeHybridResilientRedisDatabase
            {
                ThrowOnMethod = nameof(IHybridResilientRedisDatabase.HashDeleteAsync)
            };
            var repository = new CacheWalRepository(fake, log);

            var act = async () => await repository.FlushWalAsync(1, Guid.NewGuid(), "node-a", [Guid.NewGuid()]);

            await act.Should().NotThrowAsync();
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Fact]
        public Task GetWalSizeDoesNotSwallowExceptionsUnlikeItsSiblingMethodsAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase
            {
                ThrowOnMethod = nameof(IHybridResilientRedisDatabase.HashLengthAsync)
            };
            var repository = new CacheWalRepository(fake, TestLog.NoOp);

            var act = async () => await repository.GetWalSizeAsync(1, Guid.NewGuid(), "node-a");

            return act.Should().ThrowAsync<InvalidOperationException>();
        }
    }
}