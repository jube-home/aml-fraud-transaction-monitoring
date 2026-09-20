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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Data.Context;
using Jube.Service.Exceptions.RedisCallCounter;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.RedisCallCounter;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.RedisCallCounter
{
    using RedisCallCounterService = RedisCallCounterService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class RedisCallCounterServiceTests(DatabaseFixture fx) : IAsyncLifetime
    {
        private static readonly IStringLocalizerFactory localizers =
            new ResourceManagerStringLocalizerFactory(Options.Create(new LocalizationOptions()),
                NullLoggerFactory.Instance);

        private readonly List<int> createdIds = [];

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await using var dbContext = fx.GetDbContext();

            foreach (var id in createdIds)
            {
                await dbContext.RedisCallCounter.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<RedisCallCounterService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return RedisCallCounterService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task<int> CreateCounterAsync(DbContext dbContext, string call, long count = 5,
            long totalMicroseconds = 5000, long minMicroseconds = 500, long maxMicroseconds = 1500,
            DateTime? createdDate = null)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.RedisCallCounter
            {
                Call = call,
                Count = count,
                TotalMicroseconds = totalMicroseconds,
                MinMicroseconds = minMicroseconds,
                MaxMicroseconds = maxMicroseconds,
                CreatedDate = createdDate ?? DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
            return id;
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.InsertAsync";

            var firstId = await CreateCounterAsync(dbContext, call, 3, 3000,
                800, 1200);
            var secondId = await CreateCounterAsync(dbContext, call, 7, 9000,
                900, 1600);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Call == call).ToList();
            mine.Should().HaveCount(2);
            mine[0].Id.Should().Be(secondId);
            mine[1].Id.Should().Be(firstId);
            mine[0].Count.Should().Be(7);
            mine[0].TotalMicroseconds.Should().Be(9000);
            mine[0].MinMicroseconds.Should().Be(900);
            mine[0].MaxMicroseconds.Should().Be(1600);
        }

        [Fact]
        public async Task ListClampsTakeToOneHundredThousandAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var result = await service.ListAsync(500000);

            result.Rows.Count.Should().BeLessOrEqualTo(100000);
        }

        [Fact]
        public async Task ListWithoutPermissionThrowsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithoutPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task NonLandlordHoldingEverySpecificationIsForbiddenAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var service = await BuildServiceAsync(dbContext, fx.Seed.UserWithPermission);

            await Assert.ThrowsAsync<ForbiddenException>(() => service.ListAsync());
        }

        [Fact]
        public async Task CreateWithNullUserNameThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() => BuildServiceAsync(dbContext, null));
        }

        [Fact]
        public async Task CreateWithUnknownTenantThrowsNotAuthenticatedAsync()
        {
            await using var dbContext = fx.GetDbContext();

            await Assert.ThrowsAsync<NotAuthenticatedException>(() =>
                BuildServiceAsync(dbContext, fx.Seed.UnknownUser));
        }

        [Fact]
        public async Task ListFiltersByDateRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateCounterAsync(dbContext, $"{call}Old", createdDate: oldDate);
            await CreateCounterAsync(dbContext, $"{call}New", createdDate: newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.Call.StartsWith(call)).ToList();
            mine.Should().ContainSingle();
            mine[0].Call.Should().Be($"{call}New");
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateCounterAsync(dbContext, $"{call}Old", createdDate: twoHoursAgo);
            await CreateCounterAsync(dbContext, $"{call}New", createdDate: justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.Call.StartsWith(call)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].Call.Should().Be($"{call}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueCall = $"UniqueRepository{Guid.NewGuid():N}.InsertAsync";

            await CreateCounterAsync(dbContext, uniqueCall);
            await CreateCounterAsync(dbContext, "OtherRepository.InsertAsync");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueCall[..12].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.Call == uniqueCall || r.Call == "OtherRepository.InsertAsync")
                .ToList();
            mine.Should().ContainSingle();
            mine[0].Call.Should().Be(uniqueCall);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";
            await CreateCounterAsync(dbContext, call);
            await CreateCounterAsync(dbContext, call);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";
            await CreateCounterAsync(dbContext, call);
            await CreateCounterAsync(dbContext, call);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";
            await CreateCounterAsync(dbContext, call);

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.Call == call);
        }

        [Fact]
        public async Task ListSortsByCountAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            await CreateCounterAsync(dbContext, call, 30);
            await CreateCounterAsync(dbContext, call, 10);
            await CreateCounterAsync(dbContext, call, 20);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: call, sortField: "count", sortDirection: "asc");
            ascending.Rows.Select(r => r.Count).Should().Equal(10, 20, 30);

            var descending = await service.ListAsync(search: call, sortField: "count", sortDirection: "desc");
            descending.Rows.Select(r => r.Count).Should().Equal(30, 20, 10);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            await CreateCounterAsync(dbContext, call, 20);
            await CreateCounterAsync(dbContext, call, 10);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call, sortField: "count",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Count).Should().Equal(10, 20);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            await CreateCounterAsync(dbContext, call, 10);
            await CreateCounterAsync(dbContext, call, 20);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call, sortField: "count", sortDirection: "banana");

            result.Rows.Select(r => r.Count).Should().Equal(20, 10);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            var firstId = await CreateCounterAsync(dbContext, call, createdDate: DateTime.UtcNow.AddMinutes(-30));
            var secondId = await CreateCounterAsync(dbContext, call, createdDate: DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call, sortField: "notARealColumn");

            result.Rows.Select(r => r.Id).Should().Equal(secondId, firstId);
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";
            for (var i = 0; i < 5; i++)
            {
                await CreateCounterAsync(dbContext, call);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: call);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForCountAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";

            foreach (var value in new long[] { 1, 2, 3, 4, 5 })
            {
                await CreateCounterAsync(dbContext, call, value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call);

            var stats = result.Statistics.Columns["count"];
            stats.Min.Should().Be(1);
            stats.Max.Should().Be(5);
            stats.Mean.Should().Be(3);
            stats.Median.Should().Be(3);
            stats.StandardDeviation.Should().BeApproximately(1.5811, 0.001);
            stats.Histogram.Sum(b => b.Frequency).Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsIncludesEveryContinuousMeasuredColumnAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var call = $"{DatabaseFixture.Prefix}Repository{Guid.NewGuid():N}.Method";
            await CreateCounterAsync(dbContext, call);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: call);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo(
                "count", "totalMicroseconds", "minMicroseconds", "maxMicroseconds");
        }
    }
}