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

using System.Linq;
using FluentAssertions;
using Jube.Cache;
using Jube.Cache.Observability;
using Jube.Data.Repository;
using Jube.Test.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class RedisReconnectRetryPolicyTests
    {
        [Fact]
        public void ShouldRetryDelegatesToInnerPolicyAndForwardsArguments()
        {
            var inner = new FakeInnerRetryPolicy(true);
            var policy = new RedisReconnectRetryPolicy(inner, TestLog.NoOp);

            var result = policy.ShouldRetry(3, 250);

            result.Should().BeTrue();
            inner.CallCount.Should().Be(1);
            inner.LastCurrentRetryCount.Should().Be(3);
            inner.LastTimeElapsedMilliseconds.Should().Be(250);
        }

        [Fact]
        public void ShouldRetryReturnsFalseWithoutEnqueueingAnEventWhenInnerPolicyDeclines()
        {
            RedisConnectionEventCapture.DrainAll();
            var inner = new FakeInnerRetryPolicy(false);
            var policy = new RedisReconnectRetryPolicy(inner, TestLog.NoOp);

            var result = policy.ShouldRetry(1, 100);

            result.Should().BeFalse();
            RedisConnectionEventCapture.DrainAll().Should().BeEmpty();
        }

        [Fact]
        public void ShouldRetryEnqueuesAReconnectRetryEventWhenInnerPolicyAgrees()
        {
            RedisConnectionEventCapture.DrainAll();
            var inner = new FakeInnerRetryPolicy(true);
            var policy = new RedisReconnectRetryPolicy(inner, TestLog.NoOp);

            policy.ShouldRetry(4, 500);

            var drained = RedisConnectionEventCapture.DrainAll();
            drained.Should().ContainSingle();
            var record = drained.Single();
            record.EventType.Should().Be(RedisConnectionEventType.ReconnectRetry);
            record.RetryCount.Should().Be(5);
            record.BackoffMilliseconds.Should().Be(500);
        }

        [Fact]
        public void ShouldRetryReportsTransactionsImpactedFromCacheDiagnosticsInFlightCount()
        {
            RedisConnectionEventCapture.DrainAll();
            var inner = new FakeInnerRetryPolicy(true);
            var policy = new RedisReconnectRetryPolicy(inner, TestLog.NoOp);

            CacheDiagnostics.Record("test-wrapper", () => policy.ShouldRetry(0, 10));

            var record = RedisConnectionEventCapture.DrainAll().Single();
            record.TransactionsImpacted.Should().BeGreaterThanOrEqualTo(1,
                "ShouldRetry ran inside our own CacheDiagnostics.Record wrapper, which guarantees at least " +
                "one in-flight call for its whole duration");
        }

        private sealed class FakeInnerRetryPolicy(bool shouldRetry) : IReconnectRetryPolicy
        {
            public int CallCount { get; private set; }
            public long LastCurrentRetryCount { get; private set; }
            public int LastTimeElapsedMilliseconds { get; private set; }

            public bool ShouldRetry(long currentRetryCount, int timeElapsedMillisecondsSinceLastRetry)
            {
                CallCount++;
                LastCurrentRetryCount = currentRetryCount;
                LastTimeElapsedMilliseconds = timeElapsedMillisecondsSinceLastRetry;
                return shouldRetry;
            }
        }
    }
}