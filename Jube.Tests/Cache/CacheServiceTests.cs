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
using System.Net;
using FluentAssertions;
using Jube.Cache;
using Jube.Data.Repository;
using Jube.Test.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheServiceTests
    {
        private static readonly EndPoint endPoint = new DnsEndPoint("redis-host", 6379);

        [Fact]
        public void HandleConnectionFailedIncrementsCounterCapturesEventAndLogsError()
        {
            RedisConnectionEventCapture.DrainAll();
            var diagnostics = new RedisConnectionDiagnosticsCounters();
            var log = new TestLog();
            var exception = new InvalidOperationException("boom");
            var args = new ConnectionFailedEventArgs(new object(), endPoint, ConnectionType.Interactive,
                ConnectionFailureType.SocketClosed, exception, "physical");

            CacheService.HandleConnectionFailed(diagnostics, log, args);

            diagnostics.TakeSnapshot().ConnectionFailedCount.Should().Be(1);

            var captured = RedisConnectionEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].EventType.Should().Be(RedisConnectionEventType.ConnectionFailed);
            captured[0].EndPoint.Should().Be(endPoint.ToString());
            captured[0].ConnectionType.Should().Be(ConnectionType.Interactive);
            captured[0].FailureType.Should().Be(ConnectionFailureType.SocketClosed);
            captured[0].Exception.Should().Be(exception.ToString());

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR" && e.Message.Contains("connection failed"));
        }

        [Fact]
        public void HandleConnectionRestoredIncrementsCounterCapturesEventAndLogsWarning()
        {
            RedisConnectionEventCapture.DrainAll();
            var diagnostics = new RedisConnectionDiagnosticsCounters();
            var log = new TestLog();
            var args = new ConnectionFailedEventArgs(new object(), endPoint, ConnectionType.Subscription,
                ConnectionFailureType.None, null!, "physical");

            CacheService.HandleConnectionRestored(diagnostics, log, args);

            diagnostics.TakeSnapshot().ConnectionRestoredCount.Should().Be(1);

            var captured = RedisConnectionEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].EventType.Should().Be(RedisConnectionEventType.ConnectionRestored);
            captured[0].ConnectionType.Should().Be(ConnectionType.Subscription);

            log.Entries.Should().ContainSingle(e => e.Level == "WARN" && e.Message.Contains("connection restored"));
        }

        [Fact]
        public void HandleErrorMessageIncrementsCounterCapturesEventAndLogsError()
        {
            RedisConnectionEventCapture.DrainAll();
            var diagnostics = new RedisConnectionDiagnosticsCounters();
            var log = new TestLog();
            var args = new RedisErrorEventArgs(new object(), endPoint, "ERR something went wrong");

            CacheService.HandleErrorMessage(diagnostics, log, args);

            diagnostics.TakeSnapshot().ErrorMessageCount.Should().Be(1);

            var captured = RedisConnectionEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].EventType.Should().Be(RedisConnectionEventType.ErrorMessage);
            captured[0].Message.Should().Be("ERR something went wrong");

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR" && e.Message.Contains("returned error message"));
        }

        [Fact]
        public void HandleInternalErrorIncrementsCounterCapturesEventAndLogsError()
        {
            RedisConnectionEventCapture.DrainAll();
            var diagnostics = new RedisConnectionDiagnosticsCounters();
            var log = new TestLog();
            var exception = new InvalidOperationException("internal boom");
            var args = new InternalErrorEventArgs(new object(), endPoint, ConnectionType.Interactive, exception,
                "ReadFromPipe");

            CacheService.HandleInternalError(diagnostics, log, args);

            diagnostics.TakeSnapshot().InternalErrorCount.Should().Be(1);

            var captured = RedisConnectionEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].EventType.Should().Be(RedisConnectionEventType.InternalError);
            captured[0].Origin.Should().Be("ReadFromPipe");
            captured[0].Exception.Should().Be(exception.ToString());

            log.Entries.Should().ContainSingle(e => e.Level == "ERROR" && e.Message.Contains("internal error"));
        }

        [Fact]
        public void HandleConfigurationChangedIncrementsCounterCapturesEventAndLogsWarning()
        {
            RedisConnectionEventCapture.DrainAll();
            var diagnostics = new RedisConnectionDiagnosticsCounters();
            var log = new TestLog();
            var args = new EndPointEventArgs(new object(), endPoint);

            CacheService.HandleConfigurationChanged(diagnostics, log, args);

            diagnostics.TakeSnapshot().ConfigurationChangedCount.Should().Be(1);

            var captured = RedisConnectionEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].EventType.Should().Be(RedisConnectionEventType.ConfigurationChanged);

            log.Entries.Should().ContainSingle(e => e.Level == "WARN" && e.Message.Contains("configuration changed"));
        }

        [Fact]
        public void HandleConfigurationChangedBroadcastIncrementsCounterCapturesEventAndLogsWarning()
        {
            RedisConnectionEventCapture.DrainAll();
            var diagnostics = new RedisConnectionDiagnosticsCounters();
            var log = new TestLog();
            var args = new EndPointEventArgs(new object(), endPoint);

            CacheService.HandleConfigurationChangedBroadcast(diagnostics, log, args);

            diagnostics.TakeSnapshot().ConfigurationChangedBroadcastCount.Should().Be(1);

            var captured = RedisConnectionEventCapture.DrainAll();
            captured.Should().ContainSingle();
            captured[0].EventType.Should().Be(RedisConnectionEventType.ConfigurationChangedBroadcast);

            log.Entries.Should()
                .ContainSingle(e => e.Level == "WARN" && e.Message.Contains("configuration change broadcast"));
        }
    }
}