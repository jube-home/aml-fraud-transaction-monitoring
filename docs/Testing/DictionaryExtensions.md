---
layout: default
title: Dictionary Rule Extensions
nav_order: 9
parent: Testing
---

# Dictionary Rule Extensions

Tests of the dictionary extension methods exposed to the rule DSL, the fraud and AML toolkit of ratio, velocity,
volatility and distribution helpers.

## General

### BooleanExtensionsTests

`Jube.Test.Dictionary.Extensions.BooleanExtensionsTests` — 2 tests. Category: Unit.

| Test                                                  | Kind   | Verifies                                                       |
|-------------------------------------------------------|--------|----------------------------------------------------------------|
| `ToBinaryConvertsToOneOrZero`                         | Theory | To binary converts to one or zero.                             |
| `ToStringWithTrueAndFalseValuesSelectsTheMatchingOne` | Theory | To string with true and false values selects the matching one. |

### CharExtensionsTests

`Jube.Test.Dictionary.Extensions.CharExtensionsTests` — 33 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                       |
|--------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `ConvertToUtf32CombinesASurrogatePairIntoItsCodePoint`             | Fact   | Convert to utf32 combines a surrogate pair into its code point.                |
| `GetNumericValueReturnsTheDigitsValueOrMinusOneForNonDigits`       | Theory | Get numeric value returns the digits value or minus one for non digits.        |
| `GetUnicodeCategoryDelegatesToTheBclImplementation`                | Theory | Get unicode category delegates to the bcl implementation.                      |
| `InChecksMembershipAgainstTheProvidedValues`                       | Theory | In checks membership against the provided values.                              |
| `NotInIsTheInverseOfIn`                                            | Theory | Not in is the inverse of in.                                                   |
| `IsControlDelegatesToTheBcl`                                       | Theory | Is control delegates to the bcl.                                               |
| `IsDigitDelegatesToTheBcl`                                         | Theory | Is digit delegates to the bcl.                                                 |
| `IsHighSurrogateDelegatesToTheBcl`                                 | Theory | Is high surrogate delegates to the bcl.                                        |
| `IsLetterDelegatesToTheBcl`                                        | Theory | Is letter delegates to the bcl.                                                |
| `IsLetterOrDigitDelegatesToTheBcl`                                 | Theory | Is letter or digit delegates to the bcl.                                       |
| `IsLowerDelegatesToTheBcl`                                         | Theory | Is lower delegates to the bcl.                                                 |
| `IsLowSurrogateDelegatesToTheBcl`                                  | Theory | Is low surrogate delegates to the bcl.                                         |
| `IsNumberDelegatesToTheBcl`                                        | Theory | Is number delegates to the bcl.                                                |
| `IsPunctuationDelegatesToTheBcl`                                   | Theory | Is punctuation delegates to the bcl.                                           |
| `IsSeparatorDelegatesToTheBcl`                                     | Theory | Is separator delegates to the bcl.                                             |
| `IsSurrogateDelegatesToTheBcl`                                     | Theory | Is surrogate delegates to the bcl.                                             |
| `IsSurrogatePairRecognisesAValidHighLowPairAndRejectsAnInvalidOne` | Fact   | Is surrogate pair recognises a valid high low pair and rejects an invalid one. |
| `IsSymbolDelegatesToTheBcl`                                        | Theory | Is symbol delegates to the bcl.                                                |
| `IsUpperDelegatesToTheBcl`                                         | Theory | Is upper delegates to the bcl.                                                 |
| `IsWhiteSpaceDelegatesToTheBcl`                                    | Theory | Is white space delegates to the bcl.                                           |
| `RepeatBuildsAStringOfTheCharacterRepeatedNTimes`                  | Fact   | Repeat builds a string of the character repeated n times.                      |
| `RepeatWithZeroProducesAnEmptyString`                              | Fact   | Repeat with zero produces an empty string.                                     |
| `ToEnumeratesInclusivelyFromThisCharacterToTheTarget`              | Fact   | To enumerates inclusively from this character to the target.                   |
| `ToReversesTheSequenceWhenTheTargetPrecedesThisCharacter`          | Fact   | To reverses the sequence when the target precedes this character.              |
| `ToWithTheSameStartAndEndYieldsASingleCharacter`                   | Fact   | To with the same start and end yields a single character.                      |
| `ToLowerWithoutCultureConvertsToLowercase`                         | Fact   | To lower without culture converts to lowercase.                                |
| `ToLowerWithAnExplicitCultureUsesThatCulturesRules`                | Fact   | To lower with an explicit culture uses that cultures rules.                    |
| `ToLowerInvariantIgnoresCurrentCulture`                            | Fact   | To lower invariant ignores current culture.                                    |
| `ToStringConvertsACharacterToASingleCharacterString`               | Fact   | To string converts a character to a single character string.                   |
| `ToUpperWithoutCultureConvertsToUppercase`                         | Fact   | To upper without culture converts to uppercase.                                |
| `ToUpperWithAnExplicitCultureUsesThatCulturesRules`                | Fact   | To upper with an explicit culture uses that cultures rules.                    |
| `ToUpperInvariantIgnoresCurrentCulture`                            | Fact   | To upper invariant ignores current culture.                                    |
| `NonAlphabeticCharactersAreUnchangedByCasingConversions`           | Fact   | Non alphabetic characters are unchanged by casing conversions.                 |

### DateAndStructuredDataExtensionsTests

`Jube.Test.Dictionary.Extensions.DateAndStructuredDataExtensionsTests` — 15 tests. Category: Unit.

| Test                                                | Kind   | Verifies                                                 |
|-----------------------------------------------------|--------|----------------------------------------------------------|
| `AgeAsOfCountsCompletedYears`                       | Theory | Age as of counts completed years.                        |
| `AgeInDaysAsOfIgnoresTheTimeOfDay`                  | Fact   | Age in days as of ignores the time of day.               |
| `ComparisonHelpersCompareInstants`                  | Fact   | Comparison helpers compare instants.                     |
| `WholeMonthsBetweenCountsCompletedMonths`           | Theory | Whole months between counts completed months.            |
| `WholeYearsBetweenCountsCompletedYears`             | Fact   | Whole years between counts completed years.              |
| `AddBusinessDaysSkipsWeekends`                      | Theory | Add business days skips weekends.                        |
| `AddBusinessDaysAgreesWithBusinessDaysUntil`        | Fact   | Add business days agrees with business days until.       |
| `QuarterBoundariesAndFiscalPeriods`                 | Fact   | Quarter boundaries and fiscal periods.                   |
| `IsLastDayOfMonthHandlesLeapYears`                  | Fact   | Is last day of month handles leap years.                 |
| `IntervalOverlapsTreatsTouchingIntervalsAsDisjoint` | Fact   | Interval overlaps treats touching intervals as disjoint. |
| `EpochConversionsRoundTrip`                         | Fact   | Epoch conversions round trip.                            |
| `Iso8601AndExactParsing`                            | Fact   | Iso8601 and exact parsing.                               |
| `TimeZoneHelpersUseIanaZones`                       | Fact   | Time zone helpers use iana zones.                        |
| `JsonHelpersNavigateDottedAndIndexedPaths`          | Fact   | Json helpers navigate dotted and indexed paths.          |
| `XmlHelpersReadValuesAndRefuseDtds`                 | Fact   | Xml helpers read values and refuse dtds.                 |

### DateTimeExtensionsTests

`Jube.Test.Dictionary.Extensions.DateTimeExtensionsTests` — 63 tests. Category: Unit.

| Test                                                                                          | Kind   | Verifies                                                                                                     |
|-----------------------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------------------------|
| `AgeIsComputedFromTodayRelativeToTheBirthDate`                                                | Fact   | Age is computed from today relative to the birth date.                                                       |
| `AgeDoesNotCountThisYearWhenTheBirthdayHasNotYetOccurred`                                     | Fact   | Age does not count this year when the birthday has not yet occurred.                                         |
| `AgeOnTheExactBirthdayCountsTheYearAlready`                                                   | Fact   | Age on the exact birthday counts the year already.                                                           |
| `BetweenIsTrueOnlyStrictlyInsideTheRangeExcludingBothEndpoints`                               | Fact   | Between is true only strictly inside the range excluding both endpoints.                                     |
| `ConvertTimeBySystemTimeZoneIdWithASingleZoneConvertsFromLocalToTheDestinationZone`           | Fact   | Convert time by system time zone id with a single zone converts from local to the destination zone.          |
| `ConvertTimeBySystemTimeZoneIdWithSourceAndDestinationConvertsBetweenTheTwoNamedZones`        | Fact   | Convert time by system time zone id with source and destination converts between the two named zones.        |
| `ConvertTimeBySystemTimeZoneIdThrowsForAnUnknownTimeZoneId`                                   | Fact   | Convert time by system time zone id throws for an unknown time zone id.                                      |
| `ConvertTimeWithASingleDestinationZoneMatchesTimeZoneInfoConvertTime`                         | Fact   | Convert time with a single destination zone matches time zone info convert time.                             |
| `ConvertTimeWithSourceAndDestinationZonesMatchesTimeZoneInfoConvertTime`                      | Fact   | Convert time with source and destination zones matches time zone info convert time.                          |
| `ConvertTimeFromUtcConvertsAUtcInstantIntoTheDestinationZonesLocalTime`                       | Fact   | Convert time from utc converts a utc instant into the destination zones local time.                          |
| `ConvertTimeToUtcWithNoSourceZoneReturnsAnAlreadyUtcValueUnchanged`                           | Fact   | Convert time to utc with no source zone returns an already utc value unchanged.                              |
| `ConvertTimeToUtcWithAnExplicitSourceZoneThrowsWhenTheKindIsUtcButTheZoneIsNot`               | Fact   | Convert time to utc with an explicit source zone throws when the kind is utc but the zone is not.            |
| `ConvertTimeToUtcWithNoSourceZoneConvertsAnUnspecifiedOrLocalDateTimeToUtc`                   | Fact   | Convert time to utc with no source zone converts an unspecified or local date time to utc.                   |
| `ConvertTimeToUtcWithAnExplicitSourceZoneConvertsToUtc`                                       | Fact   | Convert time to utc with an explicit source zone converts to utc.                                            |
| `ElapsedReturnsTheDurationSinceTheGivenDateTimeMeasuredAgainstNow`                            | Fact   | Elapsed returns the duration since the given date time measured against now.                                 |
| `EndOfDayReturnsOneMillisecondBeforeMidnightOfTheSameDay`                                     | Fact   | End of day returns one millisecond before midnight of the same day.                                          |
| `EndOfMonthReturnsOneMillisecondBeforeMidnightOnTheFirstOfTheNextMonth`                       | Fact   | End of month returns one millisecond before midnight on the first of the next month.                         |
| `EndOfMonthHandlesFebruaryInALeapYearCorrectly`                                               | Fact   | End of month handles february in a leap year correctly.                                                      |
| `EndOfWeekReturnsTheLastMomentOfTheDayBeforeTheNextStartDayOfWeek`                            | Theory | End of week returns the last moment of the day before the next start day of week.                            |
| `EndOfYearReturnsOneMillisecondBeforeMidnightOnJanuaryFirstOfTheNextYear`                     | Fact   | End of year returns one millisecond before midnight on january first of the next year.                       |
| `FirstDayOfWeekReturnsTheSundayOfThatWeekAtMidnight`                                          | Fact   | First day of week returns the sunday of that week at midnight.                                               |
| `InReturnsTrueOnlyWhenTheExactDateTimeIsAmongTheProvidedValues`                               | Fact   | In returns true only when the exact date time is among the provided values.                                  |
| `NotInIsTheInverseOfIn`                                                                       | Fact   | Not in is the inverse of in.                                                                                 |
| `InRangeIsInclusiveOfBothEndpointsUnlikeBetween`                                              | Fact   | In range is inclusive of both endpoints unlike between.                                                      |
| `IsAfternoonIsTrueAtOrAfterNoon`                                                              | Theory | Is afternoon is true at or after noon.                                                                       |
| `IsMorningIsTrueOnlyBeforeNoon`                                                               | Theory | Is morning is true only before noon.                                                                         |
| `IsDateEqualComparesOnlyTheDatePortionIgnoringTime`                                           | Fact   | Is date equal compares only the date portion ignoring time.                                                  |
| `IsTimeEqualComparesOnlyTheTimeOfDayIgnoringTheDate`                                          | Fact   | Is time equal compares only the time of day ignoring the date.                                               |
| `IsDaylightSavingTimeDelegatesToTheLegacyTimeZoneApi`                                         | Fact   | Is daylight saving time delegates to the legacy time zone api.                                               |
| `IsFutureIsTrueForADateTimeAfterNow`                                                          | Fact   | Is future is true for a date time after now.                                                                 |
| `IsPastIsTrueForADateTimeBeforeNow`                                                           | Fact   | Is past is true for a date time before now.                                                                  |
| `IsNowIsPracticallyAlwaysFalseBecauseExactEqualityToTheCurrentInstantIsNotAchievable`         | Fact   | Is now is practically always false because exact equality to the current instant is not achievable.          |
| `IsTodayIsTrueForAnyTimeOnTheCurrentDateAndFalseOtherwise`                                    | Fact   | Is today is true for any time on the current date and false otherwise.                                       |
| `IsWeekDayIsFalseOnlyForSaturdayAndSunday`                                                    | Theory | Is week day is false only for saturday and sunday.                                                           |
| `LastDayOfWeekReturnsTheSaturdayOfThatWeekAtMidnight`                                         | Fact   | Last day of week returns the saturday of that week at midnight.                                              |
| `SetTimeWithOnlyHourZeroesOutMinutesSecondsAndMilliseconds`                                   | Fact   | Set time with only hour zeroes out minutes seconds and milliseconds.                                         |
| `SetTimeWithHourAndMinuteZeroesOutSecondsAndMilliseconds`                                     | Fact   | Set time with hour and minute zeroes out seconds and milliseconds.                                           |
| `SetTimeWithHourMinuteAndSecondZeroesOutMilliseconds`                                         | Fact   | Set time with hour minute and second zeroes out milliseconds.                                                |
| `SetTimeWithAllFourComponentsReplacesTheTimePortionEntirely`                                  | Fact   | Set time with all four components replaces the time portion entirely.                                        |
| `StartOfDayZeroesOutTheTimePortion`                                                           | Fact   | Start of day zeroes out the time portion.                                                                    |
| `StartOfMonthReturnsTheFirstOfTheMonthAtMidnight`                                             | Fact   | Start of month returns the first of the month at midnight.                                                   |
| `StartOfWeekReturnsTheRequestedStartDayOfWeekAtMidnightNoLaterThanTheInput`                   | Theory | Start of week returns the requested start day of week at midnight no later than the input.                   |
| `StartOfYearReturnsJanuaryFirstAtMidnight`                                                    | Fact   | Start of year returns january first at midnight.                                                             |
| `ToEpochTimeSpanMeasuresTheDurationSinceTheUnixEpoch`                                         | Fact   | To epoch time span measures the duration since the unix epoch.                                               |
| `ToEpochTimeSpanIsNegativeForADateTimeBeforeTheEpoch`                                         | Fact   | To epoch time span is negative for a date time before the epoch.                                             |
| `TomorrowAddsExactlyOneDayPreservingTheTimeOfDay`                                             | Fact   | Tomorrow adds exactly one day preserving the time of day.                                                    |
| `YesterdaySubtractsExactlyOneDayPreservingTheTimeOfDay`                                       | Fact   | Yesterday subtracts exactly one day preserving the time of day.                                              |
| `TomorrowAndYesterdayAreInversesOfEachOther`                                                  | Fact   | Tomorrow and yesterday are inverses of each other.                                                           |
| `EachFormatStringProducesWhatDateTimeToStringWouldProduceUnderTheSameCulture`                 | Theory | Each format string produces what date time to string would produce under the same culture.                   |
| `TheStringCultureNameOverloadAgreesWithTheCultureInfoOverload`                                | Theory | The string culture name overload agrees with the culture info overload.                                      |
| `TheSortableDateTimeStringFormatIsCultureInvariantByDesignEvenWithADifferentCultureRequested` | Fact   | The sortable date time string format is culture invariant by design even with a different culture requested. |
| `ToLongDateTimeStringIsAnAliasForTheFullDateTimeStringFormat`                                 | Fact   | To long date time string is an alias for the full date time string format.                                   |
| `ToLongDateTimeStringWithACultureNameAgreesWithTheCultureInfoOverload`                        | Fact   | To long date time string with a culture name agrees with the culture info overload.                          |
| `ToLongDateTimeStringMatchesDateTimeToStringWithTheFFormat`                                   | Fact   | To long date time string matches date time to string with the f format.                                      |
| `BusinessDaysUntilCountsWeekdaysAfterTheStartUpToAndIncludingTheEnd`                          | Fact   | Business days until counts weekdays after the start up to and including the end.                             |
| `BusinessDaysUntilOfTheSameDateIsZero`                                                        | Fact   | Business days until of the same date is zero.                                                                |
| `BusinessDaysUntilIsNegativeWhenTheOtherDateIsEarlier`                                        | Fact   | Business days until is negative when the other date is earlier.                                              |
| `QuarterOfYearMapsTheMonthToItsQuarter`                                                       | Theory | Quarter of year maps the month to its quarter.                                                               |
| `IsoWeekOfYearOfTheFirstMondayOfTwentyTwentyFourIsWeekOne`                                    | Fact   | Iso week of year of the first monday of twenty twenty four is week one.                                      |
| `IsoWeekOfYearDelegatesToTheBclIsoWeekImplementation`                                         | Fact   | Iso week of year delegates to the bcl iso week implementation.                                               |
| `AgeInDaysCountsWholeDaysSinceTheDate`                                                        | Fact   | Age in days counts whole days since the date.                                                                |
| `AgeInDaysIsNegativeForAFutureDate`                                                           | Fact   | Age in days is negative for a future date.                                                                   |
| `IsWithinBusinessHoursChecksTheHourAgainstAHalfOpenRange`                                     | Theory | Is within business hours checks the hour against a half open range.                                          |

### DoubleCalculationCollisionTests

`Jube.Test.Dictionary.Extensions.DoubleCalculationCollisionTests` — 3 tests. Category: Unit.

| Test                                                                       | Kind | Verifies                                                                                 |
|----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `NoCalculationNameIsAnInstanceOrStaticMemberOfDoubleWhichVbWouldBindFirst` | Fact | No calculation name is an instance or static member of double which vb would bind first. |
| `EveryCalculationIsAnExtensionOnDoubleReturningADouble`                    | Fact | Every calculation is an extension on double returning a double.                          |
| `EveryCalculationHasAboveBelowInRangeAndOutsideRangeComparisonSteps`       | Fact | Every calculation has above below in range and outside range comparison steps.           |

### DoubleCalculationExtensionsTests

`Jube.Test.Dictionary.Extensions.DoubleCalculationExtensionsTests` — 82 tests. Category: Unit.

