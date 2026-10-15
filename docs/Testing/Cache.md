---
layout: default
title: Cache and Redis
nav_order: 12
parent: Testing
---

# Cache and Redis

Tests of the cache layer: Redis serialisation, callbacks, observability and cache-backed lookups used by the engine.

## General

### CacheServiceSentinelEventTests

`Jube.Test.Cache.CacheServiceSentinelEventTests` — 1 test. Category: Unit. Collection: JubeCache.

| Test                                             | Kind | Verifies                                               |
|--------------------------------------------------|------|--------------------------------------------------------|
| `HandleSentinelEventCapturesEventAndLogsWarning` | Fact | Handle sentinel event captures event and logs warning. |

### CacheServiceTests

`Jube.Test.Cache.CacheServiceTests` — 6 tests. Category: Unit. Collection: JubeCache.

| Test                                                                              | Kind | Verifies                                                                                   |
|-----------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `HandleConnectionFailedIncrementsCounterCapturesEventAndLogsError`                | Fact | Handle connection failed increments counter captures event and logs error.                 |
| `HandleConnectionRestoredIncrementsCounterCapturesEventAndLogsWarning`            | Fact | Handle connection restored increments counter captures event and logs warning.             |
| `HandleErrorMessageIncrementsCounterCapturesEventAndLogsError`                    | Fact | Handle error message increments counter captures event and logs error.                     |
| `HandleInternalErrorIncrementsCounterCapturesEventAndLogsError`                   | Fact | Handle internal error increments counter captures event and logs error.                    |
| `HandleConfigurationChangedIncrementsCounterCapturesEventAndLogsWarning`          | Fact | Handle configuration changed increments counter captures event and logs warning.           |
| `HandleConfigurationChangedBroadcastIncrementsCounterCapturesEventAndLogsWarning` | Fact | Handle configuration changed broadcast increments counter captures event and logs warning. |

### RedisConnectionDiagnosticsCountersTests

`Jube.Test.Cache.RedisConnectionDiagnosticsCountersTests` — 3 tests. Category: Unit. Collection: JubeCache.

| Test                                           | Kind | Verifies                                           |
|------------------------------------------------|------|----------------------------------------------------|
| `TakeSnapshotReflectsEachCounterIndependently` | Fact | Take snapshot reflects each counter independently. |
| `TakeSnapshotResetsAllCountersToZero`          | Fact | Take snapshot resets all counters to zero.         |
| `FreshInstanceStartsAtZero`                    | Fact | Fresh instance starts at zero.                     |

### RedisConnectionEventCaptureTests

`Jube.Test.Cache.RedisConnectionEventCaptureTests` — 3 tests. Category: Unit. Collection: JubeCache.

| Test                                                         | Kind | Verifies                                                               |
|--------------------------------------------------------------|------|------------------------------------------------------------------------|
| `EnqueueThenDrainAllReturnsRecordsInOrderAndEmptiesTheQueue` | Fact | Enqueue then drain all returns records in order and empties the queue. |
| `TakeDroppedCountResetsToZeroAfterReading`                   | Fact | Take dropped count resets to zero after reading.                       |
| `EnqueueBeyondTheCapDropsInsteadOfGrowingTheQueueUnbounded`  | Fact | Enqueue beyond the cap drops instead of growing the queue unbounded.   |

### RedisReconnectRetryPolicyTests

`Jube.Test.Cache.RedisReconnectRetryPolicyTests` — 4 tests. Category: Unit. Collection: JubeCache.

