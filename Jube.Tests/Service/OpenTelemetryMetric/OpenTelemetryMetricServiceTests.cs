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
using Jube.Service.Exceptions.OpenTelemetryMetric;
using Jube.Service.OpenTelemetryMetric;
using Jube.Service.Reactivity;
using Jube.Service.Reactivity.Interfaces;
using Jube.Test.Infrastructure;
using Jube.Test.Infrastructure.DatabaseFixture;
using LinqToDB;
using log4net;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Jube.Test.Service.OpenTelemetryMetric
{
    using OpenTelemetryMetricService = OpenTelemetryMetricService;

    [Trait("Category", "Service")]
    [Collection("Database")]
    public sealed class OpenTelemetryMetricServiceTests(DatabaseFixture fx) : IAsyncLifetime
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
                await dbContext.OpenTelemetryMetric.Where(w => w.Id == id).DeleteAsync();
            }
        }

        private static Task<OpenTelemetryMetricService> BuildServiceAsync(
            DbContext dbContext, string? userName, ILog? log = null, ILog? auditLog = null,
            IServiceChangeBus? serviceChangeBus = null)
        {
            return OpenTelemetryMetricService.CreateAsync(
                dbContext, userName, log ?? TestLog.NoOp, localizers, serviceChangeBus ?? new NullServiceChangeBus(),
                auditLog ?? TestLog.NoOp);
        }

        private async Task CreateEntryAsync(DbContext dbContext, string metricName, string tags,
            DateTime? occurredDate = null, double sum = 390.0)
        {
            var id = await dbContext.InsertWithInt32IdentityAsync(new Data.Poco.OpenTelemetryMetric
            {
                OccurredDate = occurredDate ?? DateTime.UtcNow,
                MetricName = metricName,
                InstrumentType = "Histogram",
                Tags = tags,
                Count = 20,
                Sum = sum,
                Min = 10.0,
                Max = 29.0,
                CreatedDate = DateTime.UtcNow,
                Instance = $"{DatabaseFixture.Prefix}Instance"
            }).ConfigureAwait(false);

            createdIds.Add(id);
        }

        [Fact]
        public async Task ListReturnsMostRecentFirstAndProjectsFieldsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, metricName, "stage=Gateway");
            await CreateEntryAsync(dbContext, metricName, "stage=Activation");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync();

            var mine = result.Rows.Where(r => r.MetricName == metricName).ToList();
            mine.Should().HaveCount(2);
            mine[0].Tags.Should().Be("stage=Activation");
            mine[1].Tags.Should().Be("stage=Gateway");
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
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";

            var oldDate = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var newDate = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{metricName}Old", "stage=Gateway", oldDate);
            await CreateEntryAsync(dbContext, $"{metricName}New", "stage=Gateway", newDate);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(from: newDate.AddMinutes(-1));

            var mine = result.Rows.Where(r => r.MetricName.StartsWith(metricName)).ToList();
            mine.Should().ContainSingle();
            mine[0].MetricName.Should().Be($"{metricName}New");
        }

        [Fact]
        public async Task ListFiltersBySearchAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var uniqueMetricName = $"UniqueMetric{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, uniqueMetricName, "stage=Gateway");
            await CreateEntryAsync(dbContext, "OtherMetric", "stage=Gateway");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueMetricName[..14].ToUpperInvariant());

            var mine = result.Rows.Where(r => r.MetricName == uniqueMetricName || r.MetricName == "OtherMetric")
                .ToList();
            mine.Should().ContainSingle();
            mine[0].MetricName.Should().Be(uniqueMetricName);
        }

        [Fact]
        public async Task ListFiltersByTagsAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            var uniqueTags = $"model=UniqueModel{Guid.NewGuid():N}";

            await CreateEntryAsync(dbContext, metricName, uniqueTags);
            await CreateEntryAsync(dbContext, metricName, "model=OtherModel");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: uniqueTags);

            var mine = result.Rows.Where(r => r.MetricName == metricName).ToList();
            mine.Should().ContainSingle();
            mine[0].Tags.Should().Be(uniqueTags);
        }

        [Fact]
        public async Task ListWithZeroSamplePercentageExcludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway");
            await CreateEntryAsync(dbContext, metricName, "stage=Activation");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName, samplePercentage: 0);

            result.Rows.Should().BeEmpty();
        }

        [Fact]
        public async Task ListWithHundredSamplePercentageIncludesEveryMatchingRowAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway");
            await CreateEntryAsync(dbContext, metricName, "stage=Activation");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName, samplePercentage: 100);

            result.Rows.Should().HaveCount(2);
        }

        [Fact]
        public async Task ListClampsSamplePercentageToZeroToOneHundredRangeAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var negative = await service.ListAsync(search: metricName, samplePercentage: -10);
            negative.Rows.Should().BeEmpty();

            var overHundred = await service.ListAsync(search: metricName, samplePercentage: 150);
            overHundred.Rows.Should().ContainSingle();
        }

        [Fact]
        public async Task ListIsNotTenantScopedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway");

            var otherTenantService = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var otherTenantResult = await otherTenantService.ListAsync();

            otherTenantResult.Rows.Should().Contain(r => r.MetricName == metricName);
        }

        [Fact]
        public async Task ListDefaultsToLastHourWhenFromAndToOmittedAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";

            var twoHoursAgo = DateTime.UtcNow.AddHours(-2);
            var justNow = DateTime.UtcNow;
            await CreateEntryAsync(dbContext, $"{metricName}Old", "stage=Gateway", twoHoursAgo);
            await CreateEntryAsync(dbContext, $"{metricName}New", "stage=Gateway", justNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName);

            var mine = result.Rows.Where(r => r.MetricName.StartsWith(metricName)).ToList();
            mine.Should().ContainSingle("the default from/to window is now the last hour, not the last day");
            mine[0].MetricName.Should().Be($"{metricName}New");
        }

        [Fact]
        public async Task ListSortsBySumAscendingAndDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 3000);
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 1000);
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);

            var ascending = await service.ListAsync(search: metricName, sortField: "sum", sortDirection: "asc");
            ascending.Rows.Select(r => r.Sum).Should().Equal(1000, 2000, 3000);

            var descending = await service.ListAsync(search: metricName, sortField: "sum", sortDirection: "desc");
            descending.Rows.Select(r => r.Sum).Should().Equal(3000, 2000, 1000);
        }

        [Theory]
        [InlineData("asc")]
        [InlineData("ASC")]
        [InlineData("Asc")]
        public async Task ListSortDirectionIsCaseInsensitiveForAscendingAsync(string ascendingKeyword)
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 2000);
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 1000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName, sortField: "sum",
                sortDirection: ascendingKeyword);

            result.Rows.Select(r => r.Sum).Should().Equal(1000, 2000);
        }

        [Fact]
        public async Task ListWithGarbageSortDirectionFallsBackToDescendingAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 1000);
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: 2000);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName, sortField: "sum", sortDirection: "banana");

            result.Rows.Select(r => r.Sum).Should().Equal(2000, 1000);
        }

        [Fact]
        public async Task ListWithUnrecognisedSortFieldFallsBackToMostRecentFirstAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway",
                DateTime.UtcNow.AddMinutes(-30));
            await CreateEntryAsync(dbContext, metricName, "stage=Activation", DateTime.UtcNow);

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName, sortField: "notARealColumn");

            var mine = result.Rows.Where(r => r.MetricName == metricName).ToList();
            mine[0].Tags.Should().Be("stage=Activation");
            mine[1].Tags.Should().Be("stage=Gateway");
        }

        [Fact]
        public async Task ListTotalReflectsFullFilteredCountEvenWhenTakeIsSmallerAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            for (var i = 0; i < 5; i++)
            {
                await CreateEntryAsync(dbContext, metricName, "stage=Gateway");
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(2, search: metricName);

            result.Rows.Should().HaveCount(2);
            result.Total.Should().Be(5);
        }

        [Fact]
        public async Task ListStatisticsComputesExactValuesForSumAsync()
        {
            await using var dbContext = fx.GetDbContext();
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";

            foreach (var value in new double[] { 1, 2, 3, 4, 5 })
            {
                await CreateEntryAsync(dbContext, metricName, "stage=Gateway", sum: value);
            }

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName);

            var stats = result.Statistics.Columns["sum"];
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
            var metricName = $"{DatabaseFixture.Prefix}Metric{Guid.NewGuid():N}";
            await CreateEntryAsync(dbContext, metricName, "stage=Gateway");

            var service = await BuildServiceAsync(dbContext, fx.Seed.LandlordUser);
            var result = await service.ListAsync(search: metricName);

            result.Statistics.Columns.Keys.Should().BeEquivalentTo("count", "sum", "min", "max");
        }
    }
}