---
layout: default
title: Data Access, Migrations and Archive
nav_order: 22
parent: Testing
---

# Data Access, Migrations and Archive

Tests of the data layer itself: repositories, migrations, reporting SQL, resilient Npgsql behaviour, archive writes,
preservation and volume characteristics.

## Archive writes

### ArchiveReprocessingWriteTests

`Jube.Test.ArchiveWrites.ArchiveReprocessingWriteTests` — 7 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                        |
|--------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `UnchangedKeysAreLeftAloneWithoutAVersionAsync`                          | Fact | Unchanged keys are left alone without a version.                                |
| `AChangedKeyIsVersionedAndUpdatedInPlaceAsync`                           | Fact | A changed key is versioned and updated in place.                                |
| `NewKeysAreAddedAndKeysNoLongerProducedAreRemovedWithTheirVersionsAsync` | Fact | New keys are added and keys no longer produced are removed with their versions. |
| `AnotherRecordsKeysAreNeverTouchedAsync`                                 | Fact | Another records keys are never touched.                                         |
| `TheArchiveRecordIsVersionedAndUpdatedAsync`                             | Fact | The archive record is versioned and updated.                                    |
| `ABatchUpdatesEveryRecordInOneGoAndReportsTheOnesThatAreGoneAsync`       | Fact | A batch updates every record in one go and reports the ones that are gone.      |
| `UpdatingAnArchiveRecordThatDoesNotExistThrowsAsync`                     | Fact | Updating an archive record that does not exist throws.                          |

## Migrations

### AbstractionCalculationCoderOnlyMigrationTests

`Jube.Test.Migrations.AbstractionCalculationCoderOnlyMigrationTests` — 26 tests. Category: Unit. Collection:
EvalExpressionRegistry.

| Test                                                                              | Kind   | Verifies                                                                                       |
|-----------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `EachArithmeticTypeBecomesItsFluentMethod`                                        | Theory | Each arithmetic type becomes its fluent method.                                                |
| `SpacesInAnAbstractionNameBecomeUnderscoresAsTheEngineDoes`                       | Fact   | Spaces in an abstraction name become underscores as the engine does.                           |
| `SurroundingWhiteSpaceIsTrimmed`                                                  | Fact   | Surrounding white space is trimmed.                                                            |
| `OnlyTheFourArithmeticTypesAreConverted`                                          | Theory | Only the four arithmetic types are converted.                                                  |
| `ANameThatCannotBeATokenIsNotConvertedSoNothingCanBeInjected`                     | Theory | A name that cannot be a token is not converted so nothing can be injected.                     |
| `APlanConvertsWhatItCanAndKeepsAPlaceholderForWhatItCannot`                       | Fact   | A plan converts what it can and keeps a placeholder for what it cannot.                        |
| `ThePlaceholderRecordsWhatTheCalculationWasSoDroppingTheColumnsLosesNothing`      | Theory | The placeholder records what the calculation was so dropping the columns loses nothing.        |
| `ThePlaceholderIsAlwaysASingleCommentLineSoANameCannotInjectCode`                 | Fact   | The placeholder is always a single comment line so a name cannot inject code.                  |
| `ThePlaceholderCapsVeryLongNames`                                                 | Fact   | The placeholder caps very long names.                                                          |
| `APlaceholderIsNotAValidFunctionSoItCannotBeActivatedByAccident`                  | Fact   | A placeholder is not a valid function so it cannot be activated by accident.                   |
| `APlanIgnoresRowsThatAreAlreadyCoderOrHaveNoType`                                 | Fact   | A plan ignores rows that are already coder or have no type.                                    |
| `APlanOfNothingIsEmpty`                                                           | Fact   | A plan of nothing is empty.                                                                    |
| `TheMigrationCarriesAVersionAndHasNoDownStep`                                     | Fact   | The migration carries a version and has no down step.                                          |
| `AMigratedCalculationGivesTheSameResultAsTheOldArithmetic`                        | Theory | A migrated calculation gives the same result as the old arithmetic.                            |
| `ADivisionByZeroStillStoresZeroNotNaNOrInfinity`                                  | Fact   | A division by zero still stores zero not na n or infinity.                                     |
| `AnOverflowStillStoresZero`                                                       | Fact   | An overflow still stores zero.                                                                 |
| `AMissingAbstractionStillReadsAsZeroRatherThanAnException`                        | Fact   | A missing abstraction still reads as zero rather than an exception.                            |
| `NamesWithSpacesResolveAgainstTheUnderscoredAbstractionKeys`                      | Fact   | Names with spaces resolve against the underscored abstraction keys.                            |
| `AMigratedScriptPassesTheSameParseTheEngineAppliesAtSync`                         | Fact   | A migrated script passes the same parse the engine applies at sync.                            |
| `AMigratedScriptNamingAnAbstractionThatDoesNotExistIsRefusedAtTranslation`        | Fact   | A migrated script naming an abstraction that does not exist is refused at translation.         |
| `ZeroIfUndefinedKeepsFiniteNumbersAndZeroesTheRest`                               | Theory | Zero if undefined keeps finite numbers and zeroes the rest.                                    |
| `ZeroIfUndefinedCompletesAFluentCalculationSoTheOldStoredZeroIsAChoice`           | Fact   | Zero if undefined completes a fluent calculation so the old stored zero is a choice.           |
| `TheDtoNoLongerExposesTheArithmeticSurface`                                       | Fact   | The dto no longer exposes the arithmetic surface.                                              |
| `ThePocoNoLongerMapsTheRemovedColumnsSoLinq2DbNeverSelectsThem`                   | Fact   | The poco no longer maps the removed columns so linq2 db never selects them.                    |
| `MessagePackKeysOfTheRemainingPropertiesAreUnchangedSoTheRemovedSlotsAreReserved` | Fact   | Message pack keys of the remaining properties are unchanged so the removed slots are reserved. |
| `MessagePackKeysOfTheVersionPocoAreUnchangedSoTheRemovedSlotsAreReserved`         | Fact   | Message pack keys of the version poco are unchanged so the removed slots are reserved.         |

