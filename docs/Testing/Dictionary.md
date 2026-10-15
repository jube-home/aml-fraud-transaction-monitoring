---
layout: default
title: Dictionary and Fuzzy Matching
nav_order: 8
parent: Testing
---

# Dictionary and Fuzzy Matching

Tests of the dictionary data structures, fuzzy matching and serialisation that support rule evaluation.

## General

### DictionaryNoBoxingTests

`Jube.Test.Dictionary.DictionaryNoBoxingTests` — 36 tests. Category: Unit.

| Test                                                                       | Kind   | Verifies                                                                               |
|----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `ANewDictionaryIsEmpty`                                                    | Fact   | A new dictionary is empty.                                                             |
| `TheIndexerReturnsADefaultInternalValueForAMissingKeyRatherThanThrowing`   | Fact   | The indexer returns a default internal value for a missing key rather than throwing.   |
| `TheIndexerSetterAddsANewKeyAndTheGetterReadsItBack`                       | Fact   | The indexer setter adds a new key and the getter reads it back.                        |
| `TheIndexerSetterUpdatesAnExistingKeyInPlaceWithoutChangingCount`          | Fact   | The indexer setter updates an existing key in place without changing count.            |
| `TheIndexerSetterAdjustsTheEstimatedSizeWhenReplacingAValue`               | Fact   | The indexer setter adjusts the estimated size when replacing a value.                  |
| `TryAddOnlySucceedsTheFirstTimeForAGivenKey`                               | Theory | Try add only succeeds the first time for a given key.                                  |
| `TryAddSupportsEveryPrimitiveOverload`                                     | Fact   | Try add supports every primitive overload.                                             |
| `TryAddInternsNonLiteralStringKeysSoRepeatedLookupsShareOneStringInstance` | Fact   | Try add interns non literal string keys so repeated lookups share one string instance. |
| `AddDoesNotInternTheKeyUnlikeTryAdd`                                       | Fact   | Add does not intern the key unlike try add.                                            |
| `ContainsKeyReflectsPresenceOfTheKeyOnly`                                  | Fact   | Contains key reflects presence of the key only.                                        |
| `RemoveDeletesAnExistingKeyAndReturnsTrue`                                 | Fact   | Remove deletes an existing key and returns true.                                       |
| `RemoveOfAMissingKeyReturnsFalseAndLeavesTheDictionaryUnchanged`           | Fact   | Remove of a missing key returns false and leaves the dictionary unchanged.             |
| `RemoveFromTheMiddlePreservesTheRelativeOrderOfTheRemainingEntries`        | Fact   | Remove from the middle preserves the relative order of the remaining entries.          |
| `RemoveReducesTheEstimatedSizeByTheRemovedEntriesContribution`             | Fact   | Remove reduces the estimated size by the removed entries contribution.                 |
| `TryGetValueReturnsTrueAndTheValueWhenPresent`                             | Fact   | Try get value returns true and the value when present.                                 |
| `TryGetValueReturnsFalseAndADefaultValueWhenAbsent`                        | Fact   | Try get value returns false and a default value when absent.                           |
| `ClearRemovesAllEntriesAndResetsTheEstimatedSize`                          | Fact   | Clear removes all entries and resets the estimated size.                               |
| `ClearOnAnAlreadyEmptyDictionaryIsANoOp`                                   | Fact   | Clear on an already empty dictionary is a no op.                                       |
| `AddSilentlyIgnoresADuplicateKeyRatherThanThrowingOrOverwriting`           | Fact   | Add silently ignores a duplicate key rather than throwing or overwriting.              |
| `AddSupportsEveryPrimitiveOverloadAndTheRawInternalValueOverload`          | Fact   | Add supports every primitive overload and the raw internal value overload.             |
| `AddUncheckedAppendsEvenWhenTheKeyAlreadyExistsProducingADuplicateEntry`   | Fact   | Add unchecked appends even when the key already exists producing a duplicate entry.    |
| `AddUncheckedLookupsReturnTheFirstMatchingEntryWhenDuplicatesExist`        | Fact   | Add unchecked lookups return the first matching entry when duplicates exist.           |
| `AddUncheckedSupportsEveryPrimitiveOverload`                               | Fact   | Add unchecked supports every primitive overload.                                       |
| `TheDictionaryGrowsAutomaticallyBeyondItsDefaultInitialCapacity`           | Fact   | The dictionary grows automatically beyond its default initial capacity.                |
| `AnExplicitInitialCapacityIsHonouredAndStillGrowsWhenExceeded`             | Fact   | An explicit initial capacity is honoured and still grows when exceeded.                |
| `ForeachEnumeratesEveryEntryInInsertionOrder`                              | Fact   | Foreach enumerates every entry in insertion order.                                     |
| `TheGenericIEnumerableInterfaceIsUsableViaLinq`                            | Fact   | The generic i enumerable interface is usable via linq.                                 |
| `TheNonGenericIEnumerableInterfaceIsAlsoImplemented`                       | Fact   | The non generic i enumerable interface is also implemented.                            |
| `TheStructEnumeratorSupportsManualMoveNextCurrentAndReset`                 | Fact   | The struct enumerator supports manual move next current and reset.                     |
| `EnumeratingAnEmptyDictionaryYieldsNoElements`                             | Fact   | Enumerating an empty dictionary yields no elements.                                    |
| `DisposeClearsTheDictionarySoItBehavesAsEmptyAfterwards`                   | Fact   | Dispose clears the dictionary so it behaves as empty afterwards.                       |
| `EstimatedSizeBytesGrowsWithLongerStringValues`                            | Fact   | Estimated size bytes grows with longer string values.                                  |
| `EstimatedSizeBytesIsPositiveForEveryPrimitiveValueType`                   | Fact   | Estimated size bytes is positive for every primitive value type.                       |
| `AnIntKeyedDictionaryWorksIdenticallyToAStringKeyedOne`                    | Fact   | An int keyed dictionary works identically to a string keyed one.                       |
| `TheDictionarySatisfiesItsPublicInterfaceContract`                         | Fact   | The dictionary satisfies its public interface contract.                                |
| `TheDictionaryImplementsISizedForCacheAccounting`                          | Fact   | The dictionary implements i sized for cache accounting.                                |