| Test                                                                      | Kind | Verifies                                                                           |
|---------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ShouldRetryDelegatesToInnerPolicyAndForwardsArguments`                   | Fact | Should retry delegates to inner policy and forwards arguments.                     |
| `ShouldRetryReturnsFalseWithoutEnqueueingAnEventWhenInnerPolicyDeclines`  | Fact | Should retry returns false without enqueueing an event when inner policy declines. |
| `ShouldRetryEnqueuesAReconnectRetryEventWhenInnerPolicyAgrees`            | Fact | Should retry enqueues a reconnect retry event when inner policy agrees.            |
| `ShouldRetryReportsTransactionsImpactedFromCacheDiagnosticsInFlightCount` | Fact | Should retry reports transactions impacted from cache diagnostics in flight count. |

### RedisSentinelEventCaptureTests

`Jube.Test.Cache.RedisSentinelEventCaptureTests` — 3 tests. Category: Unit. Collection: JubeCache.

| Test                                                         | Kind | Verifies                                                               |
|--------------------------------------------------------------|------|------------------------------------------------------------------------|
| `EnqueueThenDrainAllReturnsRecordsInOrderAndEmptiesTheQueue` | Fact | Enqueue then drain all returns records in order and empties the queue. |
| `TakeDroppedCountResetsToZeroAfterReading`                   | Fact | Take dropped count resets to zero after reading.                       |
| `EnqueueBeyondTheCapDropsInsteadOfGrowingTheQueueUnbounded`  | Fact | Enqueue beyond the cap drops instead of growing the queue unbounded.   |

## Observability

### CacheCallCountersTests

`Jube.Test.Cache.Observability.CacheCallCountersTests` — 4 tests. Category: Unit. Collection: JubeCache.

| Test                                                              | Kind | Verifies                                                                    |
|-------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `RecordAccumulatesCountTotalMinAndMaxForOneCall`                  | Fact | Record accumulates count total min and max for one call.                    |
| `TakeSnapshotResetsCountersButKeepsTheAccumulatorForTheNextCycle` | Fact | Take snapshot resets counters but keeps the accumulator for the next cycle. |
| `TakeSnapshotOmitsCallsWithZeroCount`                             | Fact | Take snapshot omits calls with zero count.                                  |
| `RecordUnderConcurrentLoadProducesExactAggregatesAsync`           | Fact | Record under concurrent load produces exact aggregates.                     |

### CacheDiagnosticsTests

`Jube.Test.Cache.Observability.CacheDiagnosticsTests` — 6 tests. Category: Unit. Collection: JubeCache.

| Test                                                                        | Kind | Verifies                                                                          |
|-----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `RecordAsyncOfTReturnsFuncResultAndRecordsOneCallAsync`                     | Fact | Record async of t returns func result and records one call.                       |
| `RecordAsyncVoidRecordsOneCallAsync`                                        | Fact | Record async void records one call.                                               |
| `RecordOfTReturnsFuncResultAndRecordsOneCall`                               | Fact | Record of t returns func result and records one call.                             |
| `RecordWithTimeSpanRecordsOneCallWithTheGivenDuration`                      | Fact | Record with time span records one call with the given duration.                   |
| `InFlightCallCountIncrementsWhileAsyncCallIsPendingAndDecrementsAfterAsync` | Fact | In flight call count increments while async call is pending and decrements after. |
| `InFlightCallCountDecrementsEvenWhenTheDelegateThrowsAsync`                 | Fact | In flight call count decrements even when the delegate throws.                    |

## Redis

### CacheAbstractionRepositoryTests

`Jube.Test.Cache.Redis.CacheAbstractionRepositoryTests` — 6 tests. Category: Unit. Collection: JubeCache.

| Test                                                                       | Kind | Verifies                                                                           |
|----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `UpsertThenGetPreferReplicaReturnsStoredValueAsync`                        | Fact | Upsert then get prefer replica returns stored value.                               |
| `GetPreferReplicaOnMissingKeyReturnsNullAsync`                             | Fact | Get prefer replica on missing key returns null.                                    |
| `DeleteRemovesTheStoredValueAsync`                                         | Fact | Delete removes the stored value.                                                   |
| `UpsertUsesDocumentedAbstractionKeyFormatAsync`                            | Fact | Upsert uses documented abstraction key format.                                     |
| `GetAsyncGroupsBySearchKeyAndSearchValueAndReturnsZeroForMissingRuleAsync` | Fact | Get async groups by search key and search value and returns zero for missing rule. |
| `GetAsyncWhenOneGroupFailsStillReturnsOtherGroupsAsync`                    | Fact | Get async when one group fails still returns other groups.                         |

### CacheCallbackPublishSubscribeTests

`Jube.Test.Cache.Redis.CacheCallbackPublishSubscribeTests` — 18 tests. Category: Unit. Collection: JubeCache.

| Test                                                                                       | Kind | Verifies                                                                                                  |
|--------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `AddToDictionaryFromSubscriptionWithSelfOriginChannelIsIgnored`                            | Fact | Add to dictionary from subscription with self origin channel is ignored.                                  |
| `AddToDictionaryFromSubscriptionWithOtherOriginChannelAddsToCallbacks`                     | Fact | Add to dictionary from subscription with other origin channel adds to callbacks.                          |
| `AddToDictionaryFromSubscriptionWithTooFewChannelSegmentsIsCaughtAndLogged`                | Fact | Add to dictionary from subscription with too few channel segments is caught and logged.                   |
| `AddToDictionaryFromSubscriptionWithUnparsableGuidIsCaughtAndLogged`                       | Fact | Add to dictionary from subscription with unparsable guid is caught and logged.                            |
| `RemoveFromDictionaryFromSubscriptionWithSelfOriginChannelIsIgnored`                       | Fact | Remove from dictionary from subscription with self origin channel is ignored.                             |
| `RemoveFromDictionaryFromSubscriptionWithOtherOriginChannelRemovesFromCallbacks`           | Fact | Remove from dictionary from subscription with other origin channel removes from callbacks.                |
| `RemoveFromDictionaryFromSubscriptionWithUnparsableValueIsCaughtAndLogged`                 | Fact | Remove from dictionary from subscription with unparsable value is caught and logged.                      |
| `AddToDictionaryForNewGuidCreatesAnAlreadyCompletedTaskCompletionSource`                   | Fact | Add to dictionary for new guid creates an already completed task completion source.                       |
| `AddToDictionaryForExistingPendingTaskCompletionSourceCompletesTheSameInstance`            | Fact | Add to dictionary for existing pending task completion source completes the same instance.                |
| `SweepExpiredCallbacksRemovesOnlyCompletedEntriesOlderThanThreshold`                       | Fact | Sweep expired callbacks removes only completed entries older than threshold.                              |
| `SweepExpiredCallbacksWithNoEntriesDoesNothing`                                            | Fact | Sweep expired callbacks with no entries does nothing.                                                     |
| `SweepExpiredCallbacksDoesNotTouchAPendingCallbackOnItsFirstObservation`                   | Fact | Sweep expired callbacks does not touch a pending callback on its first observation.                       |
| `SweepExpiredCallbacksFaultsAndRemovesAPendingCallbackOnceItHasNeverArrivedPastItsTimeout` | Fact | Sweep expired callbacks faults and removes a pending callback once it has never arrived past its timeout. |
| `SweepExpiredCallbacksDoesNotFaultAPendingCallbackStillWithinItsTimeout`                   | Fact | Sweep expired callbacks does not fault a pending callback still within its timeout.                       |
| `SweepExpiredCallbacksIgnoresAPendingEntryThatIsSubsequentlyRemovedBeforeItExpires`        | Fact | Sweep expired callbacks ignores a pending entry that is subsequently removed before it expires.           |
| `PublishAsyncAddsToCallbacksAndPublishesToCallbackSetChannelAsync`                         | Fact | Publish async adds to callbacks and publishes to callback set channel.                                    |
| `DeleteAsyncRemovesFromCallbacksAndPublishesToCallbackRemoveChannelAsync`                  | Fact | Delete async removes from callbacks and publishes to callback remove channel.                             |
| `PublishAsyncSwallowsExceptionsAndLogsThemInsteadAsync`                                    | Fact | Publish async swallows exceptions and logs them instead.                                                  |

### CachePayloadLatestRepositoryDeleteByReferenceDateAsyncPostgresBackedTests

`Jube.Test.Cache.Redis.CachePayloadLatestRepositoryDeleteByReferenceDateAsyncPostgresBackedTests` — 1 test. Category:
Service. Collection: Database.

| Test                                                                                                                         | Kind | Verifies                                                                                                                                      |
|------------------------------------------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------------------------------------------|
| `DeleteByReferenceDateAsyncAppliesAPerSearchKeyOverrideAndFallsBackToTheGlobalThresholdAndPersistsTheRealReferenceDateAsync` | Fact | Delete by reference date async applies a per search key override and falls back to the global threshold and persists the real reference date. |

### CachePayloadLatestRepositoryIntervalTests

`Jube.Test.Cache.Redis.CachePayloadLatestRepositoryIntervalTests` — 5 tests. Category: Unit. Collection: JubeCache.

| Test                                                          | Kind   | Verifies                                                              |
|---------------------------------------------------------------|--------|-----------------------------------------------------------------------|
| `EachRecognisedIntervalSubtractsFromReferenceDate`            | Theory | Each recognised interval subtracts from reference date.               |
| `SecondsIntervalSupportsVeryShortRetentionWindows`            | Fact   | Seconds interval supports very short retention windows.               |
| `UnrecognisedIntervalFallsBackToDays`                         | Fact   | Unrecognised interval falls back to days.                             |
| `ZeroIntervalValueReturnsReferenceDateUnchanged`              | Fact   | Zero interval value returns reference date unchanged.                 |
| `NegativeIntervalValuePushesTheThresholdIntoTheFutureInstead` | Fact   | Negative interval value pushes the threshold into the future instead. |

### CachePayloadLatestRepositoryTests

`Jube.Test.Cache.Redis.CachePayloadLatestRepositoryTests` — 9 tests. Category: Unit. Collection: JubeCache.

| Test                                                                                       | Kind | Verifies                                                                                           |
|--------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `UpsertAsyncWithoutPayloadWritesPayloadLatestHashReferenceDateLatestHashAndSortedSetAsync` | Fact | Upsert async without payload writes payload latest hash reference date latest hash and sorted set. |
| `UpsertAsyncWithPayloadUsesKeyIncludingInstanceEntryGuidAndLeavesFieldUnsetAsync`          | Fact | Upsert async with payload uses key including instance entry guid and leaves field unset.           |
| `GetDistinctKeysPreferReplicaAsyncWithNoDateFilterReturnsAllFieldNamesAsync`               | Fact | Get distinct keys prefer replica async with no date filter returns all field names.                |
| `GetDistinctKeysPreferReplicaAsyncWithDateBeforeFiltersOutNewerEntriesAsync`               | Fact | Get distinct keys prefer replica async with date before filters out newer entries.                 |
| `GetDistinctKeysPreferReplicaAsyncWithDateRangeFiltersToWindowAsync`                       | Fact | Get distinct keys prefer replica async with date range filters to window.                          |
| `UpsertAsyncWritesAKeyThatGetDistinctKeysPreferReplicaAsyncCanFindAsync`                   | Fact | Upsert async writes a key that get distinct keys prefer replica async can find.                    |
| `BatchSortedSetRemoveAsyncRemovesAllValuesAcrossMultipleThousandEntryBatchesAsync`         | Fact | Batch sorted set remove async removes all values across multiple thousand entry batches.           |
| `BatchHashDeleteAsyncDeletesAllFieldsAcrossMultipleThousandEntryBatchesAsync`              | Fact | Batch hash delete async deletes all fields across multiple thousand entry batches.                 |
| `AggregateResponseTimesForBulkInsertGroupsByTaskTypeAndSumsComputeTime`                    | Fact | Aggregate response times for bulk insert groups by task type and sums compute time.                |

### CachePayloadRepositoryLocalCacheTests

`Jube.Test.Cache.Redis.CachePayloadRepositoryLocalCacheTests` — 12 tests. Category: Unit. Collection: JubeCache.

| Test                                                                              | Kind | Verifies                                                                                          |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `CheckThatTheEventDidNotComeFromThisHostReturnsTrueOnlyWhenBothHostAndGuidMatch`  | Fact | Check that the event did not come from this host returns true only when both host and guid match. |
| `HandleHashSetMessageIgnoresMessageThatOriginatedFromThisInstance`                | Fact | Handle hash set message ignores message that originated from this instance.                       |
| `HandleHashSetMessageFromAnotherInstanceAddsToLocalCache`                         | Fact | Handle hash set message from another instance adds to local cache.                                |
| `HandleHashRemoveMessageIgnoresMessageThatOriginatedFromThisInstance`             | Fact | Handle hash remove message ignores message that originated from this instance.                    |
| `HandleHashRemoveMessageFromAnotherInstanceRemovesFromLocalCache`                 | Fact | Handle hash remove message from another instance removes from local cache.                        |
| `DeleteFromLocalCacheSubscriptionBranchCountsOnceOnAHitAndDoesNotIncrementMiss`   | Fact | Delete from local cache subscription branch counts once on a hit and does not increment miss.     |
| `DeleteFromLocalCacheSubscriptionBranchCountsOnceAndIncrementsMissOnAMiss`        | Fact | Delete from local cache subscription branch counts once and increments miss on a miss.            |
| `DeleteFromLocalCacheNonSubscriptionBranchDoesNotIncrementHashRemoveMissOnAHit`   | Fact | Delete from local cache non subscription branch does not increment hash remove miss on a hit.     |
| `DeleteFromLocalCacheNonSubscriptionBranchIncrementsHashRemoveMissOnAGenuineMiss` | Fact | Delete from local cache non subscription branch increments hash remove miss on a genuine miss.    |
| `DeleteFromLocalCacheDoesNothingWhenLocalCacheIsDisabled`                         | Fact | Delete from local cache does nothing when local cache is disabled.                                |
| `GetPayloadBatchAsyncPrefersLocalCacheAndFallsBackToRedisOnMissAsync`             | Fact | Get payload batch async prefers local cache and falls back to redis on miss.                      |
| `GetPayloadBatchAsyncAlwaysGoesToRedisWhenLocalCacheDisabledAsync`                | Fact | Get payload batch async always goes to redis when local cache disabled.                           |

### CachePayloadRepositoryTests

`Jube.Test.Cache.Redis.CachePayloadRepositoryTests` — 11 tests. Category: Unit. Collection: JubeCache.

| Test                                                                            | Kind | Verifies                                                                             |
|---------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `InsertAsyncWritesPayloadHashAndReferenceDateSortedSetAsync`                    | Fact | Insert async writes payload hash and reference date sorted set.                      |
| `InsertAsyncWithPublishSubscribeDisabledPublishesNothingAsync`                  | Fact | Insert async with publish subscribe disabled publishes nothing.                      |
| `InsertAsyncWithStorePayloadCountsAndBytesIncrementsCountersAsync`              | Fact | Insert async with store payload counts and bytes increments counters.                |
| `UpsertAsyncSwallowsExceptionsAndLogsAsync`                                     | Fact | Upsert async swallows exceptions and logs.                                           |
| `GetSortedSetKeysAsyncTouchesLruJournalAndExcludesGivenGuidAsync`               | Fact | Get sorted set keys async touches lru journal and excludes given guid.               |
| `InsertPayloadJournalAndLedgerAsyncWritesExpectedKeysAsync`                     | Fact | Insert payload journal and ledger async writes expected keys.                        |
| `PurgeExpiredLruJournalEntriesAsyncRemovesOnlyEntriesOlderThanMaxAgeAsync`      | Fact | Purge expired lru journal entries async removes only entries older than max age.     |
| `AggregateResponseTimesForBulkInsertGroupsByTaskTypeAndSumsComputeTime`         | Fact | Aggregate response times for bulk insert groups by task type and sums compute time.  |
| `BatchSortedSetRemoveAsyncRemovesAllValuesAcrossMultipleBatchesAsync`           | Fact | Batch sorted set remove async removes all values across multiple batches.            |
| `CleanupIdempotencyAsyncRemovesJournalEntryOnceUnderlyingSetIsEmptyAsync`       | Fact | Cleanup idempotency async removes journal entry once underlying set is empty.        |
| `CleanupIdempotencyAsyncKeepsJournalEntryWhenUnderlyingSetStillHasMembersAsync` | Fact | Cleanup idempotency async keeps journal entry when underlying set still has members. |

### CacheRateLimitRepositoryTests

`Jube.Test.Cache.Redis.CacheRateLimitRepositoryTests` — 5 tests. Category: Unit. Collection: JubeCache.

| Test                                                                     | Kind | Verifies                                                                      |
|--------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `EachCallIncrementsAndReturnsTheRunningCountAsync`                       | Fact | Each call increments and returns the running count.                           |
| `DifferentSourceKeysAreCountedIndependentlyAsync`                        | Fact | Different source keys are counted independently.                              |
| `DifferentRateLimitNamesAreCountedIndependentlyForTheSameSourceKeyAsync` | Fact | Different rate limit names are counted independently for the same source key. |
| `TheKeyIsExpiredOnceOnTheFirstIncrementOnlyAsync`                        | Fact | The key is expired once on the first increment only.                          |
| `AnExceptionFromRedisIsSwallowedAndTreatedAsNotExceededAsync`            | Fact | An exception from redis is swallowed and treated as not exceeded.             |

### CacheReferenceDateRepositoryTests

`Jube.Test.Cache.Redis.CacheReferenceDateRepositoryTests` — 5 tests. Category: Unit. Collection: JubeCache.

| Test                                                              | Kind | Verifies                                                              |
|-------------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `UpsertThenGetReferenceDateRoundTripsToMillisecondPrecisionAsync` | Fact | Upsert then get reference date round trips to millisecond precision.  |
| `UpsertUsesDocumentedReferenceDateKeyFormatAsync`                 | Fact | Upsert uses documented reference date key format.                     |
| `GetReferenceDateOnGenuineCacheMissReturnsNullAsync`              | Fact | Get reference date on genuine cache miss returns null.                |
| `GetReferenceDatePreferReplicaOnGenuineCacheMissReturnsNullAsync` | Fact | Get reference date prefer replica on genuine cache miss returns null. |
| `UpsertOverwritesPreviousReferenceDateForSameModelAsync`          | Fact | Upsert overwrites previous reference date for same model.             |

### CacheSanctionRepositoryTests

`Jube.Test.Cache.Redis.CacheSanctionRepositoryTests` — 5 tests. Category: Unit. Collection: JubeCache.

| Test                                                                                  | Kind | Verifies                                                                                    |
|---------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `InsertThenGetRoundTripsValueAndCreatedDateAsync`                                     | Fact | Insert then get round trips value and created date.                                         |
| `GetOnMissingKeyReturnsNullAsync`                                                     | Fact | Get on missing key returns null.                                                            |
| `InsertUsesDocumentedSanctionKeyFormatWithSearchStringAndDistanceCompositeFieldAsync` | Fact | Insert uses documented sanction key format with search string and distance composite field. |
| `InsertWithNullValueRoundTripsAsNullAsync`                                            | Fact | Insert with null value round trips as null.                                                 |
| `DifferentDistanceThresholdsForSameStringAreStoredIndependentlyAsync`                 | Fact | Different distance thresholds for same string are stored independently.                     |

### CacheTtlCounterEntryRepositoryTests

`Jube.Test.Cache.Redis.CacheTtlCounterEntryRepositoryTests` — 7 tests. Category: Unit. Collection: JubeCache.

| Test                                                                               | Kind | Verifies                                                                                  |
|------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `UpsertThenGetAllExpiredReturnsTheEntryOnceReferenceDateHasPassedAsync`            | Fact | Upsert then get all expired returns the entry once reference date has passed.             |
| `GetAllExpiredExcludesEntriesNewerThanTheReferenceDateThresholdAsync`              | Fact | Get all expired excludes entries newer than the reference date threshold.                 |
| `GetAllExpiredCorrectlyMapsDataNameAndDataValueAsync`                              | Fact | Get all expired correctly maps data name and data value.                                  |
| `DeleteRemovesTheEntryFromBothTheCounterHashAndTheExpiryIndexAsync`                | Fact | Delete removes the entry from both the counter hash and the expiry index.                 |
| `GetAggregationSumsOnlyEntriesWithinTheDateRangeAsync`                             | Fact | Get aggregation sums only entries within the date range.                                  |
| `GetAggregationOnNoMatchingEntriesReturnsZeroAsync`                                | Fact | Get aggregation on no matching entries returns zero.                                      |
| `UpsertIncrementsAnExistingEntryAtTheSameReferenceDateInsteadOfDuplicatingItAsync` | Fact | Upsert increments an existing entry at the same reference date instead of duplicating it. |

### CacheTtlCounterRepositoryTests

`Jube.Test.Cache.Redis.CacheTtlCounterRepositoryTests` — 6 tests. Category: Unit. Collection: JubeCache.

| Test                                                                         | Kind | Verifies                                                                           |
|------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `IncrementThenGetReturnsTheAccumulatedValueAsync`                            | Fact | Increment then get returns the accumulated value.                                  |
| `IncrementUsesDocumentedTtlCounterKeyFormatAsync`                            | Fact | Increment uses documented ttl counter key format.                                  |
| `DecrementToExactlyZeroDeletesTheHashFieldAndReturnsZeroAsync`               | Fact | Decrement to exactly zero deletes the hash field and returns zero.                 |
| `DecrementBelowZeroAlsoClampsToZeroAndDeletesTheHashFieldAsync`              | Fact | Decrement below zero also clamps to zero and deletes the hash field.               |
| `DecrementAboveZeroReturnsTheRemainingValueAndKeepsTheHashFieldAsync`        | Fact | Decrement above zero returns the remaining value and keeps the hash field.         |
| `GetOnANeverIncrementedCounterReturnsZeroDespiteTheInternalCastFailureAsync` | Fact | Get on a never incremented counter returns zero despite the internal cast failure. |

### CacheWalRepositoryTests

`Jube.Test.Cache.Redis.CacheWalRepositoryTests` — 6 tests. Category: Unit. Collection: JubeCache.

| Test                                                             | Kind | Verifies                                                             |
|------------------------------------------------------------------|------|----------------------------------------------------------------------|
| `InsertThenGetWalSizeReflectsTheStoredEntryAsync`                | Fact | Insert then get wal size reflects the stored entry.                  |
| `InsertUsesDocumentedWalKeyFormatAsync`                          | Fact | Insert uses documented wal key format.                               |
| `FlushWalRemovesOnlyTheSpecifiedEntriesAsync`                    | Fact | Flush wal removes only the specified entries.                        |
| `InsertSwallowsAndLogsOnFailureAsync`                            | Fact | Insert swallows and logs on failure.                                 |
| `FlushWalSwallowsAndLogsOnFailureAsync`                          | Fact | Flush wal swallows and logs on failure.                              |
| `GetWalSizeDoesNotSwallowExceptionsUnlikeItsSiblingMethodsAsync` | Fact | Get wal size does not swallow exceptions unlike its sibling methods. |

### IdempotencyRepositoriesTests

`Jube.Test.Cache.Redis.IdempotencyRepositoriesTests` — 5 tests. Category: Unit. Collection: JubeCache.

| Test                                                                              | Kind   | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `FirstClaimSucceedsAndSecondClaimForSameTransactionIsReportedAsDuplicateAsync`    | Theory | First claim succeeds and second claim for same transaction is reported as duplicate.      |
| `DifferentTransactionEntryGuidsDoNotCollideAsync`                                 | Theory | Different transaction entry guids do not collide.                                         |
| `ClaimRecordsTheKeyInTheSharedIdempotencyJournalForCleanupAsync`                  | Theory | Claim records the key in the shared idempotency journal for cleanup.                      |
| `OnUnderlyingFailureMidClaimReturnsFalseSinceTheClaimMayNotHaveBeenRecordedAsync` | Theory | On underlying failure mid claim returns false since the claim may not have been recorded. |
| `OnUnderlyingFailureDuringTheInitialDuplicateCheckAlsoReturnsFalseAsync`          | Theory | On underlying failure during the initial duplicate check also returns false.              |

### PayloadPruneAndAbstractionRuleFetchTests

`Jube.Test.Cache.Redis.PayloadPruneAndAbstractionRuleFetchTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                                             | Kind | Verifies                                                                                                   |
|--------------------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------------|
| `PayloadPrunedByTheRealPruneJobDisappearsFromTheAbstractionRuleFetchPathButSurvivorsRemainAsync` | Fact | Payload pruned by the real prune job disappears from the abstraction rule fetch path but survivors remain. |
| `PayloadLatestWrittenThroughTheRealUpsertIsPrunedAndAuditedByTheRealPruneJobAsync`               | Fact | Payload latest written through the real upsert is pruned and audited by the real prune job.                |

