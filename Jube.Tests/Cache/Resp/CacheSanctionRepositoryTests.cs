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
using Jube.Cache.Resp;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Resp
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheSanctionRepositoryTests
    {
        [Fact]
        public async Task InsertThenGetRoundTripsValueAndCreatedDateAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, "v", "Robert Mugabe", 2, 0.87);
            var result = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "v", "Robert Mugabe", 2);

            result.Should().NotBeNull();
            result!.Value.Should().Be(0.87);
            result.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task GetOnMissingKeyReturnsNullAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);

            var result = await repository.GetByMultiPartStringDistanceThresholdAsync(1, Guid.NewGuid(),
                "v", "Nobody Here", 2);

            result.Should().BeNull();
        }

        [Fact]
        public async Task InsertUsesDocumentedSanctionKeyFormatWithSearchStringAndDistanceCompositeFieldAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(9, modelGuid, "v", "John Smith", 3, 0.5);

            var raw = await fake.HashGetAsync($"Sanction:9:{modelGuid:N}", "v:John Smith:3");
            raw.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task InsertWithNullValueRoundTripsAsNullAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, "v", "Someone", 1, null);
            var result = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "v", "Someone", 1);

            result.Should().NotBeNull();
            result!.Value.Should().BeNull();
        }

        [Fact]
        public async Task DifferentDistanceThresholdsForSameStringAreStoredIndependentlyAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, "v", "Jane Doe", 1, 0.1);
            await repository.InsertAsync(1, modelGuid, "v", "Jane Doe", 2, 0.2);

            var distanceOne = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "v", "Jane Doe", 1);
            var distanceTwo = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "v", "Jane Doe", 2);

            distanceOne!.Value.Should().Be(0.1);
            distanceTwo!.Value.Should().Be(0.2);
        }

        [Fact]
        public async Task DifferentVariantsForSameStringAndDistanceAreStoredIndependentlyAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, "a", "Jane Doe", 1, 0.1);
            await repository.InsertAsync(1, modelGuid, "b", "Jane Doe", 1, 0.2);

            var a = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "a", "Jane Doe", 1);
            var b = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "b", "Jane Doe", 1);
            var c = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "c", "Jane Doe", 1);

            a!.Value.Should().Be(0.1);
            b!.Value.Should().Be(0.2);
            c.Should().BeNull();
        }

        [Fact]
        public async Task TheMatchDetailJsonRoundTripsWithTheValueAsync()
        {
            var fake = new FakeHybridResilientRespDatabase();
            var repository = new CacheSanctionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.InsertAsync(1, modelGuid, "v", "Jane Doe", 1, 0.5, "[{\"Id\":1}]");
            var withDetail = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "v", "Jane Doe", 1);

            await repository.InsertAsync(1, modelGuid, "v", "John Doe", 1, 0.5);
            var without = await repository.GetByMultiPartStringDistanceThresholdAsync(1, modelGuid, "v", "John Doe", 1);

            withDetail!.MatchesJson.Should().Be("[{\"Id\":1}]");
            without!.MatchesJson.Should().BeNull();
        }
    }
}