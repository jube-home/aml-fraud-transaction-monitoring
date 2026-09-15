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

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenSuppressedAsync()
        {
            var context = NewContext();
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, true, null,
                null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenThisIsAReprocessingRunAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId = 3;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheModelHasActivationWatcherDisabledAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Flags.EnableActivationWatcher = false;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheWatchCountHasReachedTheThresholdAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModel.Counters.MaxActivationWatcherThreshold = 1;
            context.EntityAnalysisModel.Counters.ActivationWatcherCount = 1;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

            context.EntityAnalysisModel.ConcurrentQueues.PersistToActivationWatcherAsync.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheRandomSampleDrawFailsAsync()
        {
            var context = NewContext(0.99);
            context.EntityAnalysisModel.Counters.ActivationWatcherSample = 0.1;
            var rule = NewRule();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

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

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

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

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

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

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

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

            await context.ActivationRuleActivationWatcherAsync(rule, false, null,
                null);

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
            var redis = new FakeHybridResilientRedisDatabase();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null, redis);

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
            var redis = new FakeHybridResilientRedisDatabase();

            await context.ActivationRuleActivationWatcherAsync(rule, false, null, redis);

            redis.PublishedMessages.Should().BeEmpty();
            context.EntityAnalysisModel.Counters.ActivationWatcherCount.Should().Be(0);
        }

        [Fact]
        public Task DoesNotThrowWhenAmqpIsDisabledAndTheRabbitMqChannelIsNullAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "False",
                ["StreamingActivationWatcher"] = "False"
            });
            var rule = NewRule();

            var act = async () => await context.ActivationRuleActivationWatcherAsync(rule, false,
                null, null);

            return act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task SwallowsExceptionsAndLogsRatherThanThrowingAsync()
        {
            var context = NewContext(environmentOverrides: new Dictionary<string, string>
            {
                ["ActivationWatcherAllowPersist"] = "False",
                ["StreamingActivationWatcher"] = "True"
            });
            var rule = NewRule();
            var redis = new FakeHybridResilientRedisDatabase
            {
                ThrowOnMethod = "PublishAsync"
            };

            var act = async () => await context.ActivationRuleActivationWatcherAsync(rule, false,
                null, redis);

            await act.Should().NotThrowAsync();
            context.EntityAnalysisModel.Counters.ActivationWatcherCount.Should().Be(0);
        }

        private sealed class FixedRandom(double fixedNextDouble) : Random
        {
            public override double NextDouble()
            {
                return fixedNextDouble;
            }
        }
    }
}