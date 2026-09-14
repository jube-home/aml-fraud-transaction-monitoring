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
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheReferenceDateRepositoryTests
    {
        [Fact]
        public async Task UpsertThenGetReferenceDateRoundTripsToMillisecondPrecisionAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheReferenceDateRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();
            var referenceDate = new DateTime(2024, 6, 15, 10, 30, 45, 123, DateTimeKind.Utc);

            await repository.UpsertReferenceDateAsync(1, modelGuid, referenceDate);
            var result = await repository.GetReferenceDateAsync(1, modelGuid);

            result.Should().Be(referenceDate);
        }

        [Fact]
        public async Task UpsertUsesDocumentedReferenceDateKeyFormatAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheReferenceDateRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertReferenceDateAsync(3, modelGuid, DateTime.UtcNow);

            var raw = await fake.HashGetAsync("ReferenceDate:3", $"{modelGuid:N}");
            raw.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task GetReferenceDateOnGenuineCacheMissReturnsNullAsync()
        {
            var log = new TestLog();
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheReferenceDateRepository(fake, log);

            var result = await repository.GetReferenceDateAsync(1, Guid.NewGuid());

            result.Should().BeNull();
            log.Entries.Should().NotContain(e => e.Level == "ERROR");
        }

        [Fact]
        public async Task GetReferenceDatePreferReplicaOnGenuineCacheMissReturnsNullAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheReferenceDateRepository(fake, TestLog.NoOp);

            var result = await repository.GetReferenceDatePreferReplicaAsync(1, Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpsertOverwritesPreviousReferenceDateForSameModelAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheReferenceDateRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertReferenceDateAsync(1, modelGuid,
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            await repository.UpsertReferenceDateAsync(1, modelGuid,
                new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            var result = await repository.GetReferenceDateAsync(1, modelGuid);

            result.Should().Be(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}