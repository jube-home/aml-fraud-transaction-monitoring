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
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Engine.Helpers;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ActivationRuleActivationWatcherExtensionsTests
    {
        private static Context NewContext(double randomDraw = 0.1,
            IReadOnlyDictionary<string, string>? environmentOverrides = null)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                JsonSerializationHelper = new JsonSerializationHelper(),
                Instance =
                {
                    TenantRegistryId = 7
                },
                Flags =
                {
                    EnableActivationWatcher = true
                },
                Counters =
                {
                    MaxActivationWatcherThreshold = 100,
                    ActivationWatcherSample = 1
                }
            };

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>()
                },
                Random = new FixedRandom(randomDraw),
                Environment = TestDynamicEnvironment.Create(environmentOverrides),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        private static EntityAnalysisModelActivationRule NewRule(bool sendToActivationWatcher = true)
        {
            return new EntityAnalysisModelActivationRule
            {
                Name = "HighValueTransaction",
                SendToActivationWatcher = sendToActivationWatcher,
                ResponseElevationKey = "AccountId",
                ResponseElevation = 50,
                ResponseElevationContent = "Blocked",
                ResponseElevationForeColor = "#000000",
                ResponseElevationBackColor = "#ff0000"
            };
        }

        [Fact]
        public async Task DoesNothingWhenTheRuleIsNotFlaggedForTheWatcherAsync()
        {
            var context = NewContext();
            var rule = NewRule(false);

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenSuppressedAsync()
        {
            var context = NewContext();
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, true, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenThisIsAReprocessingRunAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId = 3;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheModelHasActivationWatcherDisabledAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Flags.EnableActivationWatcher = false;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheWatchCountHasReachedTheThresholdAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold = 1;
            context.EntityAnalysisModel.Counters.ActivationWatcherCount = 1;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheRandomSampleDrawFailsAsync()
        {
            var context = NewContext(0.99);
            context.EntityAnalysisModel.Counters.ActivationWatcherSample = 0.1;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task PersistsToTheWatcherQueueWithFieldsCopiedFromTheRuleWhenPersistIsAllowedAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "True",
                ["StreamingActivationWatcher"] = "False"
            });

            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            var watcher = context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single();
            watcher.ActivationRuleSummary.Should().Be("HighValueTransaction");
            watcher.ResponseElevation.Should().Be(50);
            watcher.ResponseElevationContent.Should().Be("Blocked");
            watcher.ForeColor.Should().Be("#000000");
            watcher.BackColor.Should().Be("#ff0000");
            watcher.TenantRegistryId.Should().Be(7);
        }

        [Fact]
        public async Task DoesNotPersistToTheWatcherQueueWhenPersistIsDisallowedAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "False",
                ["StreamingActivationWatcher"] = "False"
            });

            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task UsesTheResponseElevationKeyValueFromThePayloadWhenPresentAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "True",
                ["StreamingActivationWatcher"] = "False"
            });
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "Test5");
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            var watcher = context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single();
            watcher.Key.Should().Be("AccountId");
            watcher.KeyValue.Should().Be("Test5");
        }

        [Fact]
        public async Task FallsBackToMissingForTheKeyValueWhenThePayloadDoesNotHaveItAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "True",
                ["StreamingActivationWatcher"] = "False"
            });

            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null);

            var watcher = context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single();
            watcher.Key.Should().Be("AccountId");
            watcher.KeyValue.Should().Be("Missing");
        }

        [Fact]
        public async Task PublishesToRedisAndIncrementsTheCounterWhenStreamingIsEnabledAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "False",
                ["StreamingActivationWatcher"] = "True"
            });

            var rule = NewRule();
            var cacheService = TestCacheService.Create(out var redis);

            await context.ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            redis.PublishedMessages.Should().ContainSingle();
            redis.PublishedMessages.Single().Channel.ToString().Should().Be("ActivationWatcher:7");
            context.EntityAnalysisModel.Counters.ActivationWatcherCount.Should().Be(1);
        }

        [Fact]
        public async Task DoesNotPublishToRedisOrIncrementTheCounterWhenStreamingIsDisabledAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "False",
                ["StreamingActivationWatcher"] = "False"
            });

            var rule = NewRule();
            var cacheService = TestCacheService.Create(out var redis);

            await context.ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            redis.PublishedMessages.Should().BeEmpty();
            context.EntityAnalysisModel.Counters.ActivationWatcherCount.Should().Be(0);
        }

        [Fact]
        public async Task ARedisFailureIsSwallowedInsideTheCacheRepositorySoTheCounterStillIncrementsAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "False",
                ["StreamingActivationWatcher"] = "True"
            });

            var rule = NewRule();
            var cacheService = TestCacheService.Create(out var redis);
            redis.ThrowOnMethod = "PublishAsync";

            var act = async () => await context.ActivationRuleActivationWatcherAsync(rule, false,
                cacheService);

            await act.Should().NotThrowAsync();

            context.EntityAnalysisModel.Counters.ActivationWatcherCount.Should().Be(1,
                "CacheActivationWatcherPublishSubscribe.PublishAsync swallows and logs Redis failures like every " +
                "other Jube.Cache repository, so the engine never sees the exception and the counter still moves");
        }

        private sealed class FixedRandom(double fixedNextDouble) : Random
        {
            public override double NextDouble()
            {
                return fixedNextDouble;
            }
        }

        private static Context LocatedContext(double latitude, double longitude, string accountId = "ACC-1",
            double randomDraw = 0.1, double sample = 1)
        {
            var context = NewContext(randomDraw, new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "True",
                ["StreamingActivationWatcher"] = "False"
            });

            context.EntityAnalysisModel.Counters.ActivationWatcherSample = sample;
            context.Snapshot.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Lat", DataTypeId = 6 });
            context.Snapshot.EntityAnalysisModelRequestXPaths.Add(
                new EntityAnalysisModelRequestXPath { Name = "Lon", DataTypeId = 7 });
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Lat", latitude);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("Lon", longitude);
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", accountId);

            return context;
        }

        [Fact]
        public async Task CarriesTheEntitysPreviousLocationOntoTheNextRecordedActivationAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();
            var firstContext = LocatedContext(51.5, -0.12);
            var secondContext = LocatedContext(48.86, 2.35);

            await firstContext.ActivationRuleActivationWatcherAsync(rule, false, cacheService);
            await secondContext.ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            var first = firstContext.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single();
            var second = secondContext.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single();

            first.PreviousLatitude.Should().BeNull();
            second.PreviousLatitude.Should().Be(51.5);
            second.PreviousLongitude.Should().Be(-0.12);
            second.PreviousCreatedDate.Should().NotBeNull();
        }

        [Fact]
        public async Task RemembersTheLocationEvenWhenTheSampleDeclinesToRecordTheActivationAsync()
        {
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();
            var unsampled = LocatedContext(51.5, -0.12, randomDraw: 0.99, sample: 0.1);
            var sampled = LocatedContext(48.86, 2.35);

            await unsampled.ActivationRuleActivationWatcherAsync(rule, false, cacheService);
            await sampled.ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            unsampled.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
            sampled.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single().PreviousLatitude
                .Should().Be(51.5);
        }

        [Fact]
        public async Task KeepsEntitiesApartAndIgnoresAnActivationWithNoLocationOrNoKeyValueAsync()
        {
            var cacheService = TestCacheService.Create(out var redis);
            var rule = NewRule();

            await LocatedContext(51.5, -0.12, "ACC-1").ActivationRuleActivationWatcherAsync(rule, false, cacheService);
            await LocatedContext(0, 0, "ACC-2").ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            var other = LocatedContext(40.7, -74, "ACC-2");

            await other.ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            other.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Single().PreviousLatitude
                .Should().BeNull();

            var missing = LocatedContext(40.7, -74);

            missing.EntityAnalysisModelInstanceEntryPayload.Payload.Remove("AccountId");

            await missing.ActivationRuleActivationWatcherAsync(rule, false, cacheService);

            (await redis.StringGetAsync("ActivationWatcherLastLocation:7:AccountId:Missing")).HasValue.Should()
                .BeFalse();
            (await redis.StringGetAsync("ActivationWatcherLastLocation:7:AccountId:ACC-2")).HasValue.Should().BeTrue();
        }
    }
}
