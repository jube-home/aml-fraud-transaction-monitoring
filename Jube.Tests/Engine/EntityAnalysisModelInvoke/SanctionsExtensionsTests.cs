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
using Jube.Cache.Redis.Models;
using Jube.Cache.Redis.Serialization;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.Sanctions;
using Jube.Engine.Sanctions.Models;
using Jube.Test.Infrastructure;
using MessagePack;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class SanctionsExtensionsTests
    {
        private const byte AverageAggregation = 2;
        private const byte ConfidenceAggregation = 8;

        private static EntityAnalysisModelSanction NewSanctionCheck(string name = "SDN", int distance = 1,
            byte aggregationTypeId = AverageAggregation, char cacheInterval = 'h', int cacheValue = 1)
        {
            return new EntityAnalysisModelSanction
            {
                Name = name,
                MultipartStringDataName = "JoinedName",
                Distance = distance,
                AggregationTypeId = aggregationTypeId,
                CacheInterval = cacheInterval,
                CacheValue = cacheValue,
                MaxDistanceRatio = 0.5,
                MaxCoverageRatio = 5.0
            };
        }

        private static (Context Context, FakeHybridResilientRedisDatabase Redis) NewContextWithRedis(
            params EntityAnalysisModelSanction[] sanctionChecks)
        {
            var entityAnalysisModel = new EntityAnalysisModel();
            entityAnalysisModel.Collections.EntityAnalysisModelSanctions.AddRange(sanctionChecks);
            entityAnalysisModel.Services.CacheService = TestCacheService.Create(out var redis);

            var context = new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    Sanction = new PooledDictionary<string, double>(),
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Log = TestLog.NoOp,
                Stopwatch = Stopwatch.StartNew(),
                LogSampled = true
            };

            return (context, redis);
        }

        private static void AddSanctionEntry(Context context, int id, params string[] elementValues)
        {
            context.EntityAnalysisModel.Dependencies.SanctionsEntries.TryAdd(id, new SanctionEntry
            {
                SanctionEntryId = id,
                SanctionEntrySourceId = 1,
                SanctionEntryReference = $"REF{id}",
                SanctionElementValue = elementValues
            });
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncWithAnExactMatchPopulatesTheSanctionResponseAsync()
        {
            var (context, _) = NewContextWithRedis(NewSanctionCheck());
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().ContainKey("SDN");
            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(0);
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncWithNoMatchDoesNotPopulateTheSanctionResponseAsync()
        {
            var (context, _) = NewContextWithRedis(NewSanctionCheck(distance: 1));
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "John Smith");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().NotContainKey("SDN");
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncWhenPayloadIsMissingTheConfiguredFieldSkipsTheCheckAsync()
        {
            var (context, _) = NewContextWithRedis(NewSanctionCheck());
            AddSanctionEntry(context, 1, "osama bin laden");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().BeEmpty();
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncDispatchesToTheConfiguredAggregationTypeAsync()
        {
            var (context, _) = NewContextWithRedis(NewSanctionCheck(distance: 3,
                aggregationTypeId: ConfidenceAggregation));

            AddSanctionEntry(context, 1, "osama bin laden");
            AddSanctionEntry(context, 2, "osama bin ladin");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();

            var expected = SanctionAggregationCalculator.CalculateConfidence(
            [
                new SanctionEntryReturn { LevenshteinDistance = 0 },
                new SanctionEntryReturn { LevenshteinDistance = 1 }
            ]);

            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(expected);
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncEvaluatesEveryConfiguredCheckIndependentlyAsync()
        {
            var (context, _) = NewContextWithRedis(
                NewSanctionCheck("SDN", 0),
                NewSanctionCheck("BOE", 0));
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().ContainKey("SDN");
            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().ContainKey("BOE");
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncRecordsPerCheckTaskPerformanceWhenLogSampledAsync()
        {
            var (context, _) = NewContextWithRedis(NewSanctionCheck());
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages!.Sanctions.Should()
                .NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Sanctions!.Items
                .Should().ContainKey("SDN");
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncOnCacheMissWritesTheComputedValueIntoCacheAsync()
        {
            var (context, redis) = NewContextWithRedis(NewSanctionCheck());
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();
            await Task.WhenAll(context.PendingWriteTasks);

            var redisKey = $"Sanction:{context.EntityAnalysisModel.Instance.TenantRegistryId}:" +
                           $"{context.EntityAnalysisModel.Instance.Guid:N}";
            var stored = await redis.HashGetAsync(redisKey, "Osama bin Laden:1");
            stored.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncOnANoMatchDoesNotWriteAnythingToCacheAsync()
        {
            var (context, redis) = NewContextWithRedis(NewSanctionCheck(distance: 0));
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Completely Different");

            await context.ExecuteSanctionsAsync();
            await Task.WhenAll(context.PendingWriteTasks);

            var redisKey = $"Sanction:{context.EntityAnalysisModel.Instance.TenantRegistryId}:" +
                           $"{context.EntityAnalysisModel.Instance.Guid:N}";
            var stored = await redis.HashGetAsync(redisKey, "Completely Different:0");
            stored.HasValue.Should().BeFalse();
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncOnAFreshNonExpiredCacheHitDoesNotRecomputeAsync()
        {
            var check = NewSanctionCheck(distance: 0, cacheInterval: 'h', cacheValue: 1);
            var (context, _) = NewContextWithRedis(check);

            await context.EntityAnalysisModel.Services.CacheService.CacheSanctionRepository.InsertAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid,
                "Osama bin Laden", check.Distance, 0.42);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(0.42);
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncOnAnExpiredCacheHitRecomputesFromLiveEntriesAsync()
        {
            var check = NewSanctionCheck(distance: 0, cacheInterval: 's', cacheValue: 30);
            var (context, redis) = NewContextWithRedis(check);
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            var redisKey = $"Sanction:{context.EntityAnalysisModel.Instance.TenantRegistryId}:" +
                           $"{context.EntityAnalysisModel.Instance.Guid:N}";
            var stale = new Sanction
            {
                Value = 999,
                CreatedDate = DateTime.UtcNow.AddSeconds(-61)
            };
            var bytes = MessagePackSerializer.Serialize(stale,
                MessagePackSerializerOptionsHelper
                    .StandardMessagePackSerializerWithCompressionOptions(false));
            await redis.HashSetAsync(redisKey, "Osama bin Laden:0", bytes);

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(0);
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncOnANonExpiredCachedNullValueStillRecomputesAsync()
        {
            var check = NewSanctionCheck(distance: 0, cacheInterval: 'h', cacheValue: 1);
            var (context, _) = NewContextWithRedis(check);
            await context.EntityAnalysisModel.Services.CacheService.CacheSanctionRepository.InsertAsync(
                context.EntityAnalysisModel.Instance.TenantRegistryId, context.EntityAnalysisModel.Instance.Guid,
                "Osama bin Laden", check.Distance, null);
            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Laden");

            await context.ExecuteSanctionsAsync();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction["SDN"].Should().Be(0);
        }

        [Fact]
        public async Task ExecuteSanctionsAsyncCacheKeyIsScopedByDistanceThresholdAsync()
        {
            var (context, redis) = NewContextWithRedis(
                NewSanctionCheck("Loose", 3),
                NewSanctionCheck("AlsoMatches"));

            AddSanctionEntry(context, 1, "osama bin laden");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.TryAdd("JoinedName", "Osama bin Ladenn");

            await context.ExecuteSanctionsAsync();
            await Task.WhenAll(context.PendingWriteTasks);

            var redisKey = $"Sanction:{context.EntityAnalysisModel.Instance.TenantRegistryId}:" +
                           $"{context.EntityAnalysisModel.Instance.Guid:N}";
            (await redis.HashGetAsync(redisKey, "Osama bin Ladenn:3")).HasValue.Should().BeTrue();
            (await redis.HashGetAsync(redisKey, "Osama bin Ladenn:1")).HasValue.Should().BeTrue();

            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().ContainKey("Loose");
            context.EntityAnalysisModelInstanceEntryPayload.Sanction.Should().ContainKey("AlsoMatches");
        }
    }
}