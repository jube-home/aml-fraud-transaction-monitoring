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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Redis;
using Jube.Cache.Redis.Models;
using Jube.ResilientRedisConnection;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Redis
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class CacheAbstractionRepositoryTests
    {
        [Fact]
        public async Task UpsertThenGetPreferReplicaReturnsStoredValueAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheAbstractionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertAsync(1, modelGuid, "AccountId", "12345", "RuleA", 42.5);
            var result = await repository.GetPreferReplicaAsync(1, modelGuid, "RuleA", "AccountId", "12345");

            result.Should().Be(42.5);
        }

        [Fact]
        public async Task GetPreferReplicaOnMissingKeyReturnsNullAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheAbstractionRepository(fake, TestLog.NoOp);

            var result = await repository.GetPreferReplicaAsync(1, Guid.NewGuid(), "RuleA", "AccountId", "12345");

            result.Should().BeNull();
        }

        [Fact]
        public async Task DeleteRemovesTheStoredValueAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheAbstractionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertAsync(1, modelGuid, "AccountId", "12345", "RuleA", 42.5);
            await repository.DeleteAsync(1, modelGuid, "AccountId", "12345", "RuleA");
            var result = await repository.GetPreferReplicaAsync(1, modelGuid, "RuleA", "AccountId", "12345");

            result.Should().BeNull();
        }

        [Fact]
        public async Task UpsertUsesDocumentedAbstractionKeyFormatAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheAbstractionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertAsync(7, modelGuid, "AccountId", "999", "RuleA", 1.0);

            var raw = await fake.HashGetAsync($"Abstraction:7:{modelGuid:N}:AccountId:999", "RuleA");
            ((double)raw).Should().Be(1.0);
        }

        [Fact]
        public async Task GetAsyncGroupsBySearchKeyAndSearchValueAndReturnsZeroForMissingRuleAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase();
            var repository = new CacheAbstractionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertAsync(1, modelGuid, "AccountId", "111", "RuleA", 10);
            await repository.UpsertAsync(1, modelGuid, "Ip", "1.2.3.4", "RuleB", 20);

            var requests = new List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>
            {
                new() { AbstractionRuleName = "RuleA", SearchKey = "AccountId", SearchValue = "111" },
                new() { AbstractionRuleName = "RuleMissing", SearchKey = "AccountId", SearchValue = "111" },
                new() { AbstractionRuleName = "RuleB", SearchKey = "Ip", SearchValue = "1.2.3.4" }
            };

            var result = await repository.GetAsync(1, modelGuid, requests);

            result["RuleA"].Should().Be(10);
            result["RuleB"].Should().Be(20);
            result["RuleMissing"].Should().Be(0);
        }

        [Fact]
        public async Task GetAsyncWhenOneGroupFailsStillReturnsOtherGroupsAsync()
        {
            var fake = new FakeHybridResilientRedisDatabase
            {
                ThrowOnMethod = nameof(IHybridResilientRedisDatabase.HashGetAsync)
            };
            var repository = new CacheAbstractionRepository(fake, TestLog.NoOp);
            var modelGuid = Guid.NewGuid();

            await repository.UpsertAsync(1, modelGuid, "Ip", "1.2.3.4", "RuleB", 20);

            var requests = new List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValue>
            {
                new() { AbstractionRuleName = "RuleA", SearchKey = "AccountId", SearchValue = "111" },
                new() { AbstractionRuleName = "RuleB", SearchKey = "Ip", SearchValue = "1.2.3.4" }
            };

            var result = await repository.GetAsync(1, modelGuid, requests);

            result.Should().NotContainKey("RuleA");
            result["RuleB"].Should().Be(20);
        }
    }
}