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
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class TtlCounterExtensionsTests
    {
        [Theory]
        [InlineData("d", 1, -1, 0, 0, 0)]
        [InlineData("h", 1, 0, -1, 0, 0)]
        [InlineData("n", 1, 0, 0, -1, 0)]
        [InlineData("s", 1, 0, 0, 0, -1)]
        public void ApplyTtlCounterIntervalSubtractsTheGivenUnitFromReferenceDate(string interval, int value,
            int expectedDays, int expectedHours, int expectedMinutes, int expectedSeconds)
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            var adjusted = TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, interval, value);

            adjusted.Should().Be(referenceDate.AddDays(expectedDays).AddHours(expectedHours)
                .AddMinutes(expectedMinutes).AddSeconds(expectedSeconds));
        }

        [Fact]
        public void ApplyTtlCounterIntervalSubtractsMonthsForM()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, "m", 1).Should().Be(new DateTime(2024, 5, 15,
                12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ApplyTtlCounterIntervalSubtractsYearsForY()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

            TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, "y", 1).Should().Be(new DateTime(2023, 6, 15,
                12, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void ApplyTtlCounterIntervalWithAVerySmallValueSupportsVeryShortLivedCounters()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

            TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, "s", 1)
                .Should().Be(new DateTime(2024, 6, 15, 12, 0, 9, DateTimeKind.Utc));
        }

        [Fact]
        public void ApplyTtlCounterIntervalWithZeroValueReturnsReferenceDateUnchanged()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

            TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, "s", 0).Should().Be(referenceDate);
        }

        [Fact]
        public void ApplyTtlCounterIntervalWithAnUnrecognisedIntervalLeavesReferenceDateUnchanged()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

            TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, "bogus", 5).Should().Be(referenceDate);
        }

        [Fact]
        public void ApplyTtlCounterIntervalWithANegativeValuePushesTheWindowIntoTheFutureInstead()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);

            TtlCounterExtensions.ApplyTtlCounterInterval(referenceDate, "s", -5)
                .Should().Be(new DateTime(2024, 6, 15, 12, 0, 15, DateTimeKind.Utc),
                    "a negative interval value adds instead of subtracts, inverting the intended window");
        }

        private static Context NewOnlineAggregationContext(
            DateTime referenceDate, EntityAnalysisModelTtlCounter counter, string dataValue,
            int tenantRegistryId = 1)
        {
            var cacheService = TestCacheService.Create(out _);

            var entityAnalysisModel = new EntityAnalysisModel
            {
                Instance =
                {
                    TenantRegistryId = tenantRegistryId,
                    Guid = Guid.NewGuid()
                },
                Flags =
                {
                    EnableTtlCounter = true
                },
                Services =
                {
                    CacheService = cacheService,
                    Log = TestLog.NoOp
                }
            };

            entityAnalysisModel.Collections.ModelTtlCounters.Add(counter);

            var payload = new DictionaryNoBoxing<string>();
            payload.Add(counter.TtlCounterDataName, dataValue);

            var context = new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = payload,
                    TtlCounter = new PooledDictionary<string, double>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    ReferenceDate = referenceDate,
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Environment = TestDynamicEnvironment.Create(),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };

            return context;
        }

        private static EntityAnalysisModelTtlCounter NewOnlineCounter(string interval, int value,
            string dataName = "AccountId", string name = "ShortLivedCounter")
        {
            return new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = name,
                TtlCounterDataName = dataName,
                OnlineAggregation = true,
                TtlCounterInterval = interval,
                TtlCounterValue = value
            };
        }

        [Fact]
        public async Task FiveSecondWindowIncludesEntriesAtBothInclusiveBoundariesAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);
            var counter = NewOnlineCounter("s", 5);
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");

            await context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid,
                counter.TtlCounterDataName, "acc-1", counter.Guid,
                new DateTime(2024, 6, 15, 12, 0, 5, DateTimeKind.Utc), 3);

            await context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository.UpsertAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid,
                counter.TtlCounterDataName, "acc-1", counter.Guid,
                new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc), 4);

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(7);
        }

        [Fact]
        public async Task FiveSecondWindowExcludesEntriesJustOutsideEitherBoundaryAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);
            var counter = NewOnlineCounter("s", 5);
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");
            var repository = context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository;

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                new DateTime(2024, 6, 15, 12, 0, 4, DateTimeKind.Utc), 100);

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                new DateTime(2024, 6, 15, 12, 0, 11, DateTimeKind.Utc), 200);

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(0,
                "both seeded entries are just outside the 5-second window and must not be summed");
        }

        [Fact]
        public async Task OneMinuteWindowIncludesEntriesInsideAndExcludesJustOutsideAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 5, 0, DateTimeKind.Utc);
            var counter = NewOnlineCounter("n", 1);
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");
            var repository = context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository;

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                new DateTime(2024, 6, 15, 12, 4, 30, DateTimeKind.Utc), 5);
            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                new DateTime(2024, 6, 15, 12, 3, 59, DateTimeKind.Utc), 1000);

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(5);
        }

        [Fact]
        public async Task ZeroLengthWindowOnlyIncludesAnEntryExactlyAtReferenceDateAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);
            var counter = NewOnlineCounter("s", 0);
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");
            var repository = context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository;

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                referenceDate, 9);

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                referenceDate.AddSeconds(-1), 1000);

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(9);
        }

        [Fact]
        public async Task DifferentDataValuesForTheSameCounterDoNotCollideAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);
            var counter = NewOnlineCounter("s", 5);
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");
            var repository = context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository;

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                referenceDate, 3);

            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-OTHER", counter.Guid,
                referenceDate, 999);

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(3);
        }

        [Fact]
        public async Task DoesNothingWhenModelLevelEnableTtlCounterIsFalseAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);
            var counter = NewOnlineCounter("s", 5);
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");
            context.EntityAnalysisModel.Flags.EnableTtlCounter = false;
            var repository = context.EntityAnalysisModel.Services.CacheService.CacheTtlCounterEntryRepository;
            await repository.UpsertAsync(context.EntityAnalysisModel.Instance.TenantRegistryId,
                context.EntityAnalysisModel.Instance.Guid, counter.TtlCounterDataName, "acc-1", counter.Guid,
                referenceDate, 3);

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenThePayloadHasNoValueForTheCountersDataNameAsync()
        {
            var referenceDate = new DateTime(2024, 6, 15, 12, 0, 10, DateTimeKind.Utc);
            var counter = NewOnlineCounter("s", 5, "MissingField");
            var context = NewOnlineAggregationContext(referenceDate, counter, "acc-1");

            context.EntityAnalysisModelInstanceEntryPayload.Payload = new DictionaryNoBoxing<string>();

            await context.ExecuteTtlCountersAsync();

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
        }
    }
}