### EvalExpressionRegistryTests

`Jube.Test.Dictionary.EvalExpressionRegistryTests` — 26 tests. Category: Unit. Collection: EvalExpressionRegistry.

| Test                                                                        | Kind   | Verifies                                                                                 |
|-----------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `WhenEnabledEvalReturnsTheCompiledResultForARegisteredBooleanExpression`    | Fact   | When enabled eval returns the compiled result for a registered boolean expression.       |
| `WhenEnabledEvalReturnsTheCompiledResultForARegisteredStringExpression`     | Fact   | When enabled eval returns the compiled result for a registered string expression.        |
| `WhenEnabledEvalReturnsTheCompiledResultForARegisteredDoubleExpression`     | Fact   | When enabled eval returns the compiled result for a registered double expression.        |
| `WhenEnabledEvalReturnsTheCompiledResultForARegisteredIntegerExpression`    | Fact   | When enabled eval returns the compiled result for a registered integer expression.       |
| `WhenEnabledEvalReturnsTheCompiledResultForARegisteredDateTimeExpression`   | Fact   | When enabled eval returns the compiled result for a registered date time expression.     |
| `EvalThrowsForAnUnregisteredNameWhenEnabled`                                | Fact   | Eval throws for an unregistered name when enabled.                                       |
| `EvalThrowsWhenDisabledEvenForARegisteredName`                              | Fact   | Eval throws when disabled even for a registered name.                                    |
| `EvalThrowsWhenEnableDynamicEvalIsUnset`                                    | Fact   | Eval throws when enable dynamic eval is unset.                                           |
| `RegisterThrowsForAnUnsupportedResultTypeId`                                | Fact   | Register throws for an unsupported result type id.                                       |
| `RegisterThrowsForANullOrEmptyName`                                         | Theory | Register throws for a null or empty name.                                                |
| `RegisterThrowsForANullOrEmptyExpression`                                   | Theory | Register throws for a null or empty expression.                                          |
| `RegisterThrowsForInvalidExpressionSyntax`                                  | Fact   | Register throws for invalid expression syntax.                                           |
| `EntriesReflectsEveryRegisteredNameAndItsResultTypeId`                      | Fact   | Entries reflects every registered name and its result type id.                           |
| `EntriesIsEmptyAfterClear`                                                  | Fact   | Entries is empty after clear.                                                            |
| `RegisteringTheSameNameTwiceOverwritesThePreviousExpression`                | Fact   | Registering the same name twice overwrites the previous expression.                      |
| `MultipleNamesCanBeRegisteredAndInvokedIndependently`                       | Fact   | Multiple names can be registered and invoked independently.                              |
| `EvalThrowsWhenTheRequestedResultTypeDoesNotMatchTheRegisteredResultTypeId` | Fact   | Eval throws when the requested result type does not match the registered result type id. |
| `EnabledIsTrueForAnyCasingOfTrue`                                           | Theory | Enabled is true for any casing of true.                                                  |
| `EnabledIsFalseForAnythingOtherThanTrue`                                    | Theory | Enabled is false for anything other than true.                                           |
| `NullInputFlowsThroughToTheCompiledExpression`                              | Fact   | Null input flows through to the compiled expression.                                     |
| `MathAndConvertAreReachableAsCommonTypes`                                   | Fact   | Math and convert are reachable as common types.                                          |
| `PlainGetTypeMetadataIsAllowedButGrantsNoFurtherReflectionAccess`           | Fact   | Plain get type metadata is allowed but grants no further reflection access.              |
| `ReflectionEscapeViaTypeAssemblyIsBlockedBySandbox`                         | Fact   | Reflection escape via type assembly is blocked by sandbox.                               |
| `ReflectionEscapeViaGetMethodsIsBlockedBySandbox`                           | Fact   | Reflection escape via get methods is blocked by sandbox.                                 |
| `FilesystemAccessIsUnreachableFromTheSandbox`                               | Fact   | Filesystem access is unreachable from the sandbox.                                       |
| `ProcessAccessIsUnreachableFromTheSandbox`                                  | Fact   | Process access is unreachable from the sandbox.                                          |

