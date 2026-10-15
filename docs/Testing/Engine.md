---
layout: default
title: Real-Time Engine
nav_order: 10
parent: Testing
---

# Real-Time Engine

Tests of the real-time model invocation engine: payload extraction, abstraction rules, TTL counters, activation and the
supporting engine plumbing.

## General

### ContainerResourceLimitReaderTests

`Jube.Test.Engine.ContainerResourceLimitReaderTests` — 1 test. Category: Unit.

| Test                                            | Kind | Verifies                                              |
|-------------------------------------------------|------|-------------------------------------------------------|
| `ReadNeverThrowsAndLeavesUnavailableFieldsNull` | Fact | Read never throws and leaves unavailable fields null. |

## Entity analysis model invoke

### AbstractionCalculationsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.AbstractionCalculationsExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                         | Kind | Verifies                                                                                    |
|------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `TheRuleResultIsStoredUnderTheCalculationName`                               | Fact | The rule result is stored under the calculation name.                                       |
| `TheRuleDelegateReceivesEveryUpstreamSource`                                 | Fact | The rule delegate receives every upstream source.                                           |
| `ALaterCalculationCanReadAnEarlierOneInTheSameInvocation`                    | Fact | A later calculation can read an earlier one in the same invocation.                         |
| `CalculationsRunInTheOrderTheyAreConfigured`                                 | Fact | Calculations run in the order they are configured.                                          |
| `AFiniteNegativeResultIsStoredUnchanged`                                     | Fact | A finite negative result is stored unchanged.                                               |
| `AnUndefinedResultIsStoredAsReturnedSoARuleThatWantsZeroUsesZeroIfUndefined` | Fact | An undefined result is stored as returned so a rule that wants zero uses zero if undefined. |
| `ReportTableAddsAnArchiveKeyWithProcessingTypeSix`                           | Fact | Report table adds an archive key with processing type six.                                  |
| `WithoutReportTableNoArchiveKeyIsAdded`                                      | Fact | Without report table no archive key is added.                                               |
| `AnExceptionThrownByTheRuleDelegateIsCaughtAndTheCalculationIsSkipped`       | Fact | An exception thrown by the rule delegate is caught and the calculation is skipped.          |
| `AnEntryWithNoCompiledDelegateIsSkippedWithoutThrowing`                      | Fact | An entry with no compiled delegate is skipped without throwing.                             |
| `WhenSampledTheStageTimingRecordsOneItemPerCalculation`                      | Fact | When sampled the stage timing records one item per calculation.                             |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns`                | Fact | When not sampled no stage timing is built but business logic still runs.                    |
| `WithNoCalculationsConfiguredNothingThrowsAndTheDictionaryStaysEmpty`        | Fact | With no calculations configured nothing throws and the dictionary stays empty.              |

### ActivationRuleActivationWatcherExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleActivationWatcherExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                               | Kind | Verifies                                                                                  |
|------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `DoesNothingWhenTheRuleIsNotFlaggedForTheWatcherAsync`                             | Fact | Does nothing when the rule is not flagged for the watcher.                                |
| `DoesNothingWhenSuppressedAsync`                                                   | Fact | Does nothing when suppressed.                                                             |
| `DoesNothingWhenThisIsAReprocessingRunAsync`                                       | Fact | Does nothing when this is a reprocessing run.                                             |
| `DoesNothingWhenTheModelHasActivationWatcherDisabledAsync`                         | Fact | Does nothing when the model has activation watcher disabled.                              |
| `DoesNothingWhenTheWatchCountHasReachedTheThresholdAsync`                          | Fact | Does nothing when the watch count has reached the threshold.                              |
| `DoesNothingWhenTheRandomSampleDrawFailsAsync`                                     | Fact | Does nothing when the random sample draw fails.                                           |
| `PersistsToTheWatcherQueueWithFieldsCopiedFromTheRuleWhenPersistIsAllowedAsync`    | Fact | Persists to the watcher queue with fields copied from the rule when persist is allowed.   |
| `DoesNotPersistToTheWatcherQueueWhenPersistIsDisallowedAsync`                      | Fact | Does not persist to the watcher queue when persist is disallowed.                         |
| `UsesTheResponseElevationKeyValueFromThePayloadWhenPresentAsync`                   | Fact | Uses the response elevation key value from the payload when present.                      |
| `FallsBackToMissingForTheKeyValueWhenThePayloadDoesNotHaveItAsync`                 | Fact | Falls back to missing for the key value when the payload does not have it.                |
| `PublishesToRedisAndIncrementsTheCounterWhenStreamingIsEnabledAsync`               | Fact | Publishes to redis and increments the counter when streaming is enabled.                  |
| `DoesNotPublishToRedisOrIncrementTheCounterWhenStreamingIsDisabledAsync`           | Fact | Does not publish to redis or increment the counter when streaming is disabled.            |
| `ARedisFailureIsSwallowedInsideTheCacheRepositorySoTheCounterStillIncrementsAsync` | Fact | A redis failure is swallowed inside the cache repository so the counter still increments. |

### ActivationRuleActivationWatcherGeolocationTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleActivationWatcherGeolocationTests` — 5 tests. Category: Unit.

| Test                                                                        | Kind | Verifies                                                                               |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `GetLatitudeReadsFromTheXPathFlaggedWithDataTypeSix`                        | Fact | Get latitude reads from the x path flagged with data type six.                         |
| `GetLongitudeReadsFromTheXPathFlaggedWithDataTypeSeven`                     | Fact | Get longitude reads from the x path flagged with data type seven.                      |
| `GetLongitudeFallsBackToTheInlineScriptPropertyFlaggedLongitudeNotLatitude` | Fact | Get longitude falls back to the inline script property flagged longitude not latitude. |
| `GetLongitudeDoesNotMatchAPropertyFlaggedLatitudeOnly`                      | Fact | Get longitude does not match a property flagged latitude only.                         |
| `GetLatitudeAndGetLongitudeDefaultToZeroWhenNothingIsConfigured`            | Fact | Get latitude and get longitude default to zero when nothing is configured.             |

### ActivationRuleCreateCaseObjectExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleCreateCaseObjectExtensionsTests` — 15 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                            |
|--------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `ReturnsNullWhenCaseWorkflowIsDisabledAsync`                                   | Fact   | Returns null when case workflow is disabled.                                        |
| `ReturnsNullWhenSuppressedAsync`                                               | Fact   | Returns null when suppressed.                                                       |
| `ReturnsNullWhenThisIsAReprocessingRunAsync`                                   | Fact   | Returns null when this is a reprocessing run.                                       |
| `StillCreatesACaseOnAnOrdinaryRunAfterTheReprocessingGuardAsync`               | Fact   | Still creates a case on an ordinary run after the reprocessing guard.               |
| `PopulatesTenantAndCaseWorkflowIdentifiersFromTheRuleAsync`                    | Fact   | Populates tenant and case workflow identifiers from the rule.                       |
| `SelectsSuspendBypassAndComputesTheDateForEachIntervalAsync`                   | Theory | Selects suspend bypass and computes the date for each interval.                     |
| `DoesNotSetASuspendBypassDateForAnUnrecognisedIntervalAsync`                   | Fact   | Does not set a suspend bypass date for an unrecognised interval.                    |
| `OpensImmediatelyWhenTheBypassSampleDrawFailsAsync`                            | Fact   | Opens immediately when the bypass sample draw fails.                                |
| `UsesTheRulesCaseKeyAndPayloadValueWhenTheCaseKeyIsPresentAsync`               | Fact   | Uses the rules case key and payload value when the case key is present.             |
| `FallsBackToTheEntityInstanceEntryIdWhenTheCaseKeyIsNullAsync`                 | Fact   | Falls back to the entity instance entry id when the case key is null.               |
| `FallsBackToTheEntityInstanceEntryIdWhenTheCaseKeyIsNotInThePayloadAsync`      | Fact   | Falls back to the entity instance entry id when the case key is not in the payload. |
| `SkipsTheIdempotencyCacheEntirelyWhenActivationRuleIdempotencyIsDisabledAsync` | Fact   | Skips the idempotency cache entirely when activation rule idempotency is disabled.  |
| `ReturnsNullWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync`       | Fact   | Returns null when idempotency is enabled and the claim has already been taken.      |
| `CreatesTheCaseWhenIdempotencyIsEnabledAndTheClaimSucceedsAsync`               | Fact   | Creates the case when idempotency is enabled and the claim succeeds.                |
| `DifferentActivationRulesClaimIndependentIdempotencySlotsForTheSameEntryAsync` | Fact   | Different activation rules claim independent idempotency slots for the same entry.  |

### ActivationRuleNotificationExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleNotificationExtensionsTests` — 9 tests. Category: Unit.

| Test                                                                                 | Kind | Verifies                                                                                    |
|--------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `DoesNothingWhenNotificationIsGloballyDisabledAsync`                                 | Fact | Does nothing when notification is globally disabled.                                        |
| `DoesNothingWhenSuppressedAsync`                                                     | Fact | Does nothing when suppressed.                                                               |
| `DoesNothingWhenTheRuleHasNotificationDisabledAsync`                                 | Fact | Does nothing when the rule has notification disabled.                                       |
| `DoesNothingWhenThisIsAReprocessingRunAsync`                                         | Fact | Does nothing when this is a reprocessing run.                                               |
| `EnqueuesToThePendingNotificationsQueueWhenAmqpIsDisabledAsync`                      | Fact | Enqueues to the pending notifications queue when amqp is disabled.                          |
| `ReplacesPayloadTokensInDestinationSubjectAndBodyAsync`                              | Fact | Replaces payload tokens in destination subject and body.                                    |
| `ReturnsWithoutEnqueuingWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync` | Fact | Returns without enqueuing when idempotency is enabled and the claim has already been taken. |
| `EnqueuesWhenIdempotencyIsEnabledAndTheClaimSucceedsAsync`                           | Fact | Enqueues when idempotency is enabled and the claim succeeds.                                |
| `NotificationIdempotencyIsClaimedIndependentlyFromCaseCreationIdempotencyAsync`      | Fact | Notification idempotency is claimed independently from case creation idempotency.           |

### ActivationRuleTtlCounterExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRuleTtlCounterExtensionsTests` — 14 tests. Category: Unit.

| Test                                                                                     | Kind | Verifies                                                                                              |
|------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `DoesNothingWhenTheRuleHasTtlCounterDisabledAsync`                                       | Fact | Does nothing when the rule has ttl counter disabled.                                                  |
| `DoesNothingWhenThisIsAReprocessingRunAsync`                                             | Fact | Does nothing when this is a reprocessing run.                                                         |
| `DoesNothingWhenNoModelMatchesTheTargetModelGuidAsync`                                   | Fact | Does nothing when no model matches the target model guid.                                             |
| `DoesNothingWhenTheModelHasNoCounterMatchingTheCounterGuidAsync`                         | Fact | Does nothing when the model has no counter matching the counter guid.                                 |
| `DoesNothingWhenThePayloadHasNoValueForTheCountersDataNameAsync`                         | Fact | Does nothing when the payload has no value for the counters data name.                                |
| `DoesNothingWhenTheTargetModelHasTtlCounterStorageDisabledAsync`                         | Fact | Does nothing when the target model has ttl counter storage disabled.                                  |
| `IncrementsByOneAndAddsANewEntryWhenSumIsNotEnabledAndTheCounterHasNoExistingValueAsync` | Fact | Increments by one and adds a new entry when sum is not enabled and the counter has no existing value. |
| `AccumulatesTheExistingInMemoryCounterValueWhenIncrementedTwiceAsync`                    | Fact | Accumulates the existing in memory counter value when incremented twice.                              |
| `SumsThePayloadValueWhenEnableSumIsSetOnTheCounterAsync`                                 | Fact | Sums the payload value when enable sum is set on the counter.                                         |
| `WritesAnUpsertEntryToTheCacheWhenTheCounterDoesNotLiveForeverAsync`                     | Fact | Writes an upsert entry to the cache when the counter does not live forever.                           |
| `DoesNotWriteAnUpsertEntryWhenTheCounterLivesForeverAsync`                               | Fact | Does not write an upsert entry when the counter lives forever.                                        |
| `SkipsTheIdempotencyCacheEntirelyWhenActivationRuleIdempotencyIsDisabledAsync`           | Fact | Skips the idempotency cache entirely when activation rule idempotency is disabled.                    |
| `DoesNotIncrementWhenIdempotencyIsEnabledAndTheClaimHasAlreadyBeenTakenAsync`            | Fact | Does not increment when idempotency is enabled and the claim has already been taken.                  |
| `StillIncrementsTheInMemoryCounterWhenTheBackgroundCacheWriteFailsAsync`                 | Fact | Still increments the in memory counter when the background cache write fails.                         |

### ActivationRulesHelperExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ActivationRulesHelperExtensionsTests` — 39 tests. Category: Unit.

| Test                                                                                           | Kind   | Verifies                                                                                                      |
|------------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------------------|
| `ActivationRuleFinishResponseElevationIncrementsCounterWhenElevationIsPositive`                | Fact   | Activation rule finish response elevation increments counter when elevation is positive.                      |
| `ActivationRuleFinishResponseElevationDoesNotIncrementCounterWhenElevationIsZeroOrNegative`    | Theory | Activation rule finish response elevation does not increment counter when elevation is zero or negative.      |
| `ActivationRuleResponseElevationAddToCountersIncrementsAndJournalsWhenElevationIsPositive`     | Fact   | Activation rule response elevation add to counters increments and journals when elevation is positive.        |
| `ActivationRuleResponseElevationAddToCountersDoesNothingWhenElevationIsZero`                   | Fact   | Activation rule response elevation add to counters does nothing when elevation is zero.                       |
| `UpdateContextStateWithActivationRulesOutcomeSetsAllThreeFieldsOnThePayload`                   | Fact   | Update context state with activation rules outcome sets all three fields on the payload.                      |
| `UpdateContextStateWithActivationRulesOutcomeAllowsANullPrevailingId`                          | Fact   | Update context state with activation rules outcome allows a null prevailing id.                               |
| `CheckSuppressedResponseElevationIsFalseWhenQueueCountIsAtOrBelowTheFrequencyLimitCounter`     | Fact   | Check suppressed response elevation is false when queue count is at or below the frequency limit counter.     |
| `CheckSuppressedResponseElevationIsTrueWhenQueueCountExceedsTheFrequencyLimitCounter`          | Fact   | Check suppressed response elevation is true when queue count exceeds the frequency limit counter.             |
| `ProcessResponseElevationDoesNothingWhenSuppressed`                                            | Fact   | Process response elevation does nothing when suppressed.                                                      |
| `ProcessResponseElevationDoesNothingWhenResponseElevationIsDisabledOnTheRule`                  | Fact   | Process response elevation does nothing when response elevation is disabled on the rule.                      |
| `ProcessResponseElevationDoesNothingWhenTheRulesElevationDoesNotExceedTheCurrentHighWaterMark` | Fact   | Process response elevation does nothing when the rules elevation does not exceed the current high water mark. |
| `ProcessResponseElevationRaisesTheHighWaterMarkAndCarriesTheValueForwardWithinAllLimits`       | Fact   | Process response elevation raises the high water mark and carries the value forward within all limits.        |
| `ProcessResponseElevationTruncatesToTheModelsMaxResponseElevationAndIncrementsThatCounter`     | Fact   | Process response elevation truncates to the models max response elevation and increments that counter.        |
| `ProcessResponseElevationTruncatesToTheGatewaysResponseElevationLimitAndIncrementsThatCounter` | Fact   | Process response elevation truncates to the gateways response elevation limit and increments that counter.    |
| `ProcessResponseElevationCopiesContentRedirectAndColoursFromTheRule`                           | Fact   | Process response elevation copies content redirect and colours from the rule.                                 |
| `ProcessResponseElevationEnqueuesAJournalEntryOnlyWhenTheLimitFlagIsEnabled`                   | Fact   | Process response elevation enqueues a journal entry only when the limit flag is enabled.                      |
| `ProcessResponseElevationDoesNotEnqueueAJournalEntryWhenTheLimitFlagIsDisabled`                | Fact   | Process response elevation does not enqueue a journal entry when the limit flag is disabled.                  |
| `ActivationRuleCountsAndArchiveHighWatermarkDoesNothingWhenSuppressed`                         | Fact   | Activation rule counts and archive high watermark does nothing when suppressed.                               |
| `ActivationRuleCountsAndArchiveHighWatermarkIncrementsCountAndSetsPrevailingWhenVisible`       | Fact   | Activation rule counts and archive high watermark increments count and sets prevailing when visible.          |
| `ActivationRuleCountsAndArchiveHighWatermarkDoesNotIncrementCountWhenNotVisible`               | Fact   | Activation rule counts and archive high watermark does not increment count when not visible.                  |
| `ActivationRuleCountsAndArchiveHighWatermarkAccumulatesAcrossMultipleVisibleRules`             | Fact   | Activation rule counts and archive high watermark accumulates across multiple visible rules.                  |
| `ActivationRuleGetOverridesIgnoresARuleThatIsNotEnabledForOverride`                            | Fact   | Activation rule get overrides ignores a rule that is not enabled for override.                                |
| `ActivationRuleGetOverridesIgnoresAForceOnARuleThatIsNotEnabledForForce`                       | Fact   | Activation rule get overrides ignores a force on a rule that is not enabled for force.                        |
| `ActivationRuleGetOverridesStillSuppressesARuleThatIsNotEnabledForForce`                       | Fact   | Activation rule get overrides still suppresses a rule that is not enabled for force.                          |
| `ActivationRuleGetOverridesIgnoresARuleBoundToADifferentOverrideKey`                           | Fact   | Activation rule get overrides ignores a rule bound to a different override key.                               |
| `ActivationRuleGetOverridesHonoursARuleBoundToItsOwnOverrideKey`                               | Fact   | Activation rule get overrides honours a rule bound to its own override key.                                   |
| `ActivationRuleGetOverridesIgnoresARuleTheModelDoesNotHave`                                    | Fact   | Activation rule get overrides ignores a rule the model does not have.                                         |
| `ActivationRuleGetOverridesReturnsNoOverridesWhenNoOverrideXPathsAreConfigured`                | Fact   | Activation rule get overrides returns no overrides when no override x paths are configured.                   |
| `ActivationRuleGetOverridesReturnsNoOverridesWhenTheOverrideKeyIsNotInThePayload`              | Fact   | Activation rule get overrides returns no overrides when the override key is not in the payload.               |
| `ActivationRuleGetOverridesSuppressesAllActivationRulesWhenTheValueIsOverridden`               | Fact   | Activation rule get overrides suppresses all activation rules when the value is overridden.                   |
| `ActivationRuleGetOverridesStaysSuppressedWhenALaterKeyIsNotSuppressed`                        | Fact   | Activation rule get overrides stays suppressed when a later key is not suppressed.                            |
| `ActivationRuleGetOverridesIsSuppressedWhenOnlyALaterKeyIsSuppressed`                          | Fact   | Activation rule get overrides is suppressed when only a later key is suppressed.                              |
| `ActivationRuleGetOverridesIsNotSuppressedWhenNoKeyIsSuppressed`                               | Fact   | Activation rule get overrides is not suppressed when no key is suppressed.                                    |
| `ActivationRuleGetOverridesUnionsActivationRuleOverridesAcrossKeys`                            | Fact   | Activation rule get overrides unions activation rule overrides across keys.                                   |
| `ActivationRuleGetOverridesLetsForceWinOverSuppressAcrossKeys`                                 | Fact   | Activation rule get overrides lets force win over suppress across keys.                                       |
| `ActivationRuleGetOverridesLetsForceWinOverAnAllActivationRulesSuppress`                       | Fact   | Activation rule get overrides lets force win over an all activation rules suppress.                           |
| `ActivationRuleGetOverridesFallsBackToTheAllActivationRulesOverride`                           | Fact   | Activation rule get overrides falls back to the all activation rules override.                                |
| `ActivationRuleGetOverridesIgnoresXPathsThatDoNotHaveOverrideEnabled`                          | Fact   | Activation rule get overrides ignores x paths that do not have override enabled.                              |
| `ActivationRuleGetOverridesDoesNotThrowWhenDependenciesHaveNotSynchronised`                    | Fact   | Activation rule get overrides does not throw when dependencies have not synchronised.                         |

### DictionaryKvpExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.DictionaryKvpExtensionsTests` — 8 tests. Category: Unit.

| Test                                                                         | Kind | Verifies                                                                                 |
|------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `ResolvesTheConfiguredValueFromThePayloadIntoTheDictionaryCache`             | Fact | Resolves the configured value from the payload into the dictionary cache.                |
| `FieldNamesAreMatchedAgainstDataNameNotTheDictionarysOwnName`                | Fact | Field names are matched against data name not the dictionarys own name.                  |
| `AKeyNotPresentInTheLookupTableResolvesToZero`                               | Fact | A key not present in the lookup table resolves to zero.                                  |
| `AFieldMissingFromThePayloadResolvesToZero`                                  | Fact | A field missing from the payload resolves to zero.                                       |
| `DoesNotOverwriteAnAlreadyResolvedValueForTheSameDictionaryName`             | Fact | Does not overwrite an already resolved value for the same dictionary name.               |
| `MultipleDictionariesMappedToTheSameFieldAreAllResolvedIndependently`        | Fact | Multiple dictionaries mapped to the same field are all resolved independently.           |
| `ADictionaryMappedToADifferentFieldIsUnaffectedByAnUnrelatedFieldResolution` | Fact | A dictionary mapped to a different field is unaffected by an unrelated field resolution. |
| `WithNoKvpDictionariesConfiguredNothingThrowsAndTheDictionaryStaysEmpty`     | Fact | With no kvp dictionaries configured nothing throws and the dictionary stays empty.       |

### EntityAnalysisModelInvokeSamplingTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.EntityAnalysisModelInvokeSamplingTests` — 8 tests. Category: Unit.

| Test                                                                                         | Kind | Verifies                                                                                                     |
|----------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------------|
| `WhenEnableSamplingIsOffEveryInvocationIsSampledRegardlessOfTheRandomDraw`                   | Fact | When enable sampling is off every invocation is sampled regardless of the random draw.                       |
| `WhenEnableSamplingIsOnAndTheDrawIsBelowThePercentageThresholdItIsSampled`                   | Fact | When enable sampling is on and the draw is below the percentage threshold it is sampled.                     |
| `WhenEnableSamplingIsOnAndTheDrawIsAboveThePercentageThresholdItIsNotSampled`                | Fact | When enable sampling is on and the draw is above the percentage threshold it is not sampled.                 |
| `WhenEnableSamplingIsOnAndSamplePercentageIsZeroNoDrawEverPasses`                            | Fact | When enable sampling is on and sample percentage is zero no draw ever passes.                                |
| `WhenEnableSamplingIsOnAndSamplePercentageIsOneHundredEveryDrawPasses`                       | Fact | When enable sampling is on and sample percentage is one hundred every draw passes.                           |
| `IsSampledOnThePayloadAlwaysMirrorsContextLogSampledExactly`                                 | Fact | Is sampled on the payload always mirrors context log sampled exactly.                                        |
| `WhenEnableLogsInfoIsOffEveryInvocationIsSampledEvenWithAStaleLowSamplePercentageConfigured` | Fact | When enable logs info is off every invocation is sampled even with a stale low sample percentage configured. |
| `WhenEnableLogsInfoIsOnSamplingStillThrottlesAsBefore`                                       | Fact | When enable logs info is on sampling still throttles as before.                                              |

### ExecuteAbstractionRulesWithoutSearchKeysExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ExecuteAbstractionRulesWithoutSearchKeysExtensionsTests` — 10 tests.
Category: Unit.

| Test                                                          | Kind | Verifies                                                                 |
|---------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `ARuleThatMatchesAddsOneToTheAbstractionDictionary`           | Fact | A rule that matches adds one to the abstraction dictionary.              |
| `ARuleThatDoesNotMatchAddsZeroToTheAbstractionDictionary`     | Fact | A rule that does not match adds zero to the abstraction dictionary.      |
| `ARuleFlaggedAsSearchIsSkippedEntirelyByThisExtension`        | Fact | A rule flagged as search is skipped entirely by this extension.          |
| `MultipleNonSearchRulesAllRunIndependently`                   | Fact | Multiple non search rules all run independently.                         |
| `AnExceptionThrownByARuleIsCaughtAndSubsequentRulesStillRun`  | Fact | An exception thrown by a rule is caught and subsequent rules still run.  |
| `AReportTableRuleAddsAnArchiveKeyWithTheAbstractionValue`     | Fact | A report table rule adds an archive key with the abstraction value.      |
| `ARuleWithoutReportTableDoesNotAddAnArchiveKey`               | Fact | A rule without report table does not add an archive key.                 |
| `WhenSampledTheStageTimingRecordsOneItemPerNonSearchRule`     | Fact | When sampled the stage timing records one item per non search rule.      |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns` | Fact | When not sampled no stage timing is built but business logic still runs. |
| `WithNoRulesConfiguredNothingThrowsAndAbstractionStaysEmpty`  | Fact | With no rules configured nothing throws and abstraction stays empty.     |

### ExecuteCacheEntriesExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ExecuteCacheEntriesExtensionsTests` — 16 tests. Category: Unit.

| Test                                                                                                    | Kind | Verifies                                                                                                                   |
|---------------------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------------------------|
| `InsertOrReplaceCacheEntriesQueuesNothingWhenCacheIsDisabled`                                           | Fact | Insert or replace cache entries queues nothing when cache is disabled.                                                     |
| `InsertOrReplaceCacheEntriesQueuesAnInsertAndWritesToPayloadWhenNotReprocessingAsync`                   | Fact | Insert or replace cache entries queues an insert and writes to payload when not reprocessing.                              |
| `ExecuteCacheDbStorageWritesNothingWhenReprocessingAsync`                                               | Fact | Execute cache db storage writes nothing when reprocessing.                                                                 |
| `ParseToReducedInternedPayloadAlwaysIncludesTheReferenceDateAtIndexMinusOne`                            | Fact | Parse to reduced interned payload always includes the reference date at index minus one.                                   |
| `ParseToReducedInternedPayloadIncludesAnInlineScriptAttributeValueWhenCacheIndexIdIsSetAndPayloadHasIt` | Fact | Parse to reduced interned payload includes an inline script attribute value when cache index id is set and payload has it. |
| `ParseToReducedInternedPayloadExcludesAnInlineScriptAttributeWithNoCacheIndexId`                        | Fact | Parse to reduced interned payload excludes an inline script attribute with no cache index id.                              |
| `ParseToReducedInternedPayloadExcludesAnInlineScriptAttributeWhosePayloadKeyIsMissing`                  | Fact | Parse to reduced interned payload excludes an inline script attribute whose payload key is missing.                        |
| `ParseToReducedInternedPayloadIncludesACachedRequestXPathValueWhenCacheIsTrueAndPayloadHasIt`           | Fact | Parse to reduced interned payload includes a cached request x path value when cache is true and payload has it.            |
| `ParseToReducedInternedPayloadExcludesARequestXPathWithCacheFalse`                                      | Fact | Parse to reduced interned payload excludes a request x path with cache false.                                              |
| `ParseToReducedInternedPayloadExcludesAPayloadEntryNotConfiguredForCachingAtAll`                        | Fact | Parse to reduced interned payload excludes a payload entry not configured for caching at all.                              |
| `UpsertCachePayloadLatestWritesTheMatchingPayloadValueForEachSearchKeyAsync`                            | Fact | Upsert cache payload latest writes the matching payload value for each search key.                                         |
| `UpsertCachePayloadLatestStillQueuesAnUpsertWithAnEmptyValueWhenThePayloadHasNoMatchAsync`              | Fact | Upsert cache payload latest still queues an upsert with an empty value when the payload has no match.                      |
| `UpsertCachePayloadLatestHandlesMultipleSearchKeysIndependentlyAsync`                                   | Fact | Upsert cache payload latest handles multiple search keys independently.                                                    |
| `ExecuteCacheDbStorageRecordsStageTimingWhenLogSampledIsTrue`                                           | Fact | Execute cache db storage records stage timing when log sampled is true.                                                    |
| `ExecuteCacheDbStorageDoesNotRecordStageTimingWhenLogSampledIsFalse`                                    | Fact | Execute cache db storage does not record stage timing when log sampled is false.                                           |
| `ExecuteCacheDbStorageQueuesBothPayloadAndPayloadLatestWrites`                                          | Fact | Execute cache db storage queues both payload and payload latest writes.                                                    |

### ExhaustiveAdaptationsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ExhaustiveAdaptationsExtensionsTests` — 14 tests. Category: Unit.

| Test                                                                              | Kind   | Verifies                                                                                       |
|-----------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `AddsTheRecalledScoreToTheExhaustiveAdaptationDictionaryUnderTheModelsName`       | Fact   | Adds the recalled score to the exhaustive adaptation dictionary under the models name.         |
| `PayloadValuesAreExtractedAndPassedThroughTheNetworkWithZScoreApplied`            | Fact   | Payload values are extracted and passed through the network with z score applied.              |
| `EachNonPayloadProcessingTypeIsExtractedFromItsOwnDictionaryAndRecalledCorrectly` | Theory | Each non payload processing type is extracted from its own dictionary and recalled correctly.  |
| `AnUnrecognisedProcessingTypeFallsBackToAbstraction`                              | Fact   | An unrecognised processing type falls back to abstraction.                                     |
| `AMissingValueFallsBackToTheVariablesConfiguredMean`                              | Fact   | A missing value falls back to the variables configured mean.                                   |
| `ANameContainingADotUsesOnlyTheSegmentAfterTheFirstDotAsTheLookupKey`             | Fact   | A name containing a dot uses only the segment after the first dot as the lookup key.           |
| `NormalisationTypeTwoAppliesTheZScoreFormulaBeforeRecall`                         | Fact   | Normalisation type two applies the z score formula before recall.                              |
| `AnExceptionDuringOneModelsRecallIsCaughtAndSubsequentModelsStillRun`             | Fact   | An exception during one models recall is caught and subsequent models still run.               |
| `WhenSampledTheStageTimingRecordsOneItemPerModel`                                 | Fact   | When sampled the stage timing records one item per model.                                      |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns`                     | Fact   | When not sampled no stage timing is built but business logic still runs.                       |
| `WithNoModelsConfiguredNothingThrowsAndTheDictionaryStaysEmpty`                   | Fact   | With no models configured nothing throws and the dictionary stays empty.                       |
| `TheExplainabilityDetailCarriesTheSameValueAsTheScoreAndReconstructsTheRawOutput` | Fact   | The explainability detail carries the same value as the score and reconstructs the raw output. |
| `AnUncalibratedModelReportsNotCalibratedAndAScoreSpaceContribution`               | Fact   | An uncalibrated model reports not calibrated and a score space contribution.                   |
| `ContributionSourceIsMappedFromTheVariablesProcessingTypeId`                      | Fact   | Contribution source is mapped from the variables processing type id.                           |

### GatewayRulesExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.GatewayRulesExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                                |
|---------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `ARuleThatMatchesSetsMatchedGatewayRuleAndTheResponseElevationLimit`      | Fact | A rule that matches sets matched gateway rule and the response elevation limit.         |
| `ARuleThatDoesNotMatchLeavesMatchedGatewayRuleFalse`                      | Fact | A rule that does not match leaves matched gateway rule false.                           |
| `ARuleIsSkippedEntirelyWhenTheRandomGatewaySampleIsAtOrAboveItsThreshold` | Fact | A rule is skipped entirely when the random gateway sample is at or above its threshold. |
| `ARuleRunsWhenTheRandomGatewaySampleIsBelowItsThreshold`                  | Fact | A rule runs when the random gateway sample is below its threshold.                      |
| `TheFirstMatchingRuleStopsEvaluationOfSubsequentRules`                    | Fact | The first matching rule stops evaluation of subsequent rules.                           |
| `ARuleThatDoesNotMatchAllowsTheNextRuleToStillBeEvaluated`                | Fact | A rule that does not match allows the next rule to still be evaluated.                  |
| `AnExceptionThrownByARuleIsCaughtAndSubsequentRulesStillRun`              | Fact | An exception thrown by a rule is caught and subsequent rules still run.                 |
| `EvaluationCounterIncrementsForEveryRuleTestedRegardlessOfMatch`          | Fact | Evaluation counter increments for every rule tested regardless of match.                |
| `ActivationCounterOnlyIncrementsWhenTheRuleActuallyMatches`               | Fact | Activation counter only increments when the rule actually matches.                      |
| `ModelInvokeGatewayCounterIncrementsOnTheModelWhenARuleMatches`           | Fact | Model invoke gateway counter increments on the model when a rule matches.               |
| `WhenSampledTheGatewayStageTimingIsPopulatedWithOneItemPerRuleTested`     | Fact | When sampled the gateway stage timing is populated with one item per rule tested.       |
| `WhenNotSampledNoStageTimingIsBuiltAtAll`                                 | Fact | When not sampled no stage timing is built at all.                                       |
| `WithNoRulesConfiguredAtAllNothingMatchesAndNoExceptionIsThrown`          | Fact | With no rules configured at all nothing matches and no exception is thrown.             |

### InlineFunctionsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.InlineFunctionsExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                       | Kind | Verifies                                                                               |
|----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `AStringResultIsAddedToThePayload`                                         | Fact | A string result is added to the payload.                                               |
| `AnIntegerResultIsAddedToThePayload`                                       | Fact | An integer result is added to the payload.                                             |
| `ADoubleResultIsAddedToThePayload`                                         | Fact | A double result is added to the payload.                                               |
| `ABooleanResultIsAddedToThePayload`                                        | Fact | A boolean result is added to the payload.                                              |
| `ANullStringResultIsAddedToThePayloadAsNullRatherThanThrowing`             | Fact | A null string result is added to the payload as null rather than throwing.             |
| `DoesNotOverwriteAnExistingPayloadValueWithTheSameName`                    | Fact | Does not overwrite an existing payload value with the same name.                       |
| `ReportTableAddsAStringArchiveKeyWithProcessingTypeThree`                  | Fact | Report table adds a string archive key with processing type three.                     |
| `ReportTableAddsADoubleArchiveKeyWithTheNumericValue`                      | Fact | Report table adds a double archive key with the numeric value.                         |
| `WithoutReportTableNoArchiveKeyIsAdded`                                    | Fact | Without report table no archive key is added.                                          |
| `AnExceptionThrownByTheRuleDelegateIsCaughtAndSubsequentFunctionsStillRun` | Fact | An exception thrown by the rule delegate is caught and subsequent functions still run. |
| `WhenSampledTheStageTimingRecordsOneItemPerFunction`                       | Fact | When sampled the stage timing records one item per function.                           |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRuns`              | Fact | When not sampled no stage timing is built but business logic still runs.               |
| `WithNoFunctionsConfiguredNothingThrowsAndThePayloadStaysUntouched`        | Fact | With no functions configured nothing throws and the payload stays untouched.           |

### InlineScriptsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.InlineScriptsExtensionsTests` — 15 tests. Category: Unit.

| Test                                                                                      | Kind | Verifies                                                                                         |
|-------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `AScriptWithNoPayloadEventIsNeverInvokedAsync`                                            | Fact | A script with no payload event is never invoked.                                                 |
| `AStringPropertyIsAddedToThePayloadAsync`                                                 | Fact | A string property is added to the payload.                                                       |
| `AnIntPropertyIsAddedToThePayloadAsync`                                                   | Fact | An int property is added to the payload.                                                         |
| `ADoublePropertyIsAddedToThePayloadAsync`                                                 | Fact | A double property is added to the payload.                                                       |
| `ANullPropertyValueIsSkippedAsync`                                                        | Fact | A null property value is skipped.                                                                |
| `AnUnsupportedPropertyTypeIsSkippedAsync`                                                 | Fact | An unsupported property type is skipped.                                                         |
| `ReportTableAddsAnArchiveKeyWithProcessingTypeOneAsync`                                   | Fact | Report table adds an archive key with processing type one.                                       |
| `WithoutReportTableNoArchiveKeyIsAddedAsync`                                              | Fact | Without report table no archive key is added.                                                    |
| `WhenExecuteAsyncDelegateReturnsFalsePropertiesAreNotProcessedAsync`                      | Fact | When execute async delegate returns false properties are not processed.                          |
| `AnExceptionThrownByTheExecuteDelegateIsCaughtAndSubsequentScriptsStillRunAsync`          | Fact | An exception thrown by the execute delegate is caught and subsequent scripts still run.          |
| `WhenTheExecuteDelegateThrowsPropertiesAreNotPopulatedFromThePossiblyBrokenInstanceAsync` | Fact | When the execute delegate throws properties are not populated from the possibly broken instance. |
| `WhenSampledTheStageTimingRecordsOneItemPerPayloadScriptAsync`                            | Fact | When sampled the stage timing records one item per payload script.                               |
| `WhenNotSampledNoStageTimingIsBuiltButBusinessLogicStillRunsAsync`                        | Fact | When not sampled no stage timing is built but business logic still runs.                         |
| `TheResponsePayloadJObjectIsClearedAfterExecutionAsync`                                   | Fact | The response payload j object is cleared after execution.                                        |
| `WithNoScriptsConfiguredNothingThrowsAsync`                                               | Fact | With no scripts configured nothing throws.                                                       |

### IterateActivationRulesExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.IterateActivationRulesExtensionsTests` — 24 tests. Category: Unit.

| Test                                                                                  | Kind | Verifies                                                                                        |
|---------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ReturnsZeroCountAndNoCaseWhenThereAreNoActivationRulesAsync`                         | Fact | Returns zero count and no case when there are no activation rules.                              |
| `ARuleThatDoesNotMatchIsNotAddedToActivationsAndDoesNotCountAsync`                    | Fact | A rule that does not match is not added to activations and does not count.                      |
| `AMatchedVisibleRuleIsAddedToActivationsAndBecomesPrevailingAsync`                    | Fact | A matched visible rule is added to activations and becomes prevailing.                          |
| `AMatchedButNotVisibleRuleIsAddedToActivationsButDoesNotCountOrBecomePrevailingAsync` | Fact | A matched but not visible rule is added to activations but does not count or become prevailing. |
| `TheLastMatchedVisibleRuleInIterationOrderBecomesPrevailingAsync`                     | Fact | The last matched visible rule in iteration order becomes prevailing.                            |
| `ARuleThatFailsActivationSamplingIsSkippedEntirelyAsync`                              | Fact | A rule that fails activation sampling is skipped entirely.                                      |
| `AForceOverrideActivatesARuleThatWouldNotOtherwiseMatchAsync`                         | Fact | A force override activates a rule that would not otherwise match.                               |
| `AForceOverrideBypassesActivationSamplingAsync`                                       | Fact | A force override bypasses activation sampling.                                                  |
| `AForceOverrideOnlyAppliesToTheRuleItIsBoundToAsync`                                  | Fact | A force override only applies to the rule it is bound to.                                       |
| `AForcedActivationIsMarkedForcedOnThePayloadAndCountedSeparatelyAsync`                | Fact | A forced activation is marked forced on the payload and counted separately.                     |
| `AGenuineMatchIsNotMarkedForcedAndDoesNotRaiseTheForcedCounterAsync`                  | Fact | A genuine match is not marked forced and does not raise the forced counter.                     |
| `ARuleSuppressedAtTheModelLevelIsStillEvaluatedButNotCountedTowardsPrevailingAsync`   | Fact | A rule suppressed at the model level is still evaluated but not counted towards prevailing.     |
| `ARuleSuppressedAtTheRuleLevelViaAnotherModelsOverrideListIsNotCountedAsync`          | Fact | A rule suppressed at the rule level via another models override list is not counted.            |
| `ARuleDisabledForReprocessingIsSuppressedDuringAReprocessingRunAsync`                 | Fact | A rule disabled for reprocessing is suppressed during a reprocessing run.                       |
| `AddsAnArchiveKeyWhenReportTableIsEnabledOnAMatchedRuleAsync`                         | Fact | Adds an archive key when report table is enabled on a matched rule.                             |
| `DoesNotAddAnArchiveKeyWhenReportTableIsDisabledAsync`                                | Fact | Does not add an archive key when report table is disabled.                                      |
| `TwoRulesSharingTheSameNameOnlyRecordTheFirstMatchAsync`                              | Fact | Two rules sharing the same name only record the first match.                                    |
| `AnInlineScriptOverrideCanFlipANonMatchingRuleToMatchedAsync`                         | Fact | An inline script override can flip a non matching rule to matched.                              |
| `AnInlineScriptThatReturnsFalseDoesNotFlipAMatchAsync`                                | Fact | An inline script that returns false does not flip a match.                                      |
| `OnlyTheFirstMatchedRuleWithCaseWorkflowEnabledCreatesACaseAsync`                     | Fact | Only the first matched rule with case workflow enabled creates a case.                          |
| `AReprocessingEligibleRuleDoesNotCreateACaseDuringAReprocessingRunAsync`              | Fact | A reprocessing eligible rule does not create a case during a reprocessing run.                  |
| `ATtlCounterIsStillIncrementedForAMatchedRuleEvenWhenSuppressedAtTheModelLevelAsync`  | Fact | A ttl counter is still incremented for a matched rule even when suppressed at the model level.  |
| `ARuleThatThrowsDuringProcessingIsLoggedAndDoesNotStopSubsequentRulesAsync`           | Fact | A rule that throws during processing is logged and does not stop subsequent rules.              |
| `EveryEvaluatedRuleGetsATimingEntryWithRuleDurationAsync`                             | Fact | Every evaluated rule gets a timing entry with rule duration.                                    |

### ReferenceDateExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ReferenceDateExtensionsTests` — 6 tests. Category: Unit.

| Test                                                                           | Kind | Verifies                                                                                |
|--------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `FirstEverCallForAModelUpsertsSinceNoReferenceDateIsStoredYetAsync`            | Fact | First ever call for a model upserts since no reference date is stored yet.              |
| `ANewerReferenceDateOverwritesTheStoredOneAsync`                               | Fact | A newer reference date overwrites the stored one.                                       |
| `AnOlderOutOfOrderReferenceDateIsRejectedAndDoesNotOverwriteTheStoredOneAsync` | Fact | An older out of order reference date is rejected and does not overwrite the stored one. |
| `DifferentTenantsWithTheSameModelGuidDoNotCollideAsync`                        | Fact | Different tenants with the same model guid do not collide.                              |
| `AFutureReferenceDateThrowsAndDoesNotUpsertAsync`                              | Fact | A future reference date throws and does not upsert.                                     |
| `AReferenceDateEqualToTheStoredOneIsAcceptedAndUpsertedAsync`                  | Fact | A reference date equal to the stored one is accepted and upserted.                      |

### ResponseTimePipelineExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ResponseTimePipelineExtensionsTests` — 7 tests. Category: Unit.

| Test                                                                        | Kind | Verifies                                                                               |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `RecordResponseTimeWhenNotSampledDoesNotCreatePipelineOrEntries`            | Fact | Record response time when not sampled does not create pipeline or entries.             |
| `RecordResponseTimeWhenSampledAddsOneEntryWithTheGivenStageName`            | Fact | Record response time when sampled adds one entry with the given stage name.            |
| `RecordResponseTimeAccumulatesOneEntryPerCallAcrossStages`                  | Fact | Record response time accumulates one entry per call across stages.                     |
| `RecordResponseTimeUpdatesLastResponseTimeElapsedMicrosecondsToTheNewEntry` | Fact | Record response time updates last response time elapsed microseconds to the new entry. |
| `RecordResponseTimeClampsAllocatedBytesToZeroWhenTheDeltaWouldBeNegative`   | Fact | Record response time clamps allocated bytes to zero when the delta would be negative.  |
| `RecordResponseTimeUsesStartBytesUsedWhenNoPriorCheckpointExists`           | Fact | Record response time uses start bytes used when no prior checkpoint exists.            |
| `RecordResponseTimeNeverProducesANegativeDurationOnTheVeryFirstCall`        | Fact | Record response time never produces a negative duration on the very first call.        |

### SanctionsExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.SanctionsExtensionsTests` — 12 tests. Category: Unit.

| Test                                                                            | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `ExecuteSanctionsAsyncWithAnExactMatchPopulatesTheSanctionResponseAsync`        | Fact | Execute sanctions async with an exact match populates the sanction response.          |
| `ExecuteSanctionsAsyncWithNoMatchDoesNotPopulateTheSanctionResponseAsync`       | Fact | Execute sanctions async with no match does not populate the sanction response.        |
| `ExecuteSanctionsAsyncWhenPayloadIsMissingTheConfiguredFieldSkipsTheCheckAsync` | Fact | Execute sanctions async when payload is missing the configured field skips the check. |
| `ExecuteSanctionsAsyncDispatchesToTheConfiguredAggregationTypeAsync`            | Fact | Execute sanctions async dispatches to the configured aggregation type.                |
| `ExecuteSanctionsAsyncEvaluatesEveryConfiguredCheckIndependentlyAsync`          | Fact | Execute sanctions async evaluates every configured check independently.               |
| `ExecuteSanctionsAsyncRecordsPerCheckTaskPerformanceWhenLogSampledAsync`        | Fact | Execute sanctions async records per check task performance when log sampled.          |
| `ExecuteSanctionsAsyncOnCacheMissWritesTheComputedValueIntoCacheAsync`          | Fact | Execute sanctions async on cache miss writes the computed value into cache.           |
| `ExecuteSanctionsAsyncOnANoMatchDoesNotWriteAnythingToCacheAsync`               | Fact | Execute sanctions async on a no match does not write anything to cache.               |
| `ExecuteSanctionsAsyncOnAFreshNonExpiredCacheHitDoesNotRecomputeAsync`          | Fact | Execute sanctions async on a fresh non expired cache hit does not recompute.          |
| `ExecuteSanctionsAsyncOnAnExpiredCacheHitRecomputesFromLiveEntriesAsync`        | Fact | Execute sanctions async on an expired cache hit recomputes from live entries.         |
| `ExecuteSanctionsAsyncOnANonExpiredCachedNullValueStillRecomputesAsync`         | Fact | Execute sanctions async on a non expired cached null value still recomputes.          |
| `ExecuteSanctionsAsyncCacheKeyIsScopedByDistanceThresholdAsync`                 | Fact | Execute sanctions async cache key is scoped by distance threshold.                    |

### SnapshotPinningTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.SnapshotPinningTests` — 2 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `AnInvocationContextKeepsThePinnedGenerationWhenTheModelPublishesANewOne` | Fact | An invocation context keeps the pinned generation when the model publishes a new one. |
| `AContextWithoutAPinnedSnapshotFollowsTheModelsCurrentOne`                | Fact | A context without a pinned snapshot follows the models current one.                   |

### TtlCounterExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.TtlCounterExtensionsTests` — 14 tests. Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                    |
|---------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `ApplyTtlCounterIntervalSubtractsTheGivenUnitFromReferenceDate`                 | Theory | Apply ttl counter interval subtracts the given unit from reference date.                    |
| `ApplyTtlCounterIntervalSubtractsMonthsForM`                                    | Fact   | Apply ttl counter interval subtracts months for m.                                          |
| `ApplyTtlCounterIntervalSubtractsYearsForY`                                     | Fact   | Apply ttl counter interval subtracts years for y.                                           |
| `ApplyTtlCounterIntervalWithAVerySmallValueSupportsVeryShortLivedCounters`      | Fact   | Apply ttl counter interval with a very small value supports very short lived counters.      |
| `ApplyTtlCounterIntervalWithZeroValueReturnsReferenceDateUnchanged`             | Fact   | Apply ttl counter interval with zero value returns reference date unchanged.                |
| `ApplyTtlCounterIntervalWithAnUnrecognisedIntervalLeavesReferenceDateUnchanged` | Fact   | Apply ttl counter interval with an unrecognised interval leaves reference date unchanged.   |
| `ApplyTtlCounterIntervalWithANegativeValuePushesTheWindowIntoTheFutureInstead`  | Fact   | Apply ttl counter interval with a negative value pushes the window into the future instead. |
| `FiveSecondWindowIncludesEntriesAtBothInclusiveBoundariesAsync`                 | Fact   | Five second window includes entries at both inclusive boundaries.                           |
| `FiveSecondWindowExcludesEntriesJustOutsideEitherBoundaryAsync`                 | Fact   | Five second window excludes entries just outside either boundary.                           |
| `OneMinuteWindowIncludesEntriesInsideAndExcludesJustOutsideAsync`               | Fact   | One minute window includes entries inside and excludes just outside.                        |
| `ZeroLengthWindowOnlyIncludesAnEntryExactlyAtReferenceDateAsync`                | Fact   | Zero length window only includes an entry exactly at reference date.                        |
| `DifferentDataValuesForTheSameCounterDoNotCollideAsync`                         | Fact   | Different data values for the same counter do not collide.                                  |
| `DoesNothingWhenModelLevelEnableTtlCounterIsFalseAsync`                         | Fact   | Does nothing when model level enable ttl counter is false.                                  |
| `DoesNothingWhenThePayloadHasNoValueForTheCountersDataNameAsync`                | Fact   | Does nothing when the payload has no value for the counters data name.                      |

### WaitTasksExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.WaitTasksExtensionsTests` — 13 tests. Category: Unit.