| Test                                                        | Kind   | Verifies                                                                 |
|-------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `RatioOfComputesTheDocumentedValue`                         | Theory | Ratio of computes the documented value.                                  |
| `RatioOfIsNaNForANaNValueAndNeverInfinite`                  | Fact   | Ratio of is na n for a na n value and never infinite.                    |
| `PercentOfComputesTheDocumentedValue`                       | Theory | Percent of computes the documented value.                                |
| `PercentOfIsNaNForANaNValueAndNeverInfinite`                | Fact   | Percent of is na n for a na n value and never infinite.                  |
| `ShareOfSumWithComputesTheDocumentedValue`                  | Theory | Share of sum with computes the documented value.                         |
| `ShareOfSumWithIsNaNForANaNValueAndNeverInfinite`           | Fact   | Share of sum with is na n for a na n value and never infinite.           |
| `ComplementComputesTheDocumentedValue`                      | Theory | Complement computes the documented value.                                |
| `ComplementIsNaNForANaNValueAndNeverInfinite`               | Fact   | Complement is na n for a na n value and never infinite.                  |
| `LogRatioOfComputesTheDocumentedValue`                      | Theory | Log ratio of computes the documented value.                              |
| `LogRatioOfIsNaNForANaNValueAndNeverInfinite`               | Fact   | Log ratio of is na n for a na n value and never infinite.                |
| `RatioOfRatiosComputesTheDocumentedValue`                   | Theory | Ratio of ratios computes the documented value.                           |
| `RatioOfRatiosIsNaNForANaNValueAndNeverInfinite`            | Fact   | Ratio of ratios is na n for a na n value and never infinite.             |
| `InverseRatioOfComputesTheDocumentedValue`                  | Theory | Inverse ratio of computes the documented value.                          |
| `InverseRatioOfIsNaNForANaNValueAndNeverInfinite`           | Fact   | Inverse ratio of is na n for a na n value and never infinite.            |
| `DifferenceFromComputesTheDocumentedValue`                  | Theory | Difference from computes the documented value.                           |
| `DifferenceFromIsNaNForANaNValueAndNeverInfinite`           | Fact   | Difference from is na n for a na n value and never infinite.             |
| `AbsoluteDifferenceFromComputesTheDocumentedValue`          | Theory | Absolute difference from computes the documented value.                  |
| `AbsoluteDifferenceFromIsNaNForANaNValueAndNeverInfinite`   | Fact   | Absolute difference from is na n for a na n value and never infinite.    |
| `PercentDifferenceFromComputesTheDocumentedValue`           | Theory | Percent difference from computes the documented value.                   |
| `PercentDifferenceFromIsNaNForANaNValueAndNeverInfinite`    | Fact   | Percent difference from is na n for a na n value and never infinite.     |
| `RelativeChangeFromComputesTheDocumentedValue`              | Theory | Relative change from computes the documented value.                      |
| `RelativeChangeFromIsNaNForANaNValueAndNeverInfinite`       | Fact   | Relative change from is na n for a na n value and never infinite.        |
| `GrowthFactorFromComputesTheDocumentedValue`                | Theory | Growth factor from computes the documented value.                        |
| `GrowthFactorFromIsNaNForANaNValueAndNeverInfinite`         | Fact   | Growth factor from is na n for a na n value and never infinite.          |
| `SlopeFromComputesTheDocumentedValue`                       | Theory | Slope from computes the documented value.                                |
| `SlopeFromIsNaNForANaNValueAndNeverInfinite`                | Fact   | Slope from is na n for a na n value and never infinite.                  |
| `CompoundGrowthRateComputesTheDocumentedValue`              | Theory | Compound growth rate computes the documented value.                      |
| `CompoundGrowthRateIsNaNForANaNValueAndNeverInfinite`       | Fact   | Compound growth rate is na n for a na n value and never infinite.        |
| `PercentDeviationFromMeanComputesTheDocumentedValue`        | Theory | Percent deviation from mean computes the documented value.               |
| `PercentDeviationFromMeanIsNaNForANaNValueAndNeverInfinite` | Fact   | Percent deviation from mean is na n for a na n value and never infinite. |
| `ModifiedZScoreComputesTheDocumentedValue`                  | Theory | Modified z score computes the documented value.                          |
| `ModifiedZScoreIsNaNForANaNValueAndNeverInfinite`           | Fact   | Modified z score is na n for a na n value and never infinite.            |
| `CoefficientOfVariationComputesTheDocumentedValue`          | Theory | Coefficient of variation computes the documented value.                  |
| `CoefficientOfVariationIsNaNForANaNValueAndNeverInfinite`   | Fact   | Coefficient of variation is na n for a na n value and never infinite.    |
| `MinMaxNormaliseComputesTheDocumentedValue`                 | Theory | Min max normalise computes the documented value.                         |
| `MinMaxNormaliseIsNaNForANaNValueAndNeverInfinite`          | Fact   | Min max normalise is na n for a na n value and never infinite.           |
| `PercentOfRangeComputesTheDocumentedValue`                  | Theory | Percent of range computes the documented value.                          |
| `PercentOfRangeIsNaNForANaNValueAndNeverInfinite`           | Fact   | Percent of range is na n for a na n value and never infinite.            |
| `SumWithComputesTheDocumentedValue`                         | Theory | Sum with computes the documented value.                                  |
| `SumWithIsNaNForANaNValueAndNeverInfinite`                  | Fact   | Sum with is na n for a na n value and never infinite.                    |
| `SumWithTreatsANullArrayAsNoOthers`                         | Fact   | Sum with treats a null array as no others.                               |
| `MeanWithComputesTheDocumentedValue`                        | Theory | Mean with computes the documented value.                                 |
| `MeanWithIsNaNForANaNValueAndNeverInfinite`                 | Fact   | Mean with is na n for a na n value and never infinite.                   |
| `MeanWithTreatsANullArrayAsNoOthers`                        | Fact   | Mean with treats a null array as no others.                              |
| `MaxWithComputesTheDocumentedValue`                         | Theory | Max with computes the documented value.                                  |
| `MaxWithIsNaNForANaNValueAndNeverInfinite`                  | Fact   | Max with is na n for a na n value and never infinite.                    |
| `MaxWithTreatsANullArrayAsNoOthers`                         | Fact   | Max with treats a null array as no others.                               |
| `MinWithComputesTheDocumentedValue`                         | Theory | Min with computes the documented value.                                  |
| `MinWithIsNaNForANaNValueAndNeverInfinite`                  | Fact   | Min with is na n for a na n value and never infinite.                    |
| `MinWithTreatsANullArrayAsNoOthers`                         | Fact   | Min with treats a null array as no others.                               |
| `SpreadWithComputesTheDocumentedValue`                      | Theory | Spread with computes the documented value.                               |
| `SpreadWithIsNaNForANaNValueAndNeverInfinite`               | Fact   | Spread with is na n for a na n value and never infinite.                 |
| `SpreadWithTreatsANullArrayAsNoOthers`                      | Fact   | Spread with treats a null array as no others.                            |
| `WeightedMeanWithComputesTheDocumentedValue`                | Theory | Weighted mean with computes the documented value.                        |
| `WeightedMeanWithIsNaNForANaNValueAndNeverInfinite`         | Fact   | Weighted mean with is na n for a na n value and never infinite.          |
| `GeometricMeanWithComputesTheDocumentedValue`               | Theory | Geometric mean with computes the documented value.                       |
| `GeometricMeanWithIsNaNForANaNValueAndNeverInfinite`        | Fact   | Geometric mean with is na n for a na n value and never infinite.         |
| `HarmonicMeanWithComputesTheDocumentedValue`                | Theory | Harmonic mean with computes the documented value.                        |
| `HarmonicMeanWithIsNaNForANaNValueAndNeverInfinite`         | Fact   | Harmonic mean with is na n for a na n value and never infinite.          |
| `ShareOfMaxComputesTheDocumentedValue`                      | Theory | Share of max computes the documented value.                              |
| `ShareOfMaxIsNaNForANaNValueAndNeverInfinite`               | Fact   | Share of max is na n for a na n value and never infinite.                |
| `ShareOfMaxTreatsANullArrayAsNoOthers`                      | Fact   | Share of max treats a null array as no others.                           |
| `HerfindahlWithComputesTheDocumentedValue`                  | Theory | Herfindahl with computes the documented value.                           |
| `HerfindahlWithIsNaNForANaNValueAndNeverInfinite`           | Fact   | Herfindahl with is na n for a na n value and never infinite.             |
| `HerfindahlWithTreatsANullArrayAsNoOthers`                  | Fact   | Herfindahl with treats a null array as no others.                        |
| `EntropyOfSharesWithComputesTheDocumentedValue`             | Theory | Entropy of shares with computes the documented value.                    |
| `EntropyOfSharesWithIsNaNForANaNValueAndNeverInfinite`      | Fact   | Entropy of shares with is na n for a na n value and never infinite.      |
| `EntropyOfSharesWithTreatsANullArrayAsNoOthers`             | Fact   | Entropy of shares with treats a null array as no others.                 |
| `HeadroomToComputesTheDocumentedValue`                      | Theory | Headroom to computes the documented value.                               |
| `HeadroomToIsNaNForANaNValueAndNeverInfinite`               | Fact   | Headroom to is na n for a na n value and never infinite.                 |
| `ExcessOverComputesTheDocumentedValue`                      | Theory | Excess over computes the documented value.                               |
| `ExcessOverIsNaNForANaNValueAndNeverInfinite`               | Fact   | Excess over is na n for a na n value and never infinite.                 |
| `ShortfallBelowComputesTheDocumentedValue`                  | Theory | Shortfall below computes the documented value.                           |
| `ShortfallBelowIsNaNForANaNValueAndNeverInfinite`           | Fact   | Shortfall below is na n for a na n value and never infinite.             |
| `DistanceToThresholdComputesTheDocumentedValue`             | Theory | Distance to threshold computes the documented value.                     |
| `DistanceToThresholdIsNaNForANaNValueAndNeverInfinite`      | Fact   | Distance to threshold is na n for a na n value and never infinite.       |
| `PercentBelowThresholdComputesTheDocumentedValue`           | Theory | Percent below threshold computes the documented value.                   |
| `PercentBelowThresholdIsNaNForANaNValueAndNeverInfinite`    | Fact   | Percent below threshold is na n for a na n value and never infinite.     |
| `ImbalanceWithComputesTheDocumentedValue`                   | Theory | Imbalance with computes the documented value.                            |
| `ImbalanceWithIsNaNForANaNValueAndNeverInfinite`            | Fact   | Imbalance with is na n for a na n value and never infinite.              |
| `RetentionAfterComputesTheDocumentedValue`                  | Theory | Retention after computes the documented value.                           |
| `RetentionAfterIsNaNForANaNValueAndNeverInfinite`           | Fact   | Retention after is na n for a na n value and never infinite.             |

### DoubleExtensionsTests

`Jube.Test.Dictionary.Extensions.DoubleExtensionsTests` — 75 tests. Category: Unit.

| Test                                                                                | Kind   | Verifies                                                                                        |
|-------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `AbsReturnsTheAbsoluteValue`                                                        | Theory | Abs returns the absolute value.                                                                 |
| `AbsOfNegativeInfinityIsPositiveInfinity`                                           | Fact   | Abs of negative infinity is positive infinity.                                                  |
| `AcosOfOneIsZero`                                                                   | Fact   | Acos of one is zero.                                                                            |
| `AcosOfAValueOutsideMinusOneToOneIsNaN`                                             | Fact   | Acos of a value outside minus one to one is na n.                                               |
| `AsinOfZeroIsZero`                                                                  | Fact   | Asin of zero is zero.                                                                           |
| `AsinOfAValueOutsideMinusOneToOneIsNaN`                                             | Fact   | Asin of a value outside minus one to one is na n.                                               |
| `Atan2MatchesMathAtan2ForAPointInEachQuadrant`                                      | Fact   | Atan2 matches math atan2 for a point in each quadrant.                                          |
| `AtanOfZeroIsZero`                                                                  | Fact   | Atan of zero is zero.                                                                           |
| `AtanOfPositiveInfinityIsHalfPi`                                                    | Fact   | Atan of positive infinity is half pi.                                                           |
| `BetweenIsExclusiveOfBothBounds`                                                    | Theory | Between is exclusive of both bounds.                                                            |
| `CeilingRoundsUpToTheSmallestIntegralValueNotLessThanTheInput`                      | Theory | Ceiling rounds up to the smallest integral value not less than the input.                       |
| `CosOfZeroIsOne`                                                                    | Fact   | Cos of zero is one.                                                                             |
| `CosOfPiIsMinusOne`                                                                 | Fact   | Cos of pi is minus one.                                                                         |
| `CoshOfZeroIsOne`                                                                   | Fact   | Cosh of zero is one.                                                                            |
| `ExpOfZeroIsOne`                                                                    | Fact   | Exp of zero is one.                                                                             |
| `ExpOfOneIsE`                                                                       | Fact   | Exp of one is e.                                                                                |
| `FloorRoundsDownToTheLargestIntegralValueNotGreaterThanTheInput`                    | Theory | Floor rounds down to the largest integral value not greater than the input.                     |
| `FromDaysConvertsToATimeSpanOfThatManyDays`                                         | Fact   | From days converts to a time span of that many days.                                            |
| `FromHoursConvertsToATimeSpanOfThatManyHours`                                       | Fact   | From hours converts to a time span of that many hours.                                          |
| `FromMillisecondsConvertsToATimeSpanOfThatManyMilliseconds`                         | Fact   | From milliseconds converts to a time span of that many milliseconds.                            |
| `FromMinutesConvertsToATimeSpanOfThatManyMinutes`                                   | Fact   | From minutes converts to a time span of that many minutes.                                      |
| `FromOaDateConvertsAnOleAutomationDateToADateTime`                                  | Fact   | From Oa date converts an ole automation date to a date time.                                    |
| `FromSecondsConvertsToATimeSpanOfThatManySeconds`                                   | Fact   | From seconds converts to a time span of that many seconds.                                      |
| `IeeeRemainderFollowsBankersRoundingOfTheQuotientNotTruncatedDivisionRemainder`     | Fact   | Ieee remainder follows bankers rounding of the quotient not truncated division remainder.       |
| `IeeeRemainderOfDivisionByZeroIsNaN`                                                | Fact   | Ieee remainder of division by zero is na n.                                                     |
| `InChecksMembershipAgainstTheProvidedValues`                                        | Theory | In checks membership against the provided values.                                               |
| `NotInIsTheInverseOfIn`                                                             | Theory | Not in is the inverse of in.                                                                    |
| `InRangeIsInclusiveOfBothBoundsUnlikeBetween`                                       | Theory | In range is inclusive of both bounds unlike between.                                            |
| `IsInfinityDetectsEitherSignOfInfinity`                                             | Theory | Is infinity detects either sign of infinity.                                                    |
| `IsNaNDetectsNotANumber`                                                            | Theory | Is na n detects not a number.                                                                   |
| `IsNegativeInfinityOnlyDetectsTheNegativeCase`                                      | Theory | Is negative infinity only detects the negative case.                                            |
| `IsPositiveInfinityOnlyDetectsThePositiveCase`                                      | Theory | Is positive infinity only detects the positive case.                                            |
| `Log10OfOneIsZero`                                                                  | Fact   | Log10 of one is zero.                                                                           |
| `Log10OfOneHundredIsTwo`                                                            | Fact   | Log10 of one hundred is two.                                                                    |
| `Log10OfZeroIsNegativeInfinity`                                                     | Fact   | Log10 of zero is negative infinity.                                                             |
| `Log10OfANegativeNumberIsNaN`                                                       | Fact   | Log10 of a negative number is na n.                                                             |
| `LogOfEIsOne`                                                                       | Fact   | Log of e is one.                                                                                |
| `LogWithAnExplicitBaseMatchesThatBasesLogarithm`                                    | Fact   | Log with an explicit base matches that bases logarithm.                                         |
| `MaxReturnsTheLargerOfTwoValues`                                                    | Theory | Max returns the larger of two values.                                                           |
| `MinReturnsTheSmallerOfTwoValues`                                                   | Theory | Min returns the smaller of two values.                                                          |
| `MaxWithNaNAlwaysReturnsNaN`                                                        | Fact   | Max with na n always returns na n.                                                              |
| `PowRaisesTheFirstOperandToTheSecond`                                               | Theory | Pow raises the first operand to the second.                                                     |
| `RoundWithNoDigitsUsesBankersRoundingOnMidpoints`                                   | Theory | Round with no digits uses bankers rounding on midpoints.                                        |
| `RoundWithDigitsRoundsToThatManyFractionalDigits`                                   | Fact   | Round with digits rounds to that many fractional digits.                                        |
| `RoundWithAMidpointRoundingModeHonoursThatMode`                                     | Theory | Round with a midpoint rounding mode honours that mode.                                          |
| `RoundWithDigitsAndModeCombinesBothBehaviours`                                      | Fact   | Round with digits and mode combines both behaviours.                                            |
| `SignReturnsMinusOneZeroOrOne`                                                      | Theory | Sign returns minus one zero or one.                                                             |
| `SinOfZeroIsZero`                                                                   | Fact   | Sin of zero is zero.                                                                            |
| `SinOfHalfPiIsOne`                                                                  | Fact   | Sin of half pi is one.                                                                          |
| `SinhOfZeroIsZero`                                                                  | Fact   | Sinh of zero is zero.                                                                           |
| `SqrtReturnsThePositiveSquareRoot`                                                  | Theory | Sqrt returns the positive square root.                                                          |
| `SqrtOfANegativeNumberIsNaN`                                                        | Fact   | Sqrt of a negative number is na n.                                                              |
| `TanOfZeroIsZero`                                                                   | Fact   | Tan of zero is zero.                                                                            |
| `TanhOfZeroIsZero`                                                                  | Fact   | Tanh of zero is zero.                                                                           |
| `TanhSaturatesToPlusOrMinusOneAtInfinity`                                           | Fact   | Tanh saturates to plus or minus one at infinity.                                                |
| `ToMoneyRoundsToTwoDecimalPlacesUsingBankersRoundingJustLikeMathRound`              | Theory | To money rounds to two decimal places using bankers rounding just like math round.              |
| `TruncateDiscardsTheFractionalPartTowardZero`                                       | Theory | Truncate discards the fractional part toward zero.                                              |
| `HaversineDistanceKilometersOfAPointToItselfIsZero`                                 | Fact   | Haversine distance kilometers of a point to itself is zero.                                     |
| `HaversineDistanceKilometersIsSymmetric`                                            | Fact   | Haversine distance kilometers is symmetric.                                                     |
| `HaversineDistanceKilometersOfOneDegreeOfLatitudeIsAboutOneHundredElevenKilometers` | Fact   | Haversine distance kilometers of one degree of latitude is about one hundred eleven kilometers. |
| `HaversineDistanceMilesAgreesWithKilometersUsingTheStandardConversionFactor`        | Fact   | Haversine distance miles agrees with kilometers using the standard conversion factor.           |
| `ImpliedTravelSpeedKmhDividesHaversineDistanceByHoursElapsed`                       | Fact   | Implied travel speed kmh divides haversine distance by hours elapsed.                           |
| `ImpliedTravelSpeedKmhWithZeroOrNegativeHoursReturnsPositiveInfinity`               | Theory | Implied travel speed kmh with zero or negative hours returns positive infinity.                 |
| `ClampRestrictsTheValueToTheGivenRange`                                             | Theory | Clamp restricts the value to the given range.                                                   |
| `LerpInterpolatesBetweenThisAndEnd`                                                 | Theory | Lerp interpolates between this and end.                                                         |
| `PercentageChangeFromComputesRelativeChange`                                        | Theory | Percentage change from computes relative change.                                                |
| `PercentageChangeFromZeroPreviousValueIsPositiveInfinityWhenCurrentIsPositive`      | Fact   | Percentage change from zero previous value is positive infinity when current is positive.       |
| `PercentageChangeFromZeroToZeroIsNaN`                                               | Fact   | Percentage change from zero to zero is na n.                                                    |
| `ZScoreMeasuresStandardDeviationsFromTheMean`                                       | Theory | Z score measures standard deviations from the mean.                                             |
| `IsRoundAmountChecksExactMultiples`                                                 | Theory | Is round amount checks exact multiples.                                                         |
| `IsRoundAmountWithNonPositiveNearestReturnsFalse`                                   | Fact   | Is round amount with non positive nearest returns false.                                        |
| `IsJustBelowThresholdFlagsAmountsWithinTheMarginBelowTheThreshold`                  | Theory | Is just below threshold flags amounts within the margin below the threshold.                    |
| `IsWithinRadiusKmDelegatesToHaversineDistance`                                      | Theory | Is within radius km delegates to haversine distance.                                            |
| `LeadingSignificantDigitExtractsTheFirstNonZeroDigit`                               | Theory | Leading significant digit extracts the first non zero digit.                                    |
| `IsWithinPercentOfChecksTheAbsolutePercentageChange`                                | Theory | Is within percent of checks the absolute percentage change.                                     |

### FlowAuditTests

`Jube.Test.Dictionary.Extensions.FlowAuditTests` — 27 tests. Category: Unit.