## Redis cache user registry api key

### CacheUserRegistryApiKeyRepositoryTests

`Jube.Test.Cache.Redis.CacheUserRegistryApiKey.CacheUserRegistryApiKeyRepositoryTests` — 7 tests. Category: Unit.
Collection: JubeCache.

| Test                                                                            | Kind | Verifies                                                                                   |
|---------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `HandleSetMessageRaisesOnCaseUserRegistryApiKeySetEventWithApiKeyHash`          | Fact | Handle set message raises on case user registry api key set event with api key hash.       |
| `HandleRemoveMessageRaisesOnCaseUserRegistryApiKeyRemoveEventWithApiKeyHash`    | Fact | Handle remove message raises on case user registry api key remove event with api key hash. |
| `HandleSetMessageWithNoSubscribersDoesNotThrow`                                 | Fact | Handle set message with no subscribers does not throw.                                     |
| `HandleRemoveMessageWithNoSubscribersDoesNotThrow`                              | Fact | Handle remove message with no subscribers does not throw.                                  |
| `PublishSetAsyncPublishesToUserRegistryApiKeySetChannelWithHostNameAsync`       | Fact | Publish set async publishes to user registry api key set channel with host name.           |
| `PublishRemoveAsyncPublishesToUserRegistryApiKeyRemoveChannelWithHostNameAsync` | Fact | Publish remove async publishes to user registry api key remove channel with host name.     |
| `PublishSetAsyncSwallowsExceptionsAndLogsThemInsteadAsync`                      | Fact | Publish set async swallows exceptions and logs them instead.                               |

