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
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ActivationRuleTtlCounterExtensionsTests
    {
        private static Context NewContext(IReadOnlyDictionary<string, string>? environmentOverrides = null)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                Instance =
                {
                    TenantRegistryId = 42,
                    Guid = Guid.NewGuid()
                }
            };

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    TtlCounter = new PooledDictionary<string, double>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid(),
                    ReferenceDate = new DateTime(2025, 1, 15, 10, 37, 42, DateTimeKind.Utc)
                },
                Environment = TestDynamicEnvironment.Create(environmentOverrides),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        private static EntityAnalysisModelActivationRule NewRule(Guid targetModelGuid, Guid counterGuid,
            bool enableTtlCounter = true)
        {
            return new EntityAnalysisModelActivationRule
            {
                EnableTtlCounter = enableTtlCounter,
                EntityAnalysisModelGuidTtlCounter = targetModelGuid,
                EntityAnalysisModelTtlCounterGuid = counterGuid
            };
        }

        private static (EntityAnalysisModel Model, EntityAnalysisModelTtlCounter Counter) NewTargetModel(
            string dataName = "CurrencyAmount", string dataValueField = "CurrencyAmount", bool enableSum = false,
            bool enableLiveForever = false, string resolutionInterval = "n",
            bool modelFlagEnableTtlCounter = true)
        {
            var counter = new EntityAnalysisModelTtlCounter
            {
                Guid = Guid.NewGuid(),
                Name = "TransactionCount",
                TtlCounterDataName = dataName,
                TtlCounterDataValue = dataValueField,
                EnableSum = enableSum,
                EnableLiveForever = enableLiveForever,
                ResolutionInterval = resolutionInterval
            };

            var model = new EntityAnalysisModel
            {
                Instance =
                {
                    Guid = Guid.NewGuid(),
                    TenantRegistryId = 1
                },
                Flags =
                {
                    EnableTtlCounter = modelFlagEnableTtlCounter
                }
            };
            model.Collections.ModelTtlCounters.Add(counter);

            return (model, counter);
        }

        [Fact]
        public async Task DoesNothingWhenTheRuleHasTtlCounterDisabledAsync()
        {
            var context = NewContext();
            var (model, counter) = NewTargetModel();
            var rule = NewRule(model.Instance.Guid, counter.Guid, false);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
            context.PendingWriteTasks.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenThisIsAReprocessingRunAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId = 4;
            var (model, counter) = NewTargetModel();
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenNoModelMatchesTheTargetModelGuidAsync()
        {
            var context = NewContext();
            var (model, counter) = NewTargetModel();
            var rule = NewRule(Guid.NewGuid(), counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheModelHasNoCounterMatchingTheCounterGuidAsync()
        {
            var context = NewContext();
            var (model, _) = NewTargetModel();
            var rule = NewRule(model.Instance.Guid, Guid.NewGuid());
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenThePayloadHasNoValueForTheCountersDataNameAsync()
        {
            var context = NewContext();
            var (model, counter) = NewTargetModel("MissingField");
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheTargetModelHasTtlCounterStorageDisabledAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "123.45");
            var (model, counter) = NewTargetModel(modelFlagEnableTtlCounter: false);
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().BeEmpty();
            context.PendingWriteTasks.Should().BeEmpty();
        }

        [Fact]
        public async Task IncrementsByOneAndAddsANewEntryWhenSumIsNotEnabledAndTheCounterHasNoExistingValueAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "123.45");
            var (model, counter) = NewTargetModel(enableSum: false);
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await Task.WhenAll(context.PendingWriteTasks);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(1);
        }

        [Fact]
        public async Task AccumulatesTheExistingInMemoryCounterValueWhenIncrementedTwiceAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", 10d);
            var (model, counter) = NewTargetModel(enableSum: true, dataValueField: "CurrencyAmount");
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await Task.WhenAll(context.PendingWriteTasks);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(20);
        }

        [Fact]
        public async Task SumsThePayloadValueWhenEnableSumIsSetOnTheCounterAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Flag", "present");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", 42.5);
            var (model, counter) = NewTargetModel("Flag", "CurrencyAmount",
                true);
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await Task.WhenAll(context.PendingWriteTasks);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(42.5);
        }

        [Fact]
        public async Task WritesAnUpsertEntryToTheCacheWhenTheCounterDoesNotLiveForeverAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "5");
            var (model, counter) = NewTargetModel(enableLiveForever: false);
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out var redis);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await Task.WhenAll(context.PendingWriteTasks);

            var expiryKey =
                $"TtlCounterEntryExpiry:{context.EntityAnalysisModel.Instance.TenantRegistryId}:{rule.EntityAnalysisModelGuidTtlCounter:N}:{counter.Guid:N}:{counter.TtlCounterDataName}";
            (await redis.SortedSetLengthAsync(expiryKey)).Should().Be(1);
        }

        [Fact]
        public async Task DoesNotWriteAnUpsertEntryWhenTheCounterLivesForeverAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "5");
            var (model, counter) = NewTargetModel(enableLiveForever: true);
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out var redis);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await Task.WhenAll(context.PendingWriteTasks);

            var expiryKey =
                $"TtlCounterEntryExpiry:{context.EntityAnalysisModel.Instance.TenantRegistryId}:{rule.EntityAnalysisModelGuidTtlCounter:N}:{counter.Guid:N}:{counter.TtlCounterDataName}";
            (await redis.SortedSetLengthAsync(expiryKey)).Should().Be(0);
            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(1);
        }

        [Fact]
        public async Task SkipsTheIdempotencyCacheEntirelyWhenActivationRuleIdempotencyIsDisabledAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "1");
            var (model, counter) = NewTargetModel();
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter.Should().ContainKey(counter.Name);
        }

        [Fact]
        public async Task DoesNotIncrementWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync()
        {
            var context = NewContext(new Dictionary<string, string> { ["ActivationRuleIdempotency"] = "True" });
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "1");
            var (model, counter) = NewTargetModel();
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out _);

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);

            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(1);
        }

        [Fact]
        public async Task StillIncrementsTheInMemoryCounterWhenTheBackgroundCacheWriteFailsAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", "1");
            var (model, counter) = NewTargetModel();
            var rule = NewRule(model.Instance.Guid, counter.Guid);
            var availableModels = new Dictionary<int, EntityAnalysisModel> { [model.Instance.Id] = model };
            var cacheService = TestCacheService.Create(out var redis);
            redis.ThrowOnMethod = "SortedSetAddAsync";

            await context.ActivationRuleTtlCounterAsync(rule, availableModels, cacheService);
            var awaitPendingWrites = async () => await Task.WhenAll(context.PendingWriteTasks);

            await awaitPendingWrites.Should().NotThrowAsync();
            context.EntityAnalysisModelInstanceEntryPayload.TtlCounter[counter.Name].Should().Be(1);
        }
    }
}