### ListDictionaryValueUniquenessMigrationTests

`Jube.Test.Migrations.ListDictionaryValueUniquenessMigrationTests` — 9 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                         |
|-----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `ListValueDuplicatesAreDedupedKeepingNewestWithAuditAndApprovalAsync`       | Fact | List value duplicates are deduped keeping newest with audit and approval.        |
| `ListValueThreeOrMoreDuplicatesKeepsOnlyTheNewestAsync`                     | Fact | List value three or more duplicates keeps only the newest.                       |
| `ListValueMultipleDuplicateGroupsInTheSameListAreDedupedIndependentlyAsync` | Fact | List value multiple duplicate groups in the same list are deduped independently. |
| `ListValueDuplicatesAcrossDifferentListsAreDedupedIndependentlyAsync`       | Fact | List value duplicates across different lists are deduped independently.          |
| `ListValueAlreadyDeletedRowSharingTheValueIsLeftUntouchedAsync`             | Fact | List value already deleted row sharing the value is left untouched.              |
| `ListValueRunningTheMigrationAgainWithNothingNewToDedupeIsANoOpAsync`       | Fact | List value running the migration again with nothing new to dedupe is a no op.    |
| `ListValueKeepsTheNewestNonExpiredDuplicateOverAnExpiredNewerOneAsync`      | Fact | List value keeps the newest non expired duplicate over an expired newer one.     |
| `DictionaryKvpDuplicatesAreDedupedKeepingNewestWithAuditAndApprovalAsync`   | Fact | Dictionary kvp duplicates are deduped keeping newest with audit and approval.    |
| `DictionaryKvpKeepsTheNewestNonExpiredDuplicateOverAnExpiredNewerOneAsync`  | Fact | Dictionary kvp keeps the newest non expired duplicate over an expired newer one. |

### PenTestHardeningMigrationTests

`Jube.Test.Migrations.PenTestHardeningMigrationTests` — 17 tests. Category: Service. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                       |
|---------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `TokensValidFromColumnExistsAsync`                                                    | Fact | Tokens valid from column exists.                                                               |
| `TokensValidFromIsANullableTimestampWithoutTimeZoneAsync`                             | Fact | Tokens valid from is a nullable timestamp without time zone.                                   |
| `EveryMigrationOfTheBranchIsRecordedAndTheRemovedOnesAreNotAsync`                     | Fact | Every migration of the branch is recorded and the removed ones are not.                        |
| `UserLogoutHasTheIntendedColumnsTypesAndNullabilityAsync`                             | Fact | User logout has the intended columns types and nullability.                                    |
| `UserLogoutIdIsTheIdentityPrimaryKeyAndInsertsGetAnIdAsync`                           | Fact | User logout id is the identity primary key and inserts get an id.                              |
| `UserLogoutHasItsTwoIndexesInTheIntendedColumnOrderAsync`                             | Fact | User logout has its two indexes in the intended column order.                                  |
| `AllowSynchronisationExistsAndTheAdministratorRoleHoldsItAsync`                       | Fact | Allow synchronisation exists and the administrator role holds it.                              |
| `EveryNameUniqueIndexExistsAsync`                                                     | Fact | Every name unique index exists.                                                                |
| `VisualisationRegistryNamesAreUniquePerTenantIgnoringCaseAmongLiveRowsAsync`          | Fact | Visualisation registry names are unique per tenant ignoring case among live rows.              |
| `AUniqueViolationIsMappedToTheNameDuplicateValidationOutcomeAsync`                    | Fact | A unique violation is mapped to the name duplicate validation outcome.                         |
| `OtherDatabaseErrorsAreNotSwallowedAsync`                                             | Fact | Other database errors are not swallowed.                                                       |
| `UserRegistryNameUniqueIndexExistsAndEnforcesAcrossTenantsIgnoringCaseAsync`          | Fact | User registry name unique index exists and enforces across tenants ignoring case.              |
| `DuplicateRenamingIsCollisionProofScopedAndIdempotentAsync`                           | Fact | Duplicate renaming is collision proof scoped and idempotent.                                   |
| `DuplicateRenamingWithoutAScopeColumnCoversTheWholeTableAsync`                        | Fact | Duplicate renaming without a scope column covers the whole table.                              |
| `GrantDuplicatesAreSoftDeletedKeepingTheLowestIdAndTheIndexEnforcesOneLiveGrantAsync` | Fact | Grant duplicates are soft deleted keeping the lowest id and the index enforces one live grant. |
| `EveryGrantTableAndParentColumnTheMigrationNamesExistsAsync`                          | Fact | Every grant table and parent column the migration names exists.                                |
| `EveryRoleGrantTableHasItsUniqueIndexAsync`                                           | Fact | Every role grant table has its unique index.                                                   |

