---
layout: default
title: Other Services
nav_order: 19
parent: Testing
---

# Other Services

Tests of the remaining service surface: agent helpers, extension methods and shared service infrastructure.

## Agent

### AdvancedAnalyticsTrainingMatrixTests

`Jube.Test.Service.Agent.AdvancedAnalyticsTrainingMatrixTests` — 13 tests. Category: Unit.

| Test                                                                    | Kind | Verifies                                                                              |
|-------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `ThePredictorsArePackedInTheOrderTheCallerNamedThemAndNotTheTableOrder` | Fact | The predictors are packed in the order the caller named them and not the table order. |
| `TheLabelIsAlwaysTheLastColumnWhateverItsPositionInTheTable`            | Fact | The label is always the last column whatever its position in the table.               |
| `APredictorMayBeLeftOutAltogether`                                      | Fact | A predictor may be left out altogether.                                               |
| `ABlankPredictorBecomesAMissingValueRatherThanAZero`                    | Fact | A blank predictor becomes a missing value rather than a zero.                         |
| `ABooleanCountsAsOneOrNought`                                           | Fact | A boolean counts as one or nought.                                                    |
| `ANumberHeldAsTextIsParsedRatherThanRefused`                            | Fact | A number held as text is parsed rather than refused.                                  |
| `TextThatIsNotANumberIsRefusedRatherThanQuietlyEncoded`                 | Fact | Text that is not a number is refused rather than quietly encoded.                     |
| `ALabelThatIsNotBinaryIsRefused`                                        | Fact | A label that is not binary is refused.                                                |
| `AMissingLabelIsRefusedBecauseThereIsNothingToImputeAnOutcomeFrom`      | Fact | A missing label is refused because there is nothing to impute an outcome from.        |
| `AColumnTheTableDoesNotHoldIsReportedWithTheOnesItDoes`                 | Fact | A column the table does not hold is reported with the ones it does.                   |
| `TheSameColumnCannotBeBothAPredictorAndTheLabel`                        | Fact | The same column cannot be both a predictor and the label.                             |
| `APredictorNamedTwiceIsRefused`                                         | Fact | A predictor named twice is refused.                                                   |
| `NamingNoPredictorAtAllIsRefused`                                       | Fact | Naming no predictor at all is refused.                                                |

### AgentHierarchyDocumentTests

`Jube.Test.Service.Agent.AgentHierarchyDocumentTests` — 9 tests. Category: Unit.

| Test                                                          | Kind | Verifies                                                                 |
|---------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `TheDocumentIsPresentAndParsesIntoLeafAgentsThatAllHoldTools` | Fact | The document is present and parses into leaf agents that all hold tools. |
| `EveryCataloguedToolIsOwnedByAnAgent`                         | Fact | Every catalogued tool is owned by an agent.                              |
| `NoAgentOwnsAToolThatTheCatalogueDoesNotCarry`                | Fact | No agent owns a tool that the catalogue does not carry.                  |
| `NoMutatingToolIsOwnedByMoreThanOneAgent`                     | Fact | No mutating tool is owned by more than one agent.                        |
| `OnlyReadsAreSharedBetweenAgents`                             | Fact | Only reads are shared between agents.                                    |
| `NoAgentHoldsMoreThanFifteenTools`                            | Fact | No agent holds more than fifteen tools.                                  |
| `EveryAgentSlugIsUniqueBecauseItIsTheAddress`                 | Fact | Every agent slug is unique because it is the address.                    |
| `EveryMutatingToolIsFlaggedForConfirmationAndNoReadIs`        | Fact | Every mutating tool is flagged for confirmation and no read is.          |
| `EveryToolCitesThePermissionSourceItWasReadFrom`              | Fact | Every tool cites the permission source it was read from.                 |

### FilterOperationCoverageTests

`Jube.Test.Service.Agent.FilterOperationCoverageTests` — 3 tests. Category: Unit.

