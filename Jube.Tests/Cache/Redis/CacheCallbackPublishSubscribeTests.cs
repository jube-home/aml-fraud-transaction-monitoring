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
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.ResilientRedisConnection;
using Jube.Test.Infrastructure;
using Xunit;
using CallbackDto = Jube.Cache.Redis.Callback.Callback;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheCallbackPublishSubscribeTests
    {
        private static readonly string hostName = Dns.GetHostName();

        private static (CacheCallbackPublishSubscribe Instance, TestLog Log,
            ConcurrentDictionary<Guid, TaskCompletionSource<CallbackDto>> Callbacks,
            FakeHybridResilientRedisDatabase Fake) NewContext(string instanceGuid = "instance-guid")
        {
            var callbacks = new ConcurrentDictionary<Guid, TaskCompletionSource<CallbackDto>>();
            var log = new TestLog();
            var fake = new FakeHybridResilientRedisDatabase();
            var instance = new CacheCallbackPublishSubscribe(callbacks, log, instanceGuid, fake);
            return (instance, log, callbacks, fake);
        }

        [Fact]
        public void AddToDictionaryFromSubscriptionWithSelfOriginChannelIsIgnored()
        {
            var (instance, _, callbacks, _) = NewContext("my-instance");
            var guid = Guid.NewGuid();
            var channel = $"CallbackSet:{hostName}:my-instance:{guid:N}";

            instance.AddToDictionaryFromSubscription(channel, "payload"u8.ToArray());

            callbacks.Should().BeEmpty();
        }

        [Fact]
        public void AddToDictionaryFromSubscriptionWithOtherOriginChannelAddsToCallbacks()
        {
            var (instance, _, callbacks, _) = NewContext("my-instance");
            var guid = Guid.NewGuid();
            var channel = $"CallbackSet:other-host:other-instance:{guid:N}";
            var payload = "payload"u8.ToArray();

            instance.AddToDictionaryFromSubscription(channel, payload);

            callbacks.Should().ContainKey(guid);
            var tcs = callbacks[guid];
            tcs.Task.IsCompletedSuccessfully.Should().BeTrue();

#pragma warning disable VSTHRD002, AsyncFixer02, VSTHRD103
            tcs.Task.Result.Payload.Should().Equal(payload);
#pragma warning restore VSTHRD002, AsyncFixer02, VSTHRD103
        }

        [Fact]
        public void AddToDictionaryFromSubscriptionWithTooFewChannelSegmentsIsCaughtAndLogged()
        {
            var (instance, log, callbacks, _) = NewContext("my-instance");

            var act = () => instance.AddToDictionaryFromSubscription("CallbackSet:onlyhost", "payload"u8.ToArray());

            act.Should().NotThrow();
            callbacks.Should().BeEmpty();
            log.Entries.Should().Contain(e => e.Level == "INFO");
        }

        [Fact]
        public void AddToDictionaryFromSubscriptionWithUnparsableGuidIsCaughtAndLogged()
        {
            var (instance, log, callbacks, _) = NewContext("my-instance");
            var channel = "CallbackSet:other-host:other-instance:not-a-guid";

            var act = () => instance.AddToDictionaryFromSubscription(channel, "payload"u8.ToArray());

            act.Should().NotThrow();
            callbacks.Should().BeEmpty();
            log.Entries.Should().Contain(e => e.Level == "INFO");
        }

        [Fact]
        public void RemoveFromDictionaryFromSubscriptionWithSelfOriginChannelIsIgnored()
        {
            var (instance, _, callbacks, _) = NewContext("my-instance");
            var guid = Guid.NewGuid();
            callbacks.TryAdd(guid, new TaskCompletionSource<CallbackDto>());
            var channel = $"CallbackRemove:{hostName}:my-instance";

            instance.RemoveFromDictionaryFromSubscription(channel, guid.ToString("N"));

            callbacks.Should().ContainKey(guid);
        }

        [Fact]
        public void RemoveFromDictionaryFromSubscriptionWithOtherOriginChannelRemovesFromCallbacks()
        {
            var (instance, _, callbacks, _) = NewContext("my-instance");
            var guid = Guid.NewGuid();
            callbacks.TryAdd(guid, new TaskCompletionSource<CallbackDto>());
            var channel = "CallbackRemove:other-host:other-instance";

            instance.RemoveFromDictionaryFromSubscription(channel, guid.ToString("N"));

            callbacks.Should().NotContainKey(guid);
        }

        [Fact]
        public void RemoveFromDictionaryFromSubscriptionWithUnparsableValueIsCaughtAndLogged()
        {
            var (instance, log, _, _) = NewContext("my-instance");
            var channel = "CallbackRemove:other-host:other-instance";

            var act = () => instance.RemoveFromDictionaryFromSubscription(channel, "not-a-guid");

            act.Should().NotThrow();
            log.Entries.Should().Contain(e => e.Level == "INFO");
        }

        [Fact]
        public void AddToDictionaryForNewGuidCreatesAnAlreadyCompletedTaskCompletionSource()
        {
            var (instance, _, callbacks, _) = NewContext();
            var guid = Guid.NewGuid();
            var payload = "hello"u8.ToArray();

            instance.AddToDictionary(payload, guid);

            callbacks.Should().ContainKey(guid);
            var tcs = callbacks[guid];
            tcs.Task.IsCompletedSuccessfully.Should().BeTrue();

#pragma warning disable VSTHRD002, AsyncFixer02, VSTHRD103
            tcs.Task.Result.Payload.Should().Equal(payload);
#pragma warning restore VSTHRD002, AsyncFixer02, VSTHRD103
        }

        [Fact]
        public void AddToDictionaryForExistingPendingTaskCompletionSourceCompletesTheSameInstance()
        {
            var (instance, _, callbacks, _) = NewContext();
            var guid = Guid.NewGuid();
            var pending = new TaskCompletionSource<CallbackDto>();
            callbacks.TryAdd(guid, pending);
            var payload = "hello"u8.ToArray();

            instance.AddToDictionary(payload, guid);

            callbacks.Should().HaveCount(1);
            ReferenceEquals(callbacks[guid], pending).Should().BeTrue(
                "AddToDictionary should complete the existing pending TaskCompletionSource rather than replacing it");
            pending.Task.IsCompletedSuccessfully.Should().BeTrue();

#pragma warning disable VSTHRD002, AsyncFixer02, VSTHRD103
            pending.Task.Result.Payload.Should().Equal(payload);
#pragma warning restore VSTHRD002, AsyncFixer02, VSTHRD103
        }

        [Fact]
        public void SweepExpiredCallbacksRemovesOnlyCompletedEntriesOlderThanThreshold()
        {
            var (instance, _, callbacks, _) = NewContext();
            var now = DateTime.UtcNow;
            var threshold = now;

            var expiredGuid = Guid.NewGuid();
            var expiredTcs = new TaskCompletionSource<CallbackDto>();
            expiredTcs.SetResult(new CallbackDto { CreatedDate = now.AddMinutes(-5), Payload = [] });
            callbacks.TryAdd(expiredGuid, expiredTcs);

            var freshGuid = Guid.NewGuid();
            var freshTcs = new TaskCompletionSource<CallbackDto>();
            freshTcs.SetResult(new CallbackDto { CreatedDate = now.AddMinutes(5), Payload = [] });
            callbacks.TryAdd(freshGuid, freshTcs);

            instance.SweepExpiredCallbacks(threshold);

            callbacks.Should()
                .NotContainKey(expiredGuid, "a completed callback older than the threshold should be swept");
            callbacks.Should().ContainKey(freshGuid, "a completed callback newer than the threshold should be kept");
        }

        [Fact]
        public void SweepExpiredCallbacksWithNoEntriesDoesNothing()
        {
            var (instance, _, callbacks, _) = NewContext();

            var act = () => instance.SweepExpiredCallbacks(DateTime.UtcNow);

            act.Should().NotThrow();
            callbacks.Should().BeEmpty();
        }

        [Fact]
        public void SweepExpiredCallbacksDoesNotTouchAPendingCallbackOnItsFirstObservation()
        {
            var (instance, _, callbacks, _) = NewContext();
            var pendingGuid = Guid.NewGuid();
            var pendingTcs = new TaskCompletionSource<CallbackDto>();
            callbacks.TryAdd(pendingGuid, pendingTcs);

            instance.SweepExpiredCallbacks(DateTime.UtcNow.AddDays(-1));

            callbacks.Should().ContainKey(pendingGuid);
            pendingTcs.Task.IsFaulted.Should().BeFalse();
        }

        [Fact]
        public void SweepExpiredCallbacksFaultsAndRemovesAPendingCallbackOnceItHasNeverArrivedPastItsTimeout()
        {
            var (instance, _, callbacks, _) = NewContext();
            var pendingGuid = Guid.NewGuid();
            var pendingTcs = new TaskCompletionSource<CallbackDto>();
            callbacks.TryAdd(pendingGuid, pendingTcs);

            instance.SweepExpiredCallbacks(DateTime.UtcNow.AddDays(-1));
            instance.SweepExpiredCallbacks(DateTime.UtcNow.AddSeconds(1));

            callbacks.Should().NotContainKey(pendingGuid,
                "a pending callback that has never arrived should be swept once past its timeout");
            pendingTcs.Task.IsFaulted.Should().BeTrue(
                "the awaiting caller should be told the callback timed out, not left hanging forever");
            pendingTcs.Task.Exception!.InnerException.Should().BeOfType<TimeoutException>();
        }

        [Fact]
        public void SweepExpiredCallbacksDoesNotFaultAPendingCallbackStillWithinItsTimeout()
        {
            var (instance, _, callbacks, _) = NewContext();
            var pendingGuid = Guid.NewGuid();
            var pendingTcs = new TaskCompletionSource<CallbackDto>();
            callbacks.TryAdd(pendingGuid, pendingTcs);

            instance.SweepExpiredCallbacks(DateTime.UtcNow.AddDays(-1));
            instance.SweepExpiredCallbacks(DateTime.UtcNow.AddDays(-1));

            callbacks.Should().ContainKey(pendingGuid);
            pendingTcs.Task.IsFaulted.Should().BeFalse();
        }

        [Fact]
        public void SweepExpiredCallbacksIgnoresAPendingEntryThatIsSubsequentlyRemovedBeforeItExpires()
        {
            var (instance, _, callbacks, _) = NewContext();
            var pendingGuid = Guid.NewGuid();
            var pendingTcs = new TaskCompletionSource<CallbackDto>();
            callbacks.TryAdd(pendingGuid, pendingTcs);

            instance.SweepExpiredCallbacks(DateTime.UtcNow.AddDays(-1));
            callbacks.TryRemove(pendingGuid, out _);

            var act = () => instance.SweepExpiredCallbacks(DateTime.UtcNow.AddSeconds(1));

            act.Should().NotThrow();
            callbacks.Should().NotContainKey(pendingGuid);
        }

        [Fact]
        public async Task PublishAsyncAddsToCallbacksAndPublishesToCallbackSetChannelAsync()
        {
            var (instance, _, callbacks, fake) = NewContext("my-instance");
            var guid = Guid.NewGuid();
            var payload = Encoding.UTF8.GetBytes("json-payload");

            await instance.PublishAsync(payload, guid);

            callbacks.Should().ContainKey(guid);

#pragma warning disable VSTHRD003
            (await callbacks[guid].Task).Payload.Should().Equal(payload);
#pragma warning restore VSTHRD003

            var message = fake.PublishedMessages.Should().ContainSingle().Subject;
            message.Channel.ToString().Should().Be($"CallbackSet:{hostName}:my-instance:{guid:N}");
            ((byte[])message.Message)!.Should().Equal(payload);
        }

        [Fact]
        public async Task DeleteAsyncRemovesFromCallbacksAndPublishesToCallbackRemoveChannelAsync()
        {
            var (instance, _, callbacks, fake) = NewContext("my-instance");
            var guid = Guid.NewGuid();
            callbacks.TryAdd(guid, new TaskCompletionSource<CallbackDto>());

            await instance.DeleteAsync(guid);

            callbacks.Should().NotContainKey(guid);

            var message = fake.PublishedMessages.Should().ContainSingle().Subject;
            message.Channel.ToString().Should().Be($"CallbackRemove:{hostName}:my-instance");
            ((string)message.Message!).Should().Be(guid.ToString("N"));
        }

        [Fact]
        public async Task PublishAsyncSwallowsExceptionsAndLogsThemInsteadAsync()
        {
            var (instance, log, _, fake) = NewContext();
            fake.ThrowOnMethod = nameof(IHybridResilientRedisDatabase.PublishAsync);

            var act = async () => await instance.PublishAsync("payload"u8.ToArray(), Guid.NewGuid());

            await act.Should().NotThrowAsync();
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }
    }
}