## Preservation

### EntityApprovalPreservationTests

`Jube.Test.Preservation.EntityApprovalPreservationTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                            |
|-----------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `ApprovalHistoryRoundTripsVerbatimAgainstPreservedVersionsAsync`            | Fact | Approval history round trips verbatim against preserved versions.                   |
| `AnExportWithoutAnyApprovalsImportsEverythingStillAwaitingApprovalAsync`    | Fact | An export without any approvals imports everything still awaiting approval.         |
| `AFileWithoutTheApprovalCollectionIsBlanketApprovedIncludingAnalyticsInstancesAsync` | Fact | A file without the approval collection is blanket approved including analytics instances. |
| `ReimportingIntoTheSameTenantReplacesTheApprovalHistoryInsteadOfAppendingAsync` | Fact | Reimporting into the same tenant replaces the approval history instead of appending. |
| `ASnapshotAlwaysCarriesUnpromotedExhaustiveTrialsForLosslessRollbackAsync`  | Fact | A snapshot always carries unpromoted exhaustive trials for lossless rollback.       |
| `ApprovalLookupsChunkTheEntityIdListInsteadOfSendingOneGiantInClauseAsync`  | Fact | Approval lookups chunk the entity id list instead of sending one giant in clause.   |

### AdvancedAnalyticsPreservationTests

`Jube.Test.Preservation.AdvancedAnalyticsPreservationTests` — 10 tests. Category: Service. Collection: Database.

| Test                                                                                       | Kind   | Verifies                                                                                          |
|--------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `FullRoundTripIntoFreshTenantReproducesEveryRowWithRemappedKeysAsync`                      | Fact   | Full round trip into fresh tenant reproduces every row with remapped keys.                        |
| `ExportContainsNoTenantIdNoCacheAndOnlyTheCallersRowsAsync`                                | Fact   | Export contains no tenant id no cache and only the callers rows.                                  |
| `ImportedActiveVersionIsTheOneTheEngineSyncLoadsAsync`                                     | Fact   | Imported active version is the one the engine sync loads.                                         |
| `ImportOverAnExistingTenantReplacesItsRowsAndSoftDeletesTheOldOnesAsync`                   | Fact   | Import over an existing tenant replaces its rows and soft deletes the old ones.                   |
| `ExportMadeWithoutTheCollectionsStillImportsAndReplacesExistingRowsLikeTheNeighboursAsync` | Fact   | Export made without the collections still imports and replaces existing rows like the neighbours. |
| `GenuineLegacyFileWithFourPayloadKeysImportsAndAnOlderReaderReadsTheNewFileAsync`          | Fact   | Genuine legacy file with four payload keys imports and an older reader reads the new file.        |
| `TenantIdsAndModelIdsInsideTheFileAreNeverTrustedAsync`                                    | Fact   | Tenant ids and model ids inside the file are never trusted.                                       |
| `ACorruptOrHostileFileIsRejectedWholeAndLeavesTheTargetUntouchedAsync`                     | Theory | A corrupt or hostile file is rejected whole and leaves the target untouched.                      |
| `AFailureAfterTheAnalyticsRowsAreInsertedRollsEverythingBackAsync`                         | Fact   | A failure after the analytics rows are inserted rolls everything back.                            |
| `ACancelledImportChangesNothingAsync`                                                      | Fact   | A cancelled import changes nothing.                                                               |

### ExhaustivePreservationTests

`Jube.Test.Preservation.ExhaustivePreservationTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                                           | Kind | Verifies                                                                             |
|--------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `EveryExhaustiveTableRoundTripsWithEveryColumnAndEveryForeignKeyRemappedAsync` | Fact | Every exhaustive table round trips with every column and every foreign key remapped. |
| `PerParentChildrenAreNotMixedAcrossParentsOrLeakedFromOtherTenantsAsync`       | Fact | Per parent children are not mixed across parents or leaked from other tenants.       |
| `AnExportMadeBeforeThePrescriptionHistogramWasCarriedStillImportsAsync`        | Fact | An export made before the prescription histogram was carried still imports.          |
| `PeekCarriesTheExhaustiveAndAnalyticsCollectionsAsync`                         | Fact | Peek carries the exhaustive and analytics collections.                               |

### PocoMessagePackKeysAreUniqueTests

`Jube.Test.Preservation.PocoMessagePackKeysAreUniqueTests` — 2 tests.

| Test                                                                  | Kind   | Verifies                                                                         |
|-----------------------------------------------------------------------|--------|----------------------------------------------------------------------------------|
| `KeysAreUniqueAndNonNegativeWithinEveryMessagePackPoco`               | Theory | Keys are unique and non negative within every message pack poco.                 |
| `EveryMappedColumnOfTheExhaustiveAndAdvancedAnalyticsFamiliesHasAKey` | Theory | Every mapped column of the exhaustive and advanced analytics families has a key. |

### PreservationModelTreeCoverageTests

`Jube.Test.Preservation.PreservationModelTreeCoverageTests` — 3 tests. Category: Unit.