## Redis callback

### CallbackTests

`Jube.Test.Cache.Redis.Callback.CallbackTests` — 1 test. Category: Unit. Collection: JubeCache.

| Test                                 | Kind | Verifies                                 |
|--------------------------------------|------|------------------------------------------|
| `HoldsAssignedCreatedDateAndPayload` | Fact | Holds assigned created date and payload. |

## Redis models

### ModelsTests

`Jube.Test.Cache.Redis.Models.ModelsTests` — 8 tests. Category: Unit. Collection: JubeCache.

| Test                                                                              | Kind | Verifies                                                                                      |
|-----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `CachePayloadLatestHoldsAssignedFields`                                           | Fact | Cache payload latest holds assigned fields.                                                   |
| `CachePayloadLatestRoundTripsThroughContractlessMessagePack`                      | Fact | Cache payload latest round trips through contractless message pack.                           |
| `CacheSanctionHoldsAssignedFieldsAndRoundTripsThroughContractlessMessagePack`     | Fact | Cache sanction holds assigned fields and round trips through contractless message pack.       |
| `CacheSanctionWithNullValueRoundTrips`                                            | Fact | Cache sanction with null value round trips.                                                   |
| `SanctionHoldsAssignedFieldsAndRoundTripsThroughContractlessMessagePack`          | Fact | Sanction holds assigned fields and round trips through contractless message pack.             |
| `EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueHoldsAssignedFields` | Fact | Entity analysis model id abstraction rule name search key search value holds assigned fields. |
| `ExpiredTtlCounterEntryHoldsAssignedFields`                                       | Fact | Expired ttl counter entry holds assigned fields.                                              |
| `LocalCacheInstanceKeyStartsAtZeroForAllCountersAndExposesTheGivenLruCache`       | Fact | Local cache instance key starts at zero for all counters and exposes the given lru cache.     |