### LruCacheConcurrentSizedDictionaryTests

`Jube.Test.Dictionary.LruCacheConcurrentSizedDictionaryTests` — 27 tests. Category: Unit.

| Test                                                                                   | Kind | Verifies                                                                                              |
|----------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `TheConstructorThrowsWhenGivenNoSizeEstimator`                                         | Fact | The constructor throws when given no size estimator.                                                  |
| `ANewCacheIsEmptyAndNotFull`                                                           | Fact | A new cache is empty and not full.                                                                    |
| `AddingAnItemMakesItRetrievableAndCountedInTotalSize`                                  | Fact | Adding an item makes it retrievable and counted in total size.                                        |
| `TheIndexerSetterIsEquivalentToAdd`                                                    | Fact | The indexer setter is equivalent to add.                                                              |
| `TheIndexerGetterThrowsForAMissingKey`                                                 | Fact | The indexer getter throws for a missing key.                                                          |
| `AddViaKeyValuePairDelegatesToAddOrUpdate`                                             | Fact | Add via key value pair delegates to add or update.                                                    |
| `UpdatingAnExistingKeyReplacesItsValueWithoutChangingTheCount`                         | Fact | Updating an existing key replaces its value without changing the count.                               |
| `AddingANewKeyIncrementsTheAddCounterNotTheUpdateCounter`                              | Fact | Adding a new key increments the add counter not the update counter.                                   |
| `UpdatingAnExistingKeyIncrementsTheUpdateCounterNotTheAddCounter`                      | Fact | Updating an existing key increments the update counter not the add counter.                           |
| `RemoveDeletesTheEntryAndUpdatesCountersAndTotalSize`                                  | Fact | Remove deletes the entry and updates counters and total size.                                         |
| `RemoveOfAMissingKeyReturnsFalseAndDoesNotAffectCounters`                              | Fact | Remove of a missing key returns false and does not affect counters.                                   |
| `RemoveByKeyValuePairOnlySucceedsWhenTheValueMatchesToo`                               | Fact | Remove by key value pair only succeeds when the value matches too.                                    |
| `TryGetValueReturnsTrueAndTheValueWhenPresent`                                         | Fact | Try get value returns true and the value when present.                                                |
| `TryGetValueReturnsFalseAndDefaultWhenAbsent`                                          | Fact | Try get value returns false and default when absent.                                                  |
| `ContainsChecksBothKeyAndValueEquality`                                                | Fact | Contains checks both key and value equality.                                                          |
| `ClearRemovesEveryEntryAndResetsTotalSizeButNotTheCounters`                            | Fact | Clear removes every entry and resets total size but not the counters.                                 |
| `KeysAndValuesExposeSnapshotsOfTheCurrentContents`                                     | Fact | Keys and values expose snapshots of the current contents.                                             |
| `CopyToFillsAnArrayStartingAtTheGivenIndex`                                            | Fact | Copy to fills an array starting at the given index.                                                   |
| `TheGenericEnumeratorYieldsEveryEntry`                                                 | Fact | The generic enumerator yields every entry.                                                            |
| `TheNonGenericEnumeratorIsAlsoUsable`                                                  | Fact | The non generic enumerator is also usable.                                                            |
| `EstimatedSizeBytesIsAnAliasForTotalSize`                                              | Fact | Estimated size bytes is an alias for total size.                                                      |
| `ResetCountersZeroesOutAllCountersButLeavesTheDataIntact`                              | Fact | Reset counters zeroes out all counters but leaves the data intact.                                    |
| `IsFullBecomesTrueOnceTheTotalSizeExceedsTheConfiguredMaximumWhenEvictionCannotKeepUp` | Fact | Is full becomes true once the total size exceeds the configured maximum when eviction cannot keep up. |
| `ExceedingTheMaxSizeEvictsEntriesDownToAtOrBelowTheEvictionThreshold`                  | Fact | Exceeding the max size evicts entries down to at or below the eviction threshold.                     |
| `AnEntryThatHasBeenAccessedIsGivenOneGraceRoundBeforeItCanBeEvicted`                   | Fact | An entry that has been accessed is given one grace round before it can be evicted.                    |
| `RemovingAnEntryCanTriggerEvictionWhenTheCacheIsStillOverThreshold`                    | Fact | Removing an entry can trigger eviction when the cache is still over threshold.                        |
| `ConcurrentAddsFromManyThreadsAllSucceedWithoutLosingOrCorruptingEntriesAsync`         | Fact | Concurrent adds from many threads all succeed without losing or corrupting entries.                   |

### PooledDictionaryTests

`Jube.Test.Dictionary.PooledDictionaryTests` — 26 tests. Category: Unit.

