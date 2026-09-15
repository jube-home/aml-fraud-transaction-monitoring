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

using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis.CacheUserRegistryApiKey;
using Jube.Cache.Redis.CacheUserRegistryApiKey.Events;
using Jube.ResilientRedisConnection;
using Jube.Test.Infrastructure;
using StackExchange.Redis;
using Xunit;

namespace Jube.Test.Cache.Redis.CacheUserRegistryApiKey
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheUserRegistryApiKeyRepositoryTests
    {
        private static CacheUserRegistryApiKeyRepositoryTestContext NewContext()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var log = new TestLog();
            var repository = new CacheUserRegistryApiKeyRepository(fake, log);
            return new CacheUserRegistryApiKeyRepositoryTestContext(fake, log, repository);
        }

        [Fact]
        public void HandleSetMessageRaisesOnCaseUserRegistryApiKeySetEventWithApiKeyHash()
        {
            var context = NewContext();
            CaseUserRegistryKeyEventArguments? received = null;
            context.Repository.OnCaseUserRegistryApiKeySetEvent += (_, args) => received = args;

            context.Repository.HandleSetMessage(new RedisValue("hash-abc"));

            received.Should().NotBeNull();
            received!.ApiKeyHash.Should().Be("hash-abc");
        }

        [Fact]
        public void HandleRemoveMessageRaisesOnCaseUserRegistryApiKeyRemoveEventWithApiKeyHash()
        {
            var context = NewContext();
            CaseUserRegistryKeyEventArguments? received = null;
            context.Repository.OnCaseUserRegistryApiKeyRemoveEvent += (_, args) => received = args;

            context.Repository.HandleRemoveMessage(new RedisValue("hash-xyz"));

            received.Should().NotBeNull();
            received!.ApiKeyHash.Should().Be("hash-xyz");
        }

        [Fact]
        public void HandleSetMessageWithNoSubscribersDoesNotThrow()
        {
            var context = NewContext();

            var act = () => context.Repository.HandleSetMessage(new RedisValue("hash-abc"));

            act.Should().NotThrow();
        }

        [Fact]
        public void HandleRemoveMessageWithNoSubscribersDoesNotThrow()
        {
            var context = NewContext();

            var act = () => context.Repository.HandleRemoveMessage(new RedisValue("hash-abc"));

            act.Should().NotThrow();
        }

        [Fact]
        public async Task PublishSetAsyncPublishesToUserRegistryApiKeySetChannelWithHostNameAsync()
        {
            var context = NewContext();
            var hostName = Dns.GetHostName();

            await context.Repository.PublishSetAsync("hash-set-value");

            var message = context.Fake.PublishedMessages.Should().ContainSingle().Subject;
            message.Channel.ToString().Should().Be($"UserRegistryApiKeySet:{hostName}");
            ((string)message.Message!).Should().Be("hash-set-value");
        }

        [Fact]
        public async Task PublishRemoveAsyncPublishesToUserRegistryApiKeyRemoveChannelWithHostNameAsync()
        {
            var context = NewContext();
            var hostName = Dns.GetHostName();

            await context.Repository.PublishRemoveAsync("hash-remove-value");

            var message = context.Fake.PublishedMessages.Should().ContainSingle().Subject;
            message.Channel.ToString().Should().Be($"UserRegistryApiKeyRemove:{hostName}");
            ((string)message.Message!).Should().Be("hash-remove-value");
        }

        [Fact]
        public async Task PublishSetAsyncSwallowsExceptionsAndLogsThemInsteadAsync()
        {
            var context = NewContext();
            context.Fake.ThrowOnMethod = nameof(IHybridResilientRedisDatabase.PublishAsync);

            var act = async () => await context.Repository.PublishSetAsync("hash");

            await act.Should().NotThrowAsync();
            context.Log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        private sealed record CacheUserRegistryApiKeyRepositoryTestContext(
            FakeHybridResilientRedisDatabase Fake,
            TestLog Log,
            CacheUserRegistryApiKeyRepository Repository);
    }
}