## Redis serialization

### MessagePackSerializerOptionsHelperTests

`Jube.Test.Cache.Redis.Serialization.MessagePackSerializerOptionsHelperTests` — 5 tests. Category: Unit. Collection:
JubeCache.

| Test                                                           | Kind   | Verifies                                                               |
|----------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `EnveloperOptionsRoundTripAnEnvelopeDictionaryNoBoxingPayload` | Theory | Enveloper options round trip an envelope dictionary no boxing payload. |
| `ContractlessStandardResolverOptionsRoundTripAPlainObject`     | Theory | Contractless standard resolver options round trip a plain object.      |
| `StandardOptionsRoundTripAContractObject`                      | Theory | Standard options round trip a contract object.                         |
| `CompressionMeaningfullyReducesSizeForARepetitivePayload`      | Fact   | Compression meaningfully reduces size for a repetitive payload.        |
| `CompressedAndUncompressedOptionsAreDistinctInstances`         | Fact   | Compressed and uncompressed options are distinct instances.            |

## Redis serialization dictionary no boxing

### EnvelopeDictionaryNoBoxingMessagePackTests

`Jube.Test.Cache.Redis.Serialization.DictionaryNoBoxing.EnvelopeDictionaryNoBoxingMessagePackTests` — 18 tests.
Category: Unit. Collection: JubeCache.