| Test                                                                              | Kind | Verifies                                                                                       |
|-----------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `ANewDictionaryWithTheDefaultConstructorIsEmpty`                                  | Fact | A new dictionary with the default constructor is empty.                                        |
| `AddThenTryGetValueRoundTripsTheValue`                                            | Fact | Add then try get value round trips the value.                                                  |
| `AddThrowsForANullKey`                                                            | Fact | Add throws for a null key.                                                                     |
| `AddThrowsWhenTheKeyAlreadyExists`                                                | Fact | Add throws when the key already exists.                                                        |
| `TryAddReturnsTrueForANewKeyAndFalseWithoutThrowingForAnExistingOne`              | Fact | Try add returns true for a new key and false without throwing for an existing one.             |
| `TryAddThrowsForANullKeyJustLikeAdd`                                              | Fact | Try add throws for a null key just like add.                                                   |
| `ContainsKeyDistinguishesPresentFromAbsent`                                       | Fact | Contains key distinguishes present from absent.                                                |
| `TheIndexerGetterReturnsTheDefaultValueForAMissingKeyRatherThanThrowing`          | Fact | The indexer getter returns the default value for a missing key rather than throwing.           |
| `TheIndexerSetterAddsWhenTheKeyIsAbsentAndOverwritesWhenPresent`                  | Fact | The indexer setter adds when the key is absent and overwrites when present.                    |
| `GetValueOrThrowReturnsTheValueWhenPresent`                                       | Fact | Get value or throw returns the value when present.                                             |
| `GetValueOrThrowThrowsKeyNotFoundWhenAbsent`                                      | Fact | Get value or throw throws key not found when absent.                                           |
| `RemoveDeletesAnExistingEntryAndReturnsTrue`                                      | Fact | Remove deletes an existing entry and returns true.                                             |
| `RemoveOfAMissingKeyReturnsFalse`                                                 | Fact | Remove of a missing key returns false.                                                         |
| `ClearEmptiesTheDictionaryCompletely`                                             | Fact | Clear empties the dictionary completely.                                                       |
| `KeysAndValuesOnlyReflectOccupiedSlots`                                           | Fact | Keys and values only reflect occupied slots.                                                   |
| `TheGenericEnumeratorYieldsEveryStoredPair`                                       | Fact | The generic enumerator yields every stored pair.                                               |
| `TheNonGenericEnumeratorIsAlsoUsable`                                             | Fact | The non generic enumerator is also usable.                                                     |
| `TheExplicitCollectionAddContainsRemoveAndCopyToWorkViaTheInterface`              | Fact | The explicit collection add contains remove and copy to work via the interface.                |
| `TheDictionaryGrowsBeyondItsDefaultInitialCapacityOfSixteen`                      | Fact | The dictionary grows beyond its default initial capacity of sixteen.                           |
| `AnExplicitInitialCapacityOfAtLeastEightGrowsByOneSlotAtATimeButStaysCorrect`     | Fact | An explicit initial capacity of at least eight grows by one slot at a time but stays correct.  |
| `AnExplicitInitialCapacityBelowEightDoublesOnResizeLikeTheDefaultConstructor`     | Fact | An explicit initial capacity below eight doubles on resize like the default constructor.       |
| `AnExplicitInitialCapacityOfZeroIsClampedToOneAndTheDictionaryGrowsNormally`      | Fact | An explicit initial capacity of zero is clamped to one and the dictionary grows normally.      |
| `HashCodesThatCollideWithTheEmptySlotSentinelAreStillFoundAfterBeingAdded`        | Fact | Hash codes that collide with the empty slot sentinel are still found after being added.        |
| `TwoDistinctKeysThatBothCollideWithTheEmptySlotSentinelAreBothFoundIndependently` | Fact | Two distinct keys that both collide with the empty slot sentinel are both found independently. |
| `DisposeReturnsTheRentedArraysWithoutThrowing`                                    | Fact | Dispose returns the rented arrays without throwing.                                            |
| `ANonStringKeyTypeWorksJustAsWellAsAStringKey`                                    | Fact | A non string key type works just as well as a string key.                                      |

## Fuzzy

### FuzzyListMatcherTests

`Jube.Test.Dictionary.Fuzzy.FuzzyListMatcherTests` — 37 tests. Category: Unit.