| Test                                                            | Kind | Verifies                                                              |
|-----------------------------------------------------------------|------|------------------------------------------------------------------------|
| `EveryModelTreeTableIsCarriedOrExplicitlyExcludedWithAReason`   | Fact | Every model tree table is carried or explicitly excluded with a reason. |
| `TheExclusionsAreRealUncarriedTablesWithReasons`                | Fact | The exclusions are real uncarried tables with reasons.                |
| `EveryApprovableKindIsCarriedSoItsHistoryCanReplay`             | Fact | Every approvable kind is carried so its history can replay.           |

### PreservationCoverageTests

`Jube.Test.Preservation.PreservationCoverageTests` — 8 tests. Category: Convention.

| Test                                                                          | Kind   | Verifies                                                                                  |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------|
| `EveryExhaustiveAndAnalyticsTableIsExportedOrExplicitlyExcludedWithAReason`   | Fact   | Every exhaustive and analytics table is exported or explicitly excluded with a reason.    |
| `ExclusionsAreRealFamilyTablesThatAreNotAlsoExportedAndCarryAReason`          | Fact   | Exclusions are real family tables that are not also exported and carry a reason.          |
| `TheCacheIsExcludedAndTheAnalyticsPayloadCarriesExactlyTheRoundTrippedTables` | Fact   | The cache is excluded and the analytics payload carries exactly the round tripped tables. |
| `TheExhaustiveTreeSpecCoversEveryExportedExhaustiveTable`                     | Fact   | The exhaustive tree spec covers every exported exhaustive table.                          |
| `EveryMappedMemberHasAUniqueMessagePackKeyOnAMessagePackObject`               | Theory | Every mapped member has a unique message pack key on a message pack object.               |
| `EnumColumnsAreWrittenAsTheirIntegerValue`                                    | Fact   | Enum columns are written as their integer value.                                          |
| `EveryAnalyticsColumnSurvivesMessagePackWhetherSetOrNull`                     | Theory | Every analytics column survives message pack whether set or null.                         |
| `KeysOfTheCoveredTypesNeverRenumberAndNewMembersOnlyAppend`                   | Fact   | Keys of the covered types never renumber and new members only append.                     |

## Reporting

### PostgresSqlValidatorTests

`Jube.Test.Reporting.PostgresSqlValidatorTests` — 16 tests. Category: Reporting.

| Test                                                                      | Kind | Verifies                                                                            |
|---------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `AssertSelectOnlyAllowsAPlainSelect`                                      | Fact | Assert select only allows a plain select.                                           |
| `AssertSelectOnlyRejectsAnInsert`                                         | Fact | Assert select only rejects an insert.                                               |
| `AssertSelectOnlyRejectsADataModifyingCte`                                | Fact | Assert select only rejects a data modifying cte.                                    |
| `EscapeStringLiteralWrapsInSingleQuotes`                                  | Fact | Escape string literal wraps in single quotes.                                       |
| `EscapeStringLiteralDoublesEmbeddedSingleQuotes`                          | Fact | Escape string literal doubles embedded single quotes.                               |
| `EscapeStringLiteralNeutralisesAClassicInjectionAttempt`                  | Fact | Escape string literal neutralises a classic injection attempt.                      |
| `RewriteTableReferencesSubstitutesATopLevelFromReference`                 | Fact | Rewrite table references substitutes a top level from reference.                    |
| `RewriteTableReferencesFindsATableHiddenInsideAWhereClauseSubquery`       | Fact | Rewrite table references finds a table hidden inside a where clause subquery.       |
| `RewriteTableReferencesLeavesAnExplicitlySafeTableUntouched`              | Fact | Rewrite table references leaves an explicitly safe table untouched.                 |
| `RewriteTableReferencesThrowsForAnUnregisteredTopLevelTable`              | Fact | Rewrite table references throws for an unregistered top level table.                |
| `RewriteTableReferencesThrowsForAnUnregisteredTableHiddenInsideASubquery` | Fact | Rewrite table references throws for an unregistered table hidden inside a subquery. |
| `RewriteTableReferencesThrowsForAnUnregisteredTableInAJoin`               | Fact | Rewrite table references throws for an unregistered table in a join.                |
| `RewriteTableReferencesThrowsForAnUnregisteredTableInACte`                | Fact | Rewrite table references throws for an unregistered table in a cte.                 |
| `RewriteTableReferencesPreservesAnExplicitAliasForColumnQualification`    | Fact | Rewrite table references preserves an explicit alias for column qualification.      |
| `RewriteTableReferencesRejectsANonSelectEvenIfEveryTableIsRegistered`     | Fact | Rewrite table references rejects a non select even if every table is registered.    |
| `RewriteTableReferencesOutputIsItselfReparseableAsAPureSelect`            | Fact | Rewrite table references output is itself reparseable as a pure select.             |

## Repository

### DeleteAsVersionRepositoryTests