| Test                                                                                                | Kind   | Verifies                                                                                                     |
|-----------------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------|
| `WaitReadTasksAsyncWithNoPendingTasksLeavesAnEmptyReadStatsBlockAsync`                              | Fact   | Wait read tasks async with no pending tasks leaves an empty read stats block.                                |
| `WaitReadTasksAsyncRecordsSanctionsAsyncComputeTimeAndMemoryAsync`                                  | Fact   | Wait read tasks async records sanctions async compute time and memory.                                       |
| `WaitReadTasksAsyncRecordsTtlCountersAsyncAndAbstractionRulesWithSearchKeysAsyncIndependentlyAsync` | Fact   | Wait read tasks async records ttl counters async and abstraction rules with search keys async independently. |
| `WaitReadTasksAsyncSkipsAFaultedTaskWithoutThrowingAsync`                                           | Fact   | Wait read tasks async skips a faulted task without throwing.                                                 |
| `WaitReadTasksAsyncWhenSampledRecordsTheJoinReadTasksStageDurationAsync`                            | Fact   | Wait read tasks async when sampled records the join read tasks stage duration.                               |
| `WaitReadTasksAsyncWhenNotSampledDoesNotRecordAnyStageAsync`                                        | Fact   | Wait read tasks async when not sampled does not record any stage.                                            |
| `WaitWriteTasksAsyncWithNoPendingTasksLeavesAnEmptyWriteStatsBlockAsync`                            | Fact   | Wait write tasks async with no pending tasks leaves an empty write stats block.                              |
| `WaitWriteTasksAsyncRecordsEachRecognisedWriteTaskTypeAsync`                                        | Theory | Wait write tasks async records each recognised write task type.                                              |
| `WaitWriteTasksAsyncSkipsAFaultedTaskWithoutThrowingAsync`                                          | Fact   | Wait write tasks async skips a faulted task without throwing.                                                |
| `WaitWriteTasksAsyncIgnoresATaskTypeItDoesNotRecogniseAsync`                                        | Fact   | Wait write tasks async ignores a task type it does not recognise.                                            |
| `WaitWriteTasksAsyncWhenSampledRecordsTheJoinWriteTasksStageDurationAsync`                          | Fact   | Wait write tasks async when sampled records the join write tasks stage duration.                             |
| `WaitWriteTasksAsyncWhenNotSampledDoesNotRecordAnyStageAsync`                                       | Fact   | Wait write tasks async when not sampled does not record any stage.                                           |
| `WaitWriteTasksAsyncRecordsMultipleDistinctWriteTaskTypesIndependentlyAsync`                        | Fact   | Wait write tasks async records multiple distinct write task types independently.                             |

## Entity analysis model invoke context extensions abstraction rules with search keys

### EntityAnalysisModelAbstractionRuleAggregatorUtilityTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.ContextExtensions.AbstractionRulesWithSearchKeys.EntityAnalysisModelAbstractionRuleAggregatorUtilityTests` — 56 tests. Category: Unit.

| Test                                                                                                      | Kind   | Verifies                                                                                                                   |
|-----------------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------|
| `AggregateReturnsZeroWhenNoMatchesWereRecordedForTheAbstractionRule`                                      | Fact   | Aggregate returns zero when no matches were recorded for the abstraction rule.                                             |
| `AggregateReturnsZeroWhenTheMatchedListForTheRuleIsPresentButEmpty`                                       | Fact   | Aggregate returns zero when the matched list for the rule is present but empty.                                            |
| `AggregateThrowsInsteadOfDegradingToZeroWhenTheAbstractionRuleItselfIsNull`                               | Fact   | Aggregate throws instead of degrading to zero when the abstraction rule itself is null.                                    |
| `ResolveSkipAndFetchIgnoresTheOffsetConfigurationEntirelyWhenOffsetIsDisabled`                            | Theory | Resolve skip and fetch ignores the offset configuration entirely when offset is disabled.                                  |
| `ResolveSkipAndFetchAppliesTheConfiguredOffsetTypeWhenOffsetIsEnabled`                                    | Theory | Resolve skip and fetch applies the configured offset type when offset is enabled.                                          |
| `OffsetFirstSelectsTheRecordAtThePositionGivenByTheOffsetValue`                                           | Fact   | Offset first selects the record at the position given by the offset value.                                                 |
| `OffsetLastCountsBackwardsFromTheMostRecentRecord`                                                        | Fact   | Offset last counts backwards from the most recent record.                                                                  |
| `OffsetSkipFirstSumsEverythingExceptTheOldestRecords`                                                     | Fact   | Offset skip first sums everything except the oldest records.                                                               |
| `OffsetTakeLastSumsOnlyTheMostRecentRecords`                                                              | Fact   | Offset take last sums only the most recent records.                                                                        |
| `OffsetSkipFirstBeyondTheAvailableRecordsYieldsAnEmptyRangeRatherThanThrowing`                            | Fact   | Offset skip first beyond the available records yields an empty range rather than throwing.                                 |
| `OffsetTakeLastBeyondTheAvailableRecordsFallsBackToEverything`                                            | Fact   | Offset take last beyond the available records falls back to everything.                                                    |
| `AggregateCountReturnsTheNumberOfDocumentsAfterOffsetReduction`                                           | Fact   | Aggregate count returns the number of documents after offset reduction.                                                    |
| `AggregateCountReturnsZeroForAnEmptyDocumentList`                                                         | Fact   | Aggregate count returns zero for an empty document list.                                                                   |
| `AggregateDistinctCountCountsUniqueValuesForTheSearchFunctionKey`                                         | Fact   | Aggregate distinct count counts unique values for the search function key.                                                 |
| `AggregateDistinctCountMatchesTheSearchFunctionKeyCaseInsensitively`                                      | Fact   | Aggregate distinct count matches the search function key case insensitively.                                               |
| `AggregateDistinctCountTreatsValuesDifferingOnlyByCaseAsTheSameDistinctEntry`                             | Fact   | Aggregate distinct count treats values differing only by case as the same distinct entry.                                  |
| `AggregateDistinctCountSkipsDocumentsThatDoNotHaveTheSearchFunctionKeyAtAll`                              | Fact   | Aggregate distinct count skips documents that do not have the search function key at all.                                  |
| `AggregateSameCountCountsDocumentsMatchingTheCurrentPayloadValue`                                         | Fact   | Aggregate same count counts documents matching the current payload value.                                                  |
| `AggregateSameCountComparesCaseInsensitively`                                                             | Fact   | Aggregate same count compares case insensitively.                                                                          |
| `AggregateSameCountDoesNotCountDocumentsThatLackTheSearchFunctionKey`                                     | Fact   | Aggregate same count does not count documents that lack the search function key.                                           |
| `AggregateSameCountTreatsAMissingPayloadFieldAsTheLiteralStringNoneWhichCanAccidentallyMatchHistoricData` | Fact   | Aggregate same count treats a missing payload field as the literal string none which can accidentally match historic data. |
| `AggregateRawReturnsTheDoubleValueOfTheSearchFunctionKeyOnTheLastDocument`                                | Fact   | Aggregate raw returns the double value of the search function key on the last document.                                    |
| `AggregateRawReturnsZeroWhenTheLastDocumentDoesNotHaveTheSearchFunctionKey`                               | Fact   | Aggregate raw returns zero when the last document does not have the search function key.                                   |
| `AggregateRawReturnsZeroRatherThanThrowingWhenTheDocumentListIsEmpty`                                     | Fact   | Aggregate raw returns zero rather than throwing when the document list is empty.                                           |
| `AggregateRawReturnsZeroWhenTheSearchFunctionKeyWasStoredAsAStringRatherThanADouble`                      | Fact   | Aggregate raw returns zero when the search function key was stored as a string rather than a double.                       |
| `RawGracefullyReturnsZeroWhenAnOffsetLeavesAnEmptyWindow`                                                 | Fact   | Raw gracefully returns zero when an offset leaves an empty window.                                                         |
| `BuildValuesArrayReadsTheSearchFunctionKeyFromEachDocumentInOrder`                                        | Fact   | Build values array reads the search function key from each document in order.                                              |
| `BuildValuesArraySubstitutesZeroRatherThanSkippingWhenADocumentLacksTheSearchFunctionKey`                 | Fact   | Build values array substitutes zero rather than skipping when a document lacks the search function key.                    |
| `BuildValuesArrayTreatsAStringTypedFieldAsZero`                                                           | Fact   | Build values array treats a string typed field as zero.                                                                    |
| `BuildValuesArrayIsEmptyWhenTheDocumentListIsEmpty`                                                       | Fact   | Build values array is empty when the document list is empty.                                                               |
| `ComputeExtremeStatisticSumsTheValues`                                                                    | Fact   | Compute extreme statistic sums the values.                                                                                 |
| `ComputeExtremeStatisticAveragesTheValues`                                                                | Fact   | Compute extreme statistic averages the values.                                                                             |
| `ComputeExtremeStatisticFindsTheMedian`                                                                   | Fact   | Compute extreme statistic finds the median.                                                                                |
| `ComputeExtremeStatisticComputesKurtosis`                                                                 | Fact   | Compute extreme statistic computes kurtosis.                                                                               |
| `ComputeExtremeStatisticComputesSkewness`                                                                 | Fact   | Compute extreme statistic computes skewness.                                                                               |
| `ComputeExtremeStatisticComputesStandardDeviation`                                                        | Fact   | Compute extreme statistic computes standard deviation.                                                                     |
| `ComputeExtremeStatisticAddsStandardDeviationToTheMean`                                                   | Fact   | Compute extreme statistic adds standard deviation to the mean.                                                             |
| `ComputeExtremeStatisticDoublesStandardDeviationBeforeAddingTheMeanForFunctionType10`                     | Fact   | Compute extreme statistic doubles standard deviation before adding the mean for function type10.                           |
| `ComputeExtremeStatisticTreatsFunctionType12IdenticallyToFunctionType10WhenCalledDirectly`                | Fact   | Compute extreme statistic treats function type12 identically to function type10 when called directly.                      |
| `FunctionType12IsInterceptedAsSameCountBeforeReachingTheExtremeStatisticSwitch`                           | Fact   | Function type12 is intercepted as same count before reaching the extreme statistic switch.                                 |
| `ComputeExtremeStatisticFindsTheMode`                                                                     | Fact   | Compute extreme statistic finds the mode.                                                                                  |
| `ComputeExtremeStatisticFindsTheMaximum`                                                                  | Fact   | Compute extreme statistic finds the maximum.                                                                               |
| `ComputeExtremeStatisticFindsTheMinimum`                                                                  | Fact   | Compute extreme statistic finds the minimum.                                                                               |
| `ComputeExtremeStatisticReturnsZeroForAnUnrecognisedFunctionType`                                         | Theory | Compute extreme statistic returns zero for an unrecognised function type.                                                  |
| `AggregateReturnsZeroWhenTheMeanOfAnEmptyRangeIsNaN`                                                      | Fact   | Aggregate returns zero when the mean of an empty range is na n.                                                            |
| `AggregateReturnsZeroWhenStandardDeviationOfASingleValueIsNaN`                                            | Fact   | Aggregate returns zero when standard deviation of a single value is na n.                                                  |
| `AggregateReturnsZeroWhenModeThrowsForAnEmptyRangeRatherThanPropagating`                                  | Fact   | Aggregate returns zero when mode throws for an empty range rather than propagating.                                        |
| `AggregateReturnsZeroWhenMaxThrowsForAnEmptyRangeRatherThanPropagating`                                   | Fact   | Aggregate returns zero when max throws for an empty range rather than propagating.                                         |
| `SinceIsZeroWhenNoOffsetIsConfiguredBecauseTheCurrentAndTestDateComeFromTheSameLastRecord`                | Fact   | Since is zero when no offset is configured because the current and test date come from the same last record.               |
| `SinceConvertsTheGapBetweenTheCurrentAndTestRecordUsingTheConfiguredIntervalType`                         | Theory | Since converts the gap between the current and test record using the configured interval type.                             |
| `SinceParsesTheSearchFunctionKeyOnBothRecordsWhenItIsPresentAndParsable`                                  | Fact   | Since parses the search function key on both records when it is present and parsable.                                      |
| `SinceDefaultsToDateTimeMinValueWhenNeitherTheSearchFunctionKeyNorCreatedDateArePresent`                  | Fact   | Since defaults to date time min value when neither the search function key nor created date are present.                   |
| `SinceThrowsWhenTheFullMatchesListIsEmptyRatherThanDegradingGracefully`                                   | Fact   | Since throws when the full matches list is empty rather than degrading gracefully.                                         |
| `SinceHasNoLocalGuardSoAnEmptyOffsetWindowIsCaughtOnlyByTheOuterErrorHandler`                             | Fact   | Since has no local guard so an empty offset window is caught only by the outer error handler.                              |
| `SanitizeNumericResultZeroesOutNonFiniteValues`                                                           | Theory | Sanitize numeric result zeroes out non finite values.                                                                      |
| `SanitizeNumericResultLeavesFiniteValuesUnchanged`                                                        | Fact   | Sanitize numeric result leaves finite values unchanged.                                                                    |

## Entity analysis model invoke extraction

### ArchiveKeyExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.ArchiveKeyExtensionsTests` — 10 tests. Category: Unit.

| Test                                                                      | Kind   | Verifies                                                                             |
|---------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `WhenReportTableIsFalseNoArchiveKeyIsAdded`                               | Fact   | When report table is false no archive key is added.                                  |
| `WhenIsReprocessIsTrueNoArchiveKeyIsAddedEvenIfReportTableIsSet`          | Fact   | When is reprocess is true no archive key is added even if report table is set.       |
| `AStringValueProducesAnArchiveKeyWithTheKeyValueStringPopulated`          | Fact   | A string value produces an archive key with the key value string populated.          |
| `AnIntValueProducesAnArchiveKeyWithTheKeyValueIntegerPopulated`           | Fact   | An int value produces an archive key with the key value integer populated.           |
| `AFloatValueProducesAnArchiveKeyWithTheKeyValueFloatPopulated`            | Fact   | A float value produces an archive key with the key value float populated.            |
| `ADateValueProducesAnArchiveKeyWithTheKeyValueDatePopulated`              | Fact   | A date value produces an archive key with the key value date populated.              |
| `ABoolValueProducesAnArchiveKeyWithTheKeyValueBooleanPopulatedAsAByte`    | Theory | A bool value produces an archive key with the key value boolean populated as a byte. |
| `PrecedenceFavoursStringOverAnyOtherValueWhenMultipleAreSuppliedTogether` | Fact   | Precedence favours string over any other value when multiple are supplied together.  |
| `NoValueSuppliedProducesAnArchiveKeyWithNoValueFieldsPopulated`           | Fact   | No value supplied produces an archive key with no value fields populated.            |
| `MultipleCallsAppendIndependentArchiveKeysToTheSameList`                  | Fact   | Multiple calls append independent archive keys to the same list.                     |

### EntityAnalysisModelDictionaryNoBoxingExtractorTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelDictionaryNoBoxingExtractorTests` — 13 tests.
Category: Unit.

| Test                                                                                            | Kind   | Verifies                                                                                                       |
|-------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------|
| `TheEntryAndReferenceDateAreCopiedFromTheirConfiguredFieldNames`                                | Fact   | The entry and reference date are copied from their configured field names.                                     |
| `TheReprocessingRuleInstanceIdIsCarriedOntoThePayload`                                          | Fact   | The reprocessing rule instance id is carried onto the payload.                                                 |
| `TheOriginalEntryBecomesThePayloadInstanceDirectly`                                             | Fact   | The original entry becomes the payload instance directly.                                                      |
| `AFieldAlreadyPresentInTheArchivedEntryIsLeftUntouchedAndNotOverwrittenByAnyDefault`            | Fact   | A field already present in the archived entry is left untouched and not overwritten by any default.            |
| `AStringFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault`                   | Fact   | A string field missing from the archived entry is backfilled from the configured default.                      |
| `AnIntegerFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault`                 | Fact   | An integer field missing from the archived entry is backfilled from the configured default.                    |
| `AnIntegerFieldWithANonNumericDefaultIsNotAddedRatherThanCorruptingThePayloadWithAZero`         | Fact   | An integer field with a non numeric default is not added rather than corrupting the payload with a zero.       |
| `AFloatLatitudeOrLongitudeFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault` | Theory | A float latitude or longitude field missing from the archived entry is backfilled from the configured default. |
| `ABooleanFieldMissingFromTheArchivedEntryIsBackfilledFromTheConfiguredDefault`                  | Theory | A boolean field missing from the archived entry is backfilled from the configured default.                     |
| `ADateFieldMissingFromTheArchivedEntryUsesTheConfiguredDefaultAsADaysOffsetFromNow`             | Fact   | A date field missing from the archived entry uses the configured default as a days offset from now.            |
| `AFieldMissingFromTheArchivedEntryWithNoConfiguredDefaultIsNotAddedToThePayload`                | Fact   | A field missing from the archived entry with no configured default is not added to the payload.                |
| `AnUnrecognisedDataTypeIdFallsBackToStringHandlingForTheDefault`                                | Fact   | An unrecognised data type id falls back to string handling for the default.                                    |
| `TheReturnedContextCarriesTheModelAndAvailableModels`                                           | Fact   | The returned context carries the model and available models.                                                   |

### EntityAnalysisModelInstanceEntryPayloadHelpersTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelInstanceEntryPayloadHelpersTests` — 6 tests.
Category: Unit.

| Test                                                        | Kind | Verifies                                                            |
|-------------------------------------------------------------|------|---------------------------------------------------------------------|
| `IdentifyingModelMetadataIsCopiedOntoThePayload`            | Fact | Identifying model metadata is copied onto the payload.              |
| `WithNoGuidSuppliedANewGuidIsGenerated`                     | Fact | With no guid supplied a new guid is generated.                      |
| `ASuppliedGuidIsUsedInsteadOfGeneratingANewOne`             | Fact | A supplied guid is used instead of generating a new one.            |
| `EveryPooledCollectionIsInitialisedAndEmpty`                | Fact | Every pooled collection is initialised and empty.                   |
| `InvokeTaskPerformanceAndCreatedDateAreAlwaysPopulated`     | Fact | Invoke task performance and created date are always populated.      |
| `TwoCallsForTheSameModelProduceIndependentPayloadInstances` | Fact | Two calls for the same model produce independent payload instances. |

### EntityAnalysisModelJsonExtractorTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Extraction.EntityAnalysisModelJsonExtractorTests` — 22 tests. Category:
Unit.

| Test                                                                                          | Kind   | Verifies                                                                                                        |
|-----------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------------|
| `TheEntryIsExtractedViaEntryXPathAndAddedToThePayloadUnderEntryName`                          | Fact   | The entry is extracted via entry x path and added to the payload under entry name.                              |
| `TheReferenceDateIsExtractedViaReferenceDateXPathAndAddedToThePayload`                        | Fact   | The reference date is extracted via reference date x path and added to the payload.                             |
| `WhenReferenceDateLocationTypeIsNowTheCurrentUtcTimeIsUsedInstead`                            | Fact   | When reference date location type is now the current utc time is used instead.                                  |
| `AFieldAlreadyPresentInThePayloadFromEntryOrReferenceDateExtractionIsNotReprocessedAsAnXPath` | Fact   | A field already present in the payload from entry or reference date extraction is not reprocessed as an x path. |
| `AStringFieldIsExtractedAndAddedToThePayload`                                                 | Fact   | A string field is extracted and added to the payload.                                                           |
| `AFloatFieldIsExtractedAsADoubleAndReportedToTheArchiveKeys`                                  | Fact   | A float field is extracted as a double and reported to the archive keys.                                        |
| `LatitudeAndLongitudeDataTypesAreExtractedAsDoublesLikeAFloat`                                | Theory | Latitude and longitude data types are extracted as doubles like a float.                                        |
| `AnIntegerFieldIsExtractedAndReported`                                                        | Fact   | An integer field is extracted and reported.                                                                     |
| `AnIntegerFieldThatFailsToParseIsSkippedWithoutThrowing`                                      | Fact   | An integer field that fails to parse is skipped without throwing.                                               |
| `ADoubleFieldThatFailsToParseIsSkippedWithoutThrowing`                                        | Fact   | A double field that fails to parse is skipped without throwing.                                                 |
| `ABooleanFieldIsParsedCaseInsensitively`                                                      | Theory | A boolean field is parsed case insensitively.                                                                   |
| `ADateFieldIsParsedAndReported`                                                               | Fact   | A date field is parsed and reported.                                                                            |
| `AnUnrecognisedDataTypeIdFallsBackToStringHandling`                                           | Fact   | An unrecognised data type id falls back to string handling.                                                     |
| `AMissingFieldWithADefaultValueFallsBackToTheDefault`                                         | Fact   | A missing field with a default value falls back to the default.                                                 |
| `AMissingFieldWithNoDefaultValueIsNotAddedToThePayload`                                       | Fact   | A missing field with no default value is not added to the payload.                                              |
| `AMissingDateFieldWithADefaultUsesTheDefaultAsADaysOffsetFromNow`                             | Fact   | A missing date field with a default uses the default as a days offset from now.                                 |
| `DeterministicEncryptionProducesTheSameCipherTextForTheSamePlainText`                         | Fact   | Deterministic encryption produces the same cipher text for the same plain text.                                 |
| `RandomEncryptionProducesDifferentCipherTextOnEachExtraction`                                 | Fact   | Random encryption produces different cipher text on each extraction.                                            |
| `AStringFieldResolvesAConfiguredKvpDictionaryValueIntoTheDictionaryCache`                     | Fact   | A string field resolves a configured kvp dictionary value into the dictionary cache.                            |
| `AnEmptyInputStreamProducesANullJObjectAndDoesNotThrow`                                       | Fact   | An empty input stream produces a null j object and does not throw.                                              |
| `MalformedJsonPropagatesAJsonReaderException`                                                 | Fact   | Malformed json propagates a json reader exception.                                                              |
| `TheReturnedContextCarriesTheModelAvailableModelsAndAsyncFlag`                                | Fact   | The returned context carries the model available models and async flag.                                         |

## Entity analysis model invoke models payload entity analysis model instance entry payload logs

### InvocationLogTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Models.Payload.EntityAnalysisModelInstanceEntryPayload.Logs.InvocationLogTests` — 4 tests. Category: Unit.

| Test                                                            | Kind   | Verifies                                                                   |
|-----------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `AddEntryAppendsToEntries`                                      | Fact   | Add entry appends to entries.                                              |
| `EveryAddedEntryIsPresentInTheSerialisedOutput`                 | Fact   | Every added entry is present in the serialised output.                     |
| `AnEmptyLogStillSerialisesTheAnchorDateWithAnEmptyEntriesArray` | Fact   | An empty log still serialises the anchor date with an empty entries array. |
| `NoPropertyOnTheInvocationLogModelIsNonPublic`                  | Theory | No property on the invocation log model is non public.                     |

## Entity analysis model invoke simulation

### AbstractionAggregationSimulatorTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.AbstractionAggregationSimulatorTests` — 6 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                      |
|---------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `CountsTheMatchingTransactionsInsideTheWindowIncludingTheCurrentOneAsync` | Fact | Counts the matching transactions inside the window including the current one. |
| `SumsTheFunctionKeyOverTheMatchesInTheWindowAsync`                        | Fact | Sums the function key over the matches in the window.                         |
| `ASearchKeyWithAShorterTtlShortensTheWindowAndSaysSoAsync`                | Fact | A search key with a shorter ttl shortens the window and says so.              |
| `ARuleThatThrowsIsReportedAsync`                                          | Fact | A rule that throws is reported.                                               |
| `EngineDefaultsApplyWhenTheRecordLeavesFieldsUnset`                       | Fact | Engine defaults apply when the record leaves fields unset.                    |
| `CachedPayloadsDecodeByIndexWithTheReferenceDateAtMinusOne`               | Fact | Cached payloads decode by index with the reference date at minus one.         |

### InlineScriptRunnerTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.InlineScriptRunnerTests` — 4 tests. Category: Unit.

| Test                                                       | Kind | Verifies                                                      |
|------------------------------------------------------------|------|---------------------------------------------------------------|
| `TheScriptReadsThePayloadAndItsPropertiesAreReturnedAsync` | Fact | The script reads the payload and its properties are returned. |
| `AScriptThatDoesNotCompileReportsItsErrorsAsync`           | Fact | A script that does not compile reports its errors.            |
| `AScriptThatThrowsIsReportedAsync`                         | Fact | A script that throws is reported.                             |
| `AScriptThatReturnsFalseContributesNothingAsync`           | Fact | A script that returns false contributes nothing.              |

### InvocationContextBuilderTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.InvocationContextBuilderTests` — 9 tests. Category: Unit.

| Test                                                           | Kind   | Verifies                                                               |
|----------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `RequestJsonFieldsAreExtractedTypedWithTheEnginesDefaults`     | Fact   | Request json fields are extracted typed with the engines defaults.     |
| `OnlyExtractionIsComputedFromRequestJsonAndStorageIsExcluded`  | Fact   | Only extraction is computed from request json and storage is excluded. |
| `TheReferenceDateComesFromTheClockWhenTheModelSaysSo`          | Fact   | The reference date comes from the clock when the model says so.        |
| `ABlankContextCarriesOnlyTheDefaults`                          | Fact   | A blank context carries only the defaults.                             |
| `AnArchivedTransactionReadsBackThroughTheEnginesOwnSerializer` | Fact   | An archived transaction reads back through the engines own serializer. |
| `TheArchiveStoresDictionaryValuesUnderDictionaryNotKvp`        | Fact   | The archive stores dictionary values under dictionary not kvp.         |
| `AnOverlaySetsTypedValuesAndReportsUnknownNamesAndBadValues`   | Fact   | An overlay sets typed values and reports unknown names and bad values. |
| `TextConvertsByCompletionDataType`                             | Theory | Text converts by completion data type.                                 |
| `ValuesFormatInvariantly`                                      | Fact   | Values format invariantly.                                             |

### RuleBacktestTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.RuleBacktestTests` — 8 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                 |
|--------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EachOutcomeIsTheCellOfTheConfusionMatrix`                         | Theory | Each outcome is the cell of the confusion matrix.                        |
| `AnActivationRuleIsCountedAgainstATagClassFromArchivedValuesAsync` | Fact   | An activation rule is counted against a tag class from archived values.  |
| `TheClassCanCombineTagsAndValuesAsync`                             | Fact   | The class can combine tags and values.                                   |
| `TheFilterRunsAsAReprocessingRuleAndOnlyKeptRowsAreEvaluatedAsync` | Fact   | The filter runs as a reprocessing rule and only kept rows are evaluated. |
| `WithoutAClassOnlyFiringIsCountedAndPagesAccumulateAsync`          | Fact   | Without a class only firing is counted and pages accumulate.             |
| `SamplesAreCappedButCountsAreNotAsync`                             | Fact   | Samples are capped but counts are not.                                   |
| `ARuntimeErrorCountsAsNotFiredAsTheEngineTreatsItAsync`            | Fact   | A runtime error counts as not fired as the engine treats it.             |
| `AnExplanationGivesTheOutcomeAndTheValuesReadAsync`                | Fact   | An explanation gives the outcome and the values read.                    |

### RuleRunnerTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.RuleRunnerTests` — 7 tests. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                               |
|----------------------------------------------------------------------|--------|------------------------------------------------------------------------|
| `AGatewayRuleRunsAgainstTheContextAsync`                             | Theory | A gateway rule runs against the context.                               |
| `AReprocessingRuleUsesTheReprocessingDelegateAsync`                  | Fact   | A reprocessing rule uses the reprocessing delegate.                    |
| `AnAbstractionRuleCompilesAsTheEnginesAbstractionRuleClassAsync`     | Fact   | An abstraction rule compiles as the engines abstraction rule class.    |
| `AnActivationRuleSeesCountersAbstractionsAndEarlierActivationsAsync` | Fact   | An activation rule sees counters abstractions and earlier activations. |
| `AnAbstractionCalculationReturnsItsNumberAsync`                      | Fact   | An abstraction calculation returns its number.                         |
| `AnInlineFunctionReturnsItsValueAsync`                               | Fact   | An inline function returns its value.                                  |
| `ARuntimeErrorIsReportedRatherThanSwallowedAsync`                    | Fact   | A runtime error is reported rather than swallowed.                     |

### TimeWindowTests

`Jube.Test.Engine.EntityAnalysisModelInvoke.Simulation.TimeWindowTests` — 6 tests. Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                          |
|-----------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `ATtlCounterWindowIsTheEnginesOwnArithmetic`                          | Theory | A ttl counter window is the engines own arithmetic.                               |
| `AnAbstractionRuleWindowIsTheEnginesVisualBasicDateAdd`               | Theory | An abstraction rule window is the engines visual basic date add.                  |
| `TheAbstractionWindowIsShortenedBySearchKeyTtlExactlyAsTheEngineDoes` | Fact   | The abstraction window is shortened by search key ttl exactly as the engine does. |
| `ACalendarWindowAlsoOffersWeeksAndMeansYearsByY`                      | Fact   | A calendar window also offers weeks and means years by y.                         |
| `AnIntervalTheEntityDoesNotAllowIsRefused`                            | Theory | An interval the entity does not allow is refused.                                 |
| `EveryIntervalHasAName`                                               | Fact   | Every interval has a name.                                                        |

## Entity analysis model manager background tasks task starters

### CachePruneTaskStarterIntervalTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarterIntervalTests` — 6 tests.
Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                  |
|---------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `EachRecognisedIntervalSubtractsFromReferenceDate`                              | Theory | Each recognised interval subtracts from reference date.                                   |
| `SecondsIntervalSupportsVeryShortRetentionWindows`                              | Fact   | Seconds interval supports very short retention windows.                                   |
| `UnrecognisedIntervalCharFallsBackToDays`                                       | Fact   | Unrecognised interval char falls back to days.                                            |
| `ZeroIntervalValueReturnsReferenceDateUnchanged`                                | Fact   | Zero interval value returns reference date unchanged.                                     |
| `NegativeIntervalValuePushesTheThresholdIntoTheFutureRiskingDeletingEverything` | Fact   | Negative interval value pushes the threshold into the future risking deleting everything. |
| `NullReferenceDateReturnsNullThresholdSoTheCallerSkipsPruning`                  | Fact   | Null reference date returns null threshold so the caller skips pruning.                   |

### CachePruneTaskStarterPostgresBackedTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarterPostgresBackedTests` — 1
test. Category: Service. Collection: Database.

| Test                                                                                              | Kind | Verifies                                                                                                    |
|---------------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `PruneModelAsyncDeletesOnlyWhatsExpiredAcrossPayloadAndPayloadLatestAndAuditsBothToPostgresAsync` | Fact | Prune model async deletes only whats expired across payload and payload latest and audits both to postgres. |

### CachePruneTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.CachePruneTaskStarterTests` — 4 tests.
Category: Unit.

| Test                                                                                 | Kind | Verifies                                                                                            |
|--------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `PruneModelDoesNothingWhenNoReferenceDateIsStoredForTheModelAsync`                   | Fact | Prune model does nothing when no reference date is stored for the model.                            |
| `PruneModelLooksUpTheReferenceDateUsingTheModelsOwnTenantAndGuidAsync`               | Fact | Prune model looks up the reference date using the models own tenant and guid.                       |
| `GetDeletionLimitOrDefaultIfNullUsesTheShippedDefaultOfOneThousandWhenNotOverridden` | Fact | Get deletion limit or default if null uses the shipped default of one thousand when not overridden. |
| `GetDeletionLimitOrDefaultIfNullUsesTheConfiguredOverrideWhenSet`                    | Fact | Get deletion limit or default if null uses the configured override when set.                        |

### EntityAnalysisModelSyncWatermarkQueryPostgresBackedTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.EntityAnalysisModelSyncWatermarkQueryPostgresBackedTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                                                         | Kind | Verifies                                                                                                 |
|----------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `TheWatermarkQueryTranslatesAndRunsAgainstPostgresAsync`                                     | Fact | The watermark query translates and runs against postgres.                                                |
| `ATenantWithNoRowsAtAllProducesAZeroWatermarkRatherThanFailingAsync`                         | Fact | A tenant with no rows at all produces a zero watermark rather than failing.                              |
| `TheSameDatabaseStateProducesAnEqualWatermarkSoTheEngineDoesNotRebuildAsync`                 | Fact | The same database state produces an equal watermark so the engine does not rebuild.                      |
| `AnApiKeyIssuedToAnActiveUserWithATenantRoleMovesTheWatermarkSoTheEngineResynchronisesAsync` | Fact | An api key issued to an active user with a tenant role moves the watermark so the engine resynchronises. |

### LruJournalPruneTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.LruJournalPruneTaskStarterTests` — 7 tests.
Category: Unit.

| Test                                                     | Kind   | Verifies                                                           |
|----------------------------------------------------------|--------|--------------------------------------------------------------------|
| `MinutesIntervalReturnsATimeSpanInMinutes`               | Fact   | Minutes interval returns a time span in minutes.                   |
| `HoursIntervalReturnsATimeSpanInHours`                   | Fact   | Hours interval returns a time span in hours.                       |
| `AnyOtherIntervalIncludingDIsTreatedAsDays`              | Theory | Any other interval including d is treated as days.                 |
| `AVerySmallValueSupportsAShortMaxAgeWindow`              | Fact   | A very small value supports a short max age window.                |
| `ZeroValueReturnsAZeroLengthTimeSpan`                    | Fact   | Zero value returns a zero length time span.                        |
| `ANonNumericOrMissingValueDefaultsToOne`                 | Theory | A non numeric or missing value defaults to one.                    |
| `ANegativeValueProducesANegativeTimeSpanWithNoGuardRail` | Fact   | A negative value produces a negative time span with no guard rail. |

### TtlCounterAdministrationCacheServiceTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministrationCacheServiceTests` — 6
tests. Category: Unit.

| Test                                                             | Kind   | Verifies                                                                 |
|------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `EachRecognisedIntervalSubtractsFromReferenceDate`               | Theory | Each recognised interval subtracts from reference date.                  |
| `SecondsIntervalSupportsVeryShortLivedCounters`                  | Fact   | Seconds interval supports very short lived counters.                     |
| `UnrecognisedIntervalFallsBackToDays`                            | Fact   | Unrecognised interval falls back to days.                                |
| `ZeroIntervalValueReturnsReferenceDateUnchanged`                 | Fact   | Zero interval value returns reference date unchanged.                    |
| `NegativeIntervalValuePushesTheWindowIntoTheFutureInstead`       | Fact   | Negative interval value pushes the window into the future instead.       |
| `AnOverflowingIntervalIsCaughtAndReturnsReferenceDateUnadjusted` | Fact   | An overflowing interval is caught and returns reference date unadjusted. |

### TtlCounterAdministrationTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.BackgroundTasks.TaskStarters.TtlCounterAdministrationTaskStarterTests` — 5
tests. Category: Unit.

| Test                                                                                    | Kind | Verifies                                                                                        |
|-----------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `ProcessTtlCounterDeprecationDecrementsTheCounterByTheExpiredEntrysValueAsync`          | Fact | Process ttl counter deprecation decrements the counter by the expired entrys value.             |
| `ProcessTtlCounterDeprecationDeletesTheExpiredCounterEntryAsync`                        | Fact | Process ttl counter deprecation deletes the expired counter entry.                              |
| `ProcessTtlCounterDeprecationRecordsTheRemovalBatchEntryUsingDataValueNotDataNameAsync` | Fact | Process ttl counter deprecation records the removal batch entry using data value not data name. |
| `ADecrementFailureDoesNotPreventTheEntryFromStillBeingDeletedAsync`                     | Fact | A decrement failure does not prevent the entry from still being deleted.                        |
| `MultipleExpiredEntriesProcessedInSequenceDoNotCrossContaminateBatchEntriesAsync`       | Fact | Multiple expired entries processed in sequence do not cross contaminate batch entries.          |

## Entity analysis model manager backtest

### BacktestHeartbeatTests

`Jube.Test.Engine.EntityAnalysisModelManager.Backtest.BacktestHeartbeatTests` — 8 tests. Category: Service. Collection:
Database.

| Test                                                              | Kind   | Verifies                                                                   |
|-------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `TheIntervalIsAQuarterOfTheStaleWindowBetweenOneAndTenSeconds`    | Theory | The interval is a quarter of the stale window between one and ten seconds. |
| `APageLongerThanTheStaleWindowNoLongerFailsARunningInstanceAsync` | Fact   | A page longer than the stale window no longer fails a running instance.    |
| `AnInstanceWhoseHeartbeatHasStoppedIsStillRecoveredAsync`         | Fact   | An instance whose heartbeat has stopped is still recovered.                |
| `ReportedProgressIsWrittenOnTheNextBeatAsync`                     | Fact   | Reported progress is written on the next beat.                             |
| `AStopOrARecoveryIsSeenWithinABeatEvenMidPageAsync`               | Theory | A stop or a recovery is seen within a beat even mid page.                  |
| `APageRecordsProgressAndCarriesOnUntilAStopIsSeenAsync`           | Fact   | A page records progress and carries on until a stop is seen.               |
| `ADisposedHeartbeatWritesNothingMoreAsync`                        | Fact   | A disposed heartbeat writes nothing more.                                  |
| `AFailedBeatIsRetriedRatherThanEndingTheHeartbeatAsync`           | Fact   | A failed beat is retried rather than ending the heartbeat.                 |

## Entity analysis model manager entity analysis model context extensions

### InlineScriptCompilationExtensionsTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Extensions.InlineScriptCompilationExtensionsTests` — 14 tests. Category: Unit.

| Test                                                                                             | Kind | Verifies                                                                                                 |
|--------------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `AValidCSharpScriptCompilesWithoutErrorsAndLocatesTheInterfaceImplementation`                    | Fact | A valid c sharp script compiles without errors and locates the interface implementation.                 |
| `ASyntaxOrSemanticErrorProducesCompileErrorsAndNoInterfaceImplementation`                        | Fact | A syntax or semantic error produces compile errors and no interface implementation.                      |
| `TheActivatorDelegateCreatesANewInstanceOfTheCompiledType`                                       | Fact | The activator delegate creates a new instance of the compiled type.                                      |
| `TheExecuteAsyncDelegateInvokesTheCompiledMethodAndReturnsItsResultAsync`                        | Fact | The execute async delegate invokes the compiled method and returns its result.                           |
| `ReportTableResponsePayloadAndCacheIndexAttributesAreReflectedOntoThePropertyAttribute`          | Fact | Report table response payload and cache index attributes are reflected onto the property attribute.      |
| `SearchKeyAttributeSubFieldsAreCapturedOntoTheDistinctSearchKey`                                 | Fact | Search key attribute sub fields are captured onto the distinct search key.                               |
| `SearchKeyDecoratedPropertyIsAlsoAddedAsAModelGroupingKey`                                       | Fact | Search key decorated property is also added as a model grouping key.                                     |
| `LatitudeAndLongitudeAttributesAreReflectedIndependentlyOfEachOther`                             | Fact | Latitude and longitude attributes are reflected independently of each other.                             |
| `APropertyWithNoAttributesHasEveryDecorationFlagFalseAndNoSearchKey`                             | Fact | A property with no attributes has every decoration flag false and no search key.                         |
| `EventAttributesOnExecuteAsyncArePopulatedAndOrderedByPriority`                                  | Fact | Event attributes on execute async are populated and ordered by priority.                                 |
| `RunningTheCompiledScriptThroughReflectInlineScriptHelperPopulatesThePayloadAndArchiveKeysAsync` | Fact | Running the compiled script through reflect inline script helper populates the payload and archive keys. |
| `RunningTheCompiledScriptThroughExecuteInlineScriptsAsyncEndToEndPopulatesThePayloadAsync`       | Fact | Running the compiled script through execute inline scripts async end to end populates the payload.       |
| `TheSystemWideDemonstrationVisualBasicScriptCompilesAndProducesASixDigitOtpAsync`                | Fact | The system wide demonstration visual basic script compiles and produces a six digit otp.                 |
| `CompilingTheSameCodeTwiceProducesIndependentDelegatesThatStillBehaveIdentically`                | Fact | Compiling the same code twice produces independent delegates that still behave identically.              |

## Entity analysis model manager entity analysis model context models

### SnapshotsTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Models.SnapshotsTests` — 7 tests. Category:
Unit.

| Test                                                             | Kind | Verifies                                                                    |
|------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `OneBuilderIsHandedOutPerModelForTheWholeCycle`                  | Fact | One builder is handed out per model for the whole cycle.                    |
| `EachModelGetsItsOwnBuilder`                                     | Fact | Each model gets its own builder.                                            |
| `NothingReachesAModelUntilTheCyclePublishes`                     | Fact | Nothing reaches a model until the cycle publishes.                          |
| `EveryModelTouchedInTheCycleIsPublishedTogether`                 | Fact | Every model touched in the cycle is published together.                     |
| `AModelNotTouchedInTheCycleKeepsItsSnapshot`                     | Fact | A model not touched in the cycle keeps its snapshot.                        |
| `PublishingClearsTheCycleSoTheNextOneStartsFromWhatWasPublished` | Fact | Publishing clears the cycle so the next one starts from what was published. |
| `PublishingTwiceWithoutAnotherCycleDoesNotAdvanceTheGeneration`  | Fact | Publishing twice without another cycle does not advance the generation.     |

## Entity analysis model manager entity analysis model context utilities

### EntityAnalysisModelRecordMapperTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities.EntityAnalysisModelRecordMapperTests` — 32 tests. Category: Unit.

| Test                                                                                           | Kind   | Verifies                                                                                                    |
|------------------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------------|
| `NameNullOnTheRecordDefaultsToAnEmptyString`                                                   | Fact   | Name null on the record defaults to an empty string.                                                        |
| `NameWithSpacesHasSpacesReplacedWithUnderscores`                                               | Fact   | Name with spaces has spaces replaced with underscores.                                                      |
| `EntryXPathNullOnTheRecordDefaultsToAnEmptyString`                                             | Fact   | Entry x path null on the record defaults to an empty string.                                                |
| `EntryXPathIsCopiedVerbatimWhenPresent`                                                        | Fact   | Entry x path is copied verbatim when present.                                                               |
| `ReferenceDateXPathNullOnTheRecordDefaultsToAnEmptyString`                                     | Fact   | Reference date x path null on the record defaults to an empty string.                                       |
| `ReferenceDateXPathIsCopiedVerbatimWhenPresent`                                                | Fact   | Reference date x path is copied verbatim when present.                                                      |
| `EntryNameNullOnTheRecordDefaultsToAnEmptyString`                                              | Fact   | Entry name null on the record defaults to an empty string.                                                  |
| `EntryNameIsCopiedVerbatimWhenPresent`                                                         | Fact   | Entry name is copied verbatim when present.                                                                 |
| `ReferenceDateNameNullOnTheRecordDefaultsToAnEmptyString`                                      | Fact   | Reference date name null on the record defaults to an empty string.                                         |
| `ReferenceDateNameIsCopiedVerbatimWhenPresent`                                                 | Fact   | Reference date name is copied verbatim when present.                                                        |
| `BooleanFlagIsTrueOnlyWhenTheRecordValueIsExactlyOne`                                          | Theory | Boolean flag is true only when the record value is exactly one.                                             |
| `CharFieldFallsBackToItsDocumentedDefaultWhenNull`                                             | Theory | Char field falls back to its documented default when null.                                                  |
| `IntFieldFallsBackToItsDocumentedDefaultWhenNull`                                              | Theory | Int field falls back to its documented default when null.                                                   |
| `DoubleFieldFallsBackToItsDocumentedDefaultWhenNull`                                           | Theory | Double field falls back to its documented default when null.                                                |
| `ImplicitAsyncTimeoutMillisecondsPassesThroughIncludingNull`                                   | Fact   | Implicit async timeout milliseconds passes through including null.                                          |
| `LogsWarnThresholdMillisecondsPassesThroughIncludingNull`                                      | Fact   | Logs warn threshold milliseconds passes through including null.                                             |
| `SamplePercentagePassesThroughIncludingNull`                                                   | Fact   | Sample percentage passes through including null.                                                            |
| `GuidIsAssignedWhenTheRecordGuidIsNotEmpty`                                                    | Fact   | Guid is assigned when the record guid is not empty.                                                         |
| `GuidIsLeftUnchangedWhenTheRecordGuidIsEmpty`                                                  | Fact   | Guid is left unchanged when the record guid is empty.                                                       |
| `TenantRegistryIdIsAssignedWhenPresent`                                                        | Fact   | Tenant registry id is assigned when present.                                                                |
| `TenantRegistryIdIsLeftUnchangedWhenTheRecordValueIsNull`                                      | Fact   | Tenant registry id is left unchanged when the record value is null.                                         |
| `TenantRegistryIdBeingNullIsLoggedAsAnErrorNotJustADebugMessage`                               | Fact   | Tenant registry id being null is logged as an error not just a debug message.                               |
| `MaxActivationWatcherValueResetsToZeroWhenTheRecordValueIsNull`                                | Fact   | Max activation watcher value resets to zero when the record value is null.                                  |
| `MaxActivationWatcherValueIsReplacedNotAccumulatedAcrossRepeatedSyncs`                         | Fact   | Max activation watcher value is replaced not accumulated across repeated syncs.                             |
| `MaxActivationWatcherValueTracksTheLatestDatabaseValueAcrossSyncsRatherThanTheFirst`           | Fact   | Max activation watcher value tracks the latest database value across syncs rather than the first.           |
| `FullyPopulatedRecordMapsEveryFieldOntoItsCorrespondingContextProperty`                        | Fact   | Fully populated record maps every field onto its corresponding context property.                            |
| `CreateEntityAnalysisModelWiresIdentityFromRecordAndContext`                                   | Fact   | Create entity analysis model wires identity from record and context.                                        |
| `CreateEntityAnalysisModelWiresSharedServicesByReference`                                      | Fact   | Create entity analysis model wires shared services by reference.                                            |
| `CreateEntityAnalysisModelWiresSharedContextCollectionsByReferenceSoLaterUpdatesArePropagated` | Fact   | Create entity analysis model wires shared context collections by reference so later updates are propagated. |
| `CreateEntityAnalysisModelStartsOnAnEmptySnapshotThatTheModelOwns`                             | Fact   | Create entity analysis model starts on an empty snapshot that the model owns.                               |
| `CreateEntityAnalysisModelPopulatesArchiverBuffersPerArchiverPersistThreadsSetting`            | Fact   | Create entity analysis model populates archiver buffers per archiver persist threads setting.               |
| `CreateEntityAnalysisModelReturnsAFreshInstanceForEachCall`                                    | Fact   | Create entity analysis model returns a fresh instance for each call.                                        |

### SyncWatermarkDecisionTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Context.Utilities.SyncWatermarkDecisionTests` — 12
tests. Category: Unit.

| Test                                                                  | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `TheFirstCycleForATenantAlwaysSynchronises`                           | Fact | The first cycle for a tenant always synchronises.                                  |
| `AnUnreadableWatermarkFallsBackToSynchronising`                       | Fact | An unreadable watermark falls back to synchronising.                               |
| `AnUnchangedWatermarkDoesNotSynchronise`                              | Fact | An unchanged watermark does not synchronise.                                       |
| `AnApprovalSynchronisesBecauseApprovalIsWhatMakesAChangeEffective`    | Fact | An approval synchronises because approval is what makes a change effective.        |
| `ANewOverrideSynchronises`                                            | Fact | A new override synchronises.                                                       |
| `AnOverrideDeletedWithoutChangingTheHighestIdStillSynchronises`       | Fact | An override deleted without changing the highest id still synchronises.            |
| `APromotedExhaustiveModelSynchronises`                                | Fact | A promoted exhaustive model synchronises.                                          |
| `AnExpiryStillInTheFutureDoesNotSynchronise`                          | Fact | An expiry still in the future does not synchronise.                                |
| `AnExpiryThatHasPassedSynchronisesEvenThoughNothingWasWritten`        | Fact | An expiry that has passed synchronises even though nothing was written.            |
| `AnExpiryFallingDueExactlyNowSynchronises`                            | Fact | An expiry falling due exactly now synchronises.                                    |
| `NoExpiryAtAllDoesNotSynchronise`                                     | Fact | No expiry at all does not synchronise.                                             |
| `TheExpiryThatMattersIsTheOneTheLastSynchronisationSawNotTheFreshOne` | Fact | The expiry that matters is the one the last synchronisation saw not the fresh one. |

## Entity analysis model manager entity analysis model models

### ModelSnapshotBuilderTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshotBuilderTests` — 11 tests. Category:
Unit.

| Test                                                                            | Kind | Verifies                                                                                     |
|---------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `ABuilderOverNoPreviousSnapshotProducesTheFirstGeneration`                      | Fact | A builder over no previous snapshot produces the first generation.                           |
| `EachBuildIncrementsTheGenerationFromThePreviousSnapshot`                       | Fact | Each build increments the generation from the previous snapshot.                             |
| `CollectionsThatTheCycleDoesNotReplaceAreCarriedForwardByReference`             | Fact | Collections that the cycle does not replace are carried forward by reference.                |
| `ACollectionReplacedInTheCycleDoesNotAffectThePreviousSnapshot`                 | Fact | A collection replaced in the cycle does not affect the previous snapshot.                    |
| `TheSearchKeysDictionaryIsCopiedSoMidCycleAdditionsAreNotVisibleUntilPublished` | Fact | The search keys dictionary is copied so mid cycle additions are not visible until published. |
| `ActivationRuleCountersAreSharedWithTheReplacementRuleSoLateIncrementsAreKept`  | Fact | Activation rule counters are shared with the replacement rule so late increments are kept.   |
| `GatewayRuleCountersAreSharedWithTheReplacementRuleSoLateIncrementsAreKept`     | Fact | Gateway rule counters are shared with the replacement rule so late increments are kept.      |
| `ARuleThatIsNewInThisGenerationKeepsItsOwnCounters`                             | Fact | A rule that is new in this generation keeps its own counters.                                |
| `CountersAreNotDisturbedWhenTheCycleDidNotReplaceTheRules`                      | Fact | Counters are not disturbed when the cycle did not replace the rules.                         |
| `ARuleThatAppearsTwiceInTheOutgoingGenerationDoesNotFailTheCarryForward`        | Fact | A rule that appears twice in the outgoing generation does not fail the carry forward.        |
| `BuildCarriesEveryCollectionOntoTheSnapshot`                                    | Fact | Build carries every collection onto the snapshot.                                            |

### ModelSnapshotCutoverTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.ModelSnapshotCutoverTests` — 4 tests. Category:
Unit.