| Test                                                                      | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `EveryListingServiceExposesFilterFieldsFilterAndCountOverItsListDto`      | Fact | Every listing service exposes filter fields filter and count over its list dto.       |
| `EveryFilterOperationIsARepeatableReadNamedAfterTheListAreaAndCatalogued` | Fact | Every filter operation is a repeatable read named after the list area and catalogued. |
| `EveryListDtoOffersAtLeastTheIdToFilterOn`                                | Fact | Every list dto offers at least the id to filter on.                                   |

### HierarchyAgentDefinitionsTests

`Jube.Test.Service.Agent.HierarchyAgentDefinitionsTests` — 13 tests. Category: Unit.

| Test                                                                       | Kind | Verifies                                                                                |
|----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `TheHierarchyIsRegisteredAlongsideTheOriginalThreeAgents`                  | Fact | The hierarchy is registered alongside the original three agents.                        |
| `EveryCataloguedToolIsClaimedByAtLeastOneLeafAgent`                        | Fact | Every catalogued tool is claimed by at least one leaf agent.                            |
| `EveryMutatingToolIsClaimedByExactlyOneLeafAgent`                          | Fact | Every mutating tool is claimed by exactly one leaf agent.                               |
| `OnlyIdempotentNonDestructiveReadsAreSharedBetweenLeafAgents`              | Fact | Only idempotent non destructive reads are shared between leaf agents.                   |
| `NoLeafAgentClaimsMoreThanFifteenTools`                                    | Fact | No leaf agent claims more than fifteen tools.                                           |
| `EveryLeafThatWritesAnObjectCanAlsoReadAndValidateIt`                      | Fact | Every leaf that writes an object can also read and validate it.                         |
| `EveryDelegateNamedByAnAgentExists`                                        | Fact | Every delegate named by an agent exists.                                                |
| `TheRootDelegatesOnlyToTheNineDomainsSoItsPromptDoesNotListEveryAgent`     | Fact | The root delegates only to the nine domains so its prompt does not list every agent.    |
| `EveryLeafIsReachableFromTheRootWithinTheDelegationDepthBudget`            | Fact | Every leaf is reachable from the root within the delegation depth budget.               |
| `NoParentExceedsTheFanOutBudget`                                           | Fact | No parent exceeds the fan out budget.                                                   |
| `AnAgentHoldingAMutatingToolRequiresAPreservationSnapshot`                 | Fact | An agent holding a mutating tool requires a preservation snapshot.                      |
| `TheYokeCountReportsTheWholeBoundToolSurfaceNotJustTheGeneralistsOwnTools` | Fact | The yoke count reports the whole bound tool surface not just the generalists own tools. |
| `EveryLeafDeclaresThePermissionsItsOwnToolsRequire`                        | Fact | Every leaf declares the permissions its own tools require.                              |

### PageAgentToolBinderBindTests