`Jube.Test.Repository.DeleteAsVersionRepositoryTests` — 8 tests. Category: Service. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                      |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `DeletingARequestXPathWritesThePreDeleteRowToTheVersionTableAsync`                  | Fact | Deleting a request x path writes the pre delete row to the version table.                     |
| `DeletingARequestXPathBumpsTheCoreVersionSoTheDeleteIsItsOwnApprovableVersionAsync` | Fact | Deleting a request x path bumps the core version so the delete is its own approvable version. |
| `DeletingARequestXPathWhoseVersionIsUnsetTreatsItAsVersionOneAsync`                 | Fact | Deleting a request x path whose version is unset treats it as version one.                    |
| `DeletingALockedRequestXPathThrowsAndWritesNoVersionRowAsync`                       | Fact | Deleting a locked request x path throws and writes no version row.                            |
| `DeletingAnAlreadyDeletedRequestXPathThrowsAndDoesNotVersionItTwiceAsync`           | Fact | Deleting an already deleted request x path throws and does not version it twice.              |
| `DeletingAnActivationRuleVersionsItBecauseItIsTheKindTheEngineGatesOnAsync`         | Fact | Deleting an activation rule versions it because it is the kind the engine gates on.           |
| `DeletingAListValueVersionsItThroughTheGrandparentScopedPredicateAsync`             | Fact | Deleting a list value versions it through the grandparent scoped predicate.                   |
| `DeletingAModelVersionsItThroughTheDirectlyTenantedPredicateAsync`                  | Fact | Deleting a model versions it through the directly tenanted predicate.                         |

### DeleteTransactionTests

`Jube.Test.Repository.DeleteTransactionTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                          |
|-----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `ADeleteInsideACallersTransactionIsUndoneWhenThatTransactionRollsBackAsync` | Fact | A delete inside a callers transaction is undone when that transaction rolls back. |
| `AStandaloneDeleteCommitsTheRowAndBothVersionRowsTogetherAsync`             | Fact | A standalone delete commits the row and both version rows together.               |

### DictionaryEvalExpressionRepositoryTests

`Jube.Test.Repository.DictionaryEvalExpressionRepositoryTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                    |
|-------------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `GetAsyncReturnsASeededRowAsync`                                        | Fact | Get async returns a seeded row.                                             |
| `GetAsyncReturnsMultipleSeededRowsAsync`                                | Fact | Get async returns multiple seeded rows.                                     |
| `UpdateCompileStatusAsyncMarksARowAsSuccessfullyCompiledAsync`          | Fact | Update compile status async marks a row as successfully compiled.           |
| `UpdateCompileStatusAsyncRecordsAFailureAndItsErrorMessageAsync`        | Fact | Update compile status async records a failure and its error message.        |
| `UpdateCompileStatusAsyncOverwritesAPreviousCompileErrorOnceFixedAsync` | Fact | Update compile status async overwrites a previous compile error once fixed. |
| `UpdateCompileStatusAsyncOnlyAffectsTheTargetedRowAsync`                | Fact | Update compile status async only affects the targeted row.                  |

### DictionaryKvpReviveTests

`Jube.Test.Repository.DictionaryKvpReviveTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                           | Kind | Verifies                                                              |
|----------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `ADeletedKeyValuePairIsReachableByIdAndRevivedByAnUpdateAsync` | Fact | A deleted key value pair is reachable by id and revived by an update. |
| `ReviveWritesOnlyTheTwoDeleteArchivesAndNoExtraVersionAsync`   | Fact | Revive writes only the two delete archives and no extra version.      |

### EngineSnapshotPruneTests

`Jube.Test.Repository.EngineSnapshotPruneTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                                          | Kind | Verifies                                                                               |
|-------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `ASnapshotOfAnInstanceThatStoppedReportingIsRemovedButALiveOneIsKeptAsync`    | Fact | A snapshot of an instance that stopped reporting is removed but a live one is kept.    |
| `TheInstanceDoingThePruneIsNeverRemovedEvenWithoutAFreshHeartbeatAsync`       | Fact | The instance doing the prune is never removed even without a fresh heartbeat.          |
| `ASyncRemovesTheNodeRowOfAnInstanceThatStoppedReportingButKeepsLiveOnesAsync` | Fact | A sync removes the node row of an instance that stopped reporting but keeps live ones. |

### EntityApprovalResolverTests

`Jube.Test.Repository.EntityApprovalResolverTests` — 25 tests. Category: Unit.