| Test                                                                         | Kind   | Verifies                                                                                   |
|------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `RoundTripsAnEmptyIntKeyedDictionary`                                        | Fact   | Round trips an empty int keyed dictionary.                                                 |
| `RoundTripsAnEmptyStringKeyedDictionary`                                     | Fact   | Round trips an empty string keyed dictionary.                                              |
| `RoundTripsStringValueForIntKey`                                             | Fact   | Round trips string value for int key.                                                      |
| `RoundTripsStringValueForStringKey`                                          | Fact   | Round trips string value for string key.                                                   |
| `RoundTripsSmallIntValues`                                                   | Theory | Round trips small int values.                                                              |
| `RoundTripsDoubleValuesIncludingSpecialValues`                               | Theory | Round trips double values including special values.                                        |
| `RoundTripsBoolValues`                                                       | Theory | Round trips bool values.                                                                   |
| `RoundTripsDateTimeValuesRegardlessOfKind`                                   | Theory | Round trips date time values regardless of kind.                                           |
| `RoundTripsIntValuesAtTheInt32BoundaryWithoutBeingMisreadAsADateTime`        | Theory | Round trips int values at the int32 boundary without being misread as a date time.         |
| `RoundTripsMultipleMixedTypedEntriesInOneDictionary`                         | Fact   | Round trips multiple mixed typed entries in one dictionary.                                |
| `GuidTypedValueRoundTripsCorrectly`                                          | Fact   | Guid typed value round trips correctly.                                                    |
| `EmptyGuidRoundTripsCorrectly`                                               | Fact   | Empty guid round trips correctly.                                                          |
| `MultipleGuidTypedValuesInTheSameDictionaryEachRoundTripCorrectly`           | Fact   | Multiple guid typed values in the same dictionary each round trip correctly.               |
| `GuidTypedValueRoundTripsCorrectlyForStringKeyedDictionaryAsWell`            | Fact   | Guid typed value round trips correctly for string keyed dictionary as well.                |
| `EnvelopeWithNullDataRoundTripsBackToNullData`                               | Fact   | Envelope with null data round trips back to null data.                                     |
| `EnvelopeWithNullDataRoundTripsBackToNullDataForStringKeyedDictionaryAsWell` | Fact   | Envelope with null data round trips back to null data for string keyed dictionary as well. |
| `EnvelopeWithNullDataIsDistinguishableFromAnEmptyDataOnRoundTrip`            | Fact   | Envelope with null data is distinguishable from an empty data on round trip.               |
| `GetFormatterForAnUnsupportedKeyTypeThrowsTypeInitializationException`       | Fact   | Get formatter for an unsupported key type throws type initialization exception.            |