`Jube.Test.Service.Agent.PageAgentToolBinderBindTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                              |
|---------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `ABoundToolTakesItsNameAndPermissionsFromTheCatalogue`                          | Fact | A bound tool takes its name and permissions from the catalogue.                       |
| `ABoundWriteToolRequiresASnapshotAndIsSkippedEntirelyWhenReadOnly`              | Fact | A bound write tool requires a snapshot and is skipped entirely when read only.        |
| `TheGeneratedSchemaCarriesTheParameterDescriptionsTheServiceDeclares`           | Fact | The generated schema carries the parameter descriptions the service declares.         |
| `InvokingABoundReadToolConstructsTheServiceAndReturnsItsResultAsync`            | Fact | Invoking a bound read tool constructs the service and returns its result.             |
| `InvokingABoundWriteToolReturnsItsResultWithoutWalkingTheAssociationGraphAsync` | Fact | Invoking a bound write tool returns its result without walking the association graph. |
| `InvokingABoundToolAsAUserWithoutThePermissionIsRefusedByTheServiceAsync`       | Fact | Invoking a bound tool as a user without the permission is refused by the service.     |

### PageAgentToolBinderTests

`Jube.Test.Service.Agent.PageAgentToolBinderTests` — 6 tests. Category: Service.

| Test                                                        | Kind | Verifies                                                              |
|-------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `AddBoundReadsPermissionsFromTheCatalogueNotAHandTypedCopy` | Fact | Add bound reads permissions from the catalogue not a hand typed copy. |
| `AddBoundMarksAReadToolAsNotRequiringASnapshot`             | Fact | Add bound marks a read tool as not requiring a snapshot.              |
| `AddBoundMarksAWriteToolAsRequiringASnapshot`               | Fact | Add bound marks a write tool as requiring a snapshot.                 |
| `AddBoundThrowsForAToolNameNotInTheCatalogue`               | Fact | Add bound throws for a tool name not in the catalogue.                |
| `AddBoundSkipsAWriteToolEntirelyWhenReadOnly`               | Fact | Add bound skips a write tool entirely when read only.                 |
| `AddBoundStillBindsAReadToolWhenReadOnly`                   | Fact | Add bound still binds a read tool when read only.                     |

### ServiceBoundToolCoverageTests

`Jube.Test.Service.Agent.ServiceBoundToolCoverageTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                            |
|---------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------|
| `NoMoreToolCallsRunAtOnceThanTheConfiguredCapAsync`                                   | Fact | No more tool calls run at once than the configured cap.                                             |
| `EveryLeafAgentBindsAtLeastOneToolWithTheDependenciesTheConversationServiceCanSupply` | Fact | Every leaf agent binds at least one tool with the dependencies the conversation service can supply. |
| `EveryCataloguedToolBindsRatherThanBeingSkippedForAnUnresolvableDependency`           | Fact | Every catalogued tool binds rather than being skipped for an unresolvable dependency.               |
| `TheOnlyUnbindableToolsAreTheSevenWhoseDependenciesAreNotYetSupplied`                 | Fact | The only unbindable tools are the seven whose dependencies are not yet supplied.                    |
| `AReadOnlyDeploymentBindsStrictlyFewerToolsAndKeepsEveryRead`                         | Fact | A read only deployment binds strictly fewer tools and keeps every read.                             |

### ServiceOperationIndexTests

`Jube.Test.Service.Agent.ServiceOperationIndexTests` — 4 tests. Category: Unit.

| Test                                                  | Kind | Verifies                                                      |
|-------------------------------------------------------|------|---------------------------------------------------------------|
| `TheIndexBuildsWithoutADuplicateOperationName`        | Fact | The index builds without a duplicate operation name.          |
| `EveryCataloguedToolResolvesToADecoratedMethod`       | Fact | Every catalogued tool resolves to a decorated method.         |
| `EveryResolvedMethodAgreesWithTheCatalogueOnKind`     | Fact | Every resolved method agrees with the catalogue on kind.      |
| `EveryResolvedServiceExposesAFactoryTheBinderCanFind` | Fact | Every resolved service exposes a factory the binder can find. |

### ServiceToolCatalogueTests

`Jube.Test.Service.Agent.ServiceToolCatalogueTests` — 4 tests. Category: Service.

| Test                                                                         | Kind | Verifies                                                                                |
|------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `EveryToolNameIsUniqueAcrossTheWholeCatalogue`                               | Fact | Every tool name is unique across the whole catalogue.                                   |
| `EveryToolCarriesAtLeastOneRequiredPermissionUnlessItReadsCompiledKnowledge` | Fact | Every tool carries at least one required permission unless it reads compiled knowledge. |
| `EveryCompiledKnowledgeExemptionNamesAnOperationThatExistsAndIsARead`        | Fact | Every compiled knowledge exemption names an operation that exists and is a read.        |
| `EveryDescriptorsRequiredPermissionsMatchesItsServicesOwnField`              | Fact | Every descriptors required permissions matches its services own field.                  |

### ToolInventoryDocumentTests