| Test                                                                    | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `NothingMatchesForANullValueNullListOrInvalidOptions`                   | Fact | Nothing matches for a null value null list or invalid options.                    |
| `AnEmptyListOrBlankValueNeverMatches`                                   | Fact | An empty list or blank value never matches.                                       |
| `BlankAndNullEntriesInTheListAreIgnored`                                | Fact | Blank and null entries in the list are ignored.                                   |
| `AnExactMatchIsFoundAfterNormalisation`                                 | Fact | An exact match is found after normalisation.                                      |
| `DiacriticsAreFoldedOnBothSides`                                        | Fact | Diacritics are folded on both sides.                                              |
| `DiacriticsCanBeKeptSoThatTheyDiffer`                                   | Fact | Diacritics can be kept so that they differ.                                       |
| `CaseCanBeMadeSignificant`                                              | Fact | Case can be made significant.                                                     |
| `PunctuationDoesNotPreventAMatchByDefault`                              | Fact | Punctuation does not prevent a match by default.                                  |
| `TitlesAndCompanySuffixesCanBeIgnored`                                  | Fact | Titles and company suffixes can be ignored.                                       |
| `SpacesCanBeIgnoredSoRunTogetherNamesMatch`                             | Fact | Spaces can be ignored so run together names match.                                |
| `CountReportsEveryDistinctEntryThatMatches`                             | Fact | Count reports every distinct entry that matches.                                  |
| `StopAtFirstReturnsAsSoonAsOneEntryMatches`                             | Fact | Stop at first returns as soon as one entry matches.                               |
| `BestEntryIsTheOriginalTextOfTheHighestScoringEntry`                    | Fact | Best entry is the original text of the highest scoring entry.                     |
| `BestScoreIsReportedEvenWhenNothingPassesTheThreshold`                  | Fact | Best score is reported even when nothing passes the threshold.                    |
| `ACompletelyUnrelatedValueHasNoBestEntry`                               | Fact | A completely unrelated value has no best entry.                                   |
| `MinLengthSkipsValuesThatAreTooShortToBeTrusted`                        | Fact | Min length skips values that are too short to be trusted.                         |
| `MinLengthIsMeasuredAfterNormalisation`                                 | Fact | Min length is measured after normalisation.                                       |
| `FirstLetterBlockingExcludesEntriesStartingWithADifferentLetter`        | Fact | First letter blocking excludes entries starting with a different letter.          |
| `AnyPassesWhenOneAlgorithmPassesButAllNeedsEvery`                       | Fact | Any passes when one algorithm passes but all needs every.                         |
| `AllScoresAsTheWeakestAlgorithmAndAnyAsTheStrongest`                    | Fact | All scores as the weakest algorithm and any as the strongest.                     |
| `WithinFindsAListEntryInsideLongerText`                                 | Fact | Within finds a list entry inside longer text.                                     |
| `WithinToleratesATypoInsideLongerText`                                  | Fact | Within tolerates a typo inside longer text.                                       |
| `WithinNeverMatchesWhenTheTextIsShorterThanTheEntry`                    | Fact | Within never matches when the text is shorter than the entry.                     |
| `WithinAtTheStartAndEndOfTheTextMatches`                                | Fact | Within at the start and end of the text matches.                                  |
| `WithinDoesNotMatchInsideAWord`                                         | Fact | Within does not match inside a word.                                              |
| `WithinWithSpacesRemovedSearchesCharactersInsideTheText`                | Fact | Within with spaces removed searches characters inside the text.                   |
| `WithinReportsTheBestEntryFound`                                        | Fact | Within reports the best entry found.                                              |
| `AStreamedNonListEnumerableWorks`                                       | Fact | A streamed non list enumerable works.                                             |
| `AnArrayIsTreatedAsAList`                                               | Fact | An array is treated as a list.                                                    |
| `AListChangedAfterAFirstEvaluationIsReRead`                             | Fact | A list changed after a first evaluation is re read.                               |
| `ARemovedEntryStopsMatching`                                            | Fact | A removed entry stops matching.                                                   |
| `ReplacingAFirstEntryInPlaceIsDetected`                                 | Fact | Replacing a first entry in place is detected.                                     |
| `TheSameListEvaluatedUnderDifferentNormalisationsGivesEachItsOwnAnswer` | Fact | The same list evaluated under different normalisations gives each its own answer. |
| `TwoDifferentListsNeverShareCachedEntries`                              | Fact | Two different lists never share cached entries.                                   |
| `ConcurrentEvaluationOfOneListIsConsistent`                             | Fact | Concurrent evaluation of one list is consistent.                                  |
| `MatchAlgorithmsCombinesScoresPerTheRequireAllSetting`                  | Fact | Match algorithms combines scores per the require all setting.                     |
| `ALargeListIsScannedAndTheMatchFound`                                   | Fact | A large list is scanned and the match found.                                      |

### FuzzyNormaliserTests

`Jube.Test.Dictionary.Fuzzy.FuzzyNormaliserTests` — 16 tests. Category: Unit.

| Test                                                      | Kind   | Verifies                                                           |
|-----------------------------------------------------------|--------|--------------------------------------------------------------------|
| `DefaultNormalisationFoldsCaseDiacriticsAndPunctuation`   | Theory | Default normalisation folds case diacritics and punctuation.       |
| `NullAndBlankInputNormaliseToEmpty`                       | Theory | Null and blank input normalise to empty.                           |
| `PunctuationOnlyInputNormalisesToEmpty`                   | Fact   | Punctuation only input normalises to empty.                        |
| `CaseCanBePreserved`                                      | Fact   | Case can be preserved.                                             |
| `DiacriticsCanBePreserved`                                | Fact   | Diacritics can be preserved.                                       |
| `PunctuationCanBePreserved`                               | Fact   | Punctuation can be preserved.                                      |
| `SpacesCanBeRemovedEntirely`                              | Fact   | Spaces can be removed entirely.                                    |
| `LeadingTitlesCanBeRemovedButNeverTheWholeName`           | Theory | Leading titles can be removed but never the whole name.            |
| `TrailingCompanySuffixesCanBeRemovedButNeverTheWholeName` | Theory | Trailing company suffixes can be removed but never the whole name. |
| `TitlesAndSuffixesAreNotRemovedUnlessAsked`               | Fact   | Titles and suffixes are not removed unless asked.                  |
| `TitleRemovalIsCaseInsensitiveEvenWhenCaseIsPreserved`    | Fact   | Title removal is case insensitive even when case is preserved.     |
| `EverySignatureDiffersWhenAnyFlagDiffers`                 | Fact   | Every signature differs when any flag differs.                     |
| `FoldDiacriticsLeavesPlainAsciiAlone`                     | Fact   | Fold diacritics leaves plain ascii alone.                          |
| `FoldDiacriticsKeepsTheCaseOfTheFoldedLetters`            | Fact   | Fold diacritics keeps the case of the folded letters.              |
| `TokensSplitOnSingleSpaces`                               | Theory | Tokens split on single spaces.                                     |
| `NormalisationIsIdempotent`                               | Fact   | Normalisation is idempotent.                                       |