| Test                                                                 | Kind | Verifies                                                                         |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `AnEntityWithNoApprovalAtAllHasNoEffectiveVersion`                   | Fact | An entity with no approval at all has no effective version.                      |
| `TheCurrentVersionRunsWhenItIsApproved`                              | Fact | The current version runs when it is approved.                                    |
| `AnUnapprovedEditFallsBackToTheLastApprovedVersion`                  | Fact | An unapproved edit falls back to the last approved version.                      |
| `TheHighestApprovedVersionWins`                                      | Fact | The highest approved version wins.                                               |
| `AnApprovalForAVersionAheadOfTheCurrentRowIsIgnored`                 | Fact | An approval for a version ahead of the current row is ignored.                   |
| `ARejectedVersionNeverRuns`                                          | Fact | A rejected version never runs.                                                   |
| `ARejectionOnTheCurrentVersionFallsBackToTheEarlierApprovedOne`      | Fact | A rejection on the current version falls back to the earlier approved one.       |
| `ARejectionSittingBetweenTwoApprovalsDoesNotBlockTheLaterOne`        | Fact | A rejection sitting between two approvals does not block the later one.          |
| `ARejectionOnAnOtherwiseApprovedVersionWinsOverTheApproval`          | Fact | A rejection on an otherwise approved version wins over the approval.             |
| `OneApprovalIsNotEnoughWhenTwoAreRequired`                           | Fact | One approval is not enough when two are required.                                |
| `TwoDistinctApproversSatisfyATwoApproverThreshold`                   | Fact | Two distinct approvers satisfy a two approver threshold.                         |
| `TheSameApproverTwiceDoesNotSatisfyATwoApproverThreshold`            | Fact | The same approver twice does not satisfy a two approver threshold.               |
| `AThresholdBelowOneIsTreatedAsOne`                                   | Fact | A threshold below one is treated as one.                                         |
| `WithTwoRequiredTheResolverFallsBackToAFullyApprovedEarlierVersion`  | Fact | With two required the resolver falls back to a fully approved earlier version.   |
| `EffectiveVersionsAreResolvedPerEntity`                              | Fact | Effective versions are resolved per entity.                                      |
| `AnEntityWithApprovalsButNoCurrentRowIsNotResolved`                  | Fact | An entity with approvals but no current row is not resolved.                     |
| `RejectionIsReportedForTheVersionItWasMadeAgainst`                   | Fact | Rejection is reported for the version it was made against.                       |
| `AVersionWithNoRowsAtAllIsPending`                                   | Fact | A version with no rows at all is pending.                                        |
| `AnApprovedVersionIsNotPending`                                      | Fact | An approved version is not pending.                                              |
| `ARejectedVersionIsNotPendingBecauseItNeedsAnEditToStartAgain`       | Fact | A rejected version is not pending because it needs an edit to start again.       |
| `AVersionShortOfItsApproverThresholdIsStillPending`                  | Fact | A version short of its approver threshold is still pending.                      |
| `APendingDeleteResolvesToThePreDeleteVersionSoTheEntityKeepsRunning` | Fact | A pending delete resolves to the pre delete version so the entity keeps running. |
| `AnApprovedDeleteResolvesToTheDeletedVersionSoTheEntityCanBeDropped` | Fact | An approved delete resolves to the deleted version so the entity can be dropped. |
| `ARejectedDeleteLeavesTheEntityRunningAtThePreDeleteVersion`         | Fact | A rejected delete leaves the entity running at the pre delete version.           |
| `ADeleteOfANeverApprovedEntityLeavesNothingToFallBackTo`             | Fact | A delete of a never approved entity leaves nothing to fall back to.              |

### GetApprovedEntityQueryMappingTests

`Jube.Test.Repository.GetApprovedEntityQueryMappingTests` — 12 tests. Category: Unit.

| Test                                                                    | Kind | Verifies                                                                             |
|-------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `AVersionRowMapsOntoTheEntityItIsAVersionOf`                            | Fact | A version row maps onto the entity it is a version of.                               |
| `TheEntityTakesTheParentIdAndNotTheIdOfTheVersionRow`                   | Fact | The entity takes the parent id and not the id of the version row.                    |
| `MembersTheVersionTableDoesNotCarryAreLeftUnset`                        | Fact | Members the version table does not carry are left unset.                             |
| `TheVersionNumberIsReadFromTheVersionRow`                               | Fact | The version number is read from the version row.                                     |
| `AVersionRowWithNoVersionNumberNeverMatchesAWantedVersion`              | Fact | A version row with no version number never matches a wanted version.                 |
| `TheParentIdPredicateSelectsOnlyTheRequestedParents`                    | Fact | The parent id predicate selects only the requested parents.                          |
| `TheParentIdPredicateSkipsRowsWhoseNullableParentIsNotSet`              | Fact | The parent id predicate skips rows whose nullable parent is not set.                 |
| `AListValueVersionMapsOntoTheValueItIsAVersionOf`                       | Fact | A list value version maps onto the value it is a version of.                         |
| `ADeletedRowIsRecognisedAsDeletedSoAnApprovedDeleteTakesEffect`         | Fact | A deleted row is recognised as deleted so an approved delete takes effect.           |
| `AnUndeletedRowIsNotTreatedAsDeletedWhetherTheFlagIsZeroOrUnset`        | Fact | An undeleted row is not treated as deleted whether the flag is zero or unset.        |
| `TheVersionRowBehindAPendingDeleteMapsBackToAnUndeletedEntity`          | Fact | The version row behind a pending delete maps back to an undeleted entity.            |
| `AVersionTypeWithoutTheNamedParentPropertyIsRefusedWhenTheQueryIsBuilt` | Fact | A version type without the named parent property is refused when the query is built. |

### ReviveDeletedRepositoryTests