`Jube.Test.Service.Agent.ToolInventoryDocumentTests` — 6 tests. Category: Unit.

| Test                                                             | Kind | Verifies                                                                      |
|------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `TheInventoryIsPresentAndEveryEntryHasBothAnIndexRowAndASection` | Fact | The inventory is present and every entry has both an index row and a section. |
| `TheInventoryCoversExactlyTheCatalogue`                          | Fact | The inventory covers exactly the catalogue.                                   |
| `NoOperationIsInventoriedTwice`                                  | Fact | No operation is inventoried twice.                                            |
| `EveryInventoryRowStatesTheKindTheCatalogueDeclares`             | Fact | Every inventory row states the kind the catalogue declares.                   |
| `EveryInventoryRowStatesTheAccessTheCatalogueEnforces`           | Fact | Every inventory row states the access the catalogue enforces.                 |
| `EveryInventoryRowNamesAnAgentTheHierarchyGivesTheOperationTo`   | Fact | Every inventory row names an agent the hierarchy gives the operation to.      |

### ValidateOperationCoverageTests

`Jube.Test.Service.Agent.ValidateOperationCoverageTests` — 6 tests. Category: Unit.

| Test                                                         | Kind   | Verifies                                                              |
|--------------------------------------------------------------|--------|-----------------------------------------------------------------------|
| `EveryAuthoredEntityServiceExposesValidate`                  | Fact   | Every authored entity service exposes validate.                       |
| `ValidateIsARepeatableReadNamedAfterTheCreateOperationsArea` | Fact   | Validate is a repeatable read named after the create operations area. |
| `ValidateTakesTheSameDtoAsCreate`                            | Fact   | Validate takes the same dto as create.                                |
| `EveryValidateOperationIsCataloguedOnceAsARead`              | Fact   | Every validate operation is catalogued once as a read.                |
| `EveryVbNetRuleServiceExposesACataloguedParseRule`           | Theory | Every vb net rule service exposes a catalogued parse rule.            |
| `OnlyTheVbNetRuleServicesExposeParseRule`                    | Fact   | Only the vb net rule services expose parse rule.                      |

## Dependency

### ModelEntityDeleteGuardTests