### FuzzyOptionsTests

`Jube.Test.Dictionary.Fuzzy.FuzzyOptionsTests` — 17 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                     |
|--------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `ABlankOptionStringMeansTheDefaultProfile`                         | Theory | A blank option string means the default profile.                             |
| `TheDefaultProfileIsJaroWinklerOrTokenSortWithFoldedNormalisation` | Fact   | The default profile is jaro winkler or token sort with folded normalisation. |
| `EveryAlgorithmAndAliasParsesWithItsThreshold`                     | Theory | Every algorithm and alias parses with its threshold.                         |
| `SeveralAlgorithmsCanBeCombinedWithCommasOrSemicolons`             | Fact   | Several algorithms can be combined with commas or semicolons.                |
| `OptionsWithNoAlgorithmFallBackToTheDefaultAlgorithms`             | Fact   | Options with no algorithm fall back to the default algorithms.               |
| `NormalisationFlagsAreParsed`                                      | Fact   | Normalisation flags are parsed.                                              |
| `NormalisationFlagsCanBeSwitchedBackByTheirOpposites`              | Fact   | Normalisation flags can be switched back by their opposites.                 |
| `TheLastOfConflictingFlagsWins`                                    | Fact   | The last of conflicting flags wins.                                          |
| `ScopeCombinationAndBlockingOptionsAreParsed`                      | Fact   | Scope combination and blocking options are parsed.                           |
| `WholeAndAnyReverseWithinAndAll`                                   | Fact   | Whole and any reverse within and all.                                        |
| `InvalidOptionsAreReportedNotThrown`                               | Theory | Invalid options are reported not thrown.                                     |
| `ParsingIsCachedPerString`                                         | Fact   | Parsing is cached per string.                                                |
| `ADifferentStringGivesADifferentOptionsObject`                     | Fact   | A different string gives a different options object.                         |
| `CanonicalFormIsTrimmedAndLowerCase`                               | Fact   | Canonical form is trimmed and lower case.                                    |
| `ForBuildsASingleAlgorithmProfileAndValidatesItsThreshold`         | Theory | For builds a single algorithm profile and validates its threshold.           |
| `ForCanBeScopedWithin`                                             | Fact   | For can be scoped within.                                                    |
| `DefaultThresholdsAreSensiblePerAlgorithm`                         | Fact   | Default thresholds are sensible per algorithm.                               |

### FuzzyScorersTests

`Jube.Test.Dictionary.Fuzzy.FuzzyScorersTests` — 19 tests. Category: Unit.

| Test                                                         | Kind   | Verifies                                                                |
|--------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `ExactIsAllOrNothing`                                        | Theory | Exact is all or nothing.                                                |
| `LevenshteinCountsInsertionsDeletionsAndSubstitutions`       | Theory | Levenshtein counts insertions deletions and substitutions.              |
| `DamerauCountsAnAdjacentTranspositionAsOneEdit`              | Theory | Damerau counts an adjacent transposition as one edit.                   |
| `DamerauIsNeverWorseThanLevenshtein`                         | Fact   | Damerau is never worse than levenshtein.                                |
| `DistanceScoreIsOneMinusTheDistanceOverTheLongerLength`      | Theory | Distance score is one minus the distance over the longer length.        |
| `SimilarityIsTheLevenshteinRatio`                            | Theory | Similarity is the levenshtein ratio.                                    |
| `JaroWinklerRewardsACommonPrefix`                            | Fact   | Jaro winkler rewards a common prefix.                                   |
| `DiceComparesBigramMultisets`                                | Theory | Dice compares bigram multisets.                                         |
| `DiceIsSymmetricAndCountsRepeatedBigramsOnce`                | Fact   | Dice is symmetric and counts repeated bigrams once.                     |
| `SoundexRequiresEveryTokenToSoundTheSameInOrder`             | Theory | Soundex requires every token to sound the same in order.                |
| `TokenSortIgnoresTheOrderOfWords`                            | Theory | Token sort ignores the order of words.                                  |
| `TokenSortStillPenalisesDifferentWords`                      | Fact   | Token sort still penalises different words.                             |
| `TokenSetAveragesTheBestMatchOfEachTokenOverTheLargerSet`    | Theory | Token set averages the best match of each token over the larger set.    |
| `TokenSetUsesEachTokenOnlyOnce`                              | Fact   | Token set uses each token only once.                                    |
| `InitialsMatchAnAbbreviatedTokenButNeedAFullTokenToAnchorIt` | Theory | Initials match an abbreviated token but need a full token to anchor it. |
| `EveryAlgorithmPassesAnIdenticalMultiWordName`               | Theory | Every algorithm passes an identical multi word name.                    |
| `NoAlgorithmPassesCompletelyUnrelatedNames`                  | Theory | No algorithm passes completely unrelated names.                         |
| `LevenshteinPassesOnTheEditBudgetAndScoresByRatio`           | Fact   | Levenshtein passes on the edit budget and scores by ratio.              |
| `ADamerauTranspositionPassesOneEditWhereLevenshteinNeedsTwo` | Fact   | A damerau transposition passes one edit where levenshtein needs two.    |