`Jube.Test.Repository.ReviveDeletedRepositoryTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                        |
|--------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `ADeletedRowIsReachableByIdSoItCanBeOpenedAndEditedAsync`                | Fact | A deleted row is reachable by id so it can be opened and edited.                |
| `RevivingADeletedRowMakesItLiveAgainAndArchivesTheDeletedStateOnceAsync` | Fact | Reviving a deleted row makes it live again and archives the deleted state once. |

## Repository sanctions

### SanctionEntryImportRepositoryTests

`Jube.Test.Repository.Sanctions.SanctionEntryImportRepositoryTests` — 5 tests. Category: Service. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                    |
|-----------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `InsertAsyncPersistsAndAssignsAnIdAsync`                              | Fact | Insert async persists and assigns an id.                                    |
| `UpdateAsyncPersistsTheCompletionCountsAndOutcomeAsync`               | Fact | Update async persists the completion counts and outcome.                    |
| `UpdateAsyncPersistsAFailureWithAnErrorMessageAsync`                  | Fact | Update async persists a failure with an error message.                      |
| `GetBySanctionEntrySourceIdAsyncReturnsOnlyImportsForThatSourceAsync` | Fact | Get by sanction entry source id async returns only imports for that source. |
| `GetByIdAsyncForAMissingIdReturnsNullAsync`                           | Fact | Get by id async for a missing id returns null.                              |

### SanctionEntryRejectionRepositoryTests

`Jube.Test.Repository.Sanctions.SanctionEntryRejectionRepositoryTests` — 5 tests. Category: Service. Collection:
Database.

| Test                                                                              | Kind   | Verifies                                                                                   |
|-----------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `InsertAsyncPersistsTheRejectionWithItsReasonAndRawDataAsync`                     | Fact   | Insert async persists the rejection with its reason and raw data.                          |
| `InsertAsyncRoundTripsEveryRejectionReasonAsync`                                  | Theory | Insert async round trips every rejection reason.                                           |
| `GetBySanctionEntryImportIdAsyncReturnsAllRejectionsForThatImportInAnyOrderAsync` | Fact   | Get by sanction entry import id async returns all rejections for that import in any order. |
| `GetBySanctionEntryImportIdAsyncForAnImportWithNoRejectionsReturnsEmptyAsync`     | Fact   | Get by sanction entry import id async for an import with no rejections returns empty.      |
| `GetByIdAsyncForAMissingIdReturnsNullAsync`                                       | Fact   | Get by id async for a missing id returns null.                                             |

### SanctionEntryRepositoryTests

`Jube.Test.Repository.Sanctions.SanctionEntryRepositoryTests` — 9 tests. Category: Service. Collection: Database.

| Test                                                                                | Kind | Verifies                                                                                      |
|-------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `UpsertAsyncOnANewHashInsertsAndReturnsInsertedOutcomeAsync`                        | Fact | Upsert async on a new hash inserts and returns inserted outcome.                              |
| `UpsertAsyncWithTheSameHashAndSourceASecondTimeIsUnchangedAndDoesNotDuplicateAsync` | Fact | Upsert async with the same hash and source a second time is unchanged and does not duplicate. |
| `UpsertAsyncWithTheSameHashButADifferentSourceInsertsASeparateRowAsync`             | Fact | Upsert async with the same hash but a different source inserts a separate row.                |
| `DeleteAsyncSoftDeletesAndSetsAuditColumnsAsync`                                    | Fact | Delete async soft deletes and sets audit columns.                                             |
| `UpsertAsyncOnAPreviouslyDeletedHashRevivesItAndClearsDeletionColumnsAsync`         | Fact | Upsert async on a previously deleted hash revives it and clears deletion columns.             |
| `GetAsyncExcludesSoftDeletedRowsButIncludesActiveOnesAsync`                         | Fact | Get async excludes soft deleted rows but includes active ones.                                |
| `GetActiveBySanctionEntrySourceIdAsyncOnlyReturnsRowsForThatSourceAsync`            | Fact | Get active by sanction entry source id async only returns rows for that source.               |
| `GetActiveBySanctionEntrySourceIdAsyncExcludesSoftDeletedRowsAsync`                 | Fact | Get active by sanction entry source id async excludes soft deleted rows.                      |
| `GetActiveBySanctionEntrySourceIdAsyncForASourceWithNoRowsReturnsEmptyAsync`        | Fact | Get active by sanction entry source id async for a source with no rows returns empty.         |

### SanctionEntrySourceRepositoryTests

`Jube.Test.Repository.Sanctions.SanctionEntrySourceRepositoryTests` — 3 tests. Category: Service. Collection: Database.

| Test                                                      | Kind | Verifies                                                   |
|-----------------------------------------------------------|------|------------------------------------------------------------|
| `GetByIdAsyncReturnsThePersistedConfigurationAsync`       | Fact | Get by id async returns the persisted configuration.       |
| `GetByIdAsyncForAMissingIdReturnsNullAsync`               | Fact | Get by id async for a missing id returns null.             |
| `GetAsyncIncludesEveryConfiguredSourceIncludingOursAsync` | Fact | Get async includes every configured source including ours. |

### SanctionStopTokenRepositoryTests

`Jube.Test.Repository.Sanctions.SanctionStopTokenRepositoryTests` — 4 tests. Category: Service. Collection: Database.

| Test                                                       | Kind | Verifies                                                       |
|------------------------------------------------------------|------|----------------------------------------------------------------|
| `GetAsyncReturnsTheSeededBaselineHonorificsAndTitlesAsync` | Fact | Get async returns the seeded baseline honorifics and titles.   |
| `GetAsyncOnlyReturnsTheTwoDocumentedCategoriesAsync`       | Fact | Get async only returns the two documented categories.          |
| `GetAsyncTokensAreAllLowercaseAsStoredByTheMigrationAsync` | Fact | Get async tokens are all lowercase as stored by the migration. |
| `GetAsyncTokensAreAllUniqueAsync`                          | Fact | Get async tokens are all unique.                               |

## Resilient npgsql

### PgRetryBudgetTests

`Jube.Test.ResilientNpgsql.PgRetryBudgetTests` — 3 tests. Category: Unit. Collection: PgRetryBudget.

| Test                                                    | Kind   | Verifies                                                         |
|---------------------------------------------------------|--------|------------------------------------------------------------------|
| `TheDefaultMatchesTheDocumentedSetting`                 | Fact   | The default matches the documented setting.                      |
| `AConfiguredBudgetIsWhatTheLayerReads`                  | Theory | A configured budget is what the layer reads.                     |
| `ANegativeBudgetIsFlooredAtNoRetriesRatherThanThrowing` | Theory | A negative budget is floored at no retries rather than throwing. |

### PoolClearGateTests

`Jube.Test.ResilientNpgsql.PoolClearGateTests` — 9 tests. Category: Unit.

| Test                                                    | Kind | Verifies                                                          |
|---------------------------------------------------------|------|-------------------------------------------------------------------|
| `TheFirstCallerForAKeyEnters`                           | Fact | The first caller for a key enters.                                |
| `ASecondCallerInsideTheWindowIsBlocked`                 | Fact | A second caller inside the window is blocked.                     |
| `ACallerEntersAgainOnceTheWindowHasElapsed`             | Fact | A caller enters again once the window has elapsed.                |
| `TheWindowIsMeasuredFromTheLastEntryRatherThanTheFirst` | Fact | The window is measured from the last entry rather than the first. |
| `ABlockedAttemptDoesNotExtendTheWindow`                 | Fact | A blocked attempt does not extend the window.                     |
| `KeysAreIndependentOfOneAnother`                        | Fact | Keys are independent of one another.                              |
| `ExactlyOneOfManyConcurrentCallersEntersAsync`          | Fact | Exactly one of many concurrent callers enters.                    |
| `AZeroWindowNeverBlocks`                                | Fact | A zero window never blocks.                                       |
| `TheSharedGateIsReplacedByConfiguration`                | Fact | The shared gate is replaced by configuration.                     |

### PostgresErrorClassificationTests

`Jube.Test.ResilientNpgsql.PostgresErrorClassificationTests` — 15 tests. Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                                |
|-----------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `ARecoverableStateIsRetryable`                                              | Theory | A recoverable state is retryable.                                                       |
| `AnUnrecoverableStateIsNotRetryable`                                        | Theory | An unrecoverable state is not retryable.                                                |
| `APermissionDeniedIsNotRetryableSoThePurgeFailureSurfacesRatherThanLooping` | Fact   | A permission denied is not retryable so the purge failure surfaces rather than looping. |
| `AStateThatSaysNothingAboutConnectionHealthDoesNotClearThePool`             | Theory | A state that says nothing about connection health does not clear the pool.              |
| `AStaleOrBrokenConnectionClearsThePool`                                     | Theory | A stale or broken connection clears the pool.                                           |
| `ANonPostgresFailureClearsThePool`                                          | Fact   | A non postgres failure clears the pool.                                                 |
| `AnExhaustedPoolIsRecognisedAsAConnectionAcquisitionTimeout`                | Fact   | An exhausted pool is recognised as a connection acquisition timeout.                    |
| `RecognisingAnAcquisitionTimeoutNeverReadsTheMessage`                       | Theory | Recognising an acquisition timeout never reads the message.                             |
| `AServerErrorIsNeverAConnectionAcquisitionTimeout`                          | Theory | A server error is never a connection acquisition timeout.                               |
| `ABareTimeoutIsNotAConnectionAcquisitionTimeout`                            | Fact   | A bare timeout is not a connection acquisition timeout.                                 |
| `AnExhaustedPoolNeitherClearsThePoolNorRecyclesTheConnection`               | Fact   | An exhausted pool neither clears the pool nor recycles the connection.                  |
| `AStaleOrBrokenConnectionIsRecycled`                                        | Theory | A stale or broken connection is recycled.                                               |
| `AHealthyConnectionIsNotRecycled`                                           | Theory | A healthy connection is not recycled.                                                   |
| `AReadOnlyTransactionIsRetriedRecycledAndClearedTogether`                   | Fact   | A read only transaction is retried recycled and cleared together.                       |
| `TooManyClientsIsRetriedWithoutDisturbingTheConnectionOrThePool`            | Fact   | Too many clients is retried without disturbing the connection or the pool.              |

## Volume

### BacktestVolumeTests

`Jube.Test.Volume.BacktestVolumeTests` — 2 tests. Category: Service. Collection: Database.

| Test                                                                       | Kind | Verifies                                                                          |
|----------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `TheBacktestCountsMillionsOfGeneratedRowsExactlyInBoundedMemoryAsync`      | Fact | The backtest counts millions of generated rows exactly in bounded memory.         |
| `TheBacktestReadsTheArchivePageByPageAndCountsExactlyInBoundedMemoryAsync` | Fact | The backtest reads the archive page by page and counts exactly in bounded memory. |

### ReprocessingVolumeTests

`Jube.Test.Volume.ReprocessingVolumeTests` — 1 test. Category: Service. Collection: Database.

| Test                                                                                  | Kind | Verifies                                                                                   |
|---------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `ReprocessingProcessesEveryArchivedDocumentOnceUpdatesEveryMatchInBoundedMemoryAsync` | Fact | Reprocessing processes every archived document once updates every match in bounded memory. |