| Test                                                                | Kind | Verifies                                                                 |
|---------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AModelStartsOnAnEmptyFirstSnapshot`                                | Fact | A model starts on an empty first snapshot.                               |
| `PublishingReplacesTheWholeSnapshotInOneStep`                       | Fact | Publishing replaces the whole snapshot in one step.                      |
| `AReaderHoldingASnapshotNeverObservesAMixtureOfTwoGenerationsAsync` | Fact | A reader holding a snapshot never observes a mixture of two generations. |
| `EveryGenerationAReaderObservesIsOneThatWasActuallyPublishedAsync`  | Fact | Every generation a reader observes is one that was actually published.   |

## Entity analysis model manager reprocessing

### ReprocessingDateRangeTests

`Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing.ReprocessingDateRangeTests` — 6 tests. Category: Unit.

| Test                                                             | Kind   | Verifies                                                                         |
|------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `TheStartIsTheLastReferenceDateLessTheInterval`                  | Theory | The start is the last reference date less the interval.                          |
| `AMonthBackFromTheEndOfAMonthLandsOnTheLastDayOfTheShorterMonth` | Fact   | A month back from the end of a month lands on the last day of the shorter month. |
| `AnUnknownIntervalOrANegativeValueHasNoStart`                    | Theory | An unknown interval or a negative value has no start.                            |
| `AnIntervalReachingBeforeTheCalendarStartsAtTheBeginning`        | Fact   | An interval reaching before the calendar starts at the beginning.                |
| `TheSampleIsAPercentage`                                         | Theory | The sample is a percentage.                                                      |
| `OverManyDrawsTheSampledShareMatchesThePercentage`               | Theory | Over many draws the sampled share matches the percentage.                        |

### ReprocessingTaskStarterTests

`Jube.Test.Engine.EntityAnalysisModelManager.Reprocessing.ReprocessingTaskStarterTests` — 12 tests. Category: Service.
Collection: Database.

| Test                                                                    | Kind | Verifies                                                                      |
|-------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `WithNothingWaitingNothingIsClaimedAsync`                               | Fact | With nothing waiting nothing is claimed.                                      |
| `EveryDocumentInTheWindowIsProcessedExactlyOnceAcrossPagesAndTiesAsync` | Fact | Every document in the window is processed exactly once across pages and ties. |
| `TheReprocessedArchiveCarriesTheNewModelOutputAsync`                    | Fact | The reprocessed archive carries the new model output.                         |
| `OnlyDocumentsWithinTheIntervalOfTheLatestAreProcessedAsync`            | Fact | Only documents within the interval of the latest are processed.               |
| `DocumentsArchivedAfterTheRunStartedAreNotProcessedAsync`               | Fact | Documents archived after the run started are not processed.                   |
| `ASampleOfNothingProcessesButMatchesNothingAsync`                       | Fact | A sample of nothing processes but matches nothing.                            |
| `AHalfSampleIsAPercentageNotAFractionAsync`                             | Fact | A half sample is a percentage not a fraction.                                 |
| `ARuleThatDoesNotParseFailsTheInstanceAndDoesNotBlockTheNextAsync`      | Fact | A rule that does not parse fails the instance and does not block the next.    |
| `AModelWithNoArchiveCompletesAtOnceAsync`                               | Fact | A model with no archive completes at once.                                    |
| `DeletingTheInstanceDuringTheRunStopsItAndLeavesItDeletedAsync`         | Fact | Deleting the instance during the run stops it and leaves it deleted.          |
| `ReprocessingLeavesTheCacheExactlyAsLiveTrafficLeftItAsync`             | Fact | Reprocessing leaves the cache exactly as live traffic left it.                |
| `TwoStartersNeverClaimTheSameInstanceAsync`                             | Fact | Two starters never claim the same instance.                                   |

## Integrity

### EngineInstanceLivenessTests

`Jube.Test.Engine.Integrity.EngineInstanceLivenessTests` — 3 tests. Category: Unit.

| Test                                             | Kind   | Verifies                                                 |
|--------------------------------------------------|--------|----------------------------------------------------------|
| `TheToleranceIsTwoMinutesAndTheRetentionOneHour` | Fact   | The tolerance is two minutes and the retention one hour. |
| `TheCutoffIsTheToleranceBeforeNow`               | Fact   | The cutoff is the tolerance before now.                  |
| `AHeartbeatIsLiveOnlyWithinTheTolerance`         | Theory | A heartbeat is live only within the tolerance.           |

### ModelIntegrityRulesTests

`Jube.Test.Engine.Integrity.ModelIntegrityRulesTests` — 10 tests. Category: Unit.

| Test                                                               | Kind | Verifies                                                                      |
|--------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `DependencyFindingsSeparateBrokenActiveEntitiesFromInactiveOnes`   | Fact | Dependency findings separate broken active entities from inactive ones.       |
| `OnlyActiveEntitiesTheEngineFailedToCompileAreErrors`              | Fact | Only active entities the engine failed to compile are errors.                 |
| `ConfigurationFindingsCoverTheModelTtlCountersAndSearchWindows`    | Fact | Configuration findings cover the model ttl counters and search windows.       |
| `WithoutAnEngineOnlyTheNodesAreChecked`                            | Fact | Without an engine only the nodes are checked.                                 |
| `TheEnginesLoadedEntitiesAreComparedWithTheActiveOnes`             | Fact | The engines loaded entities are compared with the active ones.                |
| `AnInactiveModelIsNotExpectedToBeLoaded`                           | Fact | An inactive model is not expected to be loaded.                               |
| `TheSnapshotRecordsWhatTheEngineModelHasLoadedAndWhetherItStarted` | Fact | The snapshot records what the engine model has loaded and whether it started. |
| `TheGraphDrawsUsesToTheUsedWithMissingNamesAsTheirOwnNodes`        | Fact | The graph draws uses to the used with missing names as their own nodes.       |
| `AFocusKeepsOnlyTheNeighbourhoodWithinTheRadius`                   | Fact | A focus keeps only the neighbourhood within the radius.                       |
| `ABigGraphKeepsTheMostConnectedNodesAndSaysSo`                     | Fact | A big graph keeps the most connected nodes and says so.                       |

## Metrics

### EtcdApiResponseTests

`Jube.Test.Engine.Metrics.EtcdApiResponseTests` — 7 tests. Category: Unit.

| Test                                                       | Kind   | Verifies                                                            |
|------------------------------------------------------------|--------|---------------------------------------------------------------------|
| `AMissingCredentialIsRejectedRatherThanReadAsAPayload`     | Fact   | A missing credential is rejected rather than read as a payload.     |
| `AnInvalidTokenIsRejectedRatherThanReadAsAPayload`         | Fact   | An invalid token is rejected rather than read as a payload.         |
| `AnAuthenticatedMaintenanceStatusIsRead`                   | Fact   | An authenticated maintenance status is read.                        |
| `AnErrorEnvelopeReturnedWithASuccessStatusIsStillRejected` | Fact   | An error envelope returned with a success status is still rejected. |
| `AnEmptyBodyIsRejected`                                    | Theory | An empty body is rejected.                                          |
| `ABodyThatIsNotAJsonObjectIsRejected`                      | Theory | A body that is not a json object is rejected.                       |
| `AServerErrorWithNoEtcdEnvelopeStillReportsItsStatus`      | Fact   | A server error with no etcd envelope still reports its status.      |

## Override

### OverrideModelSyncTests

`Jube.Test.Engine.Override.OverrideModelSyncTests` — 9 tests. Category: Service. Collection: Database.

| Test                                                             | Kind | Verifies                                                               |
|------------------------------------------------------------------|------|------------------------------------------------------------------------|
| `SyncKeepsEverySuppressedValueHeldAgainstOneKeyAsync`            | Fact | Sync keeps every suppressed value held against one key.                |
| `SyncKeepsSuppressedValuesAcrossSeveralKeysAsync`                | Fact | Sync keeps suppressed values across several keys.                      |
| `SyncKeepsEveryActivationRuleHeldAgainstOneValueAsync`           | Fact | Sync keeps every activation rule held against one value.               |
| `SyncKeepsEveryValueHeldAgainstOneActivationRuleKeyAsync`        | Fact | Sync keeps every value held against one activation rule key.           |
| `SyncMergesModelAndActivationRuleOverridesOntoOneValueAsync`     | Fact | Sync merges model and activation rule overrides onto one value.        |
| `SyncCarriesTheForceOverrideKindAsync`                           | Fact | Sync carries the force override kind.                                  |
| `SyncLetsForceWinWhereBothKindsAreHeldForOneActivationRuleAsync` | Fact | Sync lets force win where both kinds are held for one activation rule. |
| `SyncSkipsExpiredAndDeletedOverridesAsync`                       | Fact | Sync skips expired and deleted overrides.                              |
| `SyncReplacesThePreviousOverridesRatherThanAccumulatingAsync`    | Fact | Sync replaces the previous overrides rather than accumulating.         |

## Sanctions

### LevenshteinDistanceTests

`Jube.Test.Engine.Sanctions.LevenshteinDistanceTests` — 38 tests. Category: Unit.

| Test                                                                                        | Kind   | Verifies                                                                                                   |
|---------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------|
| `CheckMultipartStringWithNullOrBlankInputReturnsNoMatches`                                  | Theory | Check multipart string with null or blank input returns no matches.                                        |
| `CheckMultipartStringWithNoSanctionEntriesReturnsNoMatches`                                 | Fact   | Check multipart string with no sanction entries returns no matches.                                        |
| `CheckMultipartStringWhereEveryInputTokenIsAStopTokenReturnsNoMatches`                      | Fact   | Check multipart string where every input token is a stop token returns no matches.                         |
| `CheckMultipartStringWithEntryThatHasNoUsableTokensIsSkipped`                               | Fact   | Check multipart string with entry that has no usable tokens is skipped.                                    |
| `CheckMultipartStringWithExactMatchReturnsZeroDistance`                                     | Fact   | Check multipart string with exact match returns zero distance.                                             |
| `CheckMultipartStringWithExactMatchIsCaseInsensitive`                                       | Fact   | Check multipart string with exact match is case insensitive.                                               |
| `CheckMultipartStringNormalizesDiacriticsBeforeComparing`                                   | Fact   | Check multipart string normalizes diacritics before comparing.                                             |
| `CheckMultipartStringStripsPunctuationBeforeComparing`                                      | Fact   | Check multipart string strips punctuation before comparing.                                                |
| `CheckMultipartStringWithOneCharacterEditWithinThresholdMatches`                            | Fact   | Check multipart string with one character edit within threshold matches.                                   |
| `CheckMultipartStringWithEditDistanceBeyondMaxDistanceIsRejected`                           | Fact   | Check multipart string with edit distance beyond max distance is rejected.                                 |
| `CheckMultipartStringWithEditDistanceExactlyAtMaxDistanceMatches`                           | Fact   | Check multipart string with edit distance exactly at max distance matches.                                 |
| `CheckMultipartStringWithCompletelyUnrelatedNameIsRejected`                                 | Fact   | Check multipart string with completely unrelated name is rejected.                                         |
| `CheckMultipartStringIgnoresConfiguredHonorificsOnBothSides`                                | Fact   | Check multipart string ignores configured honorifics on both sides.                                        |
| `CheckMultipartStringSingleCharacterTokensAreAlwaysIgnoredRegardlessOfStopTokens`           | Fact   | Check multipart string single character tokens are always ignored regardless of stop tokens.               |
| `CheckMultipartStringWithCoverageRatioBelowHalfIsRejected`                                  | Fact   | Check multipart string with coverage ratio below half is rejected.                                         |
| `CheckMultipartStringWithCoverageRatioExactlyAtHalfIsAccepted`                              | Fact   | Check multipart string with coverage ratio exactly at half is accepted.                                    |
| `CheckMultipartStringWithNoMaxCoverageRatioConfiguredStillRejectsAMuchLongerInputByDefault` | Fact   | Check multipart string with no max coverage ratio configured still rejects a much longer input by default. |
| `CheckMultipartStringWithMaxCoverageRatioExplicitlyRaisedAllowsTheLongerInputThrough`       | Fact   | Check multipart string with max coverage ratio explicitly raised allows the longer input through.          |
| `CheckMultipartStringWithMaxCoverageRatioRejectsMuchLongerInputThanEntry`                   | Fact   | Check multipart string with max coverage ratio rejects much longer input than entry.                       |
| `CheckMultipartStringWithMaxCoverageRatioAllowsInputWithinTheConfiguredRatio`               | Fact   | Check multipart string with max coverage ratio allows input within the configured ratio.                   |
| `CheckMultipartStringWithMaxDistanceRatioRejectsProportionallyLargeEditOnShortToken`        | Fact   | Check multipart string with max distance ratio rejects proportionally large edit on short token.           |
| `CheckMultipartStringWithMaxDistanceRatioStillAllowsProportionallySmallEditOnLongToken`     | Fact   | Check multipart string with max distance ratio still allows proportionally small edit on long token.       |
| `CheckMultipartStringWithMaxDistanceRatioNeverAllowsBelowOneCharacterOfSlack`               | Fact   | Check multipart string with max distance ratio never allows below one character of slack.                  |
| `CheckMultipartStringDoesNotLetTwoInputTokensClaimTheSameEntryToken`                        | Fact   | Check multipart string does not let two input tokens claim the same entry token.                           |
| `CheckMultipartStringReturnsWorstNotBestPairDistanceAcrossMultipleTokens`                   | Fact   | Check multipart string returns worst not best pair distance across multiple tokens.                        |
| `CheckMultipartStringCombinesTokensFromMultipleElementValuesForOneEntry`                    | Fact   | Check multipart string combines tokens from multiple element values for one entry.                         |
| `CheckMultipartStringReturnsOnlyEntriesThatMatchFromALargerSet`                             | Fact   | Check multipart string returns only entries that match from a larger set.                                  |
| `CheckMultipartStringAcrossManyEntriesIsThreadSafeAndDeterministicInResultSet`              | Fact   | Check multipart string across many entries is thread safe and deterministic in result set.                 |
| `CheckMultipartStringEntryWithFewerTokensThanInputIgnoresInputsExtraDistinguishingToken`    | Fact   | Check multipart string entry with fewer tokens than input ignores inputs extra distinguishing token.       |
| `DefaultMaxDistanceRatioConstantMatchesTheDocumentedServerWideDefault`                      | Fact   | Default max distance ratio constant matches the documented server wide default.                            |
| `DefaultMaxCoverageRatioConstantMatchesTheDocumentedServerWideDefault`                      | Fact   | Default max coverage ratio constant matches the documented server wide default.                            |
| `DefaultConstructorAppliesTheDocumentedMaxDistanceRatioAutomatically`                       | Fact   | Default constructor applies the documented max distance ratio automatically.                               |
| `ParseNullableDistanceRatioWithUnparsableInputReturnsNull`                                  | Theory | Parse nullable distance ratio with unparsable input returns null.                                          |
| `ParseNullableDistanceRatioParsesAValidValue`                                               | Fact   | Parse nullable distance ratio parses a valid value.                                                        |
| `ParseNullableDistanceRatioClampsToUnitInterval`                                            | Theory | Parse nullable distance ratio clamps to unit interval.                                                     |
| `ParseNullableCoverageRatioWithUnparsableInputReturnsNull`                                  | Theory | Parse nullable coverage ratio with unparsable input returns null.                                          |
| `ParseNullableCoverageRatioParsesAValidValue`                                               | Fact   | Parse nullable coverage ratio parses a valid value.                                                        |
| `ParseNullableCoverageRatioClampsNegativeToZeroButHasNoUpperBound`                          | Fact   | Parse nullable coverage ratio clamps negative to zero but has no upper bound.                              |

### SanctionAggregationCalculatorTests

`Jube.Test.Engine.Sanctions.SanctionAggregationCalculatorTests` — 37 tests. Category: Unit.

| Test                                                                                     | Kind   | Verifies                                                                                            |
|------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------|
| `CalculateSumWithEmptyCollectionReturnsNull`                                             | Fact   | Calculate sum with empty collection returns null.                                                   |
| `CalculateSumAddsAllDistances`                                                           | Fact   | Calculate sum adds all distances.                                                                   |
| `CalculateSumWithSingleZeroDistanceReturnsZeroNotNull`                                   | Fact   | Calculate sum with single zero distance returns zero not null.                                      |
| `CalculateCountWithEmptyCollectionReturnsNull`                                           | Fact   | Calculate count with empty collection returns null.                                                 |
| `CalculateCountReturnsNumberOfMatches`                                                   | Fact   | Calculate count returns number of matches.                                                          |
| `CalculateMaxWithEmptyCollectionReturnsNull`                                             | Fact   | Calculate max with empty collection returns null.                                                   |
| `CalculateMaxReturnsWorstMatch`                                                          | Fact   | Calculate max returns worst match.                                                                  |
| `CalculateMinWithEmptyCollectionReturnsNull`                                             | Fact   | Calculate min with empty collection returns null.                                                   |
| `CalculateMinReturnsBestMatch`                                                           | Fact   | Calculate min returns best match.                                                                   |
| `CalculateFirstWithEmptyCollectionReturnsNull`                                           | Fact   | Calculate first with empty collection returns null.                                                 |
| `CalculateFirstReturnsElementAtIndexZeroRegardlessOfValue`                               | Fact   | Calculate first returns element at index zero regardless of value.                                  |
| `CalculateLastWithEmptyCollectionReturnsNull`                                            | Fact   | Calculate last with empty collection returns null.                                                  |
| `CalculateLastReturnsFinalElementRegardlessOfValue`                                      | Fact   | Calculate last returns final element regardless of value.                                           |
| `CalculateFirstAndLastAreSameElementForSingleMatch`                                      | Fact   | Calculate first and last are same element for single match.                                         |
| `CalculateAverageWithEmptyCollectionReturnsNull`                                         | Fact   | Calculate average with empty collection returns null.                                               |
| `CalculateAverageDividesSumByCount`                                                      | Fact   | Calculate average divides sum by count.                                                             |
| `CalculateAverageWithAllZeroDistancesReturnsZeroNotNull`                                 | Fact   | Calculate average with all zero distances returns zero not null.                                    |
| `CalculateAverageWithNonIntegerResultIsNotTruncated`                                     | Fact   | Calculate average with non integer result is not truncated.                                         |
| `CalculateDispatchesToTheCorrectAggregationForEachKnownTypeId`                           | Theory | Calculate dispatches to the correct aggregation for each known type id.                             |
| `CalculateWithUnknownAggregationTypeIdFallsBackToAverage`                                | Fact   | Calculate with unknown aggregation type id falls back to average.                                   |
| `CalculateWithZeroAggregationTypeIdFallsBackToAverage`                                   | Fact   | Calculate with zero aggregation type id falls back to average.                                      |
| `CalculateWithConfidenceTypeIdDispatchesToConfidence`                                    | Fact   | Calculate with confidence type id dispatches to confidence.                                         |
| `CalculateConfidenceWithEmptyCollectionReturnsNull`                                      | Fact   | Calculate confidence with empty collection returns null.                                            |
| `CalculateConfidenceWithSingleMatchIsPureClosenessOfThatDistance`                        | Fact   | Calculate confidence with single match is pure closeness of that distance.                          |
| `CalculateConfidenceWithSingleExactMatchIsOne`                                           | Fact   | Calculate confidence with single exact match is one.                                                |
| `CalculateConfidenceWithTwoIdenticalDistancesShortCircuitsOnZeroStdDevToCloseness`       | Fact   | Calculate confidence with two identical distances short circuits on zero std dev to closeness.      |
| `CalculateConfidenceWithExactlyTwoDistinctDistancesIsFiniteNotNaN`                       | Fact   | Calculate confidence with exactly two distinct distances is finite not na n.                        |
| `CalculateConfidenceWithTwoDistinctDistancesIsSymmetricUnderReordering`                  | Fact   | Calculate confidence with two distinct distances is symmetric under reordering.                     |
| `CalculateConfidenceWithTwoDistinctDistancesWidelySeparatedIsHigherThanCloselySeparated` | Fact   | Calculate confidence with two distinct distances widely separated is higher than closely separated. |
| `CalculateConfidenceWithThreeMatchesUsesSkewAdjustedReliability`                         | Fact   | Calculate confidence with three matches uses skew adjusted reliability.                             |
| `CalculateConfidenceWithFiveEvenlySpacedMatches`                                         | Fact   | Calculate confidence with five evenly spaced matches.                                               |
| `CalculateConfidenceWithTwoNearDuplicatesAndOneOutlierIsLowerThanIsolatedMatch`          | Fact   | Calculate confidence with two near duplicates and one outlier is lower than isolated match.         |
| `CalculateConfidenceWithManyFarMatchesAndOneExactMatchIsHigh`                            | Fact   | Calculate confidence with many far matches and one exact match is high.                             |
| `CalculateConfidenceIsRoundedToFourDecimalPlaces`                                        | Fact   | Calculate confidence is rounded to four decimal places.                                             |
| `CalculateConfidenceNeverExceedsOne`                                                     | Fact   | Calculate confidence never exceeds one.                                                             |
| `CalculateConfidenceIsNeverNegative`                                                     | Fact   | Calculate confidence is never negative.                                                             |
| `CalculateConfidenceIsNeverNaNAcrossASweepOfClusterSizes`                                | Fact   | Calculate confidence is never na n across a sweep of cluster sizes.                                 |

### SanctionEntryFileImporterReconciliationTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterReconciliationTests` — 7 tests. Category: Service. Collection:
Database.

