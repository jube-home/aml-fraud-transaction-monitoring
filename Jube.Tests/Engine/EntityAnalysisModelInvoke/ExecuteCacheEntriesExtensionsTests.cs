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
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.TasksPerformance;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;
using InlineScriptPropertyAttribute =
    Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models.EntityAnalysisModelInlineScript.
    EntityAnalysisModelInlineScriptPropertyAttribute.EntityAnalysisModelInlineScriptPropertyAttribute;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ExecuteCacheEntriesExtensionsTests
    {
        private static Context NewContext(DateTime? referenceDate = null,
            int? reprocessingRuleInstanceId = null, bool enableCache = true, bool logSampled = true)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                Instance =
                {
                    TenantRegistryId = 7,
                    Guid = Guid.NewGuid()
                },
                Flags =
                {
                    EnableCache = enableCache
                }
            };

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    ReferenceDate = referenceDate ?? new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                    EntityAnalysisModelReprocessingRuleInstanceId = reprocessingRuleInstanceId,
                    InvokeTaskPerformance = new InvokeTaskPerformance()
                },
                Environment = TestDynamicEnvironment.Create(),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = logSampled
            };
        }

        [Fact]
        public void InsertOrReplaceCacheEntriesQueuesNothingWhenCacheIsDisabled()
        {
            var context = NewContext(enableCache: false);
            var cacheService = TestCacheService.Create(out _);

            ExecuteCacheEntriesExtensions.InsertOrReplaceCacheEntries(context, cacheService);

            context.PendingWriteTasks.Should().BeEmpty();
        }

        [Fact]
        public async Task InsertOrReplaceCacheEntriesQueuesAnInsertAndWritesToPayloadWhenNotReprocessingAsync()
        {
            var context = NewContext(reprocessingRuleInstanceId: null);
            var cacheService = TestCacheService.Create(out var redis);

            ExecuteCacheEntriesExtensions.InsertOrReplaceCacheEntries(context, cacheService);
            context.PendingWriteTasks.Should().ContainSingle();
            await Task.WhenAll(context.PendingWriteTasks);

            var payloadKey =
                $"Payload:{context.EntityAnalysisModel.Instance.TenantRegistryId}:{context.EntityAnalysisModel.Instance.Guid:N}";
            var field = $"{context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid:N}";
            var raw = await redis.HashGetAsync(payloadKey, field);

            raw.HasValue.Should().BeTrue("InsertAsync should have written the reduced payload to this key/field");
            var unpacked = cacheService.CachePayloadRepository.Unpack((byte[])raw!);
            unpacked.TryGetValue(-1, out var storedReferenceDate).Should().BeTrue();
            storedReferenceDate.AsDateTime().Should().Be(context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate);
        }

        [Fact]
        public async Task InsertOrReplaceCacheEntriesQueuesAnUpsertToTheSamePayloadKeyWhenReprocessingAsync()
        {
            var context = NewContext(reprocessingRuleInstanceId: 4);
            var cacheService = TestCacheService.Create(out var redis);

            ExecuteCacheEntriesExtensions.InsertOrReplaceCacheEntries(context, cacheService);
            context.PendingWriteTasks.Should().ContainSingle();
            await Task.WhenAll(context.PendingWriteTasks);

            var payloadKey =
                $"Payload:{context.EntityAnalysisModel.Instance.TenantRegistryId}:{context.EntityAnalysisModel.Instance.Guid:N}";
            var field = $"{context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid:N}";
            var raw = await redis.HashGetAsync(payloadKey, field);

            raw.HasValue.Should().BeTrue(
                "UpsertAsync (the reprocessing path) must reach the same Payload key/field as InsertAsync");
        }

        [Fact]
        public void ParseToReducedInternedPayloadAlwaysIncludesTheReferenceDateAtIndexMinusOne()
        {
            var context = NewContext();

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.TryGetValue(-1, out var value).Should().BeTrue();
            value.AsDateTime().Should().Be(context.EntityAnalysisModelInstanceEntryPayload.ReferenceDate);
        }

        [Fact]
        public void
            ParseToReducedInternedPayloadIncludesAnInlineScriptAttributeValueWhenCacheIndexIdIsSetAndPayloadHasIt()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Amount", "123.45");
            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(
                new EntityAnalysisModelInlineScript
                {
                    EntityAnalysisModelInlineScriptPropertyAttributes =
                    {
                        ["Amount"] = new InlineScriptPropertyAttribute { CacheIndexId = 5 }
                    }
                });

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.TryGetValue(5, out var value).Should().BeTrue();
            value.AsString().Should().Be("123.45");
        }

        [Fact]
        public void ParseToReducedInternedPayloadExcludesAnInlineScriptAttributeWithNoCacheIndexId()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Amount", "123.45");
            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(
                new EntityAnalysisModelInlineScript
                {
                    EntityAnalysisModelInlineScriptPropertyAttributes =
                    {
                        ["Amount"] = new InlineScriptPropertyAttribute { CacheIndexId = null }
                    }
                });

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.Count.Should().Be(1, "only the reference date at -1 should be present");
        }

        [Fact]
        public void ParseToReducedInternedPayloadExcludesAnInlineScriptAttributeWhosePayloadKeyIsMissing()
        {
            var context = NewContext();

            context.EntityAnalysisModel.Collections.EntityAnalysisModelInlineScripts.Add(
                new EntityAnalysisModelInlineScript
                {
                    EntityAnalysisModelInlineScriptPropertyAttributes =
                    {
                        ["Amount"] = new InlineScriptPropertyAttribute { CacheIndexId = 5 }
                    }
                });

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.TryGetValue(5, out _).Should().BeFalse();
            reduced.Count.Should().Be(1);
        }

        [Fact]
        public void ParseToReducedInternedPayloadIncludesACachedRequestXPathValueWhenCacheIsTrueAndPayloadHasIt()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "ACC-1");
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "AccountId",
                    Cache = true,
                    CacheIndexId = 9
                });

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.TryGetValue(9, out var value).Should().BeTrue();
            value.AsString().Should().Be("ACC-1");
        }

        [Fact]
        public void ParseToReducedInternedPayloadExcludesARequestXPathWithCacheFalse()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "ACC-1");
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "AccountId",
                    Cache = false,
                    CacheIndexId = 9
                });

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.TryGetValue(9, out _).Should().BeFalse();
            reduced.Count.Should().Be(1);
        }

        [Fact]
        public void ParseToReducedInternedPayloadExcludesAPayloadEntryNotConfiguredForCachingAtAll()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("UnrelatedField", "should-not-appear");
            context.EntityAnalysisModel.Collections.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath
                {
                    Name = "SomeOtherField",
                    Cache = true,
                    CacheIndexId = 1
                });

            var reduced = ExecuteCacheEntriesExtensions.ParseToReducedInternedPayload(context);

            reduced.Count.Should().Be(1,
                "only the reference date should be present - UnrelatedField is not configured for caching");
        }

        [Fact]
        public async Task UpsertCachePayloadLatestWritesTheMatchingPayloadValueForEachSearchKeyAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "ACC-1");
            var cacheService = TestCacheService.Create(out var redis);
            var searchKeys = new Dictionary<string, DistinctSearchKey> { ["AccountId"] = new() };

            ExecuteCacheEntriesExtensions.UpsertCachePayloadLatest(context, cacheService, searchKeys);
            context.PendingWriteTasks.Should().ContainSingle();
            await Task.WhenAll(context.PendingWriteTasks);

            var payloadLatestKey =
                $"PayloadLatest:{context.EntityAnalysisModel.Instance.TenantRegistryId}:{context.EntityAnalysisModel.Instance.Guid:N}:AccountId";
            var raw = await redis.HashGetAsync(payloadLatestKey, "ACC-1");
            raw.HasValue.Should().BeTrue();
        }

        [Fact]
        public async Task UpsertCachePayloadLatestStillQueuesAnUpsertWithAnEmptyValueWhenThePayloadHasNoMatchAsync()
        {
            var context = NewContext();
            var cacheService = TestCacheService.Create(out var redis);
            var searchKeys = new Dictionary<string, DistinctSearchKey> { ["AccountId"] = new() };

            ExecuteCacheEntriesExtensions.UpsertCachePayloadLatest(context, cacheService, searchKeys);
            context.PendingWriteTasks.Should().ContainSingle();
            await Task.WhenAll(context.PendingWriteTasks);

            var payloadLatestKey =
                $"PayloadLatest:{context.EntityAnalysisModel.Instance.TenantRegistryId}:{context.EntityAnalysisModel.Instance.Guid:N}:AccountId";

            var raw = await redis.HashGetAsync(payloadLatestKey, string.Empty);
            raw.HasValue.Should().BeTrue("current behaviour upserts using an empty-string field, not a skip");
        }

        [Fact]
        public async Task UpsertCachePayloadLatestHandlesMultipleSearchKeysIndependentlyAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "ACC-1");

            var cacheService = TestCacheService.Create(out var redis);
            var searchKeys = new Dictionary<string, DistinctSearchKey>
            {
                ["AccountId"] = new(),
                ["IpAddress"] = new()
            };

            ExecuteCacheEntriesExtensions.UpsertCachePayloadLatest(context, cacheService, searchKeys);
            context.PendingWriteTasks.Should().HaveCount(2);
            await Task.WhenAll(context.PendingWriteTasks);

            var tenant = context.EntityAnalysisModel.Instance.TenantRegistryId;
            var model = context.EntityAnalysisModel.Instance.Guid;

            (await redis.HashGetAsync($"PayloadLatest:{tenant}:{model:N}:AccountId", "ACC-1"))
                .HasValue.Should().BeTrue();
            (await redis.HashGetAsync($"PayloadLatest:{tenant}:{model:N}:IpAddress", string.Empty))
                .HasValue.Should().BeTrue();
        }

        [Fact]
        public void ExecuteCacheDbStorageRecordsStageTimingWhenLogSampledIsTrue()
        {
            var context = NewContext(logSampled: true);
            var cacheService = TestCacheService.Create(out _);

            context.ExecuteCacheDbStorage(cacheService, new Dictionary<string, DistinctSearchKey>());

            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().NotBeNull();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages!.CacheDbStorage.Should()
                .NotBeNull();
        }

        [Fact]
        public void ExecuteCacheDbStorageDoesNotRecordStageTimingWhenLogSampledIsFalse()
        {
            var context = NewContext(logSampled: false);
            var cacheService = TestCacheService.Create(out _);

            var act = () => context.ExecuteCacheDbStorage(cacheService, new Dictionary<string, DistinctSearchKey>());

            act.Should().NotThrow();
            context.EntityAnalysisModelInstanceEntryPayload.InvokeTaskPerformance.Stages.Should().BeNull();
        }

        [Fact]
        public void ExecuteCacheDbStorageQueuesBothPayloadAndPayloadLatestWrites()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "ACC-1");
            var cacheService = TestCacheService.Create(out _);
            var searchKeys = new Dictionary<string, DistinctSearchKey> { ["AccountId"] = new() };

            context.ExecuteCacheDbStorage(cacheService, searchKeys);
            context.PendingWriteTasks.Should().HaveCount(2);
        }
    }
}