| Test                                                                | Kind   | Verifies                                                                       |
|---------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `ADefaultFlowIsUndecidedAndConvertsFalse`                           | Fact   | A default flow is undecided and converts false.                                |
| `ADefaultFlowCanStillBeDecided`                                     | Fact   | A default flow can still be decided.                                           |
| `EveryParseStepLeavesADecidedFlowDecidedWithoutParsing`             | Fact   | Every parse step leaves a decided flow decided without parsing.                |
| `ParseStepsKeepTheScoreAndLabelOfTheFlow`                           | Fact   | Parse steps keep the score and label of the flow.                              |
| `AFailedParseKeepsTheScoreButRecordsTheReason`                      | Fact   | A failed parse keeps the score but records the reason.                         |
| `RawTransformersBeginAPipelineOrReturnAPlainNumberWithoutStart`     | Fact   | Raw transformers begin a pipeline or return a plain number without start.      |
| `RawTransformersFailClosedOnBadInput`                               | Fact   | Raw transformers fail closed on bad input.                                     |
| `OrNaNAndOrZeroPassAValueThrough`                                   | Theory | Or na n and or zero pass a value through.                                      |
| `ANullNullableIsNaNForOrNaNAndZeroForOrZero`                        | Fact   | A null nullable is na n for or na n and zero for or zero.                      |
| `ANonFiniteNullableIsNaNForOrNaNAndZeroForOrZero`                   | Fact   | A non finite nullable is na n for or na n and zero for or zero.                |
| `OrBetweenTwoPipelinesIsTrueWhenEitherMatched`                      | Fact   | Or between two pipelines is true when either matched.                          |
| `AndBetweenTwoPipelinesIsTrueOnlyWhenBothMatched`                   | Fact   | And between two pipelines is true only when both matched.                      |
| `OperatorsAcceptPipelinesOfDifferentTypesThroughTheBooleanForm`     | Fact   | Operators accept pipelines of different types through the boolean form.        |
| `AnUndecidedRejectedBrokenOrErroredPipelineIsFalseInACombination`   | Fact   | An undecided rejected broken or errored pipeline is false in a combination.    |
| `NotIsTrueForEverythingExceptAMatchedPipeline`                      | Fact   | Not is true for everything except a matched pipeline.                          |
| `PlainArithmeticOnANumberReturnsANumberSoItFitsANumericRule`        | Fact   | Plain arithmetic on a number returns a number so it fits a numeric rule.       |
| `ToDoubleOrNaNReadsInvariantNumbers`                                | Theory | To double or na n reads invariant numbers.                                     |
| `ToDoubleOrNaNIsNaNForAnythingThatIsNotAFiniteNumber`               | Theory | To double or na n is na n for anything that is not a finite number.            |
| `ToNumberReadsTheValueOfANumericFlowAndIsNaNWhenErrored`            | Fact   | To number reads the value of a numeric flow and is na n when errored.          |
| `APathologicalRegexTimesOutQuicklyAndIsFalseNotAHang`               | Fact   | A pathological regex times out quickly and is false not a hang.                |
| `ARegexThatIsInvalidIsFalseNotAnException`                          | Fact   | A regex that is invalid is false not an exception.                             |
| `EveryFoldedLetterMapsToItsPlainForm`                               | Theory | Every folded letter maps to its plain form.                                    |
| `AMoreThanTheCacheLimitOfDistinctOptionStringsStillParsesCorrectly` | Fact   | A more than the cache limit of distinct option strings still parses correctly. |
| `ATimeZoneWithDaylightSavingDoesNotShiftAWinterHour`                | Fact   | A time zone with daylight saving does not shift a winter hour.                 |
| `AZoneBoundaryCrossingMidnightChangesTheWeekday`                    | Fact   | A zone boundary crossing midnight changes the weekday.                         |
| `ALocalKindValueIsConvertedViaUniversalTimeForZoneSteps`            | Fact   | A local kind value is converted via universal time for zone steps.             |
| `AnUnspecifiedAndAUtcValueAreTreatedTheSameByZoneSteps`             | Fact   | An unspecified and a utc value are treated the same by zone steps.             |

### FlowBooleanTests

`Jube.Test.Dictionary.Extensions.FlowBooleanTests` — 2 tests. Category: Unit.

| Test                                      | Kind   | Verifies                                         |
|-------------------------------------------|--------|--------------------------------------------------|
| `IsTrueAppliesTheOutcomeOfEveryStepKind`  | Theory | Is true applies the outcome of every step kind.  |
| `IsFalseAppliesTheOutcomeOfEveryStepKind` | Theory | Is false applies the outcome of every step kind. |

### FlowDateTimeTests

`Jube.Test.Dictionary.Extensions.FlowDateTimeTests` — 27 tests. Category: Unit.

| Test                                                                | Kind   | Verifies                                                                       |
|---------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `IsWeekdayAppliesTheOutcomeOfEveryStepKind`                         | Theory | Is weekday applies the outcome of every step kind.                             |
| `IsWeekendAppliesTheOutcomeOfEveryStepKind`                         | Theory | Is weekend applies the outcome of every step kind.                             |
| `IsMorningAppliesTheOutcomeOfEveryStepKind`                         | Theory | Is morning applies the outcome of every step kind.                             |
| `IsAfternoonAppliesTheOutcomeOfEveryStepKind`                       | Theory | Is afternoon applies the outcome of every step kind.                           |
| `BeforeAppliesTheOutcomeOfEveryStepKind`                            | Theory | Before applies the outcome of every step kind.                                 |
| `AfterAppliesTheOutcomeOfEveryStepKind`                             | Theory | After applies the outcome of every step kind.                                  |
| `BetweenAppliesTheOutcomeOfEveryStepKind`                           | Theory | Between applies the outcome of every step kind.                                |
| `InRangeAppliesTheOutcomeOfEveryStepKind`                           | Theory | In range applies the outcome of every step kind.                               |
| `SameDayAsAppliesTheOutcomeOfEveryStepKind`                         | Theory | Same day as applies the outcome of every step kind.                            |
| `WithinBusinessHoursAppliesTheOutcomeOfEveryStepKind`               | Theory | Within business hours applies the outcome of every step kind.                  |
| `OutsideBusinessHoursAppliesTheOutcomeOfEveryStepKind`              | Theory | Outside business hours applies the outcome of every step kind.                 |
| `WithinMinutesOfAppliesTheOutcomeOfEveryStepKind`                   | Theory | Within minutes of applies the outcome of every step kind.                      |
| `WithinHoursOfAppliesTheOutcomeOfEveryStepKind`                     | Theory | Within hours of applies the outcome of every step kind.                        |
| `WithinDaysOfAppliesTheOutcomeOfEveryStepKind`                      | Theory | Within days of applies the outcome of every step kind.                         |
| `MoreThanMinutesAfterAppliesTheOutcomeOfEveryStepKind`              | Theory | More than minutes after applies the outcome of every step kind.                |
| `MoreThanDaysAfterAppliesTheOutcomeOfEveryStepKind`                 | Theory | More than days after applies the outcome of every step kind.                   |
| `DayOfWeekInAppliesTheOutcomeOfEveryStepKind`                       | Theory | Day of week in applies the outcome of every step kind.                         |
| `IsStartOfMonthAppliesTheOutcomeOfEveryStepKind`                    | Theory | Is start of month applies the outcome of every step kind.                      |
| `IsEndOfMonthAppliesTheOutcomeOfEveryStepKind`                      | Theory | Is end of month applies the outcome of every step kind.                        |
| `WithinBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind`         | Theory | Within business hours in zone applies the outcome of every step kind.          |
| `OutsideBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind`        | Theory | Outside business hours in zone applies the outcome of every step kind.         |
| `WithinWeekdayBusinessHoursAppliesTheOutcomeOfEveryStepKind`        | Theory | Within weekday business hours applies the outcome of every step kind.          |
| `OutsideWeekdayBusinessHoursAppliesTheOutcomeOfEveryStepKind`       | Theory | Outside weekday business hours applies the outcome of every step kind.         |
| `WithinWeekdayBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind`  | Theory | Within weekday business hours in zone applies the outcome of every step kind.  |
| `OutsideWeekdayBusinessHoursInZoneAppliesTheOutcomeOfEveryStepKind` | Theory | Outside weekday business hours in zone applies the outcome of every step kind. |
| `IsWeekdayInZoneAppliesTheOutcomeOfEveryStepKind`                   | Theory | Is weekday in zone applies the outcome of every step kind.                     |
| `IsWeekendInZoneAppliesTheOutcomeOfEveryStepKind`                   | Theory | Is weekend in zone applies the outcome of every step kind.                     |

### FlowDoubleTests