| Test                                                                                 | Kind | Verifies                                                                                       |
|--------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `ReloadingTheIdenticalFileASecondTimeLeavesEveryRowUnchangedAsync`                   | Fact | Reloading the identical file a second time leaves every row unchanged.                         |
| `AReloadWhoseFileNoLongerContainsARowSoftDeletesOnlyThatRowAsync`                    | Fact | A reload whose file no longer contains a row soft deletes only that row.                       |
| `ARowThatReappearsInALaterFileAfterBeingRemovedIsRevivedNotDuplicatedAsync`          | Fact | A row that reappears in a later file after being removed is revived not duplicated.            |
| `ReconcileRemovedAsyncWithEmptyHashesDoesNotWipeOutTheWholeSourceAsync`              | Fact | Reconcile removed async with empty hashes does not wipe out the whole source.                  |
| `ReconcileRemovedAsyncWithEmptyHashesLogsAtWarnNotInfoAsync`                         | Fact | Reconcile removed async with empty hashes logs at warn not info.                               |
| `ReconcileRemovedAsyncNeverTouchesRowsBelongingToAnotherSourceAsync`                 | Fact | Reconcile removed async never touches rows belonging to another source.                        |
| `WhenEveryRowInASourceIsGenuinelyGoneReconcileRemovedAsyncSoftDeletesAllOfThemAsync` | Fact | When every row in a source is genuinely gone reconcile removed async soft deletes all of them. |

### SanctionEntryFileImporterSdnIntegrationTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterSdnIntegrationTests` — 12 tests. Category: Service. Collection:
SanctionsSdnLoad.

| Test                                                                       | Kind   | Verifies                                                                              |
|----------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `TheLoadParsedEveryRowAndRejectedExactlyTheOneKnownMalformedTrailingLine`  | Fact   | The load parsed every row and rejected exactly the one known malformed trailing line. |
| `OnItsFirstEverLoadEveryPersistedRowWasAFreshInsertNotARevivalOrNoOp`      | Fact   | On its first ever load every persisted row was a fresh insert not a revival or no op. |
| `EveryHashParsedFromTheFileIsUnique`                                       | Fact   | Every hash parsed from the file is unique.                                            |
| `ThePersistedRowCountForTheSourceExactlyMatchesTheInsertedCountAsync`      | Fact   | The persisted row count for the source exactly matches the inserted count.            |
| `EveryPersistedRowHasANonEmptyHashElementValueAndReferenceAsync`           | Fact   | Every persisted row has a non empty hash element value and reference.                 |
| `NoTwoPersistedRowsShareTheSameHashAsync`                                  | Fact   | No two persisted rows share the same hash.                                            |
| `KnownRealSdnEntriesArePersistedWithTheirExactNameAsync`                   | Theory | Known real sdn entries are persisted with their exact name.                           |
| `AnEntryWithAnEmbeddedCommaInItsQuotedNameIsNotTruncatedAtTheCommaAsync`   | Fact   | An entry with an embedded comma in its quoted name is not truncated at the comma.     |
| `ThePayloadColumnRetainsTheFullOriginalRawRowAsync`                        | Fact   | The payload column retains the full original raw row.                                 |
| `TheImportAuditRowRecordsCountsThatAgreeWithWhatWasActuallyPersistedAsync` | Fact   | The import audit row records counts that agree with what was actually persisted.      |
| `TheRejectionAuditRowMatchesTheOneKnownMalformedTrailingLineAsync`         | Fact   | The rejection audit row matches the one known malformed trailing line.                |
| `GetAsyncAcrossAllSourcesStillIncludesOurIsolatedSourcesRowsAsync`         | Fact   | Get async across all sources still includes our isolated sources rows.                |

### SanctionEntryFileImporterTests

`Jube.Test.Engine.Sanctions.SanctionEntryFileImporterTests` — 22 tests. Category: Unit.

| Test                                                                                 | Kind   | Verifies                                                                                      |
|--------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `ImportAsyncParsesASimpleRowIntoARecordAsync`                                        | Fact   | Import async parses a simple row into a record.                                               |
| `ImportAsyncSkipsTheConfiguredNumberOfHeaderRowsAsync`                               | Fact   | Import async skips the configured number of header rows.                                      |
| `ImportAsyncWithSkipGreaterThanRowCountProducesNoRecordsAndNoRejectionsAsync`        | Fact   | Import async with skip greater than row count produces no records and no rejections.          |
| `ImportAsyncHandlesFieldsWithEmbeddedCommasViaQuotingAsync`                          | Fact   | Import async handles fields with embedded commas via quoting.                                 |
| `ImportAsyncCombinesMultipleColumnsWhenMultiPartStringIndexListsSeveralIndicesAsync` | Fact   | Import async combines multiple columns when multi part string index lists several indices.    |
| `ImportAsyncFallsBackToColumnZeroWhenAMultiPartIndexTokenIsNotANumberAsync`          | Fact   | Import async falls back to column zero when a multi part index token is not a number.         |
| `ImportAsyncCapturesTheFullRawRowAsThePayloadAsync`                                  | Fact   | Import async captures the full raw row as the payload.                                        |
| `ImportAsyncRejectsRowsWithOnlyOneFieldAsInsufficientAsync`                          | Fact   | Import async rejects rows with only one field as insufficient.                                |
| `ImportAsyncRejectsEveryDataRowWhenReferenceIndexIsNotConfiguredAsync`               | Fact   | Import async rejects every data row when reference index is not configured.                   |
| `ImportAsyncRejectsARowThatThrowsDuringBuildAsParseErrorAndContinuesAsync`           | Fact   | Import async rejects a row that throws during build as parse error and continues.             |
| `ImportAsyncEmptyFileProducesNoRecordsNoRejectionsAndEmptyHashesAsync`               | Fact   | Import async empty file produces no records no rejections and empty hashes.                   |
| `ImportAsyncWithNullOnRejectedCallbackSwallowsRejectionsWithoutThrowingAsync`        | Fact   | Import async with null on rejected callback swallows rejections without throwing.             |
| `ImportAsyncProducesTheSameHashForTheSameSourceElementAndReferenceAsync`             | Fact   | Import async produces the same hash for the same source element and reference.                |
| `ImportAsyncProducesADifferentHashForADifferentSourceAsync`                          | Fact   | Import async produces a different hash for a different source.                                |
| `ImportAsyncProducesADifferentHashForADifferentReferenceAsync`                       | Fact   | Import async produces a different hash for a different reference.                             |
| `ImportAsyncHashMatchesTheDocumentedMd5OfSourceIdPlusElementPlusReferenceAsync`      | Fact   | Import async hash matches the documented md5 of source id plus element plus reference.        |
| `ImportAsyncResultHashesContainsOnlySuccessfullyParsedRowsAsync`                     | Fact   | Import async result hashes contains only successfully parsed rows.                            |
| `NormalizeElementValueLowercasesStripsDiacriticsAndRemovesPunctuationWithoutSpacing` | Theory | Normalize element value lowercases strips diacritics and removes punctuation without spacing. |
| `NormalizeElementValueDropsHyphenWithNoSpaceUnlikeMatchTimeTokenization`             | Fact   | Normalize element value drops hyphen with no space unlike match time tokenization.            |
| `ImportAsyncParsesTheEntireRealSdnFileAndReportsConsistentCountsAsync`               | Fact   | Import async parses the entire real sdn file and reports consistent counts.                   |
| `ImportAsyncParsesSpecificKnownSdnRowsCorrectlyAsync`                                | Theory | Import async parses specific known sdn rows correctly.                                        |
| `ImportAsyncOnTheRealSdnFileProducesNormalizedTokensThatStillMatchAtQueryTimeAsync`  | Fact   | Import async on the real sdn file produces normalized tokens that still match at query time.  |

### SanctionsChangePollConfigurationTests

`Jube.Test.Engine.Sanctions.SanctionsChangePollConfigurationTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                         | Kind   | Verifies                                                                                |
|------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `TheIntervalFallsBackToTheDefault_WhenItIsMissingGarbageZeroOrNegative`      | Theory | The interval falls back to the default when it is missing garbage zero or negative.     |
| `WhenTheFlagIsFalse_NothingIsPolled_AndANewImportIsNotNoticedAsync`          | Fact   | When the flag is false nothing is polled and a new import is not noticed.               |
| `WhenTheFlagIsNotSet_TheDefaultPollsAndPicksUpANewImportAsync`               | Fact   | When the flag is not set the default polls and picks up a new import.                   |
| `AZeroNegativeOrGarbageInterval_FallsBackToTheDefault_AndDoesNotSpinAsync`   | Theory | A zero negative or garbage interval falls back to the default and does not spin.        |
| `AnIntervalAtOrAboveTheWait_IsOneSliceThatStillChecksAtTheEndOfTheWaitAsync` | Fact   | An interval at or above the wait is one slice that still checks at the end of the wait. |

### SanctionsChangePollTests

`Jube.Test.Engine.Sanctions.SanctionsChangePollTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                           |
|-------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ANewImport_BecomesVisibleInTheCache_WithoutWaitingForTheLoaderIntervalAsync` | Fact | A new import becomes visible in the cache without waiting for the loader interval. |
| `EntriesRemovedByALaterImport_LeaveTheCacheAsync`                             | Fact | Entries removed by a later import leave the cache.                                 |
| `WhenNothingHasBeenImported_TheCacheIsNotReloadedAsync`                       | Fact | When nothing has been imported the cache is not reloaded.                          |
| `AFailingFingerprintQuery_DoesNotEndTheLoop_AndTheNextPollRecoversAsync`      | Fact | A failing fingerprint query does not end the loop and the next poll recovers.      |
| `ACancellation_EndsTheLoopPromptlyAsync`                                      | Fact | A cancellation ends the loop promptly.                                             |

## Ttl counter

### TtlCounterDeprecationTimingTests

`Jube.Test.Engine.TtlCounter.TtlCounterDeprecationTimingTests` — 13 tests. Category: Service. Collection: Database.

| Test                                                                                        | Kind   | Verifies                                                                                             |
|---------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------|
| `OnlineAggregationCountExcludesAnEntryOnceItAgesPastTheIntervalAsync`                       | Fact   | Online aggregation count excludes an entry once it ages past the interval.                           |
| `OnlineAggregationSumExcludesEntriesOnceTheyAgePastTheIntervalAsync`                        | Fact   | Online aggregation sum excludes entries once they age past the interval.                             |
| `OnlineAggregationIncludesOnlyTheEntryStillWithinTheWindowWhenAnOlderOneHasAgedOutAsync`    | Fact   | Online aggregation includes only the entry still within the window when an older one has aged out.   |
| `OnlineAggregationIncludesAnEntryExactlyOnTheWindowsLowerBoundaryAsync`                     | Fact   | Online aggregation includes an entry exactly on the windows lower boundary.                          |
| `OnlineAggregationDoesNotCrossContaminateBetweenDifferentDataValuesAsync`                   | Fact   | Online aggregation does not cross contaminate between different data values.                         |
| `BatchAdministrationDecrementsAndRemovesAnExpiredCountCounterWithinAShortRealDelayAsync`    | Fact   | Batch administration decrements and removes an expired count counter within a short real delay.      |
| `BatchAdministrationDecrementsAndRemovesAnExpiredSumCounterWithinAShortRealDelayAsync`      | Fact   | Batch administration decrements and removes an expired sum counter within a short real delay.        |
| `BatchAdministrationDoesNotPruneACounterWellBeforeItsLongerIntervalHasElapsedAsync`         | Fact   | Batch administration does not prune a counter well before its longer interval has elapsed.           |
| `BatchAdministrationNeverPrunesALiveForeverCounterAsync`                                    | Fact   | Batch administration never prunes a live forever counter.                                            |
| `BatchAdministrationPrunesAZeroIntervalCounterOnAnEarlyCycleAsync`                          | Fact   | Batch administration prunes a zero interval counter on an early cycle.                               |
| `BatchAdministrationHandlesMultipleCountersOnTheSameModelIndependentlyAsync`                | Fact   | Batch administration handles multiple counters on the same model independently.                      |
| `BatchAdministrationNeverTouchesAModelThatIsNotStartedAsync`                                | Fact   | Batch administration never touches a model that is not started.                                      |
| `BatchAdministrationWindsBackEveryIntervalUnitAndValueOnceTheReferenceDateJumpsPastItAsync` | Theory | Batch administration winds back every interval unit and value once the reference date jumps past it. |

### TtlCounterModelSyncTests

`Jube.Test.Engine.TtlCounter.TtlCounterModelSyncTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `SyncPopulatesTheInMemoryCounterFromARealRowIncludingNullFieldDefaultsAsync`      | Fact | Sync populates the in memory counter from a real row including null field defaults.       |
| `SyncSkipsAnInactiveCounterDefinitionAsync`                                       | Fact | Sync skips an inactive counter definition.                                                |
| `ASyncedCounterIsPickedUpByTheRealAdministrationLoopAndDeprecatesOnScheduleAsync` | Fact | A synced counter is picked up by the real administration loop and deprecates on schedule. |