`Jube.Test.Service.Dependency.ModelEntityDeleteGuardTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                       | Kind | Verifies                                                          |
|------------------------------------------------------------|------|-------------------------------------------------------------------|
| `AListUsedInRuleTextCannotBeDeletedAndTheRuleIsNamedAsync` | Fact | A list used in rule text cannot be deleted and the rule is named. |
| `AListNothingUsesIsDeletedAsBeforeAsync`                   | Fact | A list nothing uses is deleted as before.                         |
| `ADeletedDependentNoLongerBlocksAsync`                     | Fact | A deleted dependent no longer blocks.                             |
| `AFieldATtlCounterAggregatesOnCannotBeDeletedAsync`        | Fact | A field a ttl counter aggregates on cannot be deleted.            |

## Extensions

### CalendarFactsToolsTests

`Jube.Test.Service.Extensions.CalendarFactsToolsTests` — 5 tests. Category: Unit.

| Test                                 | Kind   | Verifies                                |
|--------------------------------------|--------|-----------------------------------------|
| `DescribeDateReportsCalendarFacts`   | Fact   | Describe date reports calendar facts.   |
| `PeriodBoundariesSpanThePeriod`      | Theory | Period boundaries span the period.      |
| `PeriodBoundariesSupportSundayWeeks` | Fact   | Period boundaries support sunday weeks. |
| `FiscalAndYearFacts`                 | Fact   | Fiscal and year facts.                  |
| `BusinessHoursAndSameDay`            | Fact   | Business hours and same day.            |

### CurrencyAndCountryToolsTests

`Jube.Test.Service.Extensions.CurrencyAndCountryToolsTests` — 3 tests. Category: Unit.

| Test                                      | Kind | Verifies                                      |
|-------------------------------------------|------|-----------------------------------------------|
| `CurrencyReferenceData`                   | Fact | Currency reference data.                      |
| `MoneyArithmeticUsesTheCurrencyPrecision` | Fact | Money arithmetic uses the currency precision. |
| `CountryReferenceData`                    | Fact | Country reference data.                       |

### DateArithmeticToolsTests

`Jube.Test.Service.Extensions.DateArithmeticToolsTests` — 4 tests. Category: Unit.

| Test                       | Kind | Verifies                       |
|----------------------------|------|--------------------------------|
| `AddToDateClampsMonthEnds` | Fact | Add to date clamps month ends. |
| `BusinessDays`             | Fact | Business days.                 |
| `ElapsedTimeAndAge`        | Fact | Elapsed time and age.          |
| `IntervalsAndRanges`       | Fact | Intervals and ranges.          |

### EncodingAndHashingToolsTests

`Jube.Test.Service.Extensions.EncodingAndHashingToolsTests` — 4 tests. Category: Unit.

| Test                          | Kind   | Verifies                         |
|-------------------------------|--------|----------------------------------|
| `EncodeAndDecodeRoundTrip`    | Theory | Encode and decode round trip.    |
| `OneWayAndMalformedEncodings` | Fact   | One way and malformed encodings. |
| `DigestsAndChecksums`         | Fact   | Digests and checksums.           |
| `DescribeUuidReportsVersion`  | Fact   | Describe uuid reports version.   |

### FinancialIdentifiersToolsTests

`Jube.Test.Service.Extensions.FinancialIdentifiersToolsTests` — 3 tests. Category: Unit.

| Test                    | Kind | Verifies                |
|-------------------------|------|-------------------------|
| `BankIdentifiers`       | Fact | Bank identifiers.       |
| `CardIdentifiers`       | Fact | Card identifiers.       |
| `SecuritiesIdentifiers` | Fact | Securities identifiers. |

### FunctionToolDecorationTests

`Jube.Test.Service.Extensions.FunctionToolDecorationTests` — 4 tests. Category: Unit.

| Test                                               | Kind | Verifies                                                    |
|----------------------------------------------------|------|-------------------------------------------------------------|
| `EveryGroupLivesInItsOwnNamespaceAndSuitsOneAgent` | Fact | Every group lives in its own namespace and suits one agent. |
| `EveryToolAndParameterIsDescribedForTheModel`      | Fact | Every tool and parameter is described for the model.        |
| `ToolNamesAreGloballyUnique`                       | Fact | Tool names are globally unique.                             |
| `EveryToolHasATestThatDeclaresItCovers`            | Fact | Every tool has a test that declares it covers.              |

### FunctionToolPurityTests

`Jube.Test.Service.Extensions.FunctionToolPurityTests` — 4 tests. Category: Unit.

| Test                                                 | Kind   | Verifies                                                     |
|------------------------------------------------------|--------|--------------------------------------------------------------|
| `PureCodeReachesNoFileNetworkProcessOrNativeInterop` | Theory | Pure code reaches no file network process or native interop. |
| `ExtensionFacadesNeverReadTheClockOrARandomSource`   | Fact   | Extension facades never read the clock or a random source.   |
| `TheScanCoversEveryToolGroup`                        | Fact   | The scan covers every tool group.                            |
| `TheScannerWouldCatchAFileWrite`                     | Fact   | The scanner would catch a file write.                        |

### GeospatialToolsTests

`Jube.Test.Service.Extensions.GeospatialToolsTests` — 2 tests. Category: Unit.

| Test                   | Kind | Verifies                |
|------------------------|------|-------------------------|
| `DistancesAndSpeed`    | Fact | Distances and speed.    |
| `BearingsAndGeohashes` | Fact | Bearings and geohashes. |

### ListScreeningToolsTests

`Jube.Test.Service.Extensions.ListScreeningToolsTests` — 3 tests. Category: Unit.

| Test                                   | Kind | Verifies                                    |
|----------------------------------------|------|---------------------------------------------|
| `ScreenAgainstListReportsTheBestMatch` | Fact | Screen against list reports the best match. |
| `MembershipChecks`                     | Fact | Membership checks.                          |
| `InitialsPhoneticsAndOptions`          | Fact | Initials phonetics and options.             |

### NameSimilarityToolsTests

`Jube.Test.Service.Extensions.NameSimilarityToolsTests` — 4 tests. Category: Unit.

| Test                                       | Kind | Verifies                                         |
|--------------------------------------------|------|--------------------------------------------------|
| `CompareNamesScoresEveryAlgorithm`         | Fact | Compare names scores every algorithm.            |
| `EditDistances`                            | Fact | Edit distances.                                  |
| `PhoneticAndNormalisation`                 | Fact | Phonetic and normalisation.                      |
| `NamesMatchWithOptionsUsesTheRuleLanguage` | Fact | Names match with options uses the rule language. |

### OnlineIdentifiersToolsTests

`Jube.Test.Service.Extensions.OnlineIdentifiersToolsTests` — 3 tests. Category: Unit.

| Test                   | Kind | Verifies                 |
|------------------------|------|--------------------------|
| `EmailAddresses`       | Fact | Email addresses.         |
| `PhonesUrlsAndDomains` | Fact | Phones urls and domains. |
| `IpAddresses`          | Fact | Ip addresses.            |

### PatternMatchingToolsTests

`Jube.Test.Service.Extensions.PatternMatchingToolsTests` — 3 tests. Category: Unit.

| Test                                  | Kind | Verifies                                   |
|---------------------------------------|------|--------------------------------------------|
| `RegexTools`                          | Fact | Regex tools.                               |
| `RegexToolsExplainBadAndSlowPatterns` | Fact | Regex tools explain bad and slow patterns. |
| `EscapingAndWildcards`                | Fact | Escaping and wildcards.                    |

### PercentagesAndChangesToolsTests

`Jube.Test.Service.Extensions.PercentagesAndChangesToolsTests` — 3 tests. Category: Unit.

| Test                            | Kind | Verifies                          |
|---------------------------------|------|-----------------------------------|
| `PercentagesAreComputedExactly` | Fact | Percentages are computed exactly. |
| `RatiosAndGrowthRates`          | Fact | Ratios and growth rates.          |
| `PercentageAdjustments`         | Fact | Percentage adjustments.           |

### RoundingAndPowersToolsTests

`Jube.Test.Service.Extensions.RoundingAndPowersToolsTests` — 5 tests. Category: Unit.

| Test                                          | Kind | Verifies                                            |
|-----------------------------------------------|------|-----------------------------------------------------|
| `RoundNumberHonoursTheMidpointRule`           | Fact | Round number honours the midpoint rule.             |
| `RoundToNearestIncrementRefusesABadIncrement` | Fact | Round to nearest increment refuses a bad increment. |
| `WholeNumberRoundingFollowsItsDirection`      | Fact | Whole number rounding follows its direction.        |
| `ClampNumberLimitsToTheRange`                 | Fact | Clamp number limits to the range.                   |
| `PowersRootsAndLogarithmsAreExact`            | Fact | Powers roots and logarithms are exact.              |

### StructuredDataToolsTests

`Jube.Test.Service.Extensions.StructuredDataToolsTests` — 2 tests. Category: Unit.

| Test        | Kind | Verifies    |
|-------------|------|-------------|
| `JsonPaths` | Fact | Json paths. |
| `XmlPaths`  | Fact | Xml paths.  |

### TextCleaningToolsTests

`Jube.Test.Service.Extensions.TextCleaningToolsTests` — 2 tests. Category: Unit.

| Test            | Kind | Verifies       |
|-----------------|------|----------------|
| `Normalisation` | Fact | Normalisation. |
| `Filters`       | Fact | Filters.       |

### TextEditingToolsTests

`Jube.Test.Service.Extensions.TextEditingToolsTests` — 3 tests. Category: Unit.

| Test                | Kind | Verifies             |
|---------------------|------|----------------------|
| `CaseReverseRepeat` | Fact | Case reverse repeat. |
| `Slicing`           | Fact | Slicing.             |
| `Replacement`       | Fact | Replacement.         |

### TextExtractionToolsTests

`Jube.Test.Service.Extensions.TextExtractionToolsTests` — 4 tests. Category: Unit.

| Test                                    | Kind | Verifies                                    |
|-----------------------------------------|------|---------------------------------------------|
| `Markers`                               | Fact | Markers.                                    |
| `Tokens`                                | Fact | Tokens.                                     |
| `NumbersAndQueries`                     | Fact | Numbers and queries.                        |
| `ExtractNumbersIgnoresTheServerCulture` | Fact | Extract numbers ignores the server culture. |

### TextInspectionToolsTests

`Jube.Test.Service.Extensions.TextInspectionToolsTests` — 4 tests. Category: Unit.

| Test                             | Kind | Verifies                           |
|----------------------------------|------|------------------------------------|
| `DescribeTextMeasuresEverything` | Fact | Describe text measures everything. |
| `CountsAndRuns`                  | Fact | Counts and runs.                   |
| `Comparisons`                    | Fact | Comparisons.                       |
| `Containment`                    | Fact | Containment.                       |

### ThresholdsAndLimitsToolsTests

`Jube.Test.Service.Extensions.ThresholdsAndLimitsToolsTests` — 3 tests. Category: Unit.

| Test                    | Kind | Verifies                 |
|-------------------------|------|--------------------------|
| `NearThresholdMeasures` | Fact | Near threshold measures. |
| `LimitMeasures`         | Fact | Limit measures.          |
| `AmountShapeMeasures`   | Fact | Amount shape measures.   |

### TimeZonesAndFormatsToolsTests

`Jube.Test.Service.Extensions.TimeZonesAndFormatsToolsTests` — 4 tests. Category: Unit.

| Test                                | Kind | Verifies                               |
|-------------------------------------|------|----------------------------------------|
| `ConversionsAllowForDaylightSaving` | Fact | Conversions allow for daylight saving. |
| `ZoneDescriptions`                  | Fact | Zone descriptions.                     |
| `EpochRoundTrip`                    | Fact | Epoch round trip.                      |
| `FormattingAndParsing`              | Fact | Formatting and parsing.                |

### UnitsAndNumeralsToolsTests

`Jube.Test.Service.Extensions.UnitsAndNumeralsToolsTests` — 4 tests. Category: Unit.

| Test                                    | Kind | Verifies                                    |
|-----------------------------------------|------|---------------------------------------------|
| `ConvertUnitsWithinAndAcrossDimensions` | Fact | Convert units within and across dimensions. |
| `RomanNumerals`                         | Fact | Roman numerals.                             |
| `BaseConversion`                        | Fact | Base conversion.                            |
| `WholeNumberFacts`                      | Fact | Whole number facts.                         |

## Helpers

### ClockAndIdentifiersToolsTests

`Jube.Test.Service.Helpers.ClockAndIdentifiersToolsTests` — 3 tests. Category: Unit.

| Test                             | Kind | Verifies                             |
|----------------------------------|------|--------------------------------------|
| `ClockToolsReadTheInjectedClock` | Fact | Clock tools read the injected clock. |
| `ListTimeZoneIdsFilters`         | Fact | List time zone ids filters.          |
| `UuidTools`                      | Fact | Uuid tools.                          |

### TextListsToolsTests

`Jube.Test.Service.Helpers.TextListsToolsTests` — 2 tests. Category: Unit.

| Test              | Kind | Verifies           |
|-------------------|------|--------------------|
| `DistinctAndJoin` | Fact | Distinct and join. |
| `SetOperations`   | Fact | Set operations.    |