`Jube.Test.Dictionary.Extensions.FlowDoubleTests` — 196 tests. Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                          |
|------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `GreaterAppliesTheOutcomeOfEveryStepKind`                              | Theory | Greater applies the outcome of every step kind.                                   |
| `GreaterOrEqualAppliesTheOutcomeOfEveryStepKind`                       | Theory | Greater or equal applies the outcome of every step kind.                          |
| `LessAppliesTheOutcomeOfEveryStepKind`                                 | Theory | Less applies the outcome of every step kind.                                      |
| `LessOrEqualAppliesTheOutcomeOfEveryStepKind`                          | Theory | Less or equal applies the outcome of every step kind.                             |
| `EqualAppliesTheOutcomeOfEveryStepKind`                                | Theory | Equal applies the outcome of every step kind.                                     |
| `NotEqualAppliesTheOutcomeOfEveryStepKind`                             | Theory | Not equal applies the outcome of every step kind.                                 |
| `BetweenAppliesTheOutcomeOfEveryStepKind`                              | Theory | Between applies the outcome of every step kind.                                   |
| `InRangeAppliesTheOutcomeOfEveryStepKind`                              | Theory | In range applies the outcome of every step kind.                                  |
| `OutsideRangeAppliesTheOutcomeOfEveryStepKind`                         | Theory | Outside range applies the outcome of every step kind.                             |
| `InAppliesTheOutcomeOfEveryStepKind`                                   | Theory | In applies the outcome of every step kind.                                        |
| `NotInAppliesTheOutcomeOfEveryStepKind`                                | Theory | Not in applies the outcome of every step kind.                                    |
| `IsZeroAppliesTheOutcomeOfEveryStepKind`                               | Theory | Is zero applies the outcome of every step kind.                                   |
| `IsPositiveAppliesTheOutcomeOfEveryStepKind`                           | Theory | Is positive applies the outcome of every step kind.                               |
| `IsNegativeAppliesTheOutcomeOfEveryStepKind`                           | Theory | Is negative applies the outcome of every step kind.                               |
| `IsNaNAppliesTheOutcomeOfEveryStepKind`                                | Theory | Is na n applies the outcome of every step kind.                                   |
| `IsFiniteAppliesTheOutcomeOfEveryStepKind`                             | Theory | Is finite applies the outcome of every step kind.                                 |
| `IsWholeNumberAppliesTheOutcomeOfEveryStepKind`                        | Theory | Is whole number applies the outcome of every step kind.                           |
| `IsMultipleOfAppliesTheOutcomeOfEveryStepKind`                         | Theory | Is multiple of applies the outcome of every step kind.                            |
| `WithinPercentOfAppliesTheOutcomeOfEveryStepKind`                      | Theory | Within percent of applies the outcome of every step kind.                         |
| `DeviatesFromByPercentAppliesTheOutcomeOfEveryStepKind`                | Theory | Deviates from by percent applies the outcome of every step kind.                  |
| `IncreasedByMoreThanPercentAppliesTheOutcomeOfEveryStepKind`           | Theory | Increased by more than percent applies the outcome of every step kind.            |
| `DecreasedByMoreThanPercentAppliesTheOutcomeOfEveryStepKind`           | Theory | Decreased by more than percent applies the outcome of every step kind.            |
| `ZScoreAboveAppliesTheOutcomeOfEveryStepKind`                          | Theory | Z score above applies the outcome of every step kind.                             |
| `ZScoreBelowAppliesTheOutcomeOfEveryStepKind`                          | Theory | Z score below applies the outcome of every step kind.                             |
| `ZScoreOutsideAppliesTheOutcomeOfEveryStepKind`                        | Theory | Z score outside applies the outcome of every step kind.                           |
| `RatioAboveAppliesTheOutcomeOfEveryStepKind`                           | Theory | Ratio above applies the outcome of every step kind.                               |
| `RatioBelowAppliesTheOutcomeOfEveryStepKind`                           | Theory | Ratio below applies the outcome of every step kind.                               |
| `IsJustBelowThresholdAppliesTheOutcomeOfEveryStepKind`                 | Theory | Is just below threshold applies the outcome of every step kind.                   |
| `IsJustAboveThresholdAppliesTheOutcomeOfEveryStepKind`                 | Theory | Is just above threshold applies the outcome of every step kind.                   |
| `IsRoundAmountAppliesTheOutcomeOfEveryStepKind`                        | Theory | Is round amount applies the outcome of every step kind.                           |
| `WithinRadiusKmAppliesTheOutcomeOfEveryStepKind`                       | Theory | Within radius km applies the outcome of every step kind.                          |
| `OutsideRadiusKmAppliesTheOutcomeOfEveryStepKind`                      | Theory | Outside radius km applies the outcome of every step kind.                         |
| `DistanceKmAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Distance km above applies the outcome of every step kind.                         |
| `DistanceKmBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Distance km below applies the outcome of every step kind.                         |
| `ImpliedSpeedAboveAppliesTheOutcomeOfEveryStepKind`                    | Theory | Implied speed above applies the outcome of every step kind.                       |
| `ImpliedSpeedBelowAppliesTheOutcomeOfEveryStepKind`                    | Theory | Implied speed below applies the outcome of every step kind.                       |
| `HasValueAppliesTheOutcomeOfEveryStepKind`                             | Theory | Has value applies the outcome of every step kind.                                 |
| `HasNoValueAppliesTheOutcomeOfEveryStepKind`                           | Theory | Has no value applies the outcome of every step kind.                              |
| `RatioInRangeAppliesTheOutcomeOfEveryStepKind`                         | Theory | Ratio in range applies the outcome of every step kind.                            |
| `RatioOutsideRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Ratio outside range applies the outcome of every step kind.                       |
| `IsJustBelowAnyThresholdAppliesTheOutcomeOfEveryStepKind`              | Theory | Is just below any threshold applies the outcome of every step kind.               |
| `IsJustAboveAnyThresholdAppliesTheOutcomeOfEveryStepKind`              | Theory | Is just above any threshold applies the outcome of every step kind.               |
| `WithinRadiusMilesAppliesTheOutcomeOfEveryStepKind`                    | Theory | Within radius miles applies the outcome of every step kind.                       |
| `OutsideRadiusMilesAppliesTheOutcomeOfEveryStepKind`                   | Theory | Outside radius miles applies the outcome of every step kind.                      |
| `DistanceMilesAboveAppliesTheOutcomeOfEveryStepKind`                   | Theory | Distance miles above applies the outcome of every step kind.                      |
| `DistanceMilesBelowAppliesTheOutcomeOfEveryStepKind`                   | Theory | Distance miles below applies the outcome of every step kind.                      |
| `ImpliedSpeedMphAboveAppliesTheOutcomeOfEveryStepKind`                 | Theory | Implied speed mph above applies the outcome of every step kind.                   |
| `ImpliedSpeedMphBelowAppliesTheOutcomeOfEveryStepKind`                 | Theory | Implied speed mph below applies the outcome of every step kind.                   |
| `RatioOfAboveAppliesTheOutcomeOfEveryStepKind`                         | Theory | Ratio of above applies the outcome of every step kind.                            |
| `RatioOfBelowAppliesTheOutcomeOfEveryStepKind`                         | Theory | Ratio of below applies the outcome of every step kind.                            |
| `RatioOfInRangeAppliesTheOutcomeOfEveryStepKind`                       | Theory | Ratio of in range applies the outcome of every step kind.                         |
| `RatioOfOutsideRangeAppliesTheOutcomeOfEveryStepKind`                  | Theory | Ratio of outside range applies the outcome of every step kind.                    |
| `PercentOfAboveAppliesTheOutcomeOfEveryStepKind`                       | Theory | Percent of above applies the outcome of every step kind.                          |
| `PercentOfBelowAppliesTheOutcomeOfEveryStepKind`                       | Theory | Percent of below applies the outcome of every step kind.                          |
| `PercentOfInRangeAppliesTheOutcomeOfEveryStepKind`                     | Theory | Percent of in range applies the outcome of every step kind.                       |
| `PercentOfOutsideRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Percent of outside range applies the outcome of every step kind.                  |
| `ShareOfSumWithAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Share of sum with above applies the outcome of every step kind.                   |
| `ShareOfSumWithBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Share of sum with below applies the outcome of every step kind.                   |
| `ShareOfSumWithInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Share of sum with in range applies the outcome of every step kind.                |
| `ShareOfSumWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Share of sum with outside range applies the outcome of every step kind.           |
| `ComplementAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Complement above applies the outcome of every step kind.                          |
| `ComplementBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Complement below applies the outcome of every step kind.                          |
| `ComplementInRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Complement in range applies the outcome of every step kind.                       |
| `ComplementOutsideRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Complement outside range applies the outcome of every step kind.                  |
| `LogRatioOfAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Log ratio of above applies the outcome of every step kind.                        |
| `LogRatioOfBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Log ratio of below applies the outcome of every step kind.                        |
| `LogRatioOfInRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Log ratio of in range applies the outcome of every step kind.                     |
| `LogRatioOfOutsideRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Log ratio of outside range applies the outcome of every step kind.                |
| `RatioOfRatiosAboveAppliesTheOutcomeOfEveryStepKind`                   | Theory | Ratio of ratios above applies the outcome of every step kind.                     |
| `RatioOfRatiosBelowAppliesTheOutcomeOfEveryStepKind`                   | Theory | Ratio of ratios below applies the outcome of every step kind.                     |
| `RatioOfRatiosInRangeAppliesTheOutcomeOfEveryStepKind`                 | Theory | Ratio of ratios in range applies the outcome of every step kind.                  |
| `RatioOfRatiosOutsideRangeAppliesTheOutcomeOfEveryStepKind`            | Theory | Ratio of ratios outside range applies the outcome of every step kind.             |
| `InverseRatioOfAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Inverse ratio of above applies the outcome of every step kind.                    |
| `InverseRatioOfBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Inverse ratio of below applies the outcome of every step kind.                    |
| `InverseRatioOfInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Inverse ratio of in range applies the outcome of every step kind.                 |
| `InverseRatioOfOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Inverse ratio of outside range applies the outcome of every step kind.            |
| `DifferenceFromAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Difference from above applies the outcome of every step kind.                     |
| `DifferenceFromBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Difference from below applies the outcome of every step kind.                     |
| `DifferenceFromInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Difference from in range applies the outcome of every step kind.                  |
| `DifferenceFromOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Difference from outside range applies the outcome of every step kind.             |
| `AbsoluteDifferenceFromAboveAppliesTheOutcomeOfEveryStepKind`          | Theory | Absolute difference from above applies the outcome of every step kind.            |
| `AbsoluteDifferenceFromBelowAppliesTheOutcomeOfEveryStepKind`          | Theory | Absolute difference from below applies the outcome of every step kind.            |
| `AbsoluteDifferenceFromInRangeAppliesTheOutcomeOfEveryStepKind`        | Theory | Absolute difference from in range applies the outcome of every step kind.         |
| `AbsoluteDifferenceFromOutsideRangeAppliesTheOutcomeOfEveryStepKind`   | Theory | Absolute difference from outside range applies the outcome of every step kind.    |
| `PercentDifferenceFromAboveAppliesTheOutcomeOfEveryStepKind`           | Theory | Percent difference from above applies the outcome of every step kind.             |
| `PercentDifferenceFromBelowAppliesTheOutcomeOfEveryStepKind`           | Theory | Percent difference from below applies the outcome of every step kind.             |
| `PercentDifferenceFromInRangeAppliesTheOutcomeOfEveryStepKind`         | Theory | Percent difference from in range applies the outcome of every step kind.          |
| `PercentDifferenceFromOutsideRangeAppliesTheOutcomeOfEveryStepKind`    | Theory | Percent difference from outside range applies the outcome of every step kind.     |
| `RelativeChangeFromAboveAppliesTheOutcomeOfEveryStepKind`              | Theory | Relative change from above applies the outcome of every step kind.                |
| `RelativeChangeFromBelowAppliesTheOutcomeOfEveryStepKind`              | Theory | Relative change from below applies the outcome of every step kind.                |
| `RelativeChangeFromInRangeAppliesTheOutcomeOfEveryStepKind`            | Theory | Relative change from in range applies the outcome of every step kind.             |
| `RelativeChangeFromOutsideRangeAppliesTheOutcomeOfEveryStepKind`       | Theory | Relative change from outside range applies the outcome of every step kind.        |
| `GrowthFactorFromAboveAppliesTheOutcomeOfEveryStepKind`                | Theory | Growth factor from above applies the outcome of every step kind.                  |
| `GrowthFactorFromBelowAppliesTheOutcomeOfEveryStepKind`                | Theory | Growth factor from below applies the outcome of every step kind.                  |
| `GrowthFactorFromInRangeAppliesTheOutcomeOfEveryStepKind`              | Theory | Growth factor from in range applies the outcome of every step kind.               |
| `GrowthFactorFromOutsideRangeAppliesTheOutcomeOfEveryStepKind`         | Theory | Growth factor from outside range applies the outcome of every step kind.          |
| `SlopeFromAboveAppliesTheOutcomeOfEveryStepKind`                       | Theory | Slope from above applies the outcome of every step kind.                          |
| `SlopeFromBelowAppliesTheOutcomeOfEveryStepKind`                       | Theory | Slope from below applies the outcome of every step kind.                          |
| `SlopeFromInRangeAppliesTheOutcomeOfEveryStepKind`                     | Theory | Slope from in range applies the outcome of every step kind.                       |
| `SlopeFromOutsideRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Slope from outside range applies the outcome of every step kind.                  |
| `CompoundGrowthRateAboveAppliesTheOutcomeOfEveryStepKind`              | Theory | Compound growth rate above applies the outcome of every step kind.                |
| `CompoundGrowthRateBelowAppliesTheOutcomeOfEveryStepKind`              | Theory | Compound growth rate below applies the outcome of every step kind.                |
| `CompoundGrowthRateInRangeAppliesTheOutcomeOfEveryStepKind`            | Theory | Compound growth rate in range applies the outcome of every step kind.             |
| `CompoundGrowthRateOutsideRangeAppliesTheOutcomeOfEveryStepKind`       | Theory | Compound growth rate outside range applies the outcome of every step kind.        |
| `PercentDeviationFromMeanAboveAppliesTheOutcomeOfEveryStepKind`        | Theory | Percent deviation from mean above applies the outcome of every step kind.         |
| `PercentDeviationFromMeanBelowAppliesTheOutcomeOfEveryStepKind`        | Theory | Percent deviation from mean below applies the outcome of every step kind.         |
| `PercentDeviationFromMeanInRangeAppliesTheOutcomeOfEveryStepKind`      | Theory | Percent deviation from mean in range applies the outcome of every step kind.      |
| `PercentDeviationFromMeanOutsideRangeAppliesTheOutcomeOfEveryStepKind` | Theory | Percent deviation from mean outside range applies the outcome of every step kind. |
| `ModifiedZScoreAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Modified z score above applies the outcome of every step kind.                    |
| `ModifiedZScoreBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Modified z score below applies the outcome of every step kind.                    |
| `ModifiedZScoreInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Modified z score in range applies the outcome of every step kind.                 |
| `ModifiedZScoreOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Modified z score outside range applies the outcome of every step kind.            |
| `CoefficientOfVariationAboveAppliesTheOutcomeOfEveryStepKind`          | Theory | Coefficient of variation above applies the outcome of every step kind.            |
| `CoefficientOfVariationBelowAppliesTheOutcomeOfEveryStepKind`          | Theory | Coefficient of variation below applies the outcome of every step kind.            |
| `CoefficientOfVariationInRangeAppliesTheOutcomeOfEveryStepKind`        | Theory | Coefficient of variation in range applies the outcome of every step kind.         |
| `CoefficientOfVariationOutsideRangeAppliesTheOutcomeOfEveryStepKind`   | Theory | Coefficient of variation outside range applies the outcome of every step kind.    |
| `MinMaxNormaliseAboveAppliesTheOutcomeOfEveryStepKind`                 | Theory | Min max normalise above applies the outcome of every step kind.                   |
| `MinMaxNormaliseBelowAppliesTheOutcomeOfEveryStepKind`                 | Theory | Min max normalise below applies the outcome of every step kind.                   |
| `MinMaxNormaliseInRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Min max normalise in range applies the outcome of every step kind.                |
| `MinMaxNormaliseOutsideRangeAppliesTheOutcomeOfEveryStepKind`          | Theory | Min max normalise outside range applies the outcome of every step kind.           |
| `PercentOfRangeAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Percent of range above applies the outcome of every step kind.                    |
| `PercentOfRangeBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Percent of range below applies the outcome of every step kind.                    |
| `PercentOfRangeInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Percent of range in range applies the outcome of every step kind.                 |
| `PercentOfRangeOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Percent of range outside range applies the outcome of every step kind.            |
| `SumWithAboveAppliesTheOutcomeOfEveryStepKind`                         | Theory | Sum with above applies the outcome of every step kind.                            |
| `SumWithBelowAppliesTheOutcomeOfEveryStepKind`                         | Theory | Sum with below applies the outcome of every step kind.                            |
| `SumWithInRangeAppliesTheOutcomeOfEveryStepKind`                       | Theory | Sum with in range applies the outcome of every step kind.                         |
| `SumWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`                  | Theory | Sum with outside range applies the outcome of every step kind.                    |
| `MeanWithAboveAppliesTheOutcomeOfEveryStepKind`                        | Theory | Mean with above applies the outcome of every step kind.                           |
| `MeanWithBelowAppliesTheOutcomeOfEveryStepKind`                        | Theory | Mean with below applies the outcome of every step kind.                           |
| `MeanWithInRangeAppliesTheOutcomeOfEveryStepKind`                      | Theory | Mean with in range applies the outcome of every step kind.                        |
| `MeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`                 | Theory | Mean with outside range applies the outcome of every step kind.                   |
| `MaxWithAboveAppliesTheOutcomeOfEveryStepKind`                         | Theory | Max with above applies the outcome of every step kind.                            |
| `MaxWithBelowAppliesTheOutcomeOfEveryStepKind`                         | Theory | Max with below applies the outcome of every step kind.                            |
| `MaxWithInRangeAppliesTheOutcomeOfEveryStepKind`                       | Theory | Max with in range applies the outcome of every step kind.                         |
| `MaxWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`                  | Theory | Max with outside range applies the outcome of every step kind.                    |
| `MinWithAboveAppliesTheOutcomeOfEveryStepKind`                         | Theory | Min with above applies the outcome of every step kind.                            |
| `MinWithBelowAppliesTheOutcomeOfEveryStepKind`                         | Theory | Min with below applies the outcome of every step kind.                            |
| `MinWithInRangeAppliesTheOutcomeOfEveryStepKind`                       | Theory | Min with in range applies the outcome of every step kind.                         |
| `MinWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`                  | Theory | Min with outside range applies the outcome of every step kind.                    |
| `SpreadWithAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Spread with above applies the outcome of every step kind.                         |
| `SpreadWithBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Spread with below applies the outcome of every step kind.                         |
| `SpreadWithInRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Spread with in range applies the outcome of every step kind.                      |
| `SpreadWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Spread with outside range applies the outcome of every step kind.                 |
| `WeightedMeanWithAboveAppliesTheOutcomeOfEveryStepKind`                | Theory | Weighted mean with above applies the outcome of every step kind.                  |
| `WeightedMeanWithBelowAppliesTheOutcomeOfEveryStepKind`                | Theory | Weighted mean with below applies the outcome of every step kind.                  |
| `WeightedMeanWithInRangeAppliesTheOutcomeOfEveryStepKind`              | Theory | Weighted mean with in range applies the outcome of every step kind.               |
| `WeightedMeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`         | Theory | Weighted mean with outside range applies the outcome of every step kind.          |
| `GeometricMeanWithAboveAppliesTheOutcomeOfEveryStepKind`               | Theory | Geometric mean with above applies the outcome of every step kind.                 |
| `GeometricMeanWithBelowAppliesTheOutcomeOfEveryStepKind`               | Theory | Geometric mean with below applies the outcome of every step kind.                 |
| `GeometricMeanWithInRangeAppliesTheOutcomeOfEveryStepKind`             | Theory | Geometric mean with in range applies the outcome of every step kind.              |
| `GeometricMeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`        | Theory | Geometric mean with outside range applies the outcome of every step kind.         |
| `HarmonicMeanWithAboveAppliesTheOutcomeOfEveryStepKind`                | Theory | Harmonic mean with above applies the outcome of every step kind.                  |
| `HarmonicMeanWithBelowAppliesTheOutcomeOfEveryStepKind`                | Theory | Harmonic mean with below applies the outcome of every step kind.                  |
| `HarmonicMeanWithInRangeAppliesTheOutcomeOfEveryStepKind`              | Theory | Harmonic mean with in range applies the outcome of every step kind.               |
| `HarmonicMeanWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`         | Theory | Harmonic mean with outside range applies the outcome of every step kind.          |
| `ShareOfMaxAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Share of max above applies the outcome of every step kind.                        |
| `ShareOfMaxBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Share of max below applies the outcome of every step kind.                        |
| `ShareOfMaxInRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Share of max in range applies the outcome of every step kind.                     |
| `ShareOfMaxOutsideRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Share of max outside range applies the outcome of every step kind.                |
| `HerfindahlWithAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Herfindahl with above applies the outcome of every step kind.                     |
| `HerfindahlWithBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Herfindahl with below applies the outcome of every step kind.                     |
| `HerfindahlWithInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Herfindahl with in range applies the outcome of every step kind.                  |
| `HerfindahlWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Herfindahl with outside range applies the outcome of every step kind.             |
| `EntropyOfSharesWithAboveAppliesTheOutcomeOfEveryStepKind`             | Theory | Entropy of shares with above applies the outcome of every step kind.              |
| `EntropyOfSharesWithBelowAppliesTheOutcomeOfEveryStepKind`             | Theory | Entropy of shares with below applies the outcome of every step kind.              |
| `EntropyOfSharesWithInRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Entropy of shares with in range applies the outcome of every step kind.           |
| `EntropyOfSharesWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`      | Theory | Entropy of shares with outside range applies the outcome of every step kind.      |
| `HeadroomToAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Headroom to above applies the outcome of every step kind.                         |
| `HeadroomToBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Headroom to below applies the outcome of every step kind.                         |
| `HeadroomToInRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Headroom to in range applies the outcome of every step kind.                      |
| `HeadroomToOutsideRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Headroom to outside range applies the outcome of every step kind.                 |
| `ExcessOverAboveAppliesTheOutcomeOfEveryStepKind`                      | Theory | Excess over above applies the outcome of every step kind.                         |
| `ExcessOverBelowAppliesTheOutcomeOfEveryStepKind`                      | Theory | Excess over below applies the outcome of every step kind.                         |
| `ExcessOverInRangeAppliesTheOutcomeOfEveryStepKind`                    | Theory | Excess over in range applies the outcome of every step kind.                      |
| `ExcessOverOutsideRangeAppliesTheOutcomeOfEveryStepKind`               | Theory | Excess over outside range applies the outcome of every step kind.                 |
| `ShortfallBelowAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Shortfall below above applies the outcome of every step kind.                     |
| `ShortfallBelowBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Shortfall below below applies the outcome of every step kind.                     |
| `ShortfallBelowInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Shortfall below in range applies the outcome of every step kind.                  |
| `ShortfallBelowOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Shortfall below outside range applies the outcome of every step kind.             |
| `DistanceToThresholdAboveAppliesTheOutcomeOfEveryStepKind`             | Theory | Distance to threshold above applies the outcome of every step kind.               |
| `DistanceToThresholdBelowAppliesTheOutcomeOfEveryStepKind`             | Theory | Distance to threshold below applies the outcome of every step kind.               |
| `DistanceToThresholdInRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Distance to threshold in range applies the outcome of every step kind.            |
| `DistanceToThresholdOutsideRangeAppliesTheOutcomeOfEveryStepKind`      | Theory | Distance to threshold outside range applies the outcome of every step kind.       |
| `PercentBelowThresholdAboveAppliesTheOutcomeOfEveryStepKind`           | Theory | Percent below threshold above applies the outcome of every step kind.             |
| `PercentBelowThresholdBelowAppliesTheOutcomeOfEveryStepKind`           | Theory | Percent below threshold below applies the outcome of every step kind.             |
| `PercentBelowThresholdInRangeAppliesTheOutcomeOfEveryStepKind`         | Theory | Percent below threshold in range applies the outcome of every step kind.          |
| `PercentBelowThresholdOutsideRangeAppliesTheOutcomeOfEveryStepKind`    | Theory | Percent below threshold outside range applies the outcome of every step kind.     |
| `ImbalanceWithAboveAppliesTheOutcomeOfEveryStepKind`                   | Theory | Imbalance with above applies the outcome of every step kind.                      |
| `ImbalanceWithBelowAppliesTheOutcomeOfEveryStepKind`                   | Theory | Imbalance with below applies the outcome of every step kind.                      |
| `ImbalanceWithInRangeAppliesTheOutcomeOfEveryStepKind`                 | Theory | Imbalance with in range applies the outcome of every step kind.                   |
| `ImbalanceWithOutsideRangeAppliesTheOutcomeOfEveryStepKind`            | Theory | Imbalance with outside range applies the outcome of every step kind.              |
| `RetentionAfterAboveAppliesTheOutcomeOfEveryStepKind`                  | Theory | Retention after above applies the outcome of every step kind.                     |
| `RetentionAfterBelowAppliesTheOutcomeOfEveryStepKind`                  | Theory | Retention after below applies the outcome of every step kind.                     |
| `RetentionAfterInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Retention after in range applies the outcome of every step kind.                  |
| `RetentionAfterOutsideRangeAppliesTheOutcomeOfEveryStepKind`           | Theory | Retention after outside range applies the outcome of every step kind.             |

### FlowEntryOverloadTests

`Jube.Test.Dictionary.Extensions.FlowEntryOverloadTests` — 25 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                        |
|--------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `TheGuardFindsAWorthwhileNumberOfSteps`                                        | Fact   | The guard finds a worthwhile number of steps.                                                   |
| `EveryTypedFlowStepHasARawTypeOverloadWithTheSameShape`                        | Fact   | Every typed flow step has a raw type overload with the same shape.                              |
| `EveryRawOverloadHasATypedFlowStepSoNoneIsOrphaned`                            | Fact   | Every raw overload has a typed flow step so none is orphaned.                                   |
| `TheRawAndFlowCountsAreEqual`                                                  | Fact   | The raw and flow counts are equal.                                                              |
| `EveryRawOverloadReturnsAFlowOfItsOwnType`                                     | Fact   | Every raw overload returns a flow of its own type.                                              |
| `NoRawOverloadNameCollidesWithAnInstanceOrStaticMemberOfItsReceiver`           | Fact   | No raw overload name collides with an instance or static member of its receiver.                |
| `NoRawOverloadNameCollidesWithAPlainExtensionOfTheSameReceiverAndShape`        | Fact   | No raw overload name collides with a plain extension of the same receiver and shape.            |
| `EveryTransformerWithoutACollisionHasARawEntryOverloadReturningAFlow`          | Theory | Every transformer without a collision has a raw entry overload returning a flow.                |
| `PlainArithmeticOnADoubleReturnsANumberAndOnAFlowReturnsAFlow`                 | Theory | Plain arithmetic on a double returns a number and on a flow returns a flow.                     |
| `TransformersThatCollideWithAPlainMethodResolveToItAndReturnAPlainValue`       | Theory | Transformers that collide with a plain method resolve to it and return a plain value.           |
| `DoubleRoundIsAStaticBclMemberWhichVbBindsBeforeAnExtension`                   | Fact   | Double round is a static bcl member which vb binds before an extension.                         |
| `NoFlowMethodIsNamedAfterABareMemberOfStringSoTheRuleTokenAllowListStaysTight` | Fact   | No flow method is named after a bare member of string so the rule token allow list stays tight. |
| `NoNewExtensionMethodNameCollidesWithAMemberOfAnyTypeARuleValueCanActuallyBe`  | Fact   | No new extension method name collides with a member of any type a rule value can actually be.   |
| `EveryReviewedPreExistingCollisionIsStillActuallyPresent`                      | Fact   | Every reviewed pre existing collision is still actually present.                                |
| `ScoreAndControlStepsAreDeliberatelyFlowOnly`                                  | Fact   | Score and control steps are deliberately flow only.                                             |
| `DoubleStartIsIdempotentAndKeepsState`                                         | Fact   | Double start is idempotent and keeps state.                                                     |
| `StartOnADecidedFlowDoesNotUndecideIt`                                         | Fact   | Start on a decided flow does not undecide it.                                                   |
| `StartOfAFlowIsTheSameTypeNotANestedFlow`                                      | Fact   | Start of a flow is the same type not a nested flow.                                             |
| `EachRawTypeStartsAPipelineWithoutStart`                                       | Fact   | Each raw type starts a pipeline without start.                                                  |
| `EachPrefixWorksOnARawValue`                                                   | Fact   | Each prefix works on a raw value.                                                               |
| `ARawEntryAndAnExplicitStartGiveIdenticalResults`                              | Fact   | A raw entry and an explicit start give identical results.                                       |
| `AChainCanBeginWithATransformerOnARawValue`                                    | Fact   | A chain can begin with a transformer on a raw value.                                            |
| `AgainstFollowsAnImplicitStart`                                                | Fact   | Against follows an implicit start.                                                              |
| `NullNaNAndBlankInputsFailClosedOnARawEntry`                                   | Fact   | Null na n and blank inputs fail closed on a raw entry.                                          |
| `ARawEntryOnADecidedFlowIsNotPossibleSoAFlowReceiverUsesTheFlowStep`           | Fact   | A raw entry on a decided flow is not possible so a flow receiver uses the flow step.            |

### FlowFuzzyTests

`Jube.Test.Dictionary.Extensions.FlowFuzzyTests` — 10 tests. Category: Unit.

| Test                                                      | Kind | Verifies                                                            |
|-----------------------------------------------------------|------|---------------------------------------------------------------------|
| `FuzzyBestScoreBecomesTheNumericValueOfTheFlow`           | Fact | Fuzzy best score becomes the numeric value of the flow.             |
| `FuzzyBestScoreFeedsTheNumericSteps`                      | Fact | Fuzzy best score feeds the numeric steps.                           |
| `FuzzyMatchCountBecomesAnIntegerValue`                    | Fact | Fuzzy match count becomes an integer value.                         |
| `FuzzyBestMatchBecomesAStringValueThatCanBeTestedFurther` | Fact | Fuzzy best match becomes a string value that can be tested further. |
| `FuzzyBestMatchIsNullWhenNothingIsClose`                  | Fact | Fuzzy best match is null when nothing is close.                     |
| `ADecidedFlowSkipsTheExpensiveFuzzyTransformers`          | Fact | A decided flow skips the expensive fuzzy transformers.              |
| `TheScoreAndLabelSurviveAFuzzyTransformer`                | Fact | The score and label survive a fuzzy transformer.                    |
| `AScreeningPipelineReadsLeftToRight`                      | Fact | A screening pipeline reads left to right.                           |
| `APipelineCanEscalateFromExactThroughToFuzzy`             | Fact | A pipeline can escalate from exact through to fuzzy.                |
| `AWatchlistHitCanBeRequiredBeforeAnotherFieldIsTested`    | Fact | A watchlist hit can be required before another field is tested.     |

### FlowInt32Tests

`Jube.Test.Dictionary.Extensions.FlowInt32Tests` — 18 tests. Category: Unit.

| Test                                             | Kind   | Verifies                                                 |
|--------------------------------------------------|--------|----------------------------------------------------------|
| `GreaterAppliesTheOutcomeOfEveryStepKind`        | Theory | Greater applies the outcome of every step kind.          |
| `GreaterOrEqualAppliesTheOutcomeOfEveryStepKind` | Theory | Greater or equal applies the outcome of every step kind. |
| `LessAppliesTheOutcomeOfEveryStepKind`           | Theory | Less applies the outcome of every step kind.             |
| `LessOrEqualAppliesTheOutcomeOfEveryStepKind`    | Theory | Less or equal applies the outcome of every step kind.    |
| `EqualAppliesTheOutcomeOfEveryStepKind`          | Theory | Equal applies the outcome of every step kind.            |
| `NotEqualAppliesTheOutcomeOfEveryStepKind`       | Theory | Not equal applies the outcome of every step kind.        |
| `BetweenAppliesTheOutcomeOfEveryStepKind`        | Theory | Between applies the outcome of every step kind.          |
| `InRangeAppliesTheOutcomeOfEveryStepKind`        | Theory | In range applies the outcome of every step kind.         |
| `OutsideRangeAppliesTheOutcomeOfEveryStepKind`   | Theory | Outside range applies the outcome of every step kind.    |
| `InAppliesTheOutcomeOfEveryStepKind`             | Theory | In applies the outcome of every step kind.               |
| `NotInAppliesTheOutcomeOfEveryStepKind`          | Theory | Not in applies the outcome of every step kind.           |
| `IsZeroAppliesTheOutcomeOfEveryStepKind`         | Theory | Is zero applies the outcome of every step kind.          |
| `IsPositiveAppliesTheOutcomeOfEveryStepKind`     | Theory | Is positive applies the outcome of every step kind.      |
| `IsNegativeAppliesTheOutcomeOfEveryStepKind`     | Theory | Is negative applies the outcome of every step kind.      |
| `IsEvenAppliesTheOutcomeOfEveryStepKind`         | Theory | Is even applies the outcome of every step kind.          |
| `IsOddAppliesTheOutcomeOfEveryStepKind`          | Theory | Is odd applies the outcome of every step kind.           |
| `IsMultipleOfAppliesTheOutcomeOfEveryStepKind`   | Theory | Is multiple of applies the outcome of every step kind.   |
| `IsPrimeAppliesTheOutcomeOfEveryStepKind`        | Theory | Is prime applies the outcome of every step kind.         |

### FlowNewPredicatesTests

`Jube.Test.Dictionary.Extensions.FlowNewPredicatesTests` — 8 tests. Category: Unit.

| Test                                              | Kind   | Verifies                                                  |
|---------------------------------------------------|--------|-----------------------------------------------------------|
| `InsidePolygonAppliesEveryStepKind`               | Theory | Inside polygon applies every step kind.                   |
| `WithinAnyRadiusKmAppliesEveryStepKind`           | Fact   | Within any radius km applies every step kind.             |
| `IsDelimitedMemberAppliesEveryStepKind`           | Fact   | Is delimited member applies every step kind.              |
| `IsInAnyCidrAppliesEveryStepKind`                 | Fact   | Is in any cidr applies every step kind.                   |
| `IsCryptoWalletAddressAppliesEveryStepKind`       | Fact   | Is crypto wallet address applies every step kind.         |
| `JsonTextArrayAnyInListAppliesEveryStepKind`      | Fact   | Json text array any in list applies every step kind.      |
| `NumberInTextWithinPercentOfAppliesEveryStepKind` | Fact   | Number in text within percent of applies every step kind. |
| `DayOfMonthInAppliesEveryStepKind`                | Fact   | Day of month in applies every step kind.                  |

### FlowStringTests

`Jube.Test.Dictionary.Extensions.FlowStringTests` — 89 tests. Category: Unit.

| Test                                                           | Kind   | Verifies                                                                  |
|----------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `IsNullAppliesTheOutcomeOfEveryStepKind`                       | Theory | Is null applies the outcome of every step kind.                           |
| `IsEmptyAppliesTheOutcomeOfEveryStepKind`                      | Theory | Is empty applies the outcome of every step kind.                          |
| `IsNullOrEmptyAppliesTheOutcomeOfEveryStepKind`                | Theory | Is null or empty applies the outcome of every step kind.                  |
| `IsNullOrWhiteSpaceAppliesTheOutcomeOfEveryStepKind`           | Theory | Is null or white space applies the outcome of every step kind.            |
| `IsNotNullOrEmptyAppliesTheOutcomeOfEveryStepKind`             | Theory | Is not null or empty applies the outcome of every step kind.              |
| `IsNotNullOrWhiteSpaceAppliesTheOutcomeOfEveryStepKind`        | Theory | Is not null or white space applies the outcome of every step kind.        |
| `EqualAppliesTheOutcomeOfEveryStepKind`                        | Theory | Equal applies the outcome of every step kind.                             |
| `EqualIgnoreCaseAppliesTheOutcomeOfEveryStepKind`              | Theory | Equal ignore case applies the outcome of every step kind.                 |
| `NotEqualAppliesTheOutcomeOfEveryStepKind`                     | Theory | Not equal applies the outcome of every step kind.                         |
| `InAppliesTheOutcomeOfEveryStepKind`                           | Theory | In applies the outcome of every step kind.                                |
| `InIgnoreCaseAppliesTheOutcomeOfEveryStepKind`                 | Theory | In ignore case applies the outcome of every step kind.                    |
| `NotInAppliesTheOutcomeOfEveryStepKind`                        | Theory | Not in applies the outcome of every step kind.                            |
| `InListAppliesTheOutcomeOfEveryStepKind`                       | Theory | In list applies the outcome of every step kind.                           |
| `InListIgnoreCaseAppliesTheOutcomeOfEveryStepKind`             | Theory | In list ignore case applies the outcome of every step kind.               |
| `NotInListAppliesTheOutcomeOfEveryStepKind`                    | Theory | Not in list applies the outcome of every step kind.                       |
| `ContainsAppliesTheOutcomeOfEveryStepKind`                     | Theory | Contains applies the outcome of every step kind.                          |
| `ContainsIgnoreCaseAppliesTheOutcomeOfEveryStepKind`           | Theory | Contains ignore case applies the outcome of every step kind.              |
| `ContainsAnyAppliesTheOutcomeOfEveryStepKind`                  | Theory | Contains any applies the outcome of every step kind.                      |
| `ContainsAnyIgnoreCaseAppliesTheOutcomeOfEveryStepKind`        | Theory | Contains any ignore case applies the outcome of every step kind.          |
| `ContainsAllAppliesTheOutcomeOfEveryStepKind`                  | Theory | Contains all applies the outcome of every step kind.                      |
| `ContainsNoneAppliesTheOutcomeOfEveryStepKind`                 | Theory | Contains none applies the outcome of every step kind.                     |
| `ContainsAnyInListAppliesTheOutcomeOfEveryStepKind`            | Theory | Contains any in list applies the outcome of every step kind.              |
| `ContainsAnyInListIgnoreCaseAppliesTheOutcomeOfEveryStepKind`  | Theory | Contains any in list ignore case applies the outcome of every step kind.  |
| `ContainsAllInListAppliesTheOutcomeOfEveryStepKind`            | Theory | Contains all in list applies the outcome of every step kind.              |
| `ContainsNoneInListAppliesTheOutcomeOfEveryStepKind`           | Theory | Contains none in list applies the outcome of every step kind.             |
| `StartsWithAppliesTheOutcomeOfEveryStepKind`                   | Theory | Starts with applies the outcome of every step kind.                       |
| `EndsWithAppliesTheOutcomeOfEveryStepKind`                     | Theory | Ends with applies the outcome of every step kind.                         |
| `StartsWithAnyInListAppliesTheOutcomeOfEveryStepKind`          | Theory | Starts with any in list applies the outcome of every step kind.           |
| `MatchesAppliesTheOutcomeOfEveryStepKind`                      | Theory | Matches applies the outcome of every step kind.                           |
| `LengthEqualAppliesTheOutcomeOfEveryStepKind`                  | Theory | Length equal applies the outcome of every step kind.                      |
| `LengthGreaterAppliesTheOutcomeOfEveryStepKind`                | Theory | Length greater applies the outcome of every step kind.                    |
| `LengthLessAppliesTheOutcomeOfEveryStepKind`                   | Theory | Length less applies the outcome of every step kind.                       |
| `LengthInRangeAppliesTheOutcomeOfEveryStepKind`                | Theory | Length in range applies the outcome of every step kind.                   |
| `IsNumericAppliesTheOutcomeOfEveryStepKind`                    | Theory | Is numeric applies the outcome of every step kind.                        |
| `IsAlphaAppliesTheOutcomeOfEveryStepKind`                      | Theory | Is alpha applies the outcome of every step kind.                          |
| `IsAlphaNumericAppliesTheOutcomeOfEveryStepKind`               | Theory | Is alpha numeric applies the outcome of every step kind.                  |
| `IsAllSameCharacterAppliesTheOutcomeOfEveryStepKind`           | Theory | Is all same character applies the outcome of every step kind.             |
| `HasSequentialDigitsAppliesTheOutcomeOfEveryStepKind`          | Theory | Has sequential digits applies the outcome of every step kind.             |
| `ShannonEntropyAboveAppliesTheOutcomeOfEveryStepKind`          | Theory | Shannon entropy above applies the outcome of every step kind.             |
| `ShannonEntropyBelowAppliesTheOutcomeOfEveryStepKind`          | Theory | Shannon entropy below applies the outcome of every step kind.             |
| `IsValidEmailAppliesTheOutcomeOfEveryStepKind`                 | Theory | Is valid email applies the outcome of every step kind.                    |
| `IsNotValidEmailAppliesTheOutcomeOfEveryStepKind`              | Theory | Is not valid email applies the outcome of every step kind.                |
| `IsValidIpAppliesTheOutcomeOfEveryStepKind`                    | Theory | Is valid ip applies the outcome of every step kind.                       |
| `IsNotValidIpAppliesTheOutcomeOfEveryStepKind`                 | Theory | Is not valid ip applies the outcome of every step kind.                   |
| `IsValidIbanAppliesTheOutcomeOfEveryStepKind`                  | Theory | Is valid iban applies the outcome of every step kind.                     |
| `IsNotValidIbanAppliesTheOutcomeOfEveryStepKind`               | Theory | Is not valid iban applies the outcome of every step kind.                 |
| `IsValidBicAppliesTheOutcomeOfEveryStepKind`                   | Theory | Is valid bic applies the outcome of every step kind.                      |
| `IsNotValidBicAppliesTheOutcomeOfEveryStepKind`                | Theory | Is not valid bic applies the outcome of every step kind.                  |
| `IsValidLuhnAppliesTheOutcomeOfEveryStepKind`                  | Theory | Is valid luhn applies the outcome of every step kind.                     |
| `IsNotValidLuhnAppliesTheOutcomeOfEveryStepKind`               | Theory | Is not valid luhn applies the outcome of every step kind.                 |
| `IsValidCardExpiryAppliesTheOutcomeOfEveryStepKind`            | Theory | Is valid card expiry applies the outcome of every step kind.              |
| `IsNotValidCardExpiryAppliesTheOutcomeOfEveryStepKind`         | Theory | Is not valid card expiry applies the outcome of every step kind.          |
| `EmailDomainEqualAppliesTheOutcomeOfEveryStepKind`             | Theory | Email domain equal applies the outcome of every step kind.                |
| `EmailDomainInAppliesTheOutcomeOfEveryStepKind`                | Theory | Email domain in applies the outcome of every step kind.                   |
| `EmailDomainInListAppliesTheOutcomeOfEveryStepKind`            | Theory | Email domain in list applies the outcome of every step kind.              |
| `EmailAliasNormalisedEqualAppliesTheOutcomeOfEveryStepKind`    | Theory | Email alias normalised equal applies the outcome of every step kind.      |
| `SoundexEqualAppliesTheOutcomeOfEveryStepKind`                 | Theory | Soundex equal applies the outcome of every step kind.                     |
| `JaroWinklerAtLeastAppliesTheOutcomeOfEveryStepKind`           | Theory | Jaro winkler at least applies the outcome of every step kind.             |
| `SimilarityAtLeastAppliesTheOutcomeOfEveryStepKind`            | Theory | Similarity at least applies the outcome of every step kind.               |
| `LevenshteinAtMostAppliesTheOutcomeOfEveryStepKind`            | Theory | Levenshtein at most applies the outcome of every step kind.               |
| `EndsWithAnyInListAppliesTheOutcomeOfEveryStepKind`            | Theory | Ends with any in list applies the outcome of every step kind.             |
| `ContainsAllIgnoreCaseAppliesTheOutcomeOfEveryStepKind`        | Theory | Contains all ignore case applies the outcome of every step kind.          |
| `ContainsNoneIgnoreCaseAppliesTheOutcomeOfEveryStepKind`       | Theory | Contains none ignore case applies the outcome of every step kind.         |
| `ContainsAllInListIgnoreCaseAppliesTheOutcomeOfEveryStepKind`  | Theory | Contains all in list ignore case applies the outcome of every step kind.  |
| `ContainsNoneInListIgnoreCaseAppliesTheOutcomeOfEveryStepKind` | Theory | Contains none in list ignore case applies the outcome of every step kind. |
| `ContainsFuzzyAppliesTheOutcomeOfEveryStepKind`                | Theory | Contains fuzzy applies the outcome of every step kind.                    |
| `ContainsFuzzyAnyAppliesTheOutcomeOfEveryStepKind`             | Theory | Contains fuzzy any applies the outcome of every step kind.                |
| `ContainsFuzzyAnyInListAppliesTheOutcomeOfEveryStepKind`       | Theory | Contains fuzzy any in list applies the outcome of every step kind.        |
| `LevenshteinAtMostAnyInListAppliesTheOutcomeOfEveryStepKind`   | Theory | Levenshtein at most any in list applies the outcome of every step kind.   |
| `SimilarityAtLeastAnyInListAppliesTheOutcomeOfEveryStepKind`   | Theory | Similarity at least any in list applies the outcome of every step kind.   |
| `JaroWinklerAtLeastAnyInListAppliesTheOutcomeOfEveryStepKind`  | Theory | Jaro winkler at least any in list applies the outcome of every step kind. |
| `SomehowInListAppliesTheOutcomeOfEveryStepKind`                | Theory | Somehow in list applies the outcome of every step kind.                   |
| `SomehowInListWithAppliesTheOutcomeOfEveryStepKind`            | Theory | Somehow in list with applies the outcome of every step kind.              |
| `SomehowLikeAppliesTheOutcomeOfEveryStepKind`                  | Theory | Somehow like applies the outcome of every step kind.                      |
| `SomehowLikeWithAppliesTheOutcomeOfEveryStepKind`              | Theory | Somehow like with applies the outcome of every step kind.                 |
| `ContainsSomehowInListAppliesTheOutcomeOfEveryStepKind`        | Theory | Contains somehow in list applies the outcome of every step kind.          |
| `ContainsSomehowInListWithAppliesTheOutcomeOfEveryStepKind`    | Theory | Contains somehow in list with applies the outcome of every step kind.     |
| `NormalisedInListAppliesTheOutcomeOfEveryStepKind`             | Theory | Normalised in list applies the outcome of every step kind.                |
| `LevenshteinInListAppliesTheOutcomeOfEveryStepKind`            | Theory | Levenshtein in list applies the outcome of every step kind.               |
| `DamerauInListAppliesTheOutcomeOfEveryStepKind`                | Theory | Damerau in list applies the outcome of every step kind.                   |
| `SimilarInListAppliesTheOutcomeOfEveryStepKind`                | Theory | Similar in list applies the outcome of every step kind.                   |
| `JaroWinklerInListAppliesTheOutcomeOfEveryStepKind`            | Theory | Jaro winkler in list applies the outcome of every step kind.              |
| `DiceInListAppliesTheOutcomeOfEveryStepKind`                   | Theory | Dice in list applies the outcome of every step kind.                      |
| `SoundexInListAppliesTheOutcomeOfEveryStepKind`                | Theory | Soundex in list applies the outcome of every step kind.                   |
| `TokenSortInListAppliesTheOutcomeOfEveryStepKind`              | Theory | Token sort in list applies the outcome of every step kind.                |
| `TokenSetInListAppliesTheOutcomeOfEveryStepKind`               | Theory | Token set in list applies the outcome of every step kind.                 |
| `InitialsInListAppliesTheOutcomeOfEveryStepKind`               | Theory | Initials in list applies the outcome of every step kind.                  |
| `FuzzyScoreAtLeastAppliesTheOutcomeOfEveryStepKind`            | Theory | Fuzzy score at least applies the outcome of every step kind.              |
| `FuzzyMatchCountAtLeastAppliesTheOutcomeOfEveryStepKind`       | Theory | Fuzzy match count at least applies the outcome of every step kind.        |

### FlowTests

`Jube.Test.Dictionary.Extensions.FlowTests` — 57 tests. Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                          |
|------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `StartBeginsUndecidedWithNoScoreOrLabel`                               | Fact   | Start begins undecided with no score or label.                                    |
| `StartWorksOnAnyType`                                                  | Fact   | Start works on any type.                                                          |
| `ApplyTransitionsAnUndecidedFlowAccordingToTheKindAndTheTest`          | Theory | Apply transitions an undecided flow according to the kind and the test.           |
| `ADecidedFlowIsAbsorbingAndIgnoresEveryKindAndTest`                    | Theory | A decided flow is absorbing and ignores every kind and test.                      |
| `ADecidedFlowSkipsEveryTypedStepWithoutEvaluatingItsOutcome`           | Theory | A decided flow skips every typed step without evaluating its outcome.             |
| `OnlyAMatchedFlowConvertsToTrue`                                       | Theory | Only a matched flow converts to true.                                             |
| `ADecidedFlowStillConvertsFalseWhenReadAsAnIfCondition`                | Fact   | A decided flow still converts false when read as an if condition.                 |
| `TerminalStepsDecideAnUndecidedFlow`                                   | Fact   | Terminal steps decide an undecided flow.                                          |
| `TerminalStepsDoNotOverwriteAnAlreadyDecidedFlow`                      | Fact   | Terminal steps do not overwrite an already decided flow.                          |
| `OtherwiseStepsOnlyActWhileTheFlowIsStillUndecided`                    | Fact   | Otherwise steps only act while the flow is still undecided.                       |
| `ResetReturnsAnyDecidedFlowToUndecidedAndClearsScoreAndLabel`          | Fact   | Reset returns any decided flow to undecided and clears score and label.           |
| `ResetAllowsAnErroredFlowToBeDecidedAgain`                             | Fact   | Reset allows an errored flow to be decided again.                                 |
| `LabelIsCarriedThroughToTheDecision`                                   | Fact   | Label is carried through to the decision.                                         |
| `AgainstSwitchesTheValueButKeepsOutcomeScoreAndLabel`                  | Fact   | Against switches the value but keeps outcome score and label.                     |
| `AgainstOnADecidedFlowStaysDecidedSoLaterStepsAreSkipped`              | Fact   | Against on a decided flow stays decided so later steps are skipped.               |
| `AChainDropsOutAtTheFirstDecisionAndIgnoresEverythingAfter`            | Fact   | A chain drops out at the first decision and ignores everything after.             |
| `ARequireChainFollowedByOtherwiseMatchMatchesOnlyWhenEveryGuardPasses` | Fact   | A require chain followed by otherwise match matches only when every guard passes. |
| `ARuleWithNoTerminalStepFailsClosed`                                   | Fact   | A rule with no terminal step fails closed.                                        |
| `MatchWhenAppliesAnyBooleanExpression`                                 | Theory | Match when applies any boolean expression.                                        |
| `RejectWhenAppliesAnyBooleanExpression`                                | Theory | Reject when applies any boolean expression.                                       |
| `BreakWhenAppliesAnyBooleanExpression`                                 | Theory | Break when applies any boolean expression.                                        |
| `RequireThatGuardsOnAnyBooleanExpression`                              | Theory | Require that guards on any boolean expression.                                    |
| `EnsureThatGuardsOnAnyBooleanExpression`                               | Theory | Ensure that guards on any boolean expression.                                     |
| `BooleanExpressionsInteroperateWithTheExistingExtensionLibrary`        | Fact   | Boolean expressions interoperate with the existing extension library.             |
| `AddScoreAccumulatesWhileUndecided`                                    | Fact   | Add score accumulates while undecided.                                            |
| `AddScoreIsIgnoredOnceDecided`                                         | Fact   | Add score is ignored once decided.                                                |
| `AddScoreWhenOnlyAddsWhenTheConditionHolds`                            | Theory | Add score when only adds when the condition holds.                                |
| `AddScoreWhenIsIgnoredOnceDecided`                                     | Fact   | Add score when is ignored once decided.                                           |
| `CapScoreLimitsTheAccumulatedScore`                                    | Theory | Cap score limits the accumulated score.                                           |
| `MatchIfScoreAtLeastDecidesFromTheAccumulatedScore`                    | Theory | Match if score at least decides from the accumulated score.                       |
| `RejectIfScoreBelowDecidesFromTheAccumulatedScore`                     | Theory | Reject if score below decides from the accumulated score.                         |
| `BreakIfScoreAtLeastDecidesFromTheAccumulatedScore`                    | Theory | Break if score at least decides from the accumulated score.                       |
| `RequireScoreAtLeastGuardsOnTheAccumulatedScore`                       | Theory | Require score at least guards on the accumulated score.                           |
| `ToScoreExposesTheScoreForNumericRuleReturnTypes`                      | Fact   | To score exposes the score for numeric rule return types.                         |
| `StateProbesReflectTheOutcome`                                         | Fact   | State probes reflect the outcome.                                                 |
| `ParseDoubleReadsInvariantNumbers`                                     | Theory | Parse double reads invariant numbers.                                             |
| `ParseDoubleErrorsRatherThanThrowingOnBadInput`                        | Theory | Parse double errors rather than throwing on bad input.                            |
| `ParseDoubleIsCultureInvariant`                                        | Fact   | Parse double is culture invariant.                                                |
| `AnErroredParseStopsEveryFollowingStep`                                | Fact   | An errored parse stops every following step.                                      |
| `AParseAfterADecisionStaysDecided`                                     | Fact   | A parse after a decision stays decided.                                           |
| `ParseIntegerReadsWholeNumbers`                                        | Theory | Parse integer reads whole numbers.                                                |
| `ParseIntegerErrorsOnAnythingThatIsNotAnInt`                           | Theory | Parse integer errors on anything that is not an int.                              |
| `ParseDateReadsInvariantDates`                                         | Fact   | Parse date reads invariant dates.                                                 |
| `ParseDateErrorsOnAnythingThatIsNotADate`                              | Theory | Parse date errors on anything that is not a date.                                 |
| `ParseBooleanReadsBooleans`                                            | Theory | Parse boolean reads booleans.                                                     |
| `ParseBooleanErrorsOnAnythingElse`                                     | Theory | Parse boolean errors on anything else.                                            |
| `ParseThenTestReadsLeftToRight`                                        | Fact   | Parse then test reads left to right.                                              |
| `NumericTransformersChangeTheValueAndKeepTheState`                     | Fact   | Numeric transformers change the value and keep the state.                         |
| `DividedByZeroErrorsRatherThanProducingInfinity`                       | Fact   | Divided by zero errors rather than producing infinity.                            |
| `DividedByOnADecidedFlowLeavesItUntouched`                             | Fact   | Divided by on a decided flow leaves it untouched.                                 |
| `TransformersOnADecidedFlowKeepItDecided`                              | Fact   | Transformers on a decided flow keep it decided.                                   |
| `StringTransformersAreNullSafeAndCultureInvariant`                     | Fact   | String transformers are null safe and culture invariant.                          |
| `TransformerThenTestReadsLeftToRight`                                  | Fact   | Transformer then test reads left to right.                                        |
| `DateTransformersProduceIntegers`                                      | Fact   | Date transformers produce integers.                                               |
| `AFlowIsAValueTypeSoStepsNeverMutateAnEarlierFlow`                     | Fact   | A flow is a value type so steps never mutate an earlier flow.                     |
| `TheFlowCanBeBranchedAndEachBranchIsIndependent`                       | Fact   | The flow can be branched and each branch is independent.                          |
| `ApplyRejectsAnUnknownKind`                                            | Fact   | Apply rejects an unknown kind.                                                    |

### FlowTimeRelativeTests

`Jube.Test.Dictionary.Extensions.FlowTimeRelativeTests` — 5 tests. Category: Unit.

| Test                                              | Kind   | Verifies                                                  |
|---------------------------------------------------|--------|-----------------------------------------------------------|
| `IsPastAppliesTheOutcomeOfEveryStepKind`          | Theory | Is past applies the outcome of every step kind.           |
| `IsFutureAppliesTheOutcomeOfEveryStepKind`        | Theory | Is future applies the outcome of every step kind.         |
| `IsTodayAppliesTheOutcomeOfEveryStepKind`         | Theory | Is today applies the outcome of every step kind.          |
| `OlderThanDaysAppliesTheOutcomeOfEveryStepKind`   | Theory | Older than days applies the outcome of every step kind.   |
| `YoungerThanDaysAppliesTheOutcomeOfEveryStepKind` | Theory | Younger than days applies the outcome of every step kind. |

### FlowZoneTests

`Jube.Test.Dictionary.Extensions.FlowZoneTests` — 8 tests. Category: Unit.

| Test                                             | Kind   | Verifies                                                |
|--------------------------------------------------|--------|---------------------------------------------------------|
| `ToZoneConvertsAnUnspecifiedValueTreatedAsUtc`   | Fact   | To zone converts an unspecified value treated as utc.   |
| `ToZoneConvertsAUtcValue`                        | Fact   | To zone converts a utc value.                           |
| `ToZoneConvertsALocalValueViaItsUniversalTime`   | Fact   | To zone converts a local value via its universal time.  |
| `ToZoneAppliesDaylightSavingForTheDateConcerned` | Fact   | To zone applies daylight saving for the date concerned. |
| `ToZoneErrorsRatherThanThrowingOnAnUnknownZone`  | Theory | To zone errors rather than throwing on an unknown zone. |
| `ToZoneOnADecidedFlowLeavesItUntouched`          | Fact   | To zone on a decided flow leaves it untouched.          |
| `AZoneConversionThenAHourTestReadsLeftToRight`   | Fact   | A zone conversion then a hour test reads left to right. |
| `ZoneAwareStepsAgreeWithConvertingFirst`         | Fact   | Zone aware steps agree with converting first.           |

### GeometryAndCircularExtensionsTests

`Jube.Test.Dictionary.Extensions.GeometryAndCircularExtensionsTests` — 8 tests. Category: Unit.

| Test                                                  | Kind   | Verifies                                                        |
|-------------------------------------------------------|--------|-----------------------------------------------------------------|
| `CircularDistanceFromIsTheShorterWayAroundTheScale`   | Theory | Circular distance from is the shorter way around the scale.     |
| `CircularDistanceFromIsNaNWhenUndefined`              | Theory | Circular distance from is na n when undefined.                  |
| `IsInsidePolygonIsTrueForAPointInsideAndFalseOutside` | Fact   | Is inside polygon is true for a point inside and false outside. |
| `IsInsidePolygonIsFalseForFewerThanThreeVertices`     | Fact   | Is inside polygon is false for fewer than three vertices.       |
| `IsInsidePolygonIsFalseWhenAVertexDoesNotParse`       | Fact   | Is inside polygon is false when a vertex does not parse.        |
| `IsInsidePolygonIsFalseForANullVertexList`            | Fact   | Is inside polygon is false for a null vertex list.              |
| `IsWithinAnyRadiusKmMatchesAnyOfTheCentres`           | Fact   | Is within any radius km matches any of the centres.             |
| `IsWithinAnyRadiusKmIgnoresUnparseableCentres`        | Fact   | Is within any radius km ignores unparseable centres.            |

### HashingEncodingAndTextExtensionsTests

`Jube.Test.Dictionary.Extensions.HashingEncodingAndTextExtensionsTests` — 13 tests. Category: Unit.

| Test                                                     | Kind   | Verifies                                                       |
|----------------------------------------------------------|--------|----------------------------------------------------------------|
| `DigestsMatchTheStandardTestVectors`                     | Fact   | Digests match the standard test vectors.                       |
| `HmacsMatchTheStandardTestVectors`                       | Fact   | Hmacs match the standard test vectors.                         |
| `HexAndBase64UrlRoundTrip`                               | Fact   | Hex and base64 url round trip.                                 |
| `UnicodeNormalisedAppliesTheRequestedForm`               | Fact   | Unicode normalised applies the requested form.                 |
| `RemoveControlCharactersKeepsLineBreaksAndTabs`          | Fact   | Remove control characters keeps line breaks and tabs.          |
| `CountingHelpersCountWhatTheyName`                       | Fact   | Counting helpers count what they name.                         |
| `ShapingHelpersProduceTheExpectedText`                   | Fact   | Shaping helpers produce the expected text.                     |
| `TokenAtSupportsNegativeIndexes`                         | Theory | Token at supports negative indexes.                            |
| `TokenCountCountsEmptyTokens`                            | Fact   | Token count counts empty tokens.                               |
| `RegexHelpersReplaceCaptureAndCount`                     | Fact   | Regex helpers replace capture and count.                       |
| `UserSuppliedPatternsAreBoundedByATimeout`               | Fact   | User supplied patterns are bounded by a timeout.               |
| `FuzzyScalarHelpersScoreNamePairs`                       | Fact   | Fuzzy scalar helpers score name pairs.                         |
| `NormalisedForMatchingStripsTitlesSuffixesAndDiacritics` | Fact   | Normalised for matching strips titles suffixes and diacritics. |

### IdentifierExtensionsTests

`Jube.Test.Dictionary.Extensions.IdentifierExtensionsTests` — 17 tests. Category: Unit.

| Test                                               | Kind   | Verifies                                                    |
|----------------------------------------------------|--------|-------------------------------------------------------------|
| `IbanPartsAreExtractedFromAValidIban`              | Fact   | Iban parts are extracted from a valid iban.                 |
| `IbanPartsAreEmptyForAnInvalidIban`                | Fact   | Iban parts are empty for an invalid iban.                   |
| `BicPartsAreExtractedFromAValidBic`                | Fact   | Bic parts are extracted from a valid bic.                   |
| `LuhnCheckDigitCompletesThePayload`                | Theory | Luhn check digit completes the payload.                     |
| `CardIssuerPrefixTakesTheLeadingDigits`            | Fact   | Card issuer prefix takes the leading digits.                |
| `IsValidIsinChecksTheLuhnCheckDigit`               | Theory | Is valid isin checks the luhn check digit.                  |
| `IsValidLeiChecksIso7064Mod97`                     | Theory | Is valid lei checks iso7064 mod97.                          |
| `IsValidCusipChecksTheCheckDigit`                  | Theory | Is valid cusip checks the check digit.                      |
| `IsValidSedolChecksTheWeightedCheckDigit`          | Theory | Is valid sedol checks the weighted check digit.             |
| `IsValidAbaRoutingNumberChecksTheWeightedSum`      | Theory | Is valid aba routing number checks the weighted sum.        |
| `UuidHelpersParseAndReportTheVersion`              | Fact   | Uuid helpers parse and report the version.                  |
| `IsValidE164PhoneNumberRequiresTheCanonicalForm`   | Theory | Is valid e 164 phone number requires the canonical form.    |
| `PhoneNumberDigitsKeepsTheInternationalPrefix`     | Theory | Phone number digits keeps the international prefix.         |
| `IsValidDomainNameChecksLabels`                    | Theory | Is valid domain name checks labels.                         |
| `UrlAndDomainPartsAreExtracted`                    | Fact   | Url and domain parts are extracted.                         |
| `QueryStringValueDecodesTheNamedParameter`         | Fact   | Query string value decodes the named parameter.             |
| `IsValidCardExpiryAsOfComparesWithTheSuppliedDate` | Fact   | Is valid card expiry as of compares with the supplied date. |

### Int32ExtensionsTests

`Jube.Test.Dictionary.Extensions.Int32ExtensionsTests` — 49 tests. Category: Unit.

| Test                                                                     | Kind   | Verifies                                                                               |
|--------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `AbsReturnsTheAbsoluteValue`                                             | Theory | Abs returns the absolute value.                                                        |
| `AbsOfIntMinValueOverflowsJustLikeMathAbs`                               | Fact   | Abs of int min value overflows just like math abs.                                     |
| `BetweenIsExclusiveOfBothBoundaries`                                     | Theory | Between is exclusive of both boundaries.                                               |
| `BetweenWorksAcrossAWideValueRangeIncludingNearIntBoundaries`            | Fact   | Between works across a wide value range including near int boundaries.                 |
| `BigMulProducesTheFullLongProductOfTwoInts`                              | Theory | Big mul produces the full long product of two ints.                                    |
| `BigMulHandlesAProductThatWouldOverflowA32BitInt`                        | Fact   | Big mul handles a product that would overflow a 32 bit int.                            |
| `ConvertFromUtf32ProducesASingleCharStringForABmpCodePoint`              | Fact   | Convert from utf32 produces a single char string for a bmp code point.                 |
| `ConvertFromUtf32ProducesASurrogatePairStringForASupplementaryCodePoint` | Fact   | Convert from utf32 produces a surrogate pair string for a supplementary code point.    |
| `ConvertFromUtf32ThrowsForAnOutOfRangeCodePoint`                         | Fact   | Convert from utf32 throws for an out of range code point.                              |
| `DaysConvertsToATimeSpanOfThatManyDays`                                  | Fact   | Days converts to a time span of that many days.                                        |
| `HoursConvertsToATimeSpanOfThatManyHours`                                | Fact   | Hours converts to a time span of that many hours.                                      |
| `MinutesConvertsToATimeSpanOfThatManyMinutes`                            | Fact   | Minutes converts to a time span of that many minutes.                                  |
| `SecondsConvertsToATimeSpanOfThatManySeconds`                            | Fact   | Seconds converts to a time span of that many seconds.                                  |
| `MillisecondsConvertsToATimeSpanOfThatManyMilliseconds`                  | Fact   | Milliseconds converts to a time span of that many milliseconds.                        |
| `WeeksConvertsToATimeSpanOfThatManyWeeksExpressedAsDaysTimesSeven`       | Fact   | Weeks converts to a time span of that many weeks expressed as days times seven.        |
| `DaysInMonthMatchesTheBclImplementationIncludingLeapFebruary`            | Theory | Days in month matches the bcl implementation including leap february.                  |
| `DivRemReturnsTheQuotientAndOutputsTheRemainder`                         | Theory | Div rem returns the quotient and outputs the remainder.                                |
| `DivRemThrowsForDivisionByZero`                                          | Fact   | Div rem throws for division by zero.                                                   |
| `FactorOfChecksWhetherThisValueIsAFactorOfTheArgument`                   | Theory | Factor of checks whether this value is a factor of the argument.                       |
| `FactorOfThrowsWhenThisValueIsZeroBecauseItDividesByZero`                | Fact   | Factor of throws when this value is zero because it divides by zero.                   |
| `FromArgbBuildsAColorFromA32BitArgbValue`                                | Fact   | From argb builds a color from a 32 bit argb value.                                     |
| `FromArgbWithExplicitRedGreenBlueOverridesTheColorComponents`            | Fact   | From argb with explicit red green blue overrides the color components.                 |
| `FromArgbWithABaseColorAppliesTheNewAlphaToThatColor`                    | Fact   | From argb with a base color applies the new alpha to that color.                       |
| `FromArgbWithGreenAndBlueUsesFullyOpaqueAlpha`                           | Fact   | From argb with green and blue uses fully opaque alpha.                                 |
| `FromOleTranslatesAnOleColorValue`                                       | Fact   | From ole translates an ole color value.                                                |
| `FromWin32TranslatesAWindowsColorValueTheSameWayAsFromOle`               | Fact   | From win32 translates a windows color value the same way as from ole.                  |
| `GetBytesReturnsAFourByteArrayMatchingBitConverter`                      | Fact   | Get bytes returns a four byte array matching bit converter.                            |
| `HostToNetworkOrderThenNetworkToHostOrderRoundTrips`                     | Fact   | Host to network order then network to host order round trips.                          |
| `HostToNetworkOrderMatchesIpAddressImplementation`                       | Fact   | Host to network order matches ip address implementation.                               |
| `InChecksMembershipAgainstTheProvidedValues`                             | Theory | In checks membership against the provided values.                                      |
| `NotInIsTheInverseOfIn`                                                  | Theory | Not in is the inverse of in.                                                           |
| `InRangeIsInclusiveOfBothBoundariesUnlikeBetween`                        | Theory | In range is inclusive of both boundaries unlike between.                               |
| `IsEvenIdentifiesEvenNumbersIncludingZeroAndNegatives`                   | Theory | Is even identifies even numbers including zero and negatives.                          |
| `IsOddIdentifiesOddNumbersIncludingNegativesViaCsharpsRemainderOperator` | Theory | Is odd identifies odd numbers including negatives via csharps remainder operator.      |
| `IsEvenAndIsOddAreAlwaysExactOppositesForAnyInt`                         | Theory | Is even and is odd are always exact opposites for any int.                             |
| `IsLeapYearMatchesTheBclRulesIncludingTheCenturyException`               | Theory | Is leap year matches the bcl rules including the century exception.                    |
| `IsMultipleOfChecksDivisibility`                                         | Theory | Is multiple of checks divisibility.                                                    |
| `IsMultipleOfThrowsForAZeroFactor`                                       | Fact   | Is multiple of throws for a zero factor.                                               |
| `IsPrimeIdentifiesPrimesForPositiveInputs`                               | Theory | Is prime identifies primes for positive inputs.                                        |
| `IsPrimeReturnsFalseForZero`                                             | Fact   | Is prime returns false for zero.                                                       |
| `IsPrimeReturnsFalseForNegativeNumbers`                                  | Theory | Is prime returns false for negative numbers.                                           |
| `MaxReturnsTheLargerOfTwoValues`                                         | Theory | Max returns the larger of two values.                                                  |
| `MinReturnsTheSmallerOfTwoValues`                                        | Theory | Min returns the smaller of two values.                                                 |
| `SignReturnsMinusOneZeroOrOne`                                           | Theory | Sign returns minus one zero or one.                                                    |
| `SqlSystemTypeToSqlDbTypeMapsKnownSystemTypeIdsToTheirSqlDbType`         | Theory | Sql system type to sql db type maps known system type ids to their sql db type.        |
| `SqlSystemTypeToSqlDbTypeThrowsAGenericExceptionForAnUnrecognisedTypeId` | Fact   | Sql system type to sql db type throws a generic exception for an unrecognised type id. |
| `GreatestCommonDivisorComputesTheGcd`                                    | Theory | Greatest common divisor computes the gcd.                                              |
| `LeastCommonMultipleComputesTheLcm`                                      | Theory | Least common multiple computes the lcm.                                                |
| `DigitSumAddsTheAbsoluteDigits`                                          | Theory | Digit sum adds the absolute digits.                                                    |

### NetworkAndGeoExtensionsTests

`Jube.Test.Dictionary.Extensions.NetworkAndGeoExtensionsTests` — 10 tests. Category: Unit.

| Test                                                      | Kind   | Verifies                                                           |
|-----------------------------------------------------------|--------|--------------------------------------------------------------------|
| `AddressFamilyIsDetectedFromText`                         | Theory | Address family is detected from text.                              |
| `AddressesAreClassified`                                  | Theory | Addresses are classified.                                          |
| `IsLoopbackIpAddressRecognisesBothFamilies`               | Fact   | Is loopback ip address recognises both families.                   |
| `IsInCidrMatchesTheNetworkPrefix`                         | Theory | Is in cidr matches the network prefix.                             |
| `CidrRangeIsDescribed`                                    | Fact   | Cidr range is described.                                           |
| `IPv4AddressesConvertToAndFromNumbers`                    | Fact   | I pv4 addresses convert to and from numbers.                       |
| `NormaliseIpAddressProducesTheCanonicalText`              | Fact   | Normalise ip address produces the canonical text.                  |
| `InitialBearingDegreesPointsFromTheFirstToTheSecondPoint` | Fact   | Initial bearing degrees points from the first to the second point. |
| `GeohashRoundTripsToTheCellCentre`                        | Fact   | Geohash round trips to the cell centre.                            |
| `IsValidCoordinateChecksBothRanges`                       | Fact   | Is valid coordinate checks both ranges.                            |

### NumericAndReferenceExtensionsTests

`Jube.Test.Dictionary.Extensions.NumericAndReferenceExtensionsTests` — 19 tests. Category: Unit.

| Test                                                      | Kind   | Verifies                                                         |
|-----------------------------------------------------------|--------|------------------------------------------------------------------|
| `RoundToNearestSnapsToTheIncrement`                       | Theory | Round to nearest snaps to the increment.                         |
| `RoundToNearestIsUndefinedForANonPositiveIncrement`       | Fact   | Round to nearest is undefined for a non positive increment.      |
| `ProductWithMultipliesEveryValue`                         | Fact   | Product with multiplies every value.                             |
| `NthRootReturnsTheRealRoot`                               | Theory | Nth root returns the real root.                                  |
| `NthRootOfANegativeNumberWithAnEvenDegreeIsUndefined`     | Fact   | Nth root of a negative number with an even degree is undefined.  |
| `ModuloTakesTheSignOfTheDivisor`                          | Theory | Modulo takes the sign of the divisor.                            |
| `PercentHelpersApplyAndReverseAPercentage`                | Fact   | Percent helpers apply and reverse a percentage.                  |
| `ConvertAtRateRejectsANonPositiveRate`                    | Fact   | Convert at rate rejects a non positive rate.                     |
| `CurrencyMinorUnitExponentFollowsIso4217`                 | Theory | Currency minor unit exponent follows iso4217.                    |
| `MinorUnitsConvertExactlyWithoutBinaryDrift`              | Fact   | Minor units convert exactly without binary drift.                |
| `RoundToCurrencyUsesTheCurrencyExponent`                  | Fact   | Round to currency uses the currency exponent.                    |
| `CurrencyAndCountryCodesValidateAgainstTheEmbeddedTables` | Fact   | Currency and country codes validate against the embedded tables. |
| `CountryLookupsAcceptEitherAlpha2OrAlpha3`                | Theory | Country lookups accept either alpha2 or alpha3.                  |
| `RomanNumeralsRoundTrip`                                  | Theory | Roman numerals round trip.                                       |
| `FromRomanNumeralsRejectsNonCanonicalNumerals`            | Theory | From roman numerals rejects non canonical numerals.              |
| `RadixStringsRoundTrip`                                   | Theory | Radix strings round trip.                                        |
| `FromRadixStringRejectsADigitOutsideTheBase`              | Fact   | From radix string rejects a digit outside the base.              |
| `ConvertUnitsConvertsWithinADimension`                    | Theory | Convert units converts within a dimension.                       |
| `ConvertUnitsIsUndefinedAcrossDimensions`                 | Fact   | Convert units is undefined across dimensions.                    |

### RuleLibraryPurityTests

`Jube.Test.Dictionary.Extensions.RuleLibraryPurityTests` — 3 tests. Category: Unit.

| Test                                                 | Kind   | Verifies                                                     |
|------------------------------------------------------|--------|--------------------------------------------------------------|
| `PureCodeReachesNoFileNetworkProcessOrNativeInterop` | Theory | Pure code reaches no file network process or native interop. |
| `TheScanCoversTheRuleLibrary`                        | Fact   | The scan covers the rule library.                            |
| `TheScannerWouldCatchAFileWrite`                     | Fact   | The scanner would catch a file write.                        |

### StringExtensionsPart1Tests

`Jube.Test.Dictionary.Extensions.StringExtensionsPart1Tests` — 78 tests. Category: Unit.

| Test                                                                                           | Kind   | Verifies                                                                                                       |
|------------------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------|
| `Br2NlReplacesSelfClosingAndOpenBreakTagsWithCrlf`                                             | Fact   | Br2 nl replaces self closing and open break tags with crlf.                                                    |
| `Br2NlLeavesUnmatchedCasingUntouchedBecauseTheReplaceIsCaseSensitive`                          | Fact   | Br2 nl leaves unmatched casing untouched because the replace is case sensitive.                                |
| `CardNetworkClassifiesByIinPrefixRanges`                                                       | Theory | Card network classifies by iin prefix ranges.                                                                  |
| `CardNetworkIgnoresSpacesAndDashes`                                                            | Fact   | Card network ignores spaces and dashes.                                                                        |
| `CompareOrdinalTwoArgOverloadOrdersLexicallyByOrdinalValue`                                    | Fact   | Compare ordinal two arg overload orders lexically by ordinal value.                                            |
| `CompareOrdinalFiveArgOverloadComparesOnlyTheSpecifiedSubstrings`                              | Fact   | Compare ordinal five arg overload compares only the specified substrings.                                      |
| `ConcatTwoArgJoinsTheStringsWithNoSeparator`                                                   | Fact   | Concat two arg joins the strings with no separator.                                                            |
| `ConcatThreeArgJoinsAllThreeStrings`                                                           | Fact   | Concat three arg joins all three strings.                                                                      |
| `ConcatFourArgJoinsAllFourStrings`                                                             | Fact   | Concat four arg joins all four strings.                                                                        |
| `ConcatTreatsANullArgumentAsAnEmptyStringJustLikeStringConcat`                                 | Fact   | Concat treats a null argument as an empty string just like string concat.                                      |
| `ConcatenateJoinsAnEnumerableOfStringsWithNoSeparator`                                         | Fact   | Concatenate joins an enumerable of strings with no separator.                                                  |
| `ConcatenateOfAnEmptySequenceProducesAnEmptyString`                                            | Fact   | Concatenate of an empty sequence produces an empty string.                                                     |
| `ConcatenateWithASelectorAppliesTheProjectionBeforeJoining`                                    | Fact   | Concatenate with a selector applies the projection before joining.                                             |
| `ConcatWithAppendsEachVarargValueInOrder`                                                      | Fact   | Concat with appends each vararg value in order.                                                                |
| `ConcatWithNoValuesReturnsTheOriginalStringUnchanged`                                          | Fact   | Concat with no values returns the original string unchanged.                                                   |
| `ContainsAllReturnsTrueOnlyWhenEveryValueIsPresent`                                            | Fact   | Contains all returns true only when every value is present.                                                    |
| `ContainsAllWithComparisonTypeRespectsCaseInsensitivity`                                       | Fact   | Contains all with comparison type respects case insensitivity.                                                 |
| `ContainsAnyReturnsTrueWhenAtLeastOneValueIsPresent`                                           | Fact   | Contains any returns true when at least one value is present.                                                  |
| `ContainsAnyWithComparisonTypeRespectsCaseInsensitivity`                                       | Fact   | Contains any with comparison type respects case insensitivity.                                                 |
| `TheSingleArgContainsExtensionIsShadowedByStringsOwnInstanceContainsMethod`                    | Fact   | The single arg contains extension is shadowed by strings own instance contains method.                         |
| `TheContainsWithComparisonTypeOverloadIsAlsoShadowedByAnIdenticalBclInstanceOverload`          | Fact   | The contains with comparison type overload is also shadowed by an identical bcl instance overload.             |
| `ConvertToUtf32CombinesTheSurrogatePairAtTheGivenIndexIntoItsCodePoint`                        | Fact   | Convert to utf32 combines the surrogate pair at the given index into its code point.                           |
| `DecodeBase64RecoversTheOriginalAsciiText`                                                     | Fact   | Decode base64 recovers the original ascii text.                                                                |
| `DecodeBase64ThrowsForMalformedBase64Input`                                                    | Fact   | Decode base64 throws for malformed base64 input.                                                               |
| `EncodeBase64ThenDecodeBase64RoundTripsTheOriginalText`                                        | Fact   | Encode base64 then decode base64 round trips the original text.                                                |
| `DeserializeJsonParsesAMatchingJsonObjectIntoThePocoType`                                      | Fact   | Deserialize json parses a matching json object into the poco type.                                             |
| `DeserializeJsonReturnsDefaultRatherThanThrowingForMalformedJson`                              | Fact   | Deserialize json returns default rather than throwing for malformed json.                                      |
| `DeserializeJsonReturnsDefaultForNullEmptyOrWhitespaceInputWithoutAttemptingToParse`           | Theory | Deserialize json returns default for null empty or whitespace input without attempting to parse.               |
| `DeserializeJsonWithAnExplicitEncodingAlsoParsesCorrectly`                                     | Fact   | Deserialize json with an explicit encoding also parses correctly.                                              |
| `DeserializeJsonWithANullEncodingFallsBackToUtf8`                                              | Fact   | Deserialize json with a null encoding falls back to utf8.                                                      |
| `DigitCountCountsDigitCharacters`                                                              | Theory | Digit count counts digit characters.                                                                           |
| `LetterCountCountsLetterCharacters`                                                            | Theory | Letter count counts letter characters.                                                                         |
| `EmailDomainReturnsEverythingAfterTheAtSign`                                                   | Theory | Email domain returns everything after the at sign.                                                             |
| `EmailLocalPartReturnsEverythingBeforeTheAtSign`                                               | Theory | Email local part returns everything before the at sign.                                                        |
| `EqualsIgnoreCaseComparesOrdinallyIgnoringCase`                                                | Fact   | Equals ignore case compares ordinally ignoring case.                                                           |
| `EscapeXmlEscapesAllFiveReservedXmlCharacters`                                                 | Fact   | Escape xml escapes all five reserved xml characters.                                                           |
| `EscapeXmlOnPlainTextIsAnIdentityOperation`                                                    | Fact   | Escape xml on plain text is an identity operation.                                                             |
| `ExtractKeepsOnlyCharactersMatchingThePredicate`                                               | Fact   | Extract keeps only characters matching the predicate.                                                          |
| `ExtractWithAPredicateThatMatchesNothingReturnsAnEmptyString`                                  | Fact   | Extract with a predicate that matches nothing returns an empty string.                                         |
| `ExtractDecimalConcatenatesEveryDigitAndDotInTheStringRatherThanFindingOneNumber`              | Fact   | Extract decimal concatenates every digit and dot in the string rather than finding one number.                 |
| `ExtractDecimalThrowsWhenTheStringContainsNoDigits`                                            | Fact   | Extract decimal throws when the string contains no digits.                                                     |
| `ExtractDoubleHasTheSameWholeStringConcatenationBehaviourAsExtractDecimal`                     | Fact   | Extract double has the same whole string concatenation behaviour as extract decimal.                           |
| `ExtractInt16ConcatenatesAllDigitsHonouringOnlyALeadingMinusBeforeTheFirstDigit`               | Theory | Extract int16 concatenates all digits honouring only a leading minus before the first digit.                   |
| `ExtractInt16ThrowsForAnEmptyDigitAccumulation`                                                | Fact   | Extract int16 throws for an empty digit accumulation.                                                          |
| `ExtractInt32MergesMultipleSeparateNumbersInTheStringIntoOne`                                  | Fact   | Extract int32 merges multiple separate numbers in the string into one.                                         |
| `ExtractInt64BehavesLikeExtractInt32ButForA64BitResult`                                        | Fact   | Extract int64 behaves like extract int32 but for a 64 bit result.                                              |
| `ExtractLetterKeepsOnlyLetterCharacters`                                                       | Fact   | Extract letter keeps only letter characters.                                                                   |
| `ExtractManyDecimalFindsEachSeparateSignedDecimalNumberIndependently`                          | Fact   | Extract many decimal finds each separate signed decimal number independently.                                  |
| `ExtractManyDecimalOnAStringWithNoNumbersReturnsAnEmptyArray`                                  | Fact   | Extract many decimal on a string with no numbers returns an empty array.                                       |
| `ExtractManyDoubleFindsEachSeparateSignedNumberIndependentlyUnlikeExtractDouble`               | Fact   | Extract many double finds each separate signed number independently unlike extract double.                     |
| `ExtractManyInt16FindsEachSeparateSignedIntegerIndependently`                                  | Fact   | Extract many int16 finds each separate signed integer independently.                                           |
| `ExtractManyInt32FindsEachSeparateSignedIntegerIndependently`                                  | Fact   | Extract many int32 finds each separate signed integer independently.                                           |
| `ExtractManyInt64FindsEachSeparateSignedIntegerIndependently`                                  | Fact   | Extract many int64 finds each separate signed integer independently.                                           |
| `ExtractManyUInt16IgnoresTheMinusSignAndOnlyMatchesTheDigitsThemselves`                        | Fact   | Extract many u int16 ignores the minus sign and only matches the digits themselves.                            |
| `ExtractManyUInt32IgnoresTheMinusSignJustLikeExtractManyUInt16`                                | Fact   | Extract many u int32 ignores the minus sign just like extract many u int16.                                    |
| `ExtractManyUInt64FindsEachUnsignedNumberInTheString`                                          | Fact   | Extract many u int64 finds each unsigned number in the string.                                                 |
| `ExtractNumberKeepsOnlyCharactersThatCharIsNumberRecognises`                                   | Fact   | Extract number keeps only characters that char is number recognises.                                           |
| `ExtractUInt16ConcatenatesAllDigitsWithNoNegativeSignSupportAtAll`                             | Fact   | Extract u int16 concatenates all digits with no negative sign support at all.                                  |
| `ExtractUInt32ConcatenatesAllDigitsAcrossTheWholeString`                                       | Fact   | Extract u int32 concatenates all digits across the whole string.                                               |
| `ExtractUInt64ThrowsWhenNoDigitsArePresent`                                                    | Fact   | Extract u int64 throws when no digits are present.                                                             |
| `FormatOneArgDelegatesToStringFormat`                                                          | Fact   | Format one arg delegates to string format.                                                                     |
| `FormatTwoArgDelegatesToStringFormat`                                                          | Fact   | Format two arg delegates to string format.                                                                     |
| `FormatThreeArgDelegatesToStringFormat`                                                        | Fact   | Format three arg delegates to string format.                                                                   |
| `FormatObjectArrayOverloadDelegatesToStringFormat`                                             | Fact   | Format object array overload delegates to string format.                                                       |
| `FormatThrowsAFormatExceptionForAnUnmatchedPlaceholder`                                        | Fact   | Format throws a format exception for an unmatched placeholder.                                                 |
| `FormatWithOneArgBehavesTheSameAsFormat`                                                       | Fact   | Format with one arg behaves the same as format.                                                                |
| `FormatWithTwoArgBehavesTheSameAsFormat`                                                       | Fact   | Format with two arg behaves the same as format.                                                                |
| `FormatWithThreeArgBehavesTheSameAsFormat`                                                     | Fact   | Format with three arg behaves the same as format.                                                              |
| `FormatWithParamsArrayOverloadAcceptsAnyNumberOfValues`                                        | Fact   | Format with params array overload accepts any number of values.                                                |
| `GetAfterReturnsEverythingFollowingTheFirstOccurrenceOfTheMarker`                              | Fact   | Get after returns everything following the first occurrence of the marker.                                     |
| `GetAfterReturnsEmptyStringWhenTheMarkerIsNotFound`                                            | Fact   | Get after returns empty string when the marker is not found.                                                   |
| `GetBeforeReturnsEverythingPrecedingTheFirstOccurrenceOfTheMarker`                             | Fact   | Get before returns everything preceding the first occurrence of the marker.                                    |
| `GetBeforeReturnsEmptyStringWhenTheMarkerIsNotFound`                                           | Fact   | Get before returns empty string when the marker is not found.                                                  |
| `GetBetweenReturnsTheTextBetweenTheFirstBeforeMarkerAndTheNextAfterMarker`                     | Fact   | Get between returns the text between the first before marker and the next after marker.                        |
| `GetBetweenReturnsEmptyStringWhenTheBeforeMarkerIsAbsentButShortEnoughNotToOverrun`            | Fact   | Get between returns empty string when the before marker is absent but short enough not to overrun.             |
| `GetBetweenReturnsEmptyStringWhenTheBeforeMarkerIsAbsentAndLongerThanTheRemainingStringLength` | Fact   | Get between returns empty string when the before marker is absent and longer than the remaining string length. |
| `GetBetweenReturnsEmptyStringWhenTheAfterMarkerIsMissing`                                      | Fact   | Get between returns empty string when the after marker is missing.                                             |
| `GetNumericValueReturnsTheDigitsValueOrMinusOneForNonDigitsAtTheGivenIndex`                    | Theory | Get numeric value returns the digits value or minus one for non digits at the given index.                     |

### StringExtensionsPart2Tests

`Jube.Test.Dictionary.Extensions.StringExtensionsPart2Tests` — 53 tests. Category: Unit.

| Test                                                                                | Kind   | Verifies                                                                                              |
|-------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------------|
| `GetUnicodeCategoryLooksAtTheCharacterAtTheGivenIndex`                              | Theory | Get unicode category looks at the character at the given index.                                       |
| `HtmlAttributeEncodeEscapesQuotesButNotAllHtmlMetacharacters`                       | Fact   | Html attribute encode escapes quotes but not all html metacharacters.                                 |
| `HtmlEncodeEscapesAngleBracketsAndAmpersands`                                       | Fact   | Html encode escapes angle brackets and ampersands.                                                    |
| `HtmlDecodeReversesHtmlEncode`                                                      | Fact   | Html decode reverses html encode.                                                                     |
| `IfEmptyReturnsTheOriginalValueWhenItIsNotEmpty`                                    | Fact   | If empty returns the original value when it is not empty.                                             |
| `IfEmptyReturnsTheDefaultWhenTheOriginalIsEmpty`                                    | Fact   | If empty returns the default when the original is empty.                                              |
| `IfEmptyTreatsNullAsNotEmptyBecauseItOnlyChecksAgainstTheEmptyString`               | Fact   | If empty treats null as not empty because it only checks against the empty string.                    |
| `InChecksMembershipAgainstTheProvidedValues`                                        | Theory | In checks membership against the provided values.                                                     |
| `InitialsOfReturnsTheUppercasedFirstLetterOfEachWord`                               | Theory | Initials of returns the uppercased first letter of each word.                                         |
| `InternReturnsTheSameReferenceForEqualStringsOnceInterned`                          | Fact   | Intern returns the same reference for equal strings once interned.                                    |
| `IsAlphaAcceptsOnlyAsciiLettersAndTreatsEmptyAsTrue`                                | Theory | Is alpha accepts only ascii letters and treats empty as true.                                         |
| `IsAlphaNumericAcceptsOnlyAsciiLettersAndDigitsAndTreatsEmptyAsTrue`                | Theory | Is alpha numeric accepts only ascii letters and digits and treats empty as true.                      |
| `IsAnagramComparesCharacterMultisetsCaseSensitively`                                | Theory | Is anagram compares character multisets case sensitively.                                             |
| `IsControlLooksAtTheCharacterAtTheGivenIndex`                                       | Theory | Is control looks at the character at the given index.                                                 |
| `IsDigitLooksAtTheCharacterAtTheGivenIndex`                                         | Theory | Is digit looks at the character at the given index.                                                   |
| `IsEmptyIsTrueOnlyForTheExactEmptyString`                                           | Fact   | Is empty is true only for the exact empty string.                                                     |
| `IsEmptyThrowsOnNullBecauseItUsesReferenceEqualityViaTheEqualityOperator`           | Fact   | Is empty throws on null because it uses reference equality via the equality operator.                 |
| `IsHighSurrogateLooksAtTheCharacterAtTheGivenIndex`                                 | Fact   | Is high surrogate looks at the character at the given index.                                          |
| `IsInternedReturnsTheStringWhenInternedAndNullWhenNot`                              | Fact   | Is interned returns the string when interned and null when not.                                       |
| `IsLetterLooksAtTheCharacterAtTheGivenIndex`                                        | Theory | Is letter looks at the character at the given index.                                                  |
| `IsLetterOrDigitLooksAtTheCharacterAtTheGivenIndex`                                 | Theory | Is letter or digit looks at the character at the given index.                                         |
| `IsLikeSupportsStarWildcardAndHashDigitPlaceholder`                                 | Theory | Is like supports star wildcard and hash digit placeholder.                                            |
| `IsLowerLooksAtTheCharacterAtTheGivenIndex`                                         | Theory | Is lower looks at the character at the given index.                                                   |
| `IsLowSurrogateLooksAtTheCharacterAtTheGivenIndex`                                  | Fact   | Is low surrogate looks at the character at the given index.                                           |
| `IsMatchDelegatesToRegexIsMatch`                                                    | Fact   | Is match delegates to regex is match.                                                                 |
| `IsMatchWithOptionsHonoursTheGivenRegexOptions`                                     | Fact   | Is match with options honours the given regex options.                                                |
| `IsNotEmptyIsTheInverseOfIsEmpty`                                                   | Fact   | Is not empty is the inverse of is empty.                                                              |
| `IsNotNullDistinguishesNullFromNonNull`                                             | Fact   | Is not null distinguishes null from non null.                                                         |
| `IsNotNullOrEmptyDelegatesToStringIsNullOrEmptyNegated`                             | Fact   | Is not null or empty delegates to string is null or empty negated.                                    |
| `IsNotNullOrWhiteSpaceDelegatesToStringIsNullOrWhiteSpaceNegated`                   | Fact   | Is not null or white space delegates to string is null or white space negated.                        |
| `IsNullDistinguishesNullFromNonNull`                                                | Fact   | Is null distinguishes null from non null.                                                             |
| `IsNullOrEmptyDelegatesToTheBcl`                                                    | Fact   | Is null or empty delegates to the bcl.                                                                |
| `IsNullOrWhiteSpaceDelegatesToTheBcl`                                               | Fact   | Is null or white space delegates to the bcl.                                                          |
| `IsNumberLooksAtTheCharacterAtTheGivenIndex`                                        | Theory | Is number looks at the character at the given index.                                                  |
| `IsNumericOnlyAcceptsDigitsZeroToNineAndTreatsEmptyAsTrue`                          | Theory | Is numeric only accepts digits zero to nine and treats empty as true.                                 |
| `IsPalindromeStripsNonAlphanumericsAndIsCaseInsensitive`                            | Theory | Is palindrome strips non alphanumerics and is case insensitive.                                       |
| `IsPunctuationLooksAtTheCharacterAtTheGivenIndex`                                   | Theory | Is punctuation looks at the character at the given index.                                             |
| `IsSeparatorLooksAtTheCharacterAtTheGivenIndex`                                     | Theory | Is separator looks at the character at the given index.                                               |
| `IsSurrogateLooksAtTheCharacterAtTheGivenIndex`                                     | Fact   | Is surrogate looks at the character at the given index.                                               |
| `IsSurrogatePairLooksAtTheCharacterPairStartingAtTheGivenIndex`                     | Fact   | Is surrogate pair looks at the character pair starting at the given index.                            |
| `IsSymbolLooksAtTheCharacterAtTheGivenIndex`                                        | Theory | Is symbol looks at the character at the given index.                                                  |
| `IsUpperLooksAtTheCharacterAtTheGivenIndex`                                         | Theory | Is upper looks at the character at the given index.                                                   |
| `IsValidBicChecksTheEightOrElevenCharacterSwiftFormat`                              | Theory | Is valid bic checks the eight or eleven character swift format.                                       |
| `IsValidCardExpiryAcceptsMmSlashYyOrMmSlashYyyyNotYetElapsed`                       | Theory | Is valid card expiry accepts mm slash yy or mm slash yyyy not yet elapsed.                            |
| `IsValidEmailAcceptsCommonFormsAndRejectsObviouslyMalformedInput`                   | Theory | Is valid email accepts common forms and rejects obviously malformed input.                            |
| `IsValidIpAcceptsWellFormedIpv4AndRejectsOutOfRangeOrNonIpv4Input`                  | Theory | Is valid ip accepts well formed ipv4 and rejects out of range or non ipv4 input.                      |
| `IsValidIpRejectsALeadingZeroAsTheEntireFirstOctetEvenThoughZeroIsAValidOctetValue` | Fact   | Is valid ip rejects a leading zero as the entire first octet even though zero is a valid octet value. |
| `IsValidIpAcceptsThreeDigitLeadingZeroFormsBecauseItMatchesTextNotNumericValue`     | Fact   | Is valid ip accepts three digit leading zero forms because it matches text not numeric value.         |
| `HasSequentialDigitsDetectsAscendingOrDescendingRunsOfAtLeastTheGivenLength`        | Theory | Has sequential digits detects ascending or descending runs of at least the given length.              |
| `HasSequentialDigitsTreatsAMinimumRunLengthBelowTwoAsTwo`                           | Fact   | Has sequential digits treats a minimum run length below two as two.                                   |
| `IsAllSameCharacterChecksEveryCharacterMatchesTheFirst`                             | Theory | Is all same character checks every character matches the first.                                       |
| `IsValidLuhnValidatesTheChecksumDigit`                                              | Theory | Is valid luhn validates the checksum digit.                                                           |
| `IsValidIbanValidatesTheMod97Checksum`                                              | Theory | Is valid iban validates the mod97 checksum.                                                           |

### StringExtensionsPart3Tests

`Jube.Test.Dictionary.Extensions.StringExtensionsPart3Tests` — 103 tests. Category: Unit.

| Test                                                                   | Kind   | Verifies                                                                           |
|------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------|
| `IsWhiteSpaceChecksTheCharacterAtTheGivenIndex`                        | Fact   | Is white space checks the character at the given index.                            |
| `IsWhiteSpaceThrowsForAnOutOfRangeIndex`                               | Fact   | Is white space throws for an out of range index.                                   |
| `JavaScriptStringEncodeEscapesSpecialCharacters`                       | Fact   | Java script string encode escapes special characters.                              |
| `JavaScriptStringEncodeWithDoubleQuotesWrapsTheResultInQuotes`         | Fact   | Java script string encode with double quotes wraps the result in quotes.           |
| `JoinWithAStringArrayConcatenatesUsingTheSeparator`                    | Fact   | Join with a string array concatenates using the separator.                         |
| `JoinWithAnObjectArrayConcatenatesTheirStringRepresentations`          | Fact   | Join with an object array concatenates their string representations.               |
| `JoinWithAGenericEnumerableConcatenatesUsingTheSeparator`              | Fact   | Join with a generic enumerable concatenates using the separator.                   |
| `JoinWithAnEnumerableOfStringConcatenatesUsingTheSeparator`            | Fact   | Join with an enumerable of string concatenates using the separator.                |
| `JoinWithAStartIndexAndCountOnlyJoinsTheSelectedRange`                 | Fact   | Join with a start index and count only joins the selected range.                   |
| `JoinOnAnEmptyArrayReturnsAnEmptyString`                               | Fact   | Join on an empty array returns an empty string.                                    |
| `LeftReturnsTheRequestedNumberOfLeadingCharacters`                     | Fact   | Left returns the requested number of leading characters.                           |
| `LeftWithZeroReturnsAnEmptyString`                                     | Fact   | Left with zero returns an empty string.                                            |
| `LeftThrowsWhenTheRequestedLengthExceedsTheStringsLength`              | Fact   | Left throws when the requested length exceeds the strings length.                  |
| `LeftSafeClampsToTheStringsActualLengthInsteadOfThrowing`              | Fact   | Left safe clamps to the strings actual length instead of throwing.                 |
| `LeftSafeBehavesLikeLeftWhenTheLengthIsWithinBounds`                   | Fact   | Left safe behaves like left when the length is within bounds.                      |
| `MatchWithoutOptionsFindsTheFirstOccurrence`                           | Fact   | Match without options finds the first occurrence.                                  |
| `MatchWithOptionsRespectsThoseOptions`                                 | Fact   | Match with options respects those options.                                         |
| `MatchReturnsAnUnsuccessfulMatchWhenThereIsNoOccurrence`               | Fact   | Match returns an unsuccessful match when there is no occurrence.                   |
| `MatchesWithoutOptionsFindsEveryOccurrence`                            | Fact   | Matches without options finds every occurrence.                                    |
| `MatchesWithOptionsRespectsThoseOptions`                               | Fact   | Matches with options respects those options.                                       |
| `MatchesReturnsAnEmptyCollectionWhenThereAreNoOccurrences`             | Fact   | Matches returns an empty collection when there are no occurrences.                 |
| `Nl2BrConvertsWindowsAndUnixNewlinesToHtmlBreaks`                      | Fact   | Nl2 br converts windows and unix newlines to html breaks.                          |
| `Nl2BrLeavesTextWithoutNewlinesUnchanged`                              | Fact   | Nl2 br leaves text without newlines unchanged.                                     |
| `NotInIsTheInverseOfMembership`                                        | Theory | Not in is the inverse of membership.                                               |
| `NullIfEmptyReturnsNullForNullOrEmptyAndTheOriginalValueOtherwise`     | Fact   | Null if empty returns null for null or empty and the original value otherwise.     |
| `NullIfEmptyDoesNotTreatWhitespaceAsEmpty`                             | Fact   | Null if empty does not treat whitespace as empty.                                  |
| `ParseQueryStringParsesKeyValuePairs`                                  | Fact   | Parse query string parses key value pairs.                                         |
| `ParseQueryStringWithAnExplicitEncodingStillParsesKeyValuePairs`       | Fact   | Parse query string with an explicit encoding still parses key value pairs.         |
| `ParseQueryStringDecodesUrlEncodedValues`                              | Fact   | Parse query string decodes url encoded values.                                     |
| `RemoveDiacriticsStripsAccentsButKeepsTheBaseLetters`                  | Fact   | Remove diacritics strips accents but keeps the base letters.                       |
| `RemoveDiacriticsLeavesPlainAsciiUnchanged`                            | Fact   | Remove diacritics leaves plain ascii unchanged.                                    |
| `RemoveLetterStripsAllLettersButKeepsEverythingElse`                   | Fact   | Remove letter strips all letters but keeps everything else.                        |
| `RemoveNumberStripsAllDigitsButKeepsEverythingElse`                    | Fact   | Remove number strips all digits but keeps everything else.                         |
| `RemoveWhereStripsCharactersMatchingTheGivenPredicate`                 | Fact   | Remove where strips characters matching the given predicate.                       |
| `RemoveWhereWithAPredicateThatNeverMatchesLeavesTheStringUnchanged`    | Fact   | Remove where with a predicate that never matches leaves the string unchanged.      |
| `RepeatWithASingleCharacterStringRepeatsItNTimes`                      | Fact   | Repeat with a single character string repeats it n times.                          |
| `RepeatWithAMultiCharacterStringRepeatsTheWholeStringNTimes`           | Fact   | Repeat with a multi character string repeats the whole string n times.             |
| `RepeatWithZeroProducesAnEmptyString`                                  | Fact   | Repeat with zero produces an empty string.                                         |
| `ReplaceByEmptyRemovesEveryOccurrenceOfEachGivenValue`                 | Fact   | Replace by empty removes every occurrence of each given value.                     |
| `ReplaceByEmptyWithNoValuesLeavesTheStringUnchanged`                   | Fact   | Replace by empty with no values leaves the string unchanged.                       |
| `ReplaceWithStartIndexLengthAndValueSplicesInTheReplacement`           | Fact   | Replace with start index length and value splices in the replacement.              |
| `ReplaceFirstReplacesOnlyTheFirstOccurrence`                           | Fact   | Replace first replaces only the first occurrence.                                  |
| `ReplaceFirstLeavesTheStringUnchangedWhenThereIsNoMatch`               | Fact   | Replace first leaves the string unchanged when there is no match.                  |
| `ReplaceFirstWithANumberReplacesExactlyThatManyLeadingOccurrences`     | Theory | Replace first with a number replaces exactly that many leading occurrences.        |
| `ReplaceLastReplacesOnlyTheLastOccurrence`                             | Fact   | Replace last replaces only the last occurrence.                                    |
| `ReplaceLastLeavesTheStringUnchangedWhenThereIsNoMatch`                | Fact   | Replace last leaves the string unchanged when there is no match.                   |
| `ReplaceLastWithANumberReplacesExactlyThatManyTrailingOccurrences`     | Theory | Replace last with a number replaces exactly that many trailing occurrences.        |
| `ReplaceWhenEqualsSwapsTheEntireStringWhenItExactlyMatches`            | Fact   | Replace when equals swaps the entire string when it exactly matches.               |
| `ReplaceWhenEqualsLeavesTheStringUnchangedOnAPartialMatch`             | Fact   | Replace when equals leaves the string unchanged on a partial match.                |
| `ReverseFlipsTheCharacterOrder`                                        | Fact   | Reverse flips the character order.                                                 |
| `ReverseOfAnEmptyOrSingleCharacterStringReturnsItUnchanged`            | Fact   | Reverse of an empty or single character string returns it unchanged.               |
| `RightReturnsTheRequestedNumberOfTrailingCharacters`                   | Fact   | Right returns the requested number of trailing characters.                         |
| `RightThrowsWhenTheRequestedLengthExceedsTheStringsLength`             | Fact   | Right throws when the requested length exceeds the strings length.                 |
| `RightSafeClampsToTheStringsActualLengthInsteadOfThrowing`             | Fact   | Right safe clamps to the strings actual length instead of throwing.                |
| `RightSafeBehavesLikeRightWhenTheLengthIsWithinBounds`                 | Fact   | Right safe behaves like right when the length is within bounds.                    |
| `SplitByAMultiCharacterSeparatorSplitsCorrectly`                       | Fact   | Split by a multi character separator splits correctly.                             |
| `SplitWithRemoveEmptyEntriesOmitsEmptySegments`                        | Fact   | Split with remove empty entries omits empty segments.                              |
| `SplitDefaultsToKeepingEmptySegments`                                  | Fact   | Split defaults to keeping empty segments.                                          |
| `ToByteArrayEncodesUsingAscii`                                         | Fact   | To byte array encodes using ascii.                                                 |
| `ToByteArrayReplacesNonAsciiCharactersWithQuestionMarks`               | Fact   | To byte array replaces non ascii characters with question marks.                   |
| `ToEnumParsesAMatchingMemberName`                                      | Fact   | To enum parses a matching member name.                                             |
| `ToEnumIsCaseSensitiveBecauseItCallsEnumParseWithoutIgnoreCase`        | Fact   | To enum is case sensitive because it calls enum parse without ignore case.         |
| `ToEnumThrowsForAMemberNameThatDoesNotExist`                           | Fact   | To enum throws for a member name that does not exist.                              |
| `ToTitleCaseWithoutAnExplicitCultureUsesEnUs`                          | Fact   | To title case without an explicit culture uses en us.                              |
| `ToTitleCaseWithAnExplicitCultureUsesThatCulture`                      | Fact   | To title case with an explicit culture uses that culture.                          |
| `ToTitleCaseDoesNotLowercaseAlreadyUppercaseWords`                     | Fact   | To title case does not lowercase already uppercase words.                          |
| `ToValidDateTimeOrNullParsesAWellFormedDate`                           | Fact   | To valid date time or null parses a well formed date.                              |
| `ToValidDateTimeOrNullReturnsNullForUnparsableText`                    | Fact   | To valid date time or null returns null for unparsable text.                       |
| `ToValidDateTimeOrNullReturnsNullForAnEmptyString`                     | Fact   | To valid date time or null returns null for an empty string.                       |
| `ToXDocumentParsesWellFormedXmlIntoAnXDocument`                        | Fact   | To x document parses well formed xml into an x document.                           |
| `ToXDocumentThrowsForMalformedXml`                                     | Fact   | To x document throws for malformed xml.                                            |
| `ToXmlDocumentParsesWellFormedXmlIntoAnXmlDocument`                    | Fact   | To xml document parses well formed xml into an xml document.                       |
| `ToXmlDocumentThrowsForMalformedXml`                                   | Fact   | To xml document throws for malformed xml.                                          |
| `TruncateLeavesShortStringsUnchanged`                                  | Fact   | Truncate leaves short strings unchanged.                                           |
| `TruncateAppendsAnEllipsisWhenTheStringExceedsTheMaxLength`            | Fact   | Truncate appends an ellipsis when the string exceeds the max length.               |
| `TruncateWithACustomSuffixUsesThatSuffixInstead`                       | Fact   | Truncate with a custom suffix uses that suffix instead.                            |
| `TruncateOnANullStringReturnsNull`                                     | Fact   | Truncate on a null string returns null.                                            |
| `TruncateThrowsWhenTheMaxLengthIsShorterThanTheSuffixItself`           | Fact   | Truncate throws when the max length is shorter than the suffix itself.             |
| `UrlEncodeThenUrlDecodeRoundTripsSpecialCharacters`                    | Fact   | Url encode then url decode round trips special characters.                         |
| `UrlEncodeConvertsSpacesToPlusSigns`                                   | Fact   | Url encode converts spaces to plus signs.                                          |
| `UrlEncodeWithAnExplicitEncodingStillEncodesSpaces`                    | Fact   | Url encode with an explicit encoding still encodes spaces.                         |
| `UrlDecodeWithAnExplicitEncodingDecodesBackToTheOriginalText`          | Fact   | Url decode with an explicit encoding decodes back to the original text.            |
| `UrlEncodeToBytesThenUrlDecodeToBytesRoundTrips`                       | Fact   | Url encode to bytes then url decode to bytes round trips.                          |
| `UrlEncodeToBytesWithAnExplicitEncodingProducesTheSameTextWhenDecoded` | Fact   | Url encode to bytes with an explicit encoding produces the same text when decoded. |
| `UrlDecodeToBytesWithAnExplicitEncodingDecodesCorrectly`               | Fact   | Url decode to bytes with an explicit encoding decodes correctly.                   |
| `UrlPathEncodeEncodesSpacesAsPercentTwentyRatherThanAPlusSign`         | Fact   | Url path encode encodes spaces as percent twenty rather than a plus sign.          |
| `LevenshteinDistanceComputesTheEditDistance`                           | Theory | Levenshtein distance computes the edit distance.                                   |
| `SimilarityToOfIdenticalStringsIsOne`                                  | Fact   | Similarity to of identical strings is one.                                         |
| `SimilarityToNormalisesTheEditDistanceByTheLongerStringLength`         | Fact   | Similarity to normalises the edit distance by the longer string length.            |
| `JaroWinklerSimilarityOfIdenticalStringsIsOne`                         | Fact   | Jaro winkler similarity of identical strings is one.                               |
| `JaroWinklerSimilarityOfAnEmptyAndNonEmptyStringIsZero`                | Fact   | Jaro winkler similarity of an empty and non empty string is zero.                  |
| `JaroWinklerSimilarityMatchesTheWellKnownMarthaMarhtaExample`          | Fact   | Jaro winkler similarity matches the well known martha marhta example.              |
| `JaroWinklerSimilarityMatchesTheWellKnownDixonDicksonxExample`         | Fact   | Jaro winkler similarity matches the well known dixon dicksonx example.             |
| `SoundexEncodesNamesToTheirPhoneticCode`                               | Theory | Soundex encodes names to their phonetic code.                                      |
| `SoundexOfSimilarSoundingNamesIsTheSame`                               | Fact   | Soundex of similar sounding names is the same.                                     |
| `SoundexOfBlankOrLetterlessInputIsEmpty`                               | Theory | Soundex of blank or letterless input is empty.                                     |
| `ShannonEntropyOfAllSameCharacterIsZero`                               | Fact   | Shannon entropy of all same character is zero.                                     |
| `ShannonEntropyOfTwoEquallyLikelyCharactersIsOneBit`                   | Fact   | Shannon entropy of two equally likely characters is one bit.                       |
| `ShannonEntropyOfFourEquallyLikelyDistinctCharactersIsTwoBits`         | Fact   | Shannon entropy of four equally likely distinct characters is two bits.            |
| `ShannonEntropyOfAnEmptyOrNullStringIsZero`                            | Fact   | Shannon entropy of an empty or null string is zero.                                |
| `MaskExceptLastMasksEverythingButTheTrailingVisibleCount`              | Theory | Mask except last masks everything but the trailing visible count.                  |
| `MaskExceptLastAcceptsACustomMaskCharacter`                            | Fact   | Mask except last accepts a custom mask character.                                  |
| `NormalizeEmailAliasStripsPlusTaggingAndLowercases`                    | Theory | Normalize email alias strips plus tagging and lowercases.                          |

### StringFuzzyExtensionsTests

`Jube.Test.Dictionary.Extensions.StringFuzzyExtensionsTests` — 31 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                     |
|--------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `IsSomehowInListUsesTheDefaultProfile`                             | Theory | Is somehow in list uses the default profile.                                 |
| `IsSomehowInListIsFalseForANullList`                               | Fact   | Is somehow in list is false for a null list.                                 |
| `IsSomehowInListAcceptsAnOptionsString`                            | Fact   | Is somehow in list accepts an options string.                                |
| `AnInvalidOptionsStringFailsClosed`                                | Fact   | An invalid options string fails closed.                                      |
| `ANullOrBlankOptionsStringMeansTheDefaultProfile`                  | Fact   | A null or blank options string means the default profile.                    |
| `IsSomehowLikeComparesToASingleValue`                              | Fact   | Is somehow like compares to a single value.                                  |
| `ContainsSomehowInListFindsEntriesInsideLongerText`                | Fact   | Contains somehow in list finds entries inside longer text.                   |
| `ContainsSomehowInListForcesWithinScopeWhateverTheOptionsSay`      | Fact   | Contains somehow in list forces within scope whatever the options say.       |
| `FuzzyBestScoreIsTheHighestScoreAcrossTheList`                     | Fact   | Fuzzy best score is the highest score across the list.                       |
| `FuzzyBestScoreCanBeComparedInARule`                               | Fact   | Fuzzy best score can be compared in a rule.                                  |
| `FuzzyMatchCountCountsEntriesThatPass`                             | Fact   | Fuzzy match count counts entries that pass.                                  |
| `FuzzyBestMatchReturnsTheOriginalEntryOrNull`                      | Fact   | Fuzzy best match returns the original entry or null.                         |
| `IsValidFuzzyOptionsChecksAnOptionsStringWithoutRunningIt`         | Theory | Is valid fuzzy options checks an options string without running it.          |
| `FuzzyOptionsErrorExplainsWhatIsWrong`                             | Fact   | Fuzzy options error explains what is wrong.                                  |
| `IsNormalisedInListIsAnExactMatchAfterNormalisation`               | Fact   | Is normalised in list is an exact match after normalisation.                 |
| `IsLevenshteinInListHonoursTheEditBudget`                          | Fact   | Is levenshtein in list honours the edit budget.                              |
| `AnOutOfRangeDistanceFailsClosed`                                  | Fact   | An out of range distance fails closed.                                       |
| `IsDamerauInListTreatsATranspositionAsOneEdit`                     | Fact   | Is damerau in list treats a transposition as one edit.                       |
| `IsSimilarInListUsesTheLevenshteinRatio`                           | Fact   | Is similar in list uses the levenshtein ratio.                               |
| `IsJaroWinklerInListUsesThePrefixWeightedScore`                    | Fact   | Is jaro winkler in list uses the prefix weighted score.                      |
| `IsDiceInListUsesBigramOverlap`                                    | Fact   | Is dice in list uses bigram overlap.                                         |
| `IsSoundexInListMatchesNamesThatSoundAlike`                        | Fact   | Is soundex in list matches names that sound alike.                           |
| `IsTokenSortInListIgnoresWordOrder`                                | Fact   | Is token sort in list ignores word order.                                    |
| `IsTokenSetInListToleratesMissingWords`                            | Fact   | Is token set in list tolerates missing words.                                |
| `IsInitialsInListMatchesAbbreviatedFirstNames`                     | Fact   | Is initials in list matches abbreviated first names.                         |
| `EveryNamedAlgorithmIsFalseForNullValuesAndNullLists`              | Fact   | Every named algorithm is false for null values and null lists.               |
| `TheUserFacingExampleReadsNaturally`                               | Fact   | The user facing example reads naturally.                                     |
| `MiddleInitialsAndMissingMiddleNamesAreHandledByTheRightAlgorithm` | Fact   | Middle initials and missing middle names are handled by the right algorithm. |
| `CompanyNamesMatchAcrossLegalSuffixes`                             | Fact   | Company names match across legal suffixes.                                   |
| `ATransliteratedNameMatchesAcrossDiacritics`                       | Fact   | A transliterated name matches across diacritics.                             |
| `HyphenatedAndApostropheNamesMatchTheirPlainForms`                 | Fact   | Hyphenated and apostrophe names match their plain forms.                     |

### StringToIsoDateTimeTests

`Jube.Test.Dictionary.Extensions.StringToIsoDateTimeTests` — 2 tests. Category: Unit.

| Test                           | Kind   | Verifies                            |
|--------------------------------|--------|-------------------------------------|
| `IsoTextIsReadAsUniversalTime` | Theory | Iso text is read as universal time. |
| `TextThatIsNotADateThrows`     | Theory | Text that is not a date throws.     |

### TextNetworkAndWalletExtensionsTests

`Jube.Test.Dictionary.Extensions.TextNetworkAndWalletExtensionsTests` — 11 tests. Category: Unit.

| Test                                                    | Kind   | Verifies                                                          |
|---------------------------------------------------------|--------|-------------------------------------------------------------------|
| `FoldedForMatchingReducesEvasionToTheUnderlyingWord`    | Theory | Folded for matching reduces evasion to the underlying word.       |
| `FoldedForMatchingOfNullIsEmpty`                        | Fact   | Folded for matching of null is empty.                             |
| `TransliterateToLatinMapsCyrillicAndGreek`              | Theory | Transliterate to latin maps cyrillic and greek.                   |
| `CanonicalPairKeyIsTheSameForBothDirections`            | Fact   | Canonical pair key is the same for both directions.               |
| `DirectionSignReportsWhichSideSortsFirst`               | Fact   | Direction sign reports which side sorts first.                    |
| `NumberInTextWithinPercentOfChecksEveryNumberInTheText` | Theory | Number in text within percent of checks every number in the text. |
| `IsDelimitedMemberMatchesOneToken`                      | Theory | Is delimited member matches one token.                            |
| `IsInAnyCidrMatchesAnyOfTheRanges`                      | Fact   | Is in any cidr matches any of the ranges.                         |
| `IsCryptoWalletAddressAcceptsOnlyWellFormedAddresses`   | Theory | Is crypto wallet address accepts only well formed addresses.      |
| `IsCryptoWalletAddressIsFalseForNull`                   | Fact   | Is crypto wallet address is false for null.                       |
| `JsonTextArrayAnyInListMatchesAMemberOfTheArray`        | Fact   | Json text array any in list matches a member of the array.        |
