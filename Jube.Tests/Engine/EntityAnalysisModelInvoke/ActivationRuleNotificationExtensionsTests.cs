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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Dictionary;
using Jube.Engine.EntityAnalysisModelInvoke.Context.Extensions.ActivationRules;
using Jube.Engine.EntityAnalysisModelInvoke.Models;
using Jube.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload;
using Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.Models;
using Jube.Test.Infrastructure;
using Xunit;
using Context = Jube.Engine.EntityAnalysisModelInvoke.Context.Context;
using EntityAnalysisModel = Jube.Engine.EntityAnalysisModelManager.EntityAnalysisModel.EntityAnalysisModel;

namespace Jube.Test.Engine.EntityAnalysisModelInvoke
{
    [Trait("Category", "Unit")]
    public sealed class ActivationRuleNotificationExtensionsTests
    {
        private static Context NewContext(IReadOnlyDictionary<string, string>? environmentOverrides = null)
        {
            var entityAnalysisModel = new EntityAnalysisModel
            {
                ConcurrentQueues =
                {
                    PendingNotifications = new ConcurrentQueue<Notification>()
                }
            };

            return new Context
            {
                EntityAnalysisModel = entityAnalysisModel,
                EntityAnalysisModelInstanceEntryPayload = new EntityAnalysisModelInstanceEntryPayload
                {
                    Payload = new DictionaryNoBoxing<string>(),
                    EntityAnalysisModelInstanceEntryGuid = Guid.NewGuid()
                },
                Environment = TestDynamicEnvironment.Create(environmentOverrides),
                Stopwatch = Stopwatch.StartNew(),
                Log = TestLog.NoOp,
                LogSampled = true
            };
        }

        private static EntityAnalysisModelActivationRule NewRule(bool enableNotification = true)
        {
            return new EntityAnalysisModelActivationRule
            {
                Guid = Guid.NewGuid(),
                EnableNotification = enableNotification,
                NotificationTypeId = 1,
                NotificationDestination = "ops@example.test",
                NotificationSubject = "Alert",
                NotificationBody = "Body"
            };
        }

        [Fact]
        public async Task DoesNothingWhenNotificationIsGloballyDisabledAsync()
        {
            var context = NewContext(new Dictionary<string, string> { ["EnableNotification"] = "False" });
            var rule = NewRule();

            await context.ActivationRuleNotificationAsync(rule, false, null, null);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenSuppressedAsync()
        {
            var context = NewContext();
            var rule = NewRule();

            await context.ActivationRuleNotificationAsync(rule, true, null, null);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenTheRuleHasNotificationDisabledAsync()
        {
            var context = NewContext();
            var rule = NewRule(false);

            await context.ActivationRuleNotificationAsync(rule, false, null, null);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().BeEmpty();
        }

        [Fact]
        public async Task DoesNothingWhenThisIsAReprocessingRunAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelReprocessingRuleInstanceId = 9;
            var rule = NewRule();

            await context.ActivationRuleNotificationAsync(rule, false, null, null);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().BeEmpty();
        }

        [Fact]
        public async Task EnqueuesToThePendingNotificationsQueueWhenAmqpIsDisabledAsync()
        {
            var context = NewContext();
            var rule = NewRule();

            await context.ActivationRuleNotificationAsync(rule, false, null, null);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().ContainSingle();
            var notification = context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Single();
            notification.NotificationTypeId.Should().Be(1);
            notification.NotificationDestination.Should().Be("ops@example.test");
            notification.NotificationSubject.Should().Be("Alert");
            notification.NotificationBody.Should().Be("Body");
        }

        [Fact]
        public async Task ReplacesPayloadTokensInDestinationSubjectAndBodyAsync()
        {
            var context = NewContext();
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("AccountId", "Test5");
            context.EntityAnalysisModelInstanceEntryPayload.Payload.Add("CurrencyAmount", 123.45);
            var rule = NewRule();
            rule.NotificationDestination = "[@Payload.AccountId@]@example.test";
            rule.NotificationSubject = "Alert for [@Payload.AccountId@]";
            rule.NotificationBody = "Amount was [@Payload.CurrencyAmount@]";

            await context.ActivationRuleNotificationAsync(rule, false, null, null);

            var notification = context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Single();
            notification.NotificationDestination.Should().Be("Test5@example.test");
            notification.NotificationSubject.Should().Be("Alert for Test5");
            notification.NotificationBody.Should().Be("Amount was 123.45");
        }

        [Fact]
        public async Task ReturnsWithoutEnqueuingWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync()
        {
            var context = NewContext(new Dictionary<string, string> { ["ActivationRuleIdempotency"] = "True" });
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();

            await context.ActivationRuleNotificationAsync(rule, false, null, cacheService);
            await context.ActivationRuleNotificationAsync(rule, false, null, cacheService);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().ContainSingle();
        }

        [Fact]
        public async Task EnqueuesWhenIdempotencyIsEnabledAndTheClaimSucceedsAsync()
        {
            var context = NewContext(new Dictionary<string, string> { ["ActivationRuleIdempotency"] = "True" });
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();

            await context.ActivationRuleNotificationAsync(rule, false, null, cacheService);

            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().ContainSingle();
        }

        [Fact]
        public async Task NotificationIdempotencyIsClaimedIndependentlyFromCaseCreationIdempotencyAsync()
        {
            var context = NewContext(new Dictionary<string, string> { ["ActivationRuleIdempotency"] = "True" });
            var cacheService = TestCacheService.Create(out _);
            var rule = NewRule();

            var caseClaimed = await cacheService.CacheActivationCaseIdempotencyRepository
                .CheckAndClaimIdempotencyAsync(
                    context.EntityAnalysisModel.Instance.TenantRegistryId,
                    context.EntityAnalysisModel.Instance.Guid,
                    context.EntityAnalysisModelInstanceEntryPayload.EntityAnalysisModelInstanceEntryGuid,
                    rule.Guid);

            await context.ActivationRuleNotificationAsync(rule, false, null, cacheService);

            caseClaimed.Should().BeTrue();
            context.EntityAnalysisModel.ConcurrentQueues.PendingNotifications.Should().ContainSingle();
        }
    }
}