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
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Cache.Resp;
using Jube.ResilientRespConnection.Interfaces;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.Cache.Resp
{
    [Trait("Category", "Unit")]
    [Collection("JubeCache")]
    public sealed class IdempotencyRepositoriesTests
    {
        public delegate Task<bool> CheckAndClaim(int tenantRegistryId, Guid entityAnalysisModelGuid,
            Guid ruleOrCounterGuid, Guid entityAnalysisModelInstanceEntryGuid);

        public static IEnumerable<object[]> Repositories()
        {
            yield return
            [
                "ActivationCaseIdempotency",
                (Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim>)((fake, log) =>
                    new CacheActivationCaseIdempotencyRepository(fake, log.Log).CheckAndClaimIdempotencyAsync)
            ];
            yield return
            [
                "ActivationNotificationIdempotency",
                (Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim>)((fake, log) =>
                    new CacheActivationNotificationIdempotencyRepository(fake, log.Log).CheckAndClaimIdempotencyAsync)
            ];
            yield return
            [
                "TtlCounterIdempotency",
                (Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim>)((fake, log) =>
                    new CacheTtlCounterIdempotencyRepository(fake, log.Log).CheckAndClaimIdempotencyAsync)
            ];
        }

        [Theory]
        [MemberData(nameof(Repositories))]
        public async Task FirstClaimSucceedsAndSecondClaimForSameTransactionIsReportedAsDuplicateAsync(
            string keyPrefix, Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim> factory)
        {
            _ = keyPrefix;
            var fake = new FakeHybridResilientRespDatabase();
            var checkAndClaim = factory(fake, new LogAdapter(TestLog.NoOp));
            var modelGuid = Guid.NewGuid();
            var ruleGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();

            var first = await checkAndClaim(1, modelGuid, ruleGuid, entryGuid);
            var second = await checkAndClaim(1, modelGuid, ruleGuid, entryGuid);

            first.Should().BeTrue("the first claim for this transaction is not a duplicate");
            second.Should().BeFalse("the same transaction has already claimed idempotency for this rule");
        }

        [Theory]
        [MemberData(nameof(Repositories))]
        public async Task DifferentTransactionEntryGuidsDoNotCollideAsync(string keyPrefix,
            Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim> factory)
        {
            _ = keyPrefix;
            var fake = new FakeHybridResilientRespDatabase();
            var checkAndClaim = factory(fake, new LogAdapter(TestLog.NoOp));
            var modelGuid = Guid.NewGuid();
            var ruleGuid = Guid.NewGuid();

            var first = await checkAndClaim(1, modelGuid, ruleGuid, Guid.NewGuid());
            var second = await checkAndClaim(1, modelGuid, ruleGuid, Guid.NewGuid());

            first.Should().BeTrue();
            second.Should().BeTrue("a different transaction entry guid is not a duplicate of the first");
        }

        [Theory]
        [MemberData(nameof(Repositories))]
        public async Task ClaimRecordsTheKeyInTheSharedIdempotencyJournalForCleanupAsync(string keyPrefix,
            Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim> factory)
        {
            var fake = new FakeHybridResilientRespDatabase();
            var checkAndClaim = factory(fake, new LogAdapter(TestLog.NoOp));
            var modelGuid = Guid.NewGuid();
            var ruleGuid = Guid.NewGuid();

            await checkAndClaim(3, modelGuid, ruleGuid, Guid.NewGuid());

            var journalMembers = await fake.SetMembersAsync($"IdempotencyJournal:3:{modelGuid:N}");
            var expectedKey = $"{keyPrefix}:3:{modelGuid:N}:{ruleGuid:N}";

            journalMembers.Should().Contain(m => m.ToString() == expectedKey);
        }

        [Theory]
        [MemberData(nameof(Repositories))]
        public async Task OnUnderlyingFailureMidClaimReturnsFalseSinceTheClaimMayNotHaveBeenRecordedAsync(
            string keyPrefix, Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim> factory)
        {
            _ = keyPrefix;
            var log = new TestLog();

            var fake = new FakeHybridResilientRespDatabase
            {
                ThrowOnMethod = nameof(IHybridResilientRespDatabase.SetAddAsync)
            };

            var checkAndClaim = factory(fake, new LogAdapter(log));

            var result = await checkAndClaim(1, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

            result.Should().BeFalse("fail-closed: an underlying error must not be treated as a fresh claim");
            log.Entries.Should().Contain(e => e.Level == "ERROR");
        }

        [Theory]
        [MemberData(nameof(Repositories))]
        public async Task ConcurrentClaimsForTheSameTransactionAdmitExactlyOneAsync(string keyPrefix,
            Func<FakeHybridResilientRespDatabase, LogAdapter, CheckAndClaim> factory)
        {
            _ = keyPrefix;
            var fake = new FakeHybridResilientRespDatabase();
            var checkAndClaim = factory(fake, new LogAdapter(TestLog.NoOp));
            var modelGuid = Guid.NewGuid();
            var ruleGuid = Guid.NewGuid();
            var entryGuid = Guid.NewGuid();

            var results = await Task.WhenAll(Enumerable.Range(0, 32)
                .Select(_ => Task.Run(() => checkAndClaim(1, modelGuid, ruleGuid, entryGuid))));

            results.Count(r => r).Should().Be(1, "the claim is a single atomic set add");
        }

        public sealed class LogAdapter(TestLog log)
        {
            public TestLog Log { get; } = log;
        }
    }
}