## Models

### InternalValueTests

`Jube.Test.Dictionary.Models.InternalValueTests` — 41 tests. Category: Unit.

| Test                                                                                          | Kind   | Verifies                                                                                                   |
|-----------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------|
| `ADefaultInternalValueHasTheNoneType`                                                         | Fact   | A default internal value has the none type.                                                                |
| `AStringValueRoundTripsThroughAsStringAndToString`                                            | Fact   | A string value round trips through as string and to string.                                                |
| `ANullStringValueIsStillTypedAsStringButRendersAsTheLiteralNull`                              | Fact   | A null string value is still typed as string but renders as the literal null.                              |
| `AnEmptyStringValueRoundTrips`                                                                | Fact   | An empty string value round trips.                                                                         |
| `AnIntValueRoundTripsThroughAsInt`                                                            | Theory | An int value round trips through as int.                                                                   |
| `ADoubleValueRoundTripsThroughAsDoubleBitForBit`                                              | Theory | A double value round trips through as double bit for bit.                                                  |
| `ABoolValueRoundTripsThroughAsBool`                                                           | Theory | A bool value round trips through as bool.                                                                  |
| `ADateTimeValueIsNormalisedToUtcTicksOnConstruction`                                          | Fact   | A date time value is normalised to utc ticks on construction.                                              |
| `AnUnspecifiedKindDateTimeIsConvertedAsIfItWereLocalOnConstruction`                           | Fact   | An unspecified kind date time is converted as if it were local on construction.                            |
| `AGuidValueRoundTripsThroughAsGuid`                                                           | Fact   | A guid value round trips through as guid.                                                                  |
| `TheEmptyGuidRoundTrips`                                                                      | Fact   | The empty guid round trips.                                                                                |
| `AccessorsForTypesOtherThanTheStoredTypeReturnTheirDefaultRatherThanThrowing`                 | Theory | Accessors for types other than the stored type return their default rather than throwing.                  |
| `TwoValuesOfTheSameTypeAndContentAreEqualAndShareAHashCode`                                   | Fact   | Two values of the same type and content are equal and share a hash code.                                   |
| `DoubleEqualityUsesASmallToleranceRatherThanExactBitwiseComparison`                           | Fact   | Double equality uses a small tolerance rather than exact bitwise comparison.                               |
| `EqualityRequiresTheSameStoredTypeEvenWhenTheNumericValueMatches`                             | Fact   | Equality requires the same stored type even when the numeric value matches.                                |
| `RelationalOperatorsFallBackToNumericComparisonAcrossDifferentTypesEvenThoughEqualityDoesNot` | Fact   | Relational operators fall back to numeric comparison across different types even though equality does not. |
| `ComparingANonNumericTypeAgainstANumericTypeTreatsTheNonNumericSideAsZero`                    | Fact   | Comparing a non numeric type against a numeric type treats the non numeric side as zero.                   |
| `CompareToOrdersStringsOrdinally`                                                             | Fact   | Compare to orders strings ordinally.                                                                       |
| `CompareToOrdersIntsNumerically`                                                              | Fact   | Compare to orders ints numerically.                                                                        |
| `CompareToOrdersDatesChronologically`                                                         | Fact   | Compare to orders dates chronologically.                                                                   |
| `CompareToOrdersBooleansWithFalseBeforeTrue`                                                  | Fact   | Compare to orders booleans with false before true.                                                         |
| `AListOfInternalValuesCanBeSortedBecauseTheStructImplementsIComparable`                       | Fact   | A list of internal values can be sorted because the struct implements i comparable.                        |
| `EqualityOperatorAgainstARawIntWorks`                                                         | Theory | Equality operator against a raw int works.                                                                 |
| `RelationalOperatorsAgainstARawIntAreSymmetricWhenOperandsAreSwapped`                         | Fact   | Relational operators against a raw int are symmetric when operands are swapped.                            |
| `EqualityOperatorAgainstARawDoubleWorks`                                                      | Theory | Equality operator against a raw double works.                                                              |
| `RelationalOperatorsAgainstARawDoubleWork`                                                    | Fact   | Relational operators against a raw double work.                                                            |
| `EqualityAndRelationalOperatorsAgainstARawStringWork`                                         | Fact   | Equality and relational operators against a raw string work.                                               |
| `EqualityAndRelationalOperatorsAgainstARawDateTimeWork`                                       | Fact   | Equality and relational operators against a raw date time work.                                            |
| `EqualityOperatorAgainstARawBoolWorks`                                                        | Fact   | Equality operator against a raw bool works.                                                                |
| `EqualityOperatorAgainstARawGuidWorks`                                                        | Fact   | Equality operator against a raw guid works.                                                                |
| `ArithmeticOperatorsCombineTheNumericValueOfTwoInternalValues`                                | Fact   | Arithmetic operators combine the numeric value of two internal values.                                     |
| `ArithmeticOperatorsTreatNonNumericOperandsAsZero`                                            | Fact   | Arithmetic operators treat non numeric operands as zero.                                                   |
| `DivisionByZeroFollowsIeee754RatherThanThrowing`                                              | Fact   | Division by zero follows ieee754 rather than throwing.                                                     |
| `ArithmeticOperatorsAgainstARawDoubleWorkInBothOperandPositions`                              | Fact   | Arithmetic operators against a raw double work in both operand positions.                                  |
| `ArithmeticOperatorsAgainstARawIntWorkInBothOperandPositions`                                 | Fact   | Arithmetic operators against a raw int work in both operand positions.                                     |
| `ImplicitConversionToStringDelegatesToAsString`                                               | Fact   | Implicit conversion to string delegates to as string.                                                      |
| `ImplicitConversionToIntDelegatesToAsInt`                                                     | Fact   | Implicit conversion to int delegates to as int.                                                            |
| `ImplicitConversionToDoubleDelegatesToAsDouble`                                               | Fact   | Implicit conversion to double delegates to as double.                                                      |
| `ImplicitConversionToBoolDelegatesToAsBool`                                                   | Fact   | Implicit conversion to bool delegates to as bool.                                                          |
| `ImplicitConversionToDateTimeDelegatesToAsDateTime`                                           | Fact   | Implicit conversion to date time delegates to as date time.                                                |
| `ImplicitConversionToGuidDelegatesToAsGuid`                                                   | Fact   | Implicit conversion to guid delegates to as guid.                                                          |

## Serialization

### DictionaryNoBoxingConverterTests

`Jube.Test.Dictionary.Serialization.DictionaryNoBoxingConverterTests` — 16 tests. Category: Unit.

| Test                                                                                      | Kind | Verifies                                                                                                  |
|-------------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------|
| `WritingANullDictionaryAtTheTopLevelProducesJsonNullRatherThanInvokingTheConverter`       | Fact | Writing a null dictionary at the top level produces json null rather than invoking the converter.         |
| `WritingANullDictionaryAsANestedPropertyAlsoProducesJsonNullWithoutInvokingTheConverter`  | Fact | Writing a null dictionary as a nested property also produces json null without invoking the converter.    |
| `CallingWriteJsonDirectlyWithANullValueStillProducesAnEmptyObjectPerItsOwnDefensiveGuard` | Fact | Calling write json directly with a null value still produces an empty object per its own defensive guard. |
| `WritingAnEmptyDictionaryProducesAnEmptyJsonObject`                                       | Fact | Writing an empty dictionary produces an empty json object.                                                |
| `EachStoredTypeIsWrittenAsItsNaturalJsonRepresentation`                                   | Fact | Each stored type is written as its natural json representation.                                           |
| `ANullBackedStringValueIsWrittenAsAnEmptyJsonStringNotJsonNull`                           | Fact | A null backed string value is written as an empty json string not json null.                              |
| `ReadingAJsonIntegerProducesAnIntTypedEntry`                                              | Fact | Reading a json integer produces an int typed entry.                                                       |
| `ReadingAJsonFloatProducesADoubleTypedEntry`                                              | Fact | Reading a json float produces a double typed entry.                                                       |
| `ReadingAJsonBooleanProducesABoolTypedEntry`                                              | Fact | Reading a json boolean produces a bool typed entry.                                                       |
| `ReadingAJsonNullProducesAStringTypedEntryWithANullBackingValue`                          | Fact | Reading a json null produces a string typed entry with a null backing value.                              |
| `ReadingAPlainJsonStringProducesAStringTypedEntry`                                        | Fact | Reading a plain json string produces a string typed entry.                                                |
| `ReadingAJsonStringThatLooksLikeAGuidStillProducesAPlainStringTypedEntry`                 | Fact | Reading a json string that looks like a guid still produces a plain string typed entry.                   |
| `ReadingAJsonStringThatLooksLikeAnIsoDateIsStillDecodedAsAStringTypedEntry`               | Fact | Reading a json string that looks like an iso date is still decoded as a string typed entry.               |
| `ReadJsonPopulatesAnExistingDictionaryInsteadOfCreatingANewOneWhenOneIsSupplied`          | Fact | Read json populates an existing dictionary instead of creating a new one when one is supplied.            |
| `ReadJsonWithNoExistingInstanceCreatesANewDictionary`                                     | Fact | Read json with no existing instance creates a new dictionary.                                             |
| `MultipleFieldsOfDifferentTypesRoundTripThroughWriteThenReadCorrectlyForJsonNativeTypes`  | Fact | Multiple fields of different types round trip through write then read correctly for json native types.    |
