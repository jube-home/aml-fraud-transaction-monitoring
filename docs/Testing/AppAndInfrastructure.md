---
layout: default
title: Application and Infrastructure
nav_order: 23
parent: Testing
---

# Application and Infrastructure

Tests of application plumbing: configuration, middlewares, HTTP adaptation, cryptography, clustering, task cancellation,
validations and the web application firewall.

## App code

### AuthorizationHeaderParameterOperationFilterTests

`Jube.Test.App.Code.AuthorizationHeaderParameterOperationFilterTests` — 4 tests. Category: Unit.

| Test                                                                                    | Kind | Verifies                                                                                              |
|-----------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `ApplyDoesNotThrowAndAddsSecurityRequirementsForAMinimalApiEndpointWithNoMethodInfo`    | Fact | Apply does not throw and adds security requirements for a minimal api endpoint with no method info.   |
| `ApplyAddsSecurityRequirementsForAControllerActionWithoutAllowAnonymous`                | Fact | Apply adds security requirements for a controller action without allow anonymous.                     |
| `ApplyDoesNotAddSecurityRequirementsForAControllerActionDecoratedWithAllowAnonymous`    | Fact | Apply does not add security requirements for a controller action decorated with allow anonymous.      |
| `ApplyDoesNotAddSecurityRequirementsForTheUnauthenticatedLoginPathEvenWithNoMethodInfo` | Fact | Apply does not add security requirements for the unauthenticated login path even with no method info. |

### UtcDateTimeJsonConverterTests

`Jube.Test.App.Code.UtcDateTimeJsonConverterTests` — 8 tests. Category: Unit.

| Test                                                                | Kind | Verifies                                                                        |
|---------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `AnUnspecifiedDateTimeSerialisesWithTheUtcSuffix`                   | Fact | An unspecified date time serialises with the utc suffix.                        |
| `AUtcDateTimeSerialisesUnchangedWithTheUtcSuffix`                   | Fact | A utc date time serialises unchanged with the utc suffix.                       |
| `ALocalDateTimeSerialisesAsItsUtcInstant`                           | Fact | A local date time serialises as its utc instant.                                |
| `ANullableDateTimeSerialisesWithTheUtcSuffixAndNullStaysNull`       | Fact | A nullable date time serialises with the utc suffix and null stays null.        |
| `ADateTimeBoxedAsObjectInsideADictionarySerialisesWithTheUtcSuffix` | Fact | A date time boxed as object inside a dictionary serialises with the utc suffix. |
| `AnOffsetlessStringDeserialisesAsThatWallClockTimeInUtc`            | Fact | An offsetless string deserialises as that wall clock time in utc.               |
| `AnOffsetStringDeserialisesAsTheSameInstantInUtc`                   | Fact | An offset string deserialises as the same instant in utc.                       |
| `AZuluStringDeserialisesUnchangedInUtc`                             | Fact | A zulu string deserialises unchanged in utc.                                    |

## Optimistic concurrency

### WireCompressionTests

`Jube.Test.App.Wire.WireCompressionTests` — 6 tests. Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                    |
|------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------|
| `ALargeResponseIsCompressedAndNamesItsCompressionAndDecodesBackToTheSameRowsAsync` | Fact   | A large response is compressed and names its compression and decodes back to the same rows. |
| `AResponseBelowTheThresholdIsLeftUncompressedAndNamesNoCompressionAsync`           | Fact   | A response below the threshold is left uncompressed and names no compression.               |
| `NoneLeavesEveryResponseUncompressedWhateverItsSizeAsync`                          | Fact   | None leaves every response uncompressed whatever its size.                                  |
| `TheBytesTheServerWroteAreTheBytesTheBrowserTestsDecode`                           | Fact   | The bytes the server wrote are the bytes the browser tests decode.                          |
| `ACompressedRequestBodyIsRefusedSoNoDecompressionIsEverDoneOnInputAsync`           | Fact   | A compressed request body is refused so no decompression is ever done on input.             |
| `OnlyTheTwoNamedCompressionsParse`                                                 | Theory | Only the two named compressions parse.                                                      |

### WireBytesTests

`Jube.Test.App.Wire.WireBytesTests` — 3 tests. Category: Unit.

| Test                                                      | Kind | Verifies                                                                                                    |
|-----------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------|
| `ABytesMemberHasABytesShapeAndTheRequestDtosAreEncodable` | Fact | A byte array member has the bytes wire shape and the preservation and case file request DTOs are encodable. |
| `APackageSurvivesAMessagePackRoundTripAsABinaryValue`     | Fact | A package survives a MessagePack round trip as a binary value rather than text.                             |
| `APackageSurvivesAJsonRoundTripAsBase64Text`              | Fact | A package survives a JSON round trip as base64 text.                                                        |

### WireColumnarJsonTests

`Jube.Test.App.Wire.WireColumnarJsonTests` — 10 tests. Category: Unit.

| Test                                                                           | Kind | Verifies                                                                               |
|--------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `RowsAreWrittenOnceAsKeysAndColumnsWithMissingCellsAndNullRowsNamed`           | Fact | Rows are written once as keys and columns with missing cells and null rows named.      |
| `NoRowsAreWrittenAsAnEmptyGridWithNothingToDeclare`                            | Fact | No rows are written as an empty grid with nothing to declare.                          |
| `CellsKeepTheTextAnOrdinaryJsonResponseWouldGiveThem`                          | Fact | Cells keep the text an ordinary json response would give them.                         |
| `ADtoMemberOfRowsIsWrittenColumnarAndTheRestOfTheDtoIsOrdinaryJson`            | Fact | A dto member of rows is written columnar and the rest of the dto is ordinary json.     |
| `ARowGridIsAnsweredAsColumnarJsonWhenAskedForAndTheModeIsJsonAsync`            | Fact | A row grid is answered as columnar json when asked for and the mode is json.           |
| `AResponseWithoutRowGridsStaysOrdinaryJsonEvenWhenColumnarJsonIsAskedForAsync` | Fact | A response without row grids stays ordinary json even when columnar json is asked for. |
| `AClientThatDoesNotAskForColumnarJsonGetsTheOrdinaryJsonRowsAsync`             | Fact | A client that does not ask for columnar json gets the ordinary json rows.              |
| `MessagePackIsPreferredWhenTheModeAllowsItAndBothAreAskedForAsync`             | Fact | Message pack is preferred when the mode allows it and both are asked for.              |
| `JsonModeFallsBackToColumnarJsonWhereMessagePackIsAskedForButNotAllowedAsync`  | Fact | Json mode falls back to columnar json where message pack is asked for but not allowed. |
| `MixedModeAnswersAPlainResponseAsJsonAndARowGridAsMessagePackAsync`            | Fact | Mixed mode answers a plain response as json and a row grid as message pack.            |

### WireGeneratedDecodersTests

`Jube.Test.App.Wire.WireGeneratedDecodersTests` — 10 tests. Category: Unit.

| Test                                                                                       | Kind | Verifies                                                                                                 |
|--------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `TheTypesHashIgnoresTypesFromOutsideTheDtoAssemblyButTheFullHashDoesNot`                   | Fact | The types hash ignores types from outside the dto assembly but the full hash does not.                   |
| `TheSchemaAllowsGeneratedDecodersByDefaultAndNamesTheSwitchWhenItIsOff`                    | Fact | The schema allows generated decoders by default and names the switch when it is off.                     |
| `TheGeneratedDecodersCommittedWithTheUserInterfaceAreExactlyWhatTheServerWouldGenerateNow` | Fact | The generated decoders committed with the user interface are exactly what the server would generate now. |
| `WritingThenCheckingLeavesNothingStaleAndAnEditIsDetected`                                 | Fact | Writing then checking leaves nothing stale and an edit is detected.                                      |
| `AShapeThatNamesATypeOutsideTheDtoAssemblyStopsTheBuildInsteadOfSkippingAConversion`       | Fact | A shape that names a type outside the dto assembly stops the build instead of skipping a conversion.     |
| `ATypesFileCarriesTheHashAndTheTypesInTheShapeTheBrowserSchemaHas`                         | Fact | A types file carries the hash and the types in the shape the browser schema has.                         |
| `TheSameSchemaAlwaysGeneratesTheSameBytes`                                                 | Fact | The same schema always generates the same bytes.                                                         |
| `OnlyTypesFromTheDtoAssemblyAreGeneratedAndTheMapNamesThemByTheirFullNames`                | Fact | Only types from the dto assembly are generated and the map names them by their full names.               |
| `ListsMapsRowGridsAndNestedListsAreComposedAndPlainMembersAreReadDirectly`                 | Fact | Lists maps row grids and nested lists are composed and plain members are read directly.                  |
| `EachDtoBecomesOneFunctionThatReadsItsMembersByPositionInSchemaOrder`                      | Fact | Each dto becomes one function that reads its members by position in schema order.                        |

### WireGenerationTests

`Jube.Test.App.Wire.WireGenerationTests` — 3 tests. Category: Unit.

| Test                                                                                 | Kind   | Verifies                                                                                          |
|--------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `OnlyTheTwoWireFlagsTakeTheApplicationOutOfServingMode`                              | Theory | Only the two wire flags take the application out of serving mode.                                 |
| `GeneratingWritesBothFilesThenReportsThemCurrentAndLeavesThemUntouchedTheSecondTime` | Fact   | Generating writes both files then reports them current and leaves them untouched the second time. |
| `CheckingSucceedsOnCurrentFilesAndFailsWithTheStaleNamesWhenOneIsEdited`             | Fact   | Checking succeeds on current files and fails with the stale names when one is edited.             |

### WireJitSchemaTests

`Jube.Test.App.Wire.WireJitSchemaTests` — 4 tests. Category: Unit.

| Test                                         | Kind | Verifies                                               |
|----------------------------------------------|------|--------------------------------------------------------|
| `HeadCarriesTheRoutesAndSettingsButNoTypes`  | Fact | The head carries the routes and settings but no types. |
| `ShapesCarryOnlyTheClosureOfWhatWasAskedFor` | Fact | Shapes carry only the closure of what was asked for.   |
| `UnknownAndPrimitiveShapesYieldNoTypes`      | Fact | Unknown and primitive shapes yield no types.           |
| `EveryTypeIsReachableFromItsOwnShape`        | Fact | Every type is reachable from its own shape.            |

### WireGenericClosureTests

`Jube.Test.App.Wire.WireGenericClosureTests` — 4 tests. Category: Unit.

| Test                                                                            | Kind   | Verifies                                                                                      |
|---------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------|
| `AClosedGenericTypeAndItsArgumentAreInTheGeneratedSetWhateverCollectionWrapsIt` | Theory | A closed generic type and its argument are in the generated set whatever collection wraps it. |
| `AMemberTypedAsAClosedGenericIsFollowedToItsFields`                             | Fact   | A member typed as a closed generic is followed to its fields.                                 |
| `EveryTypeTheSchemaHoldsIsInTheGeneratedSetAndSoGetsAGeneratedDecoder`          | Fact   | Every type the schema holds is in the generated set and so gets a generated decoder.          |
| `ARowGridBehindAGenericResponseIsStillSeenAsCarryingRows`                       | Fact   | A row grid behind a generic response is still seen as carrying rows.                          |

### WireEncryptionBodyLimitsTests

`Jube.Test.App.Wire.WireEncryptionBodyLimitsTests` — 4 tests. Category: Unit.

| Test                                                                             | Kind   | Verifies                                                                                                             |
|----------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------|
| `AWellFormedSealedBodyReachesTheEndpointAsync`                                   | Fact   | A well formed sealed body reaches the endpoint.                                                                      |
| `ASealedBodyWithNoDeclaredLengthIsRefusedBeforeItIsBufferedAsync`                | Fact   | A sealed body with no declared length is refused before it is buffered.                                              |
| `ASealedBodyDeclaringMoreThanSixtyFourMegabytesIsRefusedBeforeItIsBufferedAsync` | Fact   | A sealed body declaring more than sixty four megabytes is refused before it is buffered and no replay claim is made. |
| `AnInnerTypeOutsideJsonAndMessagePackIsRefusedAsync`                             | Theory | An inner type outside json and message pack is refused and no replay claim is made.                                  |

### WireEncryptionEndToEndTests

`Jube.Test.App.Wire.WireEncryptionEndToEndTests` — 20 tests. Category: Unit.

| Test                                                                                   | Kind   | Verifies                                                                                        |
|----------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `AJsonBodyRoundTripsAndNeitherDirectionIsReadableOnTheWireAsync`                       | Fact   | A json body round trips and neither direction is readable on the wire.                          |
| `ALargeResponseIsCompressedBeforeItIsSealedSoEncryptionDoesNotCostTheCompressionAsync` | Fact   | A large response is compressed before it is sealed so encryption does not cost the compression. |
| `AMessagePackBodyAlsoRoundTripsInsideTheEnvelopeAsync`                                 | Fact   | A message pack body also round trips inside the envelope.                                       |
| `ABodylessReadHasItsResponseEncryptedAsync`                                            | Fact   | A bodyless read has its response encrypted.                                                     |
| `AnErrorResponseIsEncryptedTooAndKeepsItsStatusAsync`                                  | Fact   | An error response is encrypted too and keeps its status.                                        |
| `ARequestWithoutTheKeyHeaderIsServedAsBeforeSoEncryptionIsPerRequestAsync`             | Fact   | A request without the key header is served as before so encryption is per request.              |
| `ATamperedBodyIsRejectedAsync`                                                         | Fact   | A tampered body is rejected.                                                                    |
| `ABodySealedForAnotherPathCannotBeReplayedAtThisOneAsync`                              | Fact   | A body sealed for another path cannot be replayed at this one.                                  |
| `AnIdenticalRequestIsRejectedTheSecondTimeAsync`                                       | Fact   | An identical request is rejected the second time.                                               |
| `ARequestRepeatedAtAnotherNodeSharingTheSameRespIsRejectedAsync`                       | Fact   | A request repeated at another node sharing the same redis is rejected.                          |
| `ARespFailureRejectsTheRequestRatherThanSkippingTheReplayCheckAsync`                   | Fact   | A redis failure rejects the request rather than skipping the replay check.                      |
| `ARequestOutsideTheClockSkewIsRejectedAsync`                                           | Theory | A request outside the clock skew is rejected.                                                   |
| `AKeyWrappedForADifferentServerIsRejectedAsync`                                        | Fact   | A key wrapped for a different server is rejected.                                               |
| `AMalformedKeyHeaderIsRejectedWithoutAnErrorAsync`                                     | Theory | A malformed key header is rejected without an error.                                            |
| `AnAttackInsideAnEncryptedBodyIsStillBlockedByTheWafAsync`                             | Theory | An attack inside an encrypted body is still blocked by the waf.                                 |
| `AnUnauthenticatedCallerCannotMakeTheServerAttemptADecryptionAsync`                    | Fact   | An unauthenticated caller cannot make the server attempt a decryption.                          |
| `TheSealedFormIsNonceCiphertextAndTagAndOnlyOpensWithTheSameKeyAndContext`             | Fact   | The sealed form is nonce ciphertext and tag and only opens with the same key and context.       |
| `TwoSealsOfTheSamePlaintextNeverShareANonce`                                           | Fact   | Two seals of the same plaintext never share a nonce.                                            |
| `APublicKeyThatIsNotTheHalfOfThePrivateKeyIsRefusedAtStartup`                          | Fact   | A public key that is not the half of the private key is refused at startup.                     |
| `AKeyShorterThan2048BitsIsRefusedAtStartup`                                            | Fact   | A key shorter than2048 bits is refused at startup.                                              |

### WireEncryptionInteropTests

`Jube.Test.App.Wire.WireEncryptionInteropTests` — 3 tests. Category: Unit.

| Test                                                             | Kind | Verifies                                                                      |
|------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `AKeyWrappedByWebCryptoIsUnwrappedByTheServer`                   | Fact | A key wrapped by web crypto is unwrapped by the server.                       |
| `AMessageSealedByWebCryptoIsOpenedByTheServerWithTheSameContext` | Fact | A message sealed by web crypto is opened by the server with the same context. |
| `TheServerPublishesTheSubjectPublicKeyInfoTheBrowserImports`     | Fact | The server publishes the subject public key info the browser imports.         |

### WireEndToEndTests

`Jube.Test.App.Wire.WireEndToEndTests` — 11 tests. Category: Unit.

| Test                                                                        | Kind | Verifies                                                                             |
|-----------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `AGetAsksingForMessagePackComesBackAsMessagePackWithItsShapeAsync`          | Fact | A get asksing for message pack comes back as message pack with its shape.            |
| `AGetWithoutTheAcceptHeaderIsStillJsonAsync`                                | Fact | A get without the accept header is still json.                                       |
| `AMessagePackPostIsBoundByTheOrdinaryJsonBinderAndEchoedAsMessagePackAsync` | Fact | A message pack post is bound by the ordinary json binder and echoed as message pack. |
| `ARoutedPutWithAParameterBindsBothTheRouteValueAndTheMessagePackBodyAsync`  | Fact | A routed put with a parameter binds both the route value and the message pack body.  |
| `AJsonPostIsUntouchedAndAnswersJsonAsync`                                   | Fact | A json post is untouched and answers json.                                           |
| `AnErrorAnswersJsonEvenWhenMessagePackWasAskedForAsync`                     | Fact | An error answers json even when message pack was asked for.                          |
| `AResponseTypeThatCannotBeEncodedFallsBackToJsonAsync`                      | Fact | A response type that cannot be encoded falls back to json.                           |
| `GarbageMessagePackIsA400NotA500Async`                                      | Fact | Garbage message pack is a400 not a500.                                               |
| `TheSchemaNamesTheRequestRoutesAndItsHashMatchesTheResponseHeaderAsync`     | Fact | The schema names the request routes and its hash matches the response header.        |
| `AStaleSchemaHashIsRevalidatedEveryTimeAndNeverCachedAsImmutableAsync`      | Fact | A stale schema hash is revalidated every time and never cached as immutable.         |
| `AnUnchangedSchemaIsAnsweredWithNotModifiedWhenTheEntityTagMatchesAsync`    | Fact | An unchanged schema is answered with not modified when the entity tag matches.       |

### WireEndpointCoverageTests

`Jube.Test.App.Wire.WireEndpointCoverageTests` — 3 tests. Category: Unit.

| Test                                                               | Kind | Verifies                                                                      |
|--------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `EveryDeclaredApiResponseAndJsonRequestTypeCanTravelAsMessagePack` | Fact | Every declared api response and json request type can travel as message pack. |
| `EveryEndpointThatDeclaresNoResponseTypeIsAKnownReviewedException` | Fact | Every endpoint that declares no response type is a known reviewed exception.  |
| `VersionHistoryPayloadsCanTravelAsMessagePack`                     | Fact | Version history payloads can travel as message pack.                          |

### WireFormatterTests

`Jube.Test.App.Wire.WireFormatterTests` — 23 tests. Category: Unit.

| Test                                                                | Kind   | Verifies                                                                      |
|---------------------------------------------------------------------|--------|-------------------------------------------------------------------------------|
| `DateTimeAlwaysComesBackAsTheSameUtcInstant`                        | Theory | Date time always comes back as the same utc instant.                          |
| `DateTimeOffsetComesBackAsTheSameInstantAtZeroOffset`               | Fact   | Date time offset comes back as the same instant at zero offset.               |
| `NullableDateTimeOffsetKeepsNull`                                   | Fact   | Nullable date time offset keeps null.                                         |
| `DynamicCellsOfTheTypesJsonWritesAsTextComeOutAsExactlyThatText`    | Fact   | Dynamic cells of the types json writes as text come out as exactly that text. |
| `ADatabaseNullInADynamicCellIsWrittenAsNull`                        | Fact   | A database null in a dynamic cell is written as null.                         |
| `NullableValuesAreWrittenExactlyAsTheirNonNullableForm`             | Fact   | Nullable values are written exactly as their non nullable form.               |
| `NullableValuesRoundTripAndKeepNull`                                | Fact   | Nullable values round trip and keep null.                                     |
| `NonFiniteNullableDoubleIsRejectedOnRead`                           | Fact   | Non finite nullable double is rejected on read.                               |
| `NonFiniteDoubleIsRejectedOnRead`                                   | Theory | Non finite double is rejected on read.                                        |
| `IntegerIsAcceptedWhereADoubleIsExpected`                           | Fact   | Integer is accepted where a double is expected.                               |
| `DecimalTravelsAsANumber`                                           | Fact   | Decimal travels as a number.                                                  |
| `RowDictionariesCarryMixedRuntimeValues`                            | Fact   | Row dictionaries carry mixed runtime values.                                  |
| `UntypedMapsComeBackAsStringKeyedDictionaries`                      | Fact   | Untyped maps come back as string keyed dictionaries.                          |
| `ACharTravelsAsAOneCharacterStringLikeJson`                         | Fact   | A char travels as a one character string like json.                           |
| `AMultiCharacterStringIsRejectedWhereACharIsExpected`               | Fact   | A multi character string is rejected where a char is expected.                |
| `ColumnarRowsRestoreEveryRowExactlyIncludingMissingKeysAndNullRows` | Fact   | Columnar rows restore every row exactly including missing keys and null rows. |
| `ColumnarRowsKeepTheRowCountOfEmptyDictionariesAndOfNoRows`         | Fact   | Columnar rows keep the row count of empty dictionaries and of no rows.        |
| `ColumnarRowsKeepColumnOrderAndTypedStringValues`                   | Fact   | Columnar rows keep column order and typed string values.                      |
| `ColumnarRowsAreMuchSmallerThanTheSameRowsWrittenAsMaps`            | Fact   | Columnar rows are much smaller than the same rows written as maps.            |
| `MalformedColumnarRowsAreRejected`                                  | Theory | Malformed columnar rows are rejected.                                         |
| `ColumnarRowsWriteExactlyTheBytesTheBrowserCodecWrites`             | Fact   | Columnar rows write exactly the bytes the browser codec writes.               |
| `ColumnarRowsReadTheBytesTheBrowserCodecWrites`                     | Fact   | Columnar rows read the bytes the browser codec writes.                        |
| `ExcessiveNestingIsRejected`                                        | Fact   | Excessive nesting is rejected.                                                |

### WireHttpCompressionTests

`Jube.Test.App.Wire.WireHttpCompressionTests` — 10 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                               |
|--------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `OnlyTheThreeNamedHttpCompressionsParse`                                       | Theory | Only the three named http compressions parse.                                          |
| `OnlyTheThreeEffortLevelsParseAndTheNoCompressionLevelIsRefused`               | Theory | Only the three effort levels parse and the no compression level is refused.            |
| `TheChosenLevelReachesBothProviderOptionsAndCompressionIsAllowedOverHttps`     | Fact   | The chosen level reaches both provider options and compression is allowed over https.  |
| `BrotliCompressesWithBrotliAndNeverFallsBackToGzipAsync`                       | Fact   | Brotli compresses with brotli and never falls back to gzip.                            |
| `GzipCompressesAJsonResponseForAClientThatAcceptsItAsync`                      | Fact   | Gzip compresses a json response for a client that accepts it.                          |
| `GzipDoesNotUseBrotliEvenForAClientThatPrefersItAsync`                         | Fact   | Gzip does not use brotli even for a client that prefers it.                            |
| `AClientThatAcceptsNoEncodingGetsThePlainBodyAsync`                            | Fact   | A client that accepts no encoding gets the plain body.                                 |
| `MessagePackIsNotCompressedASecondTimeWhileTheMessagePackCompressionIsOnAsync` | Fact   | Message pack is not compressed a second time while the message pack compression is on. |
| `MessagePackIsCompressedHereWhenTheMessagePackCompressionIsOffAsync`           | Fact   | Message pack is compressed here when the message pack compression is off.              |
| `ASealedEnvelopeAndAnEventStreamAreNeverCompressedAsync`                       | Fact   | A sealed envelope and an event stream are never compressed.                            |

### WireCharMemberTests

`Jube.Test.App.Wire.WireCharMemberTests` — 3 tests. Category: Unit.

| Test                                                    | Kind | Verifies                                                            |
|---------------------------------------------------------|------|---------------------------------------------------------------------|
| `ACharMemberOfADtoTravelsAsAOneCharacterStringBothWays` | Fact | A char member of a DTO travels as a one character string both ways. |
| `ACharMemberSentAsAStringByTheSpaIsAccepted`            | Fact | A char member sent as a string by the SPA is accepted.              |
| `EveryDtoCharMemberUsesTheStringFormatter`              | Fact | Every DTO char member uses the string formatter.                    |

### WireModeTests

`Jube.Test.App.Wire.WireModeTests` — 9 tests. Category: Unit.

| Test                                                                                 | Kind   | Verifies                                                                                        |
|--------------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------------------|
| `JsonModeNeverWritesMessagePackEvenWhenAskedAsync`                                   | Fact   | Json mode never writes message pack even when asked.                                            |
| `MixedModeWritesMessagePackOnlyForResponsesThatCarryRowGridsAsync`                   | Fact   | Mixed mode writes message pack only for responses that carry row grids.                         |
| `MessagePackModeWritesMessagePackForEveryEncodableResponseAsync`                     | Fact   | Message pack mode writes message pack for every encodable response.                             |
| `ARequestBodyInMessagePackIsRejectedBeforeItIsParsedUnlessTheModeIsMessagePackAsync` | Theory | A request body in message pack is rejected before it is parsed unless the mode is message pack. |
| `MessagePackModeAcceptsAMessagePackRequestBodyAsync`                                 | Fact   | Message pack mode accepts a message pack request body.                                          |
| `TheSchemaNamesTheModeAndTheHashChangesWithItSoTheClientRefetchesAfterARestart`      | Fact   | The schema names the mode and the hash changes with it so the client refetches after a restart. |
| `ARegistryBuiltWithoutOptionsDefaultsToMessagePack`                                  | Fact   | A registry built without options defaults to message pack.                                      |
| `OnlyTypesWhoseGraphContainsRowGridsCarryRows`                                       | Fact   | Only types whose graph contains row grids carry rows.                                           |
| `OnlyTheThreeNamedModesParse`                                                        | Theory | Only the three named modes parse.                                                               |

### WireStringKeyedMapTests

`Jube.Test.App.Wire.WireStringKeyedMapTests` — 7 tests. Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                               |
|-----------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `AMapKeyedByGuidIsWrittenExactlyAsAMapKeyedByTheTextOfTheGuid`        | Fact   | A map keyed by guid is written exactly as a map keyed by the text of the guid.         |
| `IntegerAndGuidKeyedMapsRoundTripThroughEveryDictionaryInterface`     | Fact   | Integer and guid keyed maps round trip through every dictionary interface.             |
| `TheKeysAreExactlyTheNamesTheJsonSerialiserGivesTheSameMap`           | Fact   | The keys are exactly the names the json serialiser gives the same map.                 |
| `AKeyThatIsNotAValidNumberIsRefusedRatherThanSilentlyDropped`         | Fact   | A key that is not a valid number is refused rather than silently dropped.              |
| `ANullMapStaysNull`                                                   | Fact   | A null map stays null.                                                                 |
| `TheSchemaDescribesAMapWithANumericOrGuidKeyJustAsAMapWithAStringKey` | Theory | The schema describes a map with a numeric or guid key just as a map with a string key. |
| `AMapWithAKeyTheWireCannotTextualiseStaysOffTheWire`                  | Fact   | A map with a key the wire cannot textualise stays off the wire.                        |

### WirePipelineTests

`Jube.Test.App.Wire.WirePipelineTests` — 8 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                |
|--------------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `ABenignMessagePackBodyReachesTheEndpointAsTheEquivalentJsonAsync` | Fact   | Abenign message pack body reaches the endpoint as the equivalent json.  |
| `AnAttackCarriedInAMessagePackBodyIsStillBlockedByTheWafAsync`     | Theory | An attack carried in amessage pack body is still blocked by the waf.    |
| `TheSameAttackInJsonAndInMessagePackGetsTheSameVerdictAsync`       | Fact   | The same attack in json and in message pack gets the same verdict.      |
| `ANilBodyIsRejectedLikeTheJsonNullLiteralAsync`                    | Fact   | Anil body is rejected like the json null literal.                       |
| `MalformedBytesAreRejectedAs400WithoutReachingTheEndpointAsync`    | Theory | Malformed bytes are rejected as400 without reaching the endpoint.       |
| `ADeeplyNestedBodyIsRejectedAs400Async`                            | Fact   | Adeeply nested body is rejected as400.                                  |
| `AMessagePackBodyToAnEndpointThatTakesNoBodyIsRejectedAs415Async`  | Fact   | Amessage pack body to an endpoint that takes no body is rejected as415. |
| `AJsonRequestPassesThroughUntouchedAsync`                          | Fact   | Ajson request passes through untouched.                                 |

### WireResponseFilterTests

`Jube.Test.App.Wire.WireResponseFilterTests` — 7 tests. Category: Unit.

| Test                                                               | Kind | Verifies                                                                  |
|--------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `WithoutTheMessagePackAcceptHeaderTheResultIsLeftAsJsonAsync`      | Fact | Without the message pack accept header the result is left as json.        |
| `AnOkListIsWrittenAsMessagePackWithItsShapeAndSchemaHashAsync`     | Fact | An ok list is written as message pack with its shape and schema hash.     |
| `ABarePocoReturnedWithoutAnIResultIsAlsoWrittenAsMessagePackAsync` | Fact | A bare poco returned without an i result is also written as message pack. |
| `ErrorStatusesStayJsonSoTheClientErrorMappingIsUnchangedAsync`     | Fact | Error statuses stay json so the client error mapping is unchanged.        |
| `ACreatedResultIsWrittenAsMessagePackWithItsStatusAsync`           | Fact | A created result is written as message pack with its status.              |
| `ATypeThatCannotBeEncodedFallsBackToJsonAsync`                     | Fact | A type that cannot be encoded falls back to json.                         |
| `PlainTextAndEmptyResultsAreNotTouchedAsync`                       | Fact | Plain text and empty results are not touched.                             |

### WireRoundTripTests

`Jube.Test.App.Wire.WireRoundTripTests` — 5 tests. Category: Unit.

| Test                                                          | Kind | Verifies                                                                |
|---------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `EveryDtoTypeHasAWireShape`                                   | Fact | Every dto type has awire shape.                                         |
| `KeysAreUniqueWithinEveryHierarchy`                           | Fact | Keys are unique within every hierarchy.                                 |
| `EveryDtoSurvivesAMessagePackRoundTripUnchangedInItsJsonForm` | Fact | Every dto survives amessage pack round trip unchanged in its json form. |
| `SchemaFieldsAreInKeyOrderAndNamedAsTheJsonForm`              | Fact | Schema fields are in key order and named as the json form.              |
| `NullableValueTypesCarryAQuestionMarkAndCharHasItsOwnTag`     | Fact | Nullable value types carry aquestion mark and char has its own tag.     |

### OptimisticConcurrencyTests

`Jube.Test.Middlewares.OptimisticConcurrencyTests` — 2 tests. Category: Unit.

| Test                                                  | Kind   | Verifies                                                        |
|-------------------------------------------------------|--------|-----------------------------------------------------------------|
| `AVersionThatIsAbsentOrMatchesIsAccepted`             | Theory | A version that is absent or matches is accepted.                |
| `AVersionThatHasMovedOnIsRefusedAndNamesBothVersions` | Fact   | A version that has moved on is refused and names both versions. |

### VersionConflictMiddlewareTests

`Jube.Test.Middlewares.VersionConflictMiddlewareTests` — 3 tests. Category: Unit.

| Test                                               | Kind | Verifies                                                  |
|----------------------------------------------------|------|-----------------------------------------------------------|
| `AVersionConflictBecomesA409WithTheTwoVersions`    | Fact | A version conflict becomes a 409 with the two versions.   |
| `AnythingElseIsLeftToTheNextHandler`               | Fact | Anything else is left to the next handler.                |
| `ARequestThatDoesNotConflictPassesStraightThrough` | Fact | A request that does not conflict passes straight through. |

### FormAreaVersionTests

`Jube.Test.Service.Concurrency.FormAreaVersionTests` — 2 tests. Category: Unit.

| Test                                                            | Kind   | Verifies                                                                     |
|-----------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `TheMapperFromTheFormDtoCarriesTheVersionTheClientSawToThePoco` | Theory | The mapper from the form dto carries the version the client saw to the poco. |
| `MostFormAreasHaveAMapperThatTheGuardActuallyReaches`           | Fact   | Most form areas have a mapper that the guard actually reaches.               |

## Case

### SendHttpEndpointTests

`Jube.Test.Case.SendHttpEndpointTests` — 2 tests. Category: Unit.

| Test                                           | Kind | Verifies                                      |
|------------------------------------------------|------|-----------------------------------------------|
| `PostAsync_OnFailure_NeverLogsThePayloadAsync` | Fact | Post async on failure never logs the payload. |
| `GetAsync_OnFailure_NeverLogsTheFullUrlAsync`  | Fact | Get async on failure never logs the full url. |

### HookGuardTests

`Jube.Test.Case.HookGuardTests` — 11 tests. Category: Unit.

| Test                                                               | Kind   | Verifies                                                                          |
|--------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------|
| `ALiteralHostWithTokensOnlyInThePathAndQueryIsAccepted`            | Theory | A literal host with tokens only in the path and query is accepted.                |
| `ATokenValueIsEscapedSoItCanNeverAddAPathSegmentOrAQueryParameter` | Fact   | A token value is escaped so it can never add a path segment or a query parameter. |
| `AHostThatIsNotWrittenOutLiterallyIsRefused`                       | Theory | A host that is not written out literally is refused.                              |
| `ATokenThatRendersToAnotherHostIsRefused`                          | Fact   | A token that renders to another host is refused.                                  |
| `ARenderedAddressWithCredentialsInTheAuthorityIsRefused`           | Fact   | A rendered address with credentials in the authority is refused.                  |
| `AnOverlongAddressIsRefused`                                       | Fact   | An overlong address is refused.                                                   |
| `OnlyAVariableWithTheHookPrefixCanBeNamedAsACredential`            | Theory | Only a variable with the hook prefix can be named as a credential.                |
| `ACredentialIsReadOnlyFromAVariableWithTheHookPrefix`              | Fact   | A credential is read only from a variable with the hook prefix.                   |
| `AReservedOrMalformedHeaderNameIsRefused`                          | Theory | A reserved or malformed header name is refused.                                   |
| `TokensAreMatchedWithoutRegardToCaseAndUnknownOnesAreLeftAlone`    | Fact   | Tokens are matched without regard to case and unknown ones are left alone.        |
| `ABodyValueIsEscapedForJsonSoItCanNeverBreakOutOfTheString`        | Fact   | A body value is escaped for json so it can never break out of the string.         |

### IpClassifierTests

`Jube.Test.Case.IpClassifierTests` — 11 tests. Category: Unit.

| Test                                                                | Kind   | Verifies                                                                        |
|---------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `ABlockedIpv4RangeIsRefused`                                        | Theory | A blocked ipv4 range is refused.                                                |
| `APublicIpv4AddressAtTheEdgeOfABlockedRangeIsPermitted`             | Theory | A public ipv4 address at the edge of a blocked range is permitted.              |
| `ABlockedIpv6RangeIsRefused`                                        | Theory | A blocked ipv6 range is refused.                                                |
| `APublicIpv6AddressIsPermitted`                                     | Theory | A public ipv6 address is permitted.                                             |
| `AnIpv4MappedIpv6FormOfABlockedAddressIsRefused`                    | Theory | An ipv4 mapped ipv6 form of a blocked address is refused.                       |
| `TheCategoryNamesTheRangeWithoutNamingTheAddress`                   | Theory | The category names the range without naming the address.                        |
| `ALegacyHostSpellingParsesToTheAddressTheSocketLayerWouldConnectTo` | Theory | A legacy host spelling parses to the address the socket layer would connect to. |
| `BracketsAndScopeIdsDoNotHideAnIpv6Literal`                         | Theory | Brackets and scope ids do not hide an ipv6 literal.                             |
| `ANameThatIsNotAnAddressDoesNotParse`                               | Theory | A name that is not an address does not parse.                                   |
| `ACidrContainsOnlyItsOwnAddresses`                                  | Theory | A cidr contains only its own addresses.                                         |
| `AMalformedCidrDoesNotParse`                                        | Theory | A malformed cidr does not parse.                                                |

### OutboundHostPolicyTests

`Jube.Test.Case.OutboundHostPolicyTests` — 19 tests. Category: Unit.

| Test                                                                     | Kind   | Verifies                                                                               |
|--------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `AnInternalLiteralHostIsBlockedByDefault`                                | Theory | An internal literal host is blocked by default.                                        |
| `ANonHttpSchemeOrUserInfoIsRefused`                                      | Theory | A non http scheme or user info is refused.                                             |
| `APublicHostIsAcceptedAtSaveTime`                                        | Theory | A public host is accepted at save time.                                                |
| `ATokenInThePathIsNeutralisedBeforeTheHostIsChecked`                     | Fact   | A token in the path is neutralised before the host is checked.                         |
| `AnEmptyTemplateIsNotAnAbsoluteAddress`                                  | Theory | An empty template is not an absolute address.                                          |
| `ATokenCannotSupplyTheHostSoTheTemplateIsRefusedBeforeTheHostPolicyRuns` | Fact   | A token cannot supply the host so the template is refused before the host policy runs. |
| `AnAllowedHostIsAcceptedWhateverItResolvesTo`                            | Fact   | An allowed host is accepted whatever it resolves to.                                   |
| `AnAllowedHostDoesNotAllowItsNeighboursOrALookalikeSuffix`               | Fact   | An allowed host does not allow its neighbours or a lookalike suffix.                   |
| `AnAllowedCidrPermitsOnlyThatRangeAndNothingElseInternal`                | Fact   | An allowed cidr permits only that range and nothing else internal.                     |
| `AnAllowedLiteralHostOpensOnlyThatLiteral`                               | Fact   | An allowed literal host opens only that literal.                                       |
| `AnInvalidCidrIsReportedAndPermitsNothing`                               | Fact   | An invalid cidr is reported and permits nothing.                                       |
| `ResolutionToOnlyBlockedAddressesIsRefused`                              | Fact   | Resolution to only blocked addresses is refused.                                       |
| `ResolutionToNothingIsRefused`                                           | Fact   | Resolution to nothing is refused.                                                      |
| `AMixedAnswerKeepsOnlyThePublicAddresses`                                | Fact   | A mixed answer keeps only the public addresses.                                        |
| `ThePolicyIsReadFromTheDynamicEnvironment`                               | Fact   | The policy is read from the dynamic environment.                                       |
| `WithNoConfigurationNothingInternalIsAllowed`                            | Fact   | With no configuration nothing internal is allowed.                                     |
| `TheAllowListVariablesCanNeverBeNamedAsACredential`                      | Theory | The allow list variables can never be named as a credential.                           |
| `AnOrdinaryHookCredentialNameIsStillAccepted`                            | Fact   | An ordinary hook credential name is still accepted.                                    |
| `ARefusedDispatchCarriesTheCodeAndNoAddress`                             | Fact   | A refused dispatch carries the code and no address.                                    |

### GuardedHttpEndpointTests

`Jube.Test.Case.GuardedHttpEndpointTests` — 13 tests. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                                        |
|----------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `ANamedHostIsConnectedToTheAddressTheResolverReturnedAndNoOther`     | Fact   | A named host is connected to the address the resolver returned and no other.    |
| `ANameThatResolvesToAnInternalAddressIsRefusedAndNeverConnected`     | Fact   | A name that resolves to an internal address is refused and never connected.     |
| `EveryInternalAnswerFromTheResolverIsRefused`                        | Theory | Every internal answer from the resolver is refused.                             |
| `ANameThatResolvesToNothingIsRefused`                                | Fact   | A name that resolves to nothing is refused.                                     |
| `ARebindingAnswerIsCheckedAtConnectTimeNotJustAtSaveTime`            | Fact   | A rebinding answer is checked at connect time not just at save time.            |
| `AMixedAnswerIsPinnedToThePermittedAddressOnly`                      | Fact   | A mixed answer is pinned to the permitted address only.                         |
| `ALiteralInternalOrDisallowedTargetIsRefusedBeforeAnySocketIsOpened` | Theory | A literal internal or disallowed target is refused before any socket is opened. |
| `ARelativeAddressIsRefused`                                          | Fact   | A relative address is refused.                                                  |
| `ARedirectIsRefusedAndTheLocationIsNeverFollowed`                    | Theory | A redirect is refused and the location is never followed.                       |
| `ARedirectToTheMetadataAddressIsNotFollowedEither`                   | Fact   | A redirect to the metadata address is not followed either.                      |
| `AnAllowListedCidrPermitsALoopbackReceiverAndASuccessIsReported`     | Fact   | An allow listed cidr permits a loopback receiver and a success is reported.     |
| `ANon2xxFromAPermittedHostIsAFailureNotARefusal`                     | Fact   | A non2xx from a permitted host is a failure not a refusal.                      |
| `TheSharedEndpointForAnEnvironmentWithNoAllowListRefusesLoopback`    | Fact   | The shared endpoint for an environment with no allow list refuses loopback.     |

### PayloadEndpointCallTests

`Jube.Test.Case.PayloadEndpointCallTests` — 4 tests. Category: Unit.

| Test                                                         | Kind   | Verifies                                                                                                                               |
|--------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------------------|
| `AnInternalLiteralTargetIsRefusedAndNeverCalled`             | Theory | Loopback, private, metadata, localhost and decimal-spelled targets in a payload-mode endpoint are refused by the outbound host policy. |
| `AHostThatTheTransactionCouldChooseOrAnotherSchemeIsRefused` | Theory | A token in the host or authority, a non http scheme and userinfo are refused.                                                          |
| `ATokenInThePathIsRenderedAndAnAllowedHostIsCalled`          | Fact   | A token in the path is rendered and an allow-listed host is called.                                                                    |
| `ARepeatedlyFailingHostIsSuspendedAndNoLongerCalled`         | Fact   | The per-host breaker of Jube.ResilientHttp opens after repeated 5xx answers and stops further calls.                                   |

## Cluster

### ClusterComposeSecretTests

`Jube.Test.Cluster.ClusterComposeSecretTests` — 3 tests. Category: Unit.

| Test                                                         | Kind | Verifies                                                            |
|--------------------------------------------------------------|------|---------------------------------------------------------------------|
| `EveryComposeServiceRunningTheEngineAuthenticatesToEtcd`     | Fact | Every compose service running the engine authenticates to etcd.     |
| `EveryComposeServiceMountsTheSecretsItsEnvironmentTokenises` | Fact | Every compose service mounts the secrets its environment tokenises. |
| `EveryComposeServiceSecretIsDeclaredOnce`                    | Fact | Every compose service secret is declared once.                      |

### ClusterPatroniImageTests

`Jube.Test.Cluster.ClusterPatroniImageTests` — 4 tests. Category: Unit.

| Test                                                               | Kind | Verifies                                                                       |
|--------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `PatroniControlResolvesToTheShimAheadOfTheRealBinary`              | Fact | Patroni control resolves to the shim ahead of the real binary.                 |
| `TheShimLoadsTheSecretsAndHandsOffToTheRealBinaryWithoutRecursing` | Fact | The shim loads the secrets and hands off to the real binary without recursing. |
| `TheEntrypointAndTheShimReadTheirSecretsFromOneDefinition`         | Fact | The entrypoint and the shim read their secrets from one definition.            |
| `PatroniControlDefaultsToTheConfigurationFileTheContainerIsGiven`  | Fact | Patroni control defaults to the configuration file the container is given.     |

## Cryptography

### AesEncryptionTests

`Jube.Test.Cryptography.AesEncryptionTests` — 11 tests. Category: Unit.

| Test                                                                             | Kind | Verifies                                                                                       |
|----------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `IvModePropertyReflectsTheValuePassedToTheConstructor`                           | Fact | Iv mode property reflects the value passed to the constructor.                                 |
| `IvModePropertyDefaultsToRandom`                                                 | Fact | Iv mode property defaults to random.                                                           |
| `EncryptReturnsCipherTextThatCanBeDecryptedBackToTheOriginalPlainText`           | Fact | Encrypt returns cipher text that can be decrypted back to the original plain text.             |
| `EncryptWithDeterministicModeProducesTheSameCipherTextForTheSamePlainTextAndKey` | Fact | Encrypt with deterministic mode produces the same cipher text for the same plain text and key. |
| `EncryptWithDeterministicModeProducesDifferentCipherTextForDifferentPlainText`   | Fact | Encrypt with deterministic mode produces different cipher text for different plain text.       |
| `EncryptWithDeterministicModeEmbedsTheSha256HashOfThePlainTextAsTheIv`           | Fact | Encrypt with deterministic mode embeds the sha256 hash of the plain text as the iv.            |
| `EncryptWithRandomModeProducesDifferentCipherTextOnEachCall`                     | Fact | Encrypt with random mode produces different cipher text on each call.                          |
| `EncryptWithRandomModeProducesADifferentIvOnEachCall`                            | Fact | Encrypt with random mode produces a different iv on each call.                                 |
| `EncryptWithDifferentKeysProducesDifferentCipherTextForTheSamePlainText`         | Fact | Encrypt with different keys produces different cipher text for the same plain text.            |
| `EncryptRoundTripsAnEmptyPlainText`                                              | Fact | Encrypt round trips an empty plain text.                                                       |
| `EncryptReturnsValidBase64`                                                      | Fact | Encrypt returns valid base64.                                                                  |

### CryptographyExceptionsTests

`Jube.Test.Cryptography.CryptographyExceptionsTests` — 3 tests. Category: Unit.

| Test                                               | Kind | Verifies                                                |
|----------------------------------------------------|------|---------------------------------------------------------|
| `InvalidEncryptionExceptionExposesTheGivenMessage` | Fact | Invalid encryption exception exposes the given message. |
| `InvalidDecryptionExceptionExposesTheGivenMessage` | Fact | Invalid decryption exception exposes the given message. |
| `InvalidPasswordExceptionNamesTheMinimumLength`    | Fact | Invalid password exception names the minimum length.    |

### JempAesEncryptionTests

`Jube.Test.Cryptography.JempAesEncryptionTests` — 19 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                              |
|--------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `EncryptThenDecryptRoundTripsTheOriginalData`                                  | Fact   | Encrypt then decrypt round trips the original data.                                   |
| `EncryptThenDecryptRoundTripsEmptyData`                                        | Fact   | Encrypt then decrypt round trips empty data.                                          |
| `DecryptNeedsOnlyThePasswordFromAnotherInstance`                               | Fact   | Decrypt needs only the password from another instance.                                |
| `EncryptCarriesTheFormatMarkerAndVersionInTheClear`                            | Fact   | Encrypt carries the format marker and version in the clear.                           |
| `EncryptingTwiceGivesADifferentSaltNonceAndCipherText`                         | Fact   | Encrypting twice gives a different salt nonce and cipher text.                        |
| `EncryptRefusesAPasswordShorterThanTheMinimum`                                 | Theory | Encrypt refuses a password shorter than the minimum.                                  |
| `EncryptAcceptsAPasswordOfExactlyTheMinimumLength`                             | Fact   | Encrypt accepts a password of exactly the minimum length.                             |
| `IsAcceptablePasswordAppliesTheMinimum`                                        | Fact   | Is acceptable password applies the minimum.                                           |
| `EncryptOfNullDataThrowsInvalidEncryptionException`                            | Fact   | Encrypt of null data throws invalid encryption exception.                             |
| `DecryptWithAWrongPasswordThrowsInvalidDecryptionException`                    | Fact   | Decrypt with a wrong password throws invalid decryption exception.                    |
| `DecryptOfAnyTamperedByteThrowsInvalidDecryptionException`                     | Theory | Decrypt of any tampered byte throws invalid decryption exception.                     |
| `DecryptOfATamperedTagThrowsInvalidDecryptionException`                        | Fact   | Decrypt of a tampered tag throws invalid decryption exception.                        |
| `DecryptOfATruncatedPackageThrowsInvalidDecryptionException`                   | Fact   | Decrypt of a truncated package throws invalid decryption exception.                   |
| `DecryptOfTooShortDataThrowsInvalidDecryptionException`                        | Fact   | Decrypt of too short data throws invalid decryption exception.                        |
| `DecryptOfNullDataThrowsInvalidDecryptionException`                            | Fact   | Decrypt of null data throws invalid decryption exception.                             |
| `DecryptOfAnEarlierReleaseFormatThrowsInvalidDecryptionException`              | Fact   | Decrypt of an earlier release format throws invalid decryption exception.             |
| `DecryptRefusesAnIterationCountOutsideTheSupportedRange`                       | Fact   | Decrypt refuses an iteration count outside the supported range.                       |
| `DecryptRefusesAnIterationCountJustOutsideTheSupportedRangeBeforeDerivingAKey` | Theory | Decrypt refuses an iteration count of 99,999 or 1,000,001 before deriving a key.      |
| `DecryptOfAnIterationCountEditedWithinTheSupportedRangeFailsAuthentication`    | Fact   | Decrypt of an iteration count edited within the supported range fails authentication. |

### CiCategoryCoverageTests

`Jube.Test.Documentation.CiCategoryCoverageTests` — 1 test. Category: Unit.

| Test                                                        | Kind | Verifies                                                               |
|-------------------------------------------------------------|------|------------------------------------------------------------------------|
| `EveryTestCategoryIsNamedByAWorkflowFilterSoEveryTestGates` | Fact | Every test category is named by a workflow filter so every test gates. |

### ExtensionReferenceDocumentationTests

`Jube.Test.Documentation.ExtensionReferenceDocumentationTests` — 4 tests. Category: Unit.

| Test                                                                     | Kind | Verifies                                                                            |
|--------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `EveryPlainExtensionMethodHasARowThatMatchesItsDescriptionAndParameters` | Fact | Every plain extension method has a row that matches its description and parameters. |
| `NoRowDocumentsAMethodThatNoLongerExists`                                | Fact | No row documents a method that no longer exists.                                    |
| `EveryPipelineTestHasARowWhoseTextAndArgumentsMatchTheStep`              | Fact | Every pipeline test has a row whose text and arguments match the step.              |
| `EveryCalculationHasARowThatMatchesItsDescription`                       | Fact | Every calculation has a row that matches its description.                           |

### TestDocumentationTests

`Jube.Test.Documentation.TestDocumentationTests` — 3 tests. Category: Unit.

| Test                                               | Kind | Verifies                                                       |
|----------------------------------------------------|------|----------------------------------------------------------------|
| `EveryTestClassIsDocumentedExactlyOnce`            | Fact | Every test class is documented exactly once.                   |
| `EveryTestHasARowInItsClassSectionAndNoRowIsStale` | Fact | Every test has a row in its class section and no row is stale. |
| `EveryDocumentedTestClassStillExists`              | Fact | Every documented test class still exists.                      |

## Dynamic environment

### ApplicationLogCaptureTests

`Jube.Test.DynamicEnvironment.ApplicationLogCaptureTests` — 2 tests. Category: Unit. Collection: ApplicationLogCapture.

| Test                                                                         | Kind | Verifies                                                                               |
|------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `DrainAllReturnsEveryEnqueuedRecordInOrderAndEmptiesTheQueue`                | Fact | Drain all returns every enqueued record in order and empties the queue.                |
| `EnqueueBeyondMaxQueueLengthIncrementsDroppedCountInsteadOfGrowingUnbounded` | Fact | Enqueue beyond max queue length increments dropped count instead of growing unbounded. |

### EnvironmentVariablesDocumentationTests

`Jube.Test.DynamicEnvironment.EnvironmentVariablesDocumentationTests` — 6 tests. Category: Unit. Collection:
EvalExpressionRegistry.

| Test                                                      | Kind   | Verifies                                                          |
|-----------------------------------------------------------|--------|-------------------------------------------------------------------|
| `EveryConfigurationKeyInCodeIsDocumented`                 | Fact   | Every configuration key in code is documented.                    |
| `EveryDocumentedKeyStillExistsInCode`                     | Fact   | Every documented key still exists in code.                        |
| `EveryDocumentedDefaultMatchesTheDefaultInCode`           | Fact   | Every documented default matches the default in code.             |
| `TheAllowListsHoldNoEntryThatIsNoLongerNeeded`            | Fact   | The allow lists hold no entry that is no longer needed.           |
| `TheSanctionLoaderKeysAreDocumentedWithTheirRealDefaults` | Theory | The sanction loader keys are documented with their real defaults. |
| `TheSanctionsLoaderPageMentionsTheChangePollKeys`         | Fact   | The sanctions loader page mentions the change poll keys.          |

## Http adaptation engine call

### EntityAnalysisModelHttpAdaptationTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.EntityAnalysisModelHttpAdaptationTests` — 11
tests. Category: Unit. Each test serves a canned response from a loopback socket.

| Test                                                                          | Kind   | Verifies                                                                            |
|-------------------------------------------------------------------------------|--------|-------------------------------------------------------------------------------------|
| `SuccessfulResponseBodyIsReturnedAsync`                                       | Fact   | Successful response body is returned.                                               |
| `RedirectIsNotFollowedAndIsRefusedAsync`                                      | Fact   | Redirect is not followed and is refused.                                            |
| `UnsuccessfulStatusThrowsInsteadOfReturningTheErrorPageAsync`                 | Fact   | Unsuccessful status throws instead of returning the error page.                     |
| `ResponseOverTheMaximumSizeIsRefusedAsync`                                    | Fact   | Response over the maximum size is refused.                                          |
| `RecallOfAnUnsuccessfulStatusIsASuppressedAdaptationNotAnExceptionAsync`      | Fact   | Recall of an unsuccessful status is asuppressed adaptation not an exception.        |
| `RecallOfAnUnreachableEndpointIsASuppressedAdaptationAsync`                   | Fact   | Recall of an unreachable endpoint is asuppressed adaptation.                        |
| `RecallOfAnEndpointThatNeverRespondsIsASuppressedAdaptationAtTheTimeoutAsync` | Fact   | Recall of an endpoint that never responds is asuppressed adaptation at the timeout. |
| `IsPlainPathAcceptsOnlyAPathBelowTheConfiguredHost`                           | Theory | Is plain path accepts only apath below the configured host.                         |
| `StrictModeRefusesALoopbackTargetAtConnectTimeAsync`                          | Fact   | Strict mode refuses aloopback target at connect time.                               |
| `StrictModeRefusesAHostNameThatResolvesToLoopbackAsync`                       | Fact   | Strict mode refuses ahost name that resolves to loopback.                           |
| `BreakerOpensAfterRepeatedFailuresAndIsKeyedPerBreakerKeyAsync`               | Fact   | Breaker opens after repeated failures and is keyed per breaker key.                 |

### AdaptationHttpClientCacheTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.AdaptationHttpClientCacheTests` — 5 tests.
Category: Unit.

| Test                                                       | Kind | Verifies                                                            |
|------------------------------------------------------------|------|---------------------------------------------------------------------|
| `SameSettingsForATenantReuseTheClient`                     | Fact | The same settings for a tenant reuse the client.                    |
| `DifferentTenantsGetDifferentClients`                      | Fact | Different tenants get different clients.                            |
| `ChangedSettingsRetireTheOldClientOnlyAfterTheGracePeriod` | Fact | Changed settings retire the old client only after the grace period. |
| `RetiringAClientDoesNotCutShortAnInFlightRequest`          | Fact | Retiring a client does not cut short an in-flight request.          |
| `DisposingTheCacheStopsPendingRetirementsImmediately`      | Fact | Disposing the cache stops pending retirements immediately.          |

### HttpAdaptationSettingsTests

`Jube.Test.Engine.EntityAnalysisModelManager.EntityAnalysisModel.Models.HttpAdaptationSettingsTests` — 10 tests.
Category: Unit.

| Test                                                                     | Kind   | Verifies                                                                               |
|--------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------|
| `WithNoTenantUrlTheGlobalSettingsApplyAndAreTrusted`                     | Fact   | With no tenant url the global settings apply and are trusted.                          |
| `GlobalValuesAreReadFromTheEnvironment`                                  | Fact   | Global values are read from the environment.                                           |
| `UnparseableGlobalValuesFallBackToTheSecureDefaults`                     | Fact   | Unparseable global values fall back to the secure defaults.                            |
| `ATenantUrlOverridesTheGlobalUrlAndIsHeldToTheOutboundPolicy`            | Fact   | Atenant url overrides the global url and is held to the outbound policy.               |
| `ATenantWithNoTimeoutInheritsTheGlobalTimeoutButKeepsItsOwnSslSetting`   | Fact   | Atenant with no timeout inherits the global timeout but keeps its own ssl setting.     |
| `ANullSslColumnMeansValidate`                                            | Fact   | Anull ssl column means validate.                                                       |
| `AnUnsafeTenantUrlIsRefusedAtSyncEvenIfItWasStoredBypassingTheValidator` | Theory | An unsafe tenant url is refused at sync even if it was stored bypassing the validator. |
| `TheDevelopmentFlagRelaxesATenantUrlOnly`                                | Fact   | The development flag relaxes atenant url only.                                         |
| `ResilienceDefaultsMatchTheDocumentedSettingsAndNeverRetry`              | Fact   | Resilience defaults match the documented settings and never retry.                     |
| `ResilienceValuesOutsideWhatThePipelineAcceptsAreClamped`                | Fact   | Resilience values outside what the pipeline accepts are clamped.                       |

## Resilient http

### ResilientHttpExecutorTests

`Jube.Test.ResilientHttp.ResilientHttpExecutorTests` — 12 tests. Category: Unit.

| Test                                                                       | Kind   | Verifies                                                                       |
|----------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `SuccessfulCallReturnsTheValueAsync`                                       | Fact   | Successful call returns the value.                                             |
| `CircuitOpensAfterRepeatedTransientFailuresAndStopsInvokingTheActionAsync` | Fact   | Circuit opens after repeated transient failures and stops invoking the action. |
| `OneKeyOpeningTheCircuitDoesNotAffectAnotherKeyAsync`                      | Fact   | One key opening the circuit does not affect another key.                       |
| `ClientErrorsDoNotOpenTheCircuitAsync`                                     | Fact   | Client errors do not open the circuit.                                         |
| `NoRetryIsMadeByDefaultAsync`                                              | Fact   | No retry is made by default.                                                   |
| `ConfiguredRetriesRetryOnlyTransientFailuresAsync`                         | Fact   | Configured retries retry only transient failures.                              |
| `CallExceedingTheTimeoutIsRejectedAsync`                                   | Fact   | Call exceeding the timeout is rejected.                                        |
| `SamePipelineIsReusedForTheSameKeyAndOptionsAsync`                         | Fact   | Same pipeline is reused for the same key and options.                          |
| `StatusClassification`                                                     | Theory | Status classification.                                                         |
| `NetworkLevelFailuresAreTransientAndCallerCancellationIsNot`               | Fact   | Network level failures are transient and caller cancellation is not.           |
| `ThrowIfUnsuccessfulCarriesTheStatusCode`                                  | Fact   | Throw if unsuccessful carries the status code.                                 |
| `DefaultsAreConfigurableAndDocumented`                                     | Fact   | Defaults are configurable and documented.                                      |

### TenantBaseUrlPolicyTests

`Jube.Test.Case.TenantBaseUrlPolicyTests` — 8 tests. Category: Unit.

| Test                                                                           | Kind   | Verifies                                                                                   |
|--------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `PublicHttpsUrlsArePermitted`                                                  | Theory | Public https urls are permitted.                                                           |
| `UnsafeUrlsAreRefusedWithTheirCode`                                            | Theory | Unsafe urls are refused with their code.                                                   |
| `TheDevelopmentFlagPermitsHttpAndInternalHostsButNotCredentialsOrOtherSchemes` | Theory | The development flag permits http and internal hosts but not credentials or other schemes. |
| `ResolvedAddressesAreCheckedSoARebindingNameIsRefused`                         | Fact   | Resolved addresses are checked so arebinding name is refused.                              |
| `AMixedAnswerKeepsOnlyThePublicAddress`                                        | Fact   | Amixed answer keeps only the public address.                                               |
| `NoAddressIsNotResolved`                                                       | Fact   | No address is not resolved.                                                                |
| `TheDevelopmentFlagPermitsInternalAddresses`                                   | Fact   | The development flag permits internal addresses.                                           |
| `TheFlagKeyIsTheDocumentedSetting`                                             | Fact   | The flag key is the documented setting.                                                    |

## Http adaptation protocol

### AdaptationTests

`Jube.Test.HttpAdaptationProtocol.AdaptationTests` — 13 tests. Category: Unit.

| Test                                                                | Kind | Verifies                                                                     |
|---------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `IsSuppressedIsTrueWhenValueIsNull`                                 | Fact | Is suppressed is true when value is null.                                    |
| `IsSuppressedIsTrueWhenErrorIsSet`                                  | Fact | Is suppressed is true when error is set.                                     |
| `IsSuppressedIsFalseWhenValueIsSetAndErrorIsNull`                   | Fact | Is suppressed is false when value is set and error is null.                  |
| `IsSuppressedIsFalseWhenValueIsSetAndErrorIsEmpty`                  | Fact | Is suppressed is false when value is set and error is empty.                 |
| `IsSuppressedIsTrueWhenValueIsNullAndErrorIsAlsoSet`                | Fact | Is suppressed is true when value is null and error is also set.              |
| `ImplicitConversionToNullableDoubleReturnsValue`                    | Fact | Implicit conversion to nullable double returns value.                        |
| `ImplicitConversionToNullableDoubleReturnsNullWhenValueIsNull`      | Fact | Implicit conversion to nullable double returns null when value is null.      |
| `ImplicitConversionToNullableDoubleReturnsNullWhenAdaptationIsNull` | Fact | Implicit conversion to nullable double returns null when adaptation is null. |
| `HoldsAssignedFields`                                               | Fact | Holds assigned fields.                                                       |
| `IsSuppressedIsIgnoredWhenSerializedToJson`                         | Fact | Is suppressed is ignored when serialized to json.                            |
| `RecordsWithEqualValuesAreEqual`                                    | Fact | Records with equal values are equal.                                         |
| `RecordsWithDifferentValuesAreNotEqual`                             | Fact | Records with different values are not equal.                                 |
| `WithExpressionProducesAModifiedCopyWithoutMutatingTheOriginal`     | Fact | With expression produces a modified copy without mutating the original.      |

### ProtocolConstantsTests

`Jube.Test.HttpAdaptationProtocol.ProtocolConstantsTests` — 8 tests. Category: Unit.

| Test                                            | Kind | Verifies                                            |
|-------------------------------------------------|------|-----------------------------------------------------|
| `CurrentProtocolVersionIsOneDotOne`             | Fact | Current protocol version is one dot one.            |
| `FamilyConstantsHaveExpectedValues`             | Fact | Family constants have expected values.              |
| `ContributionSpaceConstantsHaveExpectedValues`  | Fact | Contribution space constants have expected values.  |
| `ContributionMethodConstantsHaveExpectedValues` | Fact | Contribution method constants have expected values. |
| `ValueSpaceConstantsHaveExpectedValues`         | Fact | Value space constants have expected values.         |
| `CalibrationMethodConstantsHaveExpectedValues`  | Fact | Calibration method constants have expected values.  |
| `StructureLearningConstantsHaveExpectedValues`  | Fact | Structure learning constants have expected values.  |
| `SourceConstantsHaveExpectedValues`             | Fact | Source constants have expected values.              |

## Http adaptation protocol calibration

### CalibrationBandTests

`Jube.Test.HttpAdaptationProtocol.Calibration.CalibrationBandTests` — 4 tests. Category: Unit.

| Test                                         | Kind | Verifies                                          |
|----------------------------------------------|------|---------------------------------------------------|
| `HoldsAssignedFields`                        | Fact | Holds assigned fields.                            |
| `AllFieldsDefaultToNullWhenNotAssigned`      | Fact | All fields default to null when not assigned.     |
| `RoundTripsThroughJson`                      | Fact | Round trips through json.                         |
| `DeserializesFromLowerCaseJsonPropertyNames` | Fact | Deserializes from lower case json property names. |

### CalibrationDescriptorTests

`Jube.Test.HttpAdaptationProtocol.Calibration.CalibrationDescriptorTests` — 3 tests. Category: Unit.

| Test                                        | Kind | Verifies                                         |
|---------------------------------------------|------|--------------------------------------------------|
| `HoldsAssignedFields`                       | Fact | Holds assigned fields.                           |
| `AllFieldsDefaultToNullWhenNotAssigned`     | Fact | All fields default to null when not assigned.    |
| `RoundTripsThroughJsonIncludingNestedBands` | Fact | Round trips through json including nested bands. |

## Http adaptation protocol contribution

### ContributionItemTests

`Jube.Test.HttpAdaptationProtocol.Contribution.ContributionItemTests` — 3 tests. Category: Unit.

| Test                                    | Kind | Verifies                                      |
|-----------------------------------------|------|-----------------------------------------------|
| `HoldsAssignedFields`                   | Fact | Holds assigned fields.                        |
| `AllFieldsDefaultToNullWhenNotAssigned` | Fact | All fields default to null when not assigned. |
| `RoundTripsThroughJson`                 | Fact | Round trips through json.                     |

### ContributionSetTests

`Jube.Test.HttpAdaptationProtocol.Contribution.ContributionSetTests` — 4 tests. Category: Unit.

| Test                                            | Kind | Verifies                                                |
|-------------------------------------------------|------|---------------------------------------------------------|
| `HoldsAssignedFields`                           | Fact | Holds assigned fields.                                  |
| `AllFieldsDefaultToNullWhenNotAssigned`         | Fact | All fields default to null when not assigned.           |
| `RoundTripsThroughJsonIncludingNestedItems`     | Fact | Round trips through json including nested items.        |
| `EmptyItemsListRoundTripsAsEmptyRatherThanNull` | Fact | Empty items list round trips as empty rather than null. |

## Http adaptation protocol journey

### JourneyDescriptorTests

`Jube.Test.HttpAdaptationProtocol.Journey.JourneyDescriptorTests` — 3 tests. Category: Unit.

| Test                                       | Kind | Verifies                                        |
|--------------------------------------------|------|-------------------------------------------------|
| `HoldsAssignedPath`                        | Fact | Holds assigned path.                            |
| `PathDefaultsToNullWhenNotAssigned`        | Fact | Path defaults to null when not assigned.        |
| `RoundTripsThroughJsonPreservingNodeOrder` | Fact | Round trips through json preserving node order. |

### JourneyNodeTests

`Jube.Test.HttpAdaptationProtocol.Journey.JourneyNodeTests` — 4 tests. Category: Unit.

| Test                                            | Kind | Verifies                                            |
|-------------------------------------------------|------|-----------------------------------------------------|
| `HoldsAssignedFields`                           | Fact | Holds assigned fields.                              |
| `SupportsCategoricalThresholdsInsteadOfNumeric` | Fact | Supports categorical thresholds instead of numeric. |
| `AllFieldsDefaultToNullWhenNotAssigned`         | Fact | All fields default to null when not assigned.       |
| `RoundTripsThroughJson`                         | Fact | Round trips through json.                           |

## Http adaptation protocol models

### ModelDescriptorTests

`Jube.Test.HttpAdaptationProtocol.Models.ModelDescriptorTests` — 7 tests. Category: Unit.

| Test                                             | Kind | Verifies                                              |
|--------------------------------------------------|------|-------------------------------------------------------|
| `HoldsAssignedFieldsForAGlmModel`                | Fact | Holds assigned fields for a glm model.                |
| `HoldsAssignedFieldsForARandomForestModel`       | Fact | Holds assigned fields for a random forest model.      |
| `HoldsAssignedFieldsForAc5Model`                 | Fact | Holds assigned fields for ac5 model.                  |
| `HoldsAssignedFieldsForABayesianNetworkModel`    | Fact | Holds assigned fields for a bayesian network model.   |
| `HoldsAssignedFieldsForANeuralNetworkModel`      | Fact | Holds assigned fields for a neural network model.     |
| `AllFieldsDefaultToNullWhenNotAssigned`          | Fact | All fields default to null when not assigned.         |
| `RoundTripsThroughJsonIncludingNestedValidation` | Fact | Round trips through json including nested validation. |

### ResultDescriptorTests

`Jube.Test.HttpAdaptationProtocol.Models.ResultDescriptorTests` — 3 tests. Category: Unit.

| Test                                    | Kind | Verifies                                      |
|-----------------------------------------|------|-----------------------------------------------|
| `HoldsAssignedFields`                   | Fact | Holds assigned fields.                        |
| `AllFieldsDefaultToNullWhenNotAssigned` | Fact | All fields default to null when not assigned. |
| `RoundTripsThroughJson`                 | Fact | Round trips through json.                     |

### ValidationDescriptorTests

`Jube.Test.HttpAdaptationProtocol.Models.ValidationDescriptorTests` — 3 tests. Category: Unit.

| Test                                    | Kind | Verifies                                      |
|-----------------------------------------|------|-----------------------------------------------|
| `HoldsAssignedFields`                   | Fact | Holds assigned fields.                        |
| `AllFieldsDefaultToNullWhenNotAssigned` | Fact | All fields default to null when not assigned. |
| `RoundTripsThroughJson`                 | Fact | Round trips through json.                     |

## Http adaptation protocol parsing

### AdaptationResponseParserTests

`Jube.Test.HttpAdaptationProtocol.Parsing.AdaptationResponseParserTests` — 17 tests. Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                           |
|------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------|
| `NullBodyReturnsAnEmptyResponseError`                                              | Fact   | Null body returns an empty response error.                                                         |
| `EmptyBodyReturnsAnEmptyResponseError`                                             | Fact   | Empty body returns an empty response error.                                                        |
| `WhitespaceOnlyBodyReturnsAnEmptyResponseError`                                    | Fact   | Whitespace only body returns an empty response error.                                              |
| `BareNumericBodyIsParsedDirectlyAsTheValue`                                        | Theory | Bare numeric body is parsed directly as the value.                                                 |
| `BracketedBareNumericBodyIsUnwrappedAndParsedAsTheValue`                           | Theory | Bracketed bare numeric body is unwrapped and parsed as the value.                                  |
| `CommaDecimalSeparatorIsNotTreatedAsANumberAndFallsThroughToAnUnparsableError`     | Fact   | Comma decimal separator is not treated as a number and falls through to an unparsable error.       |
| `MalformedJsonReturnsAnUnparsableErrorAndLogsAWarning`                             | Fact   | Malformed json returns an unparsable error and logs a warning.                                     |
| `MalformedJsonWarningLogsOnlyTheFirstTwoHundredCharactersOfTheBody`                | Fact   | The warning for an unparsable response carries at most 200 characters of the body.                 |
| `JsonArrayThatCannotDeserializeIntoAnAdaptationReturnsAnUnparsableError`           | Fact   | Json array that cannot deserialize into an adaptation returns an unparsable error.                 |
| `WarningIsNotLoggedWhenWarnLevelIsDisabled`                                        | Fact   | Warning is not logged when warn level is disabled.                                                 |
| `JsonLiteralNullDeserializesToAFreshSuppressedAdaptation`                          | Fact   | Json literal null deserializes to a fresh suppressed adaptation.                                   |
| `SimpleJsonObjectWithOnlyAValueIsParsed`                                           | Fact   | Simple json object with only a value is parsed.                                                    |
| `UnknownJsonMembersAreIgnoredRatherThanCausingAFailure`                            | Fact   | Unknown json members are ignored rather than causing a failure.                                    |
| `WhenBothErrorAndValueArePresentTheValueIsForcedToNullSoTheAdaptationIsSuppressed` | Fact   | When both error and value are present the value is forced to null so the adaptation is suppressed. |
| `ErrorOnlyResponseHasNoValueAndIsSuppressed`                                       | Fact   | Error only response has no value and is suppressed.                                                |
| `FullProtocolResponseIsParsedIntoAllNestedDescriptors`                             | Fact   | Full protocol response is parsed into all nested descriptors.                                      |
| `LeadingAndTrailingWhitespaceAroundAFullJsonObjectIsTrimmedBeforeParsing`          | Fact   | Leading and trailing whitespace around a full json object is trimmed before parsing.               |

## Middlewares

### GlobalHttpRateLimitMiddlewareTests

`Jube.Test.Middlewares.GlobalHttpRateLimitMiddlewareTests` — 6 tests. Category: Unit. Collection: JubeCache.

| Test                                                                                       | Kind | Verifies                                                                                           |
|--------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------|
| `WhenDisabledEveryRequestReachesTheEndpointAsync`                                          | Fact | When disabled every request reaches the endpoint.                                                  |
| `WhenEnabledRequestsBeyondTheThresholdGet429AndDoNotReachTheEndpointAsync`                 | Fact | When enabled requests beyond the threshold get429 and do not reach the endpoint.                   |
| `DifferentSourceIpsHaveIndependentBudgetsAsync`                                            | Fact | Different source ips have independent budgets.                                                     |
| `WhenBackplaneIsEnabledTwoMiddlewareInstancesShareTheCountThroughTheSameCacheServiceAsync` | Fact | When backplane is enabled two middleware instances share the count through the same cache service. |
| `WithoutTheBackplaneTwoMiddlewareInstancesDoNotShareTheCountAsync`                         | Fact | Without the backplane two middleware instances do not share the count.                             |
| `ANonPositiveThresholdNeverRateLimitsAsync`                                                | Fact | A non positive threshold never rate limits.                                                        |

### GlobalHttpRequestSourceIpThrottleTests

`Jube.Test.Middlewares.GlobalHttpRequestSourceIpThrottleTests` — 7 tests. Category: Unit.

| Test                                          | Kind | Verifies                                            |
|-----------------------------------------------|------|-----------------------------------------------------|
| `RequestsWithinTheThresholdAreNeverExceeded`  | Fact | Requests within the threshold are never exceeded.   |
| `TheRequestThatCrossesTheThresholdIsExceeded` | Fact | The request that crosses the threshold is exceeded. |
| `EveryRequestCountsNotJustFailures`           | Fact | Every request counts not just failures.             |
| `DifferentSourceIpsAreCountedIndependently`   | Fact | Different source ips are counted independently.     |
| `TheWindowResetsOnceItHasElapsed`             | Fact | The window resets once it has elapsed.              |
| `ANullOrEmptyRemoteIpIsNeverThrottled`        | Fact | A null or empty remote ip is never throttled.       |
| `ANonPositiveThresholdIsNeverThrottled`       | Fact | A non positive threshold is never throttled.        |

## Mocks

### MockEndpointsGoldenTests

`Jube.Test.Mocks.MockEndpointsGoldenTests` — 5 tests. Category: Unit.

| Test                                                  | Kind   | Verifies                                                      |
|-------------------------------------------------------|--------|---------------------------------------------------------------|
| `ResponseIsByteIdenticalToTheLegacyControllerAsync`   | Theory | Response is byte identical to the legacy controller.          |
| `EveryCaseHasAGoldenAndNothingElse`                   | Fact   | Every case has a golden and nothing else.                     |
| `HttpAdaptationRoutesAndVerbsAreExactlyTheLegacyOnes` | Fact   | Http adaptation routes and verbs are exactly the legacy ones. |
| `MfaIsASinglePostRoute`                               | Fact   | Mfa is a single post route.                                   |
| `EveryMockEndpointIsExplicitlyAnonymous`              | Fact   | Every mock endpoint is explicitly anonymous.                  |

## Network golden

### ExhaustiveGoldenTests

`Jube.Test.NetworkGolden.ExhaustiveGoldenTests` — 6 tests.

| Test                                          | Kind   | Verifies                                            |
|-----------------------------------------------|--------|-----------------------------------------------------|
| `RecallOfCommittedTopologyIsBitExact`         | Theory | Recall of committed topology is bit exact.          |
| `LoadedTopologyKeepsShapeAndAnnotations`      | Theory | Loaded topology keeps shape and annotations.        |
| `SerialiseThenLoadRoundTripIsBitExact`        | Theory | Serialise then load round trip is bit exact.        |
| `EveryNetworkHasExpectedOutputsForEveryProbe` | Fact   | Every network has expected outputs for every probe. |
| `RecallIsStableAcrossRepeatedCalls`           | Fact   | Recall is stable across repeated calls.             |
| `GenerateResourcesWhenRequested`              | Fact   | Generate resources when requested.                  |

## Task cancellation

### TaskHelperTests

`Jube.Test.TaskCancellation.TaskHelperTests` — 9 tests. Category: Unit.

| Test                                                                                      | Kind   | Verifies                                                                                          |
|-------------------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------------------|
| `TimedTaskResultConstructorArgumentOrderIsComputeTimeThenMemory`                          | Fact   | Timed task result constructor argument order is compute time then memory.                         |
| `TaskTypeNumericValueIsStable`                                                            | Theory | Task type numeric value is stable.                                                                |
| `MeasureTaskTimeAndMemoryAllocatedAsyncWithoutLogPropagatesFaultUnchangedAsync`           | Fact   | Measure task time and memory allocated async without log propagates fault unchanged.              |
| `MeasureTaskTimeAndMemoryAllocatedAsyncWithLogReturnsFaultedResultInsteadOfThrowingAsync` | Fact   | Measure task time and memory allocated async with log returns faulted result instead of throwing. |
| `MeasureTaskTimeAndMemoryAllocatedAsyncWithLogAndRethrowOnFaultPropagatesFaultAsync`      | Fact   | Measure task time and memory allocated async with log and rethrow on fault propagates fault.      |
| `MeasureTaskTimeAndMemoryAllocatedAsyncOnSuccessIsNotFaultedAsync`                        | Fact   | Measure task time and memory allocated async on success is not faulted.                           |
| `MeasureTimeAndMemoryAllocatedSynchronousOverloadDoesNotThrowWhenLogSuppliedOnFault`      | Fact   | Measure time and memory allocated synchronous overload does not throw when log supplied on fault. |
| `MeasureTimeAndMemoryAllocatedSynchronousOverloadWithoutLogPropagatesFault`               | Fact   | Measure time and memory allocated synchronous overload without log propagates fault.              |
| `MeasureTimeAndMemoryAllocatedSynchronousOverloadOnSuccessRecordsElapsedTime`             | Fact   | Measure time and memory allocated synchronous overload on success records elapsed time.           |

## Validations

### CaseDtoValidatorTests

`Jube.Test.Validations.CaseDtoValidatorTests` — 9 tests. Category: Unit.

| Test                                                                      | Kind   | Verifies                                                                                |
|---------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------|
| `TakingAnEngineCreatedCaseThatIsUnratedAndHasNoPayloadIsValid`            | Fact   | Taking an engine-created case that is unrated and has no payload is valid.              |
| `ReleasingAndClosingAnUnratedCaseWithNoPayloadIsValid`                    | Fact   | Releasing and closing an unrated case with no payload is valid.                         |
| `RatingsFromZeroToFiveAreValid`                                           | Theory | Ratings from zero to five are valid.                                                    |
| `ARatingAboveFiveIsRefused`                                               | Fact   | A rating above five is refused.                                                         |
| `ALockWithoutAUserIsStillRefused`                                         | Fact   | A lock without a user is still refused.                                                 |
| `ClosingWithAKnownCloseReasonIsValid`                                     | Theory | Closing with a known close reason is valid.                                             |
| `ACloseReasonOutsideTheEnumerationIsInvalid`                              | Fact   | A close reason outside the enumeration is invalid.                                      |
| `ACloseReasonNoteOverOneThousandCharactersIsInvalid`                      | Fact   | A close reason note over one thousand characters is invalid.                            |
| `AStaleCloseReasonCarriedOnAReopenIsNotRejectedBecauseTheServiceClearsIt` | Fact   | A stale close reason carried on a reopen is not rejected because the service clears it. |

### EntityAnalysisModelActivationRuleOverrideDtoValidatorTests

`Jube.Test.Validations.EntityAnalysisModelActivationRuleOverrideDtoValidatorTests` — 6 tests. Category: Service.
Collection: Database.

| Test                                                                 | Kind | Verifies                                                                   |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `ForceIsRefusedWhereTheActivationRuleIsNotEnabledForForceAsync`      | Fact | Force is refused where the activation rule is not enabled for force.       |
| `SuppressIsAdmittedWhereTheActivationRuleIsNotEnabledForForceAsync`  | Fact | Suppress is admitted where the activation rule is not enabled for force.   |
| `ForceIsAdmittedWhereTheActivationRuleIsEnabledForForceAsync`        | Fact | Force is admitted where the activation rule is enabled for force.          |
| `AnyKindIsRefusedWhereTheActivationRuleIsNotEnabledForOverrideAsync` | Fact | Any kind is refused where the activation rule is not enabled for override. |
| `OverrideIsRefusedWhereTheActivationRuleIsBoundToAnotherKeyAsync`    | Fact | Override is refused where the activation rule is bound to another key.     |
| `OverrideIsAdmittedFromAnyKeyWhereTheActivationRuleBindsNoKeyAsync`  | Fact | Override is admitted from any key where the activation rule binds no key.  |

### JsonTextTests

`Jube.Test.Validations.JsonTextTests` — 8 tests. Category: Unit.

| Test                                                  | Kind | Verifies                                                     |
|-------------------------------------------------------|------|--------------------------------------------------------------|
| `NullOrWhiteSpaceIsAcceptable`                        | Fact | Null or white space is acceptable.                           |
| `OrdinaryJsonIsAcceptable`                            | Fact | Ordinary json is acceptable.                                 |
| `InvalidJsonIsRejected`                               | Fact | Invalid json is rejected.                                    |
| `NullCodePointEscapeIsRejected`                       | Fact | Null code point escape is rejected.                          |
| `ProperlyPairedSurrogateEscapeIsAcceptable`           | Fact | Properly paired surrogate escape is acceptable.              |
| `LoneHighSurrogateEscapeIsRejected`                   | Fact | Lone high surrogate escape is rejected.                      |
| `LoneLowSurrogateEscapeIsRejected`                    | Fact | Lone low surrogate escape is rejected.                       |
| `HighSurrogateFollowedByNonSurrogateEscapeIsRejected` | Fact | High surrogate followed by non surrogate escape is rejected. |

### FilterSqlTextTests

`Jube.Test.Validations.FilterSqlTextTests` — 9 tests. Category: Unit.

| Test                                                                                      | Kind   | Verifies                                                                                                               |
|-------------------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------------------------------|
| `EmptyFilterIsAcceptable`                                                                 | Theory | Null, empty and white space filters are acceptable.                                                                    |
| `OrdinaryPredicatesAreAcceptable`                                                         | Theory | Ordinary predicates, and forbidden words inside quoted values, are acceptable.                                         |
| `HostilePredicatesAreRefused`                                                             | Theory | Stacked statements, comments, subqueries, denied functions, wrapper escapes, dollar quoting and E strings are refused. |
| `AnIdentifierEndingInEBeforeALiteralIsNotAnEscapeString`                                  | Theory | An identifier ending in e before a literal is not mistaken for an escape string.                                       |
| `ANulOrOtherControlCharacterIsRefused`                                                    | Fact   | A NUL or other control character is refused, tabs and newlines are allowed.                                            |
| `APredicateOverTheMaximumLengthIsRefused`                                                 | Fact   | A predicate over 65,536 characters is refused.                                                                         |
| `AQuotedFunctionNameThatTheTextCheckCannotSeeIsStillRefusedByTheStatementGateAtExecution` | Theory | A quoted denied function passes the text check and is refused by the statement gate.                                   |
| `AFilterThatClosesTheWrapperParenthesisToEscapeTheModelKeyIsRefusedByTheTextCheck`        | Fact   | A filter that closes the wrapper parenthesis is refused.                                                               |
| `AUnionSmuggledThroughTheWrapperIsRefusedByTheTextCheckAndByTheStatementGate`             | Fact   | A union through the wrapper is refused by both layers.                                                                 |

## Waf

### JsonBodyFlattenerTests

`Jube.Test.Waf.JsonBodyFlattenerTests` — 8 tests. Category: Unit.

| Test                                           | Kind | Verifies                                              |
|------------------------------------------------|------|-------------------------------------------------------|
| `StringValuesAreFlattenedWithTheirPath`        | Fact | String values are flattened with their path.          |
| `NestedObjectsAndArraysAreTraversed`           | Fact | Nested objects and arrays are traversed.              |
| `ObjectKeysAreThemselvesInspectableFields`     | Fact | Object keys are themselves inspectable fields.        |
| `NullValuesAreSkipped`                         | Fact | Null values are skipped.                              |
| `MalformedJsonFallsBackToInspectingTheRawBody` | Fact | Malformed json falls back to inspecting the raw body. |
| `EmptyOrWhitespaceInputYieldsNothing`          | Fact | Empty or whitespace input yields nothing.             |
| `TheNumberOfValuesIsCapped`                    | Fact | The number of values is capped.                       |
| `ExcessivelyDeepJsonDoesNotThrowAndIsBounded`  | Fact | Excessively deep json does not throw and is bounded.  |

### WafAttackRepositoryTests

`Jube.Test.Waf.WafAttackRepositoryTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                   | Kind | Verifies                                                  |
|--------------------------------------------------------|------|-----------------------------------------------------------|
| `GetLastReturnsTheInsertedRowsWithinTheDateRangeAsync` | Fact | Get last returns the inserted rows within the date range. |
| `ADateRangeExcludesOlderRowsAsync`                     | Fact | A date range excludes older rows.                         |
| `SearchMatchesTheMatchedValueCaseInsensitivelyAsync`   | Fact | Search matches the matched value case insensitively.      |
| `SearchMatchesTheRouteAsync`                           | Fact | Search matches the route.                                 |
| `CountReflectsTheSearchFilterAsync`                    | Fact | Count reflects the search filter.                         |
| `SortAscendingOrdersByCreatedDateAsync`                | Fact | Sort ascending orders by created date.                    |

### WafInspectorTests

`Jube.Test.Waf.WafInspectorTests` — 9 tests. Category: Unit.

| Test                                                             | Kind   | Verifies                                                                 |
|------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `AnAttackPayloadIsMatchedAndDropped`                             | Theory | An attack payload is matched and dropped.                                |
| `ABenignPayloadIsNotMatched`                                     | Theory | A benign payload is not matched.                                         |
| `AnEmptyRuleSetBlocksNothing`                                    | Fact   | An empty rule set blocks nothing.                                        |
| `ADetectOnlySignatureRecordsButDoesNotBlock`                     | Fact   | A detect only signature records but does not block.                      |
| `AnExceptionForTheRouteSuppressesTheMatch`                       | Fact   | An exception for the route suppresses the match.                         |
| `AnExceptionForTheFieldSuppressesOnlyThatField`                  | Fact   | An exception for the field suppresses only that field.                   |
| `AnExceptionScopedToAnotherSignatureDoesNotSuppress`             | Fact   | An exception scoped to another signature does not suppress.              |
| `AScopeMismatchMeansTheSignatureDoesNotEvaluateThatField`        | Fact   | A scope mismatch means the signature does not evaluate that field.       |
| `ManySlowSignaturesTogetherStayWithinTheOverallInspectionBudget` | Fact   | Many slow signatures together stay within the overall inspection budget. |

### WafMiddlewareTests

`Jube.Test.Waf.WafMiddlewareTests` — 5 tests. Category: Unit.

| Test                                                                   | Kind | Verifies                                                                      |
|------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `AJsonBodyAttackIsBlockedWithForbiddenAndDoesNotReachTheEndpointAsync` | Fact | A json body attack is blocked with forbidden and does not reach the endpoint. |
| `ABenignJsonBodyReachesTheEndpointWithTheBodyStillReadableAsync`       | Fact | A benign json body reaches the endpoint with the body still readable.         |
| `WhenDisabledAnAttackPassesThroughAsync`                               | Fact | When disabled an attack passes through.                                       |
| `AQueryStringAttackIsBlockedAsync`                                     | Fact | A query string attack is blocked.                                             |
| `APathTraversalAttackIsBlockedAsync`                                   | Fact | A path traversal attack is blocked.                                           |

### WafRegistryAndTimeoutTests

`Jube.Test.Waf.WafRegistryAndTimeoutTests` — 4 tests. Category: Unit.

| Test                                              | Kind | Verifies                                                   |
|---------------------------------------------------|------|------------------------------------------------------------|
| `ANewRegistryStartsEmpty`                         | Fact | A new registry starts empty.                               |
| `UpdateSwapsTheActiveRuleSet`                     | Fact | Update swaps the active rule set.                          |
| `UpdateWithNullFallsBackToEmpty`                  | Fact | Update with null falls back to empty.                      |
| `ACatastrophicRegexTimesOutAndDoesNotHangOrThrow` | Fact | A catastrophic regex times out and does not hang or throw. |

### WafRuleSetCompilerTests

`Jube.Test.Waf.WafRuleSetCompilerTests` — 8 tests. Category: Unit.

| Test                                                  | Kind   | Verifies                                                      |
|-------------------------------------------------------|--------|---------------------------------------------------------------|
| `AValidSignatureCompiles`                             | Fact   | A valid signature compiles.                                   |
| `ASignatureWithAnInvalidPatternIsSkipped`             | Fact   | A signature with an invalid pattern is skipped.               |
| `ASignatureWithNoPatternIsSkipped`                    | Fact   | A signature with no pattern is skipped.                       |
| `TargetScopeIsMappedFromTheStoredInteger`             | Theory | Target scope is mapped from the stored integer.               |
| `ARequestedTimeoutAboveTheMaximumIsCappedNotHonoured` | Fact   | A requested timeout above the maximum is capped not honoured. |
| `AnExceptionWithoutARouteRegexIsSkipped`              | Fact   | An exception without a route regex is skipped.                |
| `AnExceptionWithAnInvalidRouteRegexIsSkipped`         | Fact   | An exception with an invalid route regex is skipped.          |
| `AnExceptionWithAnInvalidFieldRegexIsSkipped`         | Fact   | An exception with an invalid field regex is skipped.          |

### LocalReturnUrlTests

`Jube.Test.Code.LocalReturnUrlTests` — 2 tests. Category: Unit.

| Test                         | Kind   | Verifies                        |
|------------------------------|--------|---------------------------------|
| `ALocalPathIsKept`           | Theory | A local path is kept.           |
| `AnythingElseBecomesTheRoot` | Theory | Anything else becomes the root. |

### ClientAssetStoreTests

`Jube.Test.Code.ClientAssetStoreTests` — 8 tests. Category: Unit.

| Test                                                                        | Kind   | Verifies                                                                                                                                                                         |
|-----------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `EveryStaticFileOfABuildIsServedFromTheStoreByThatBuildsKey`                | Fact   | Every static file a replica publishes for its build can be read back by another replica from the shared cache under that build's identifier.                                     |
| `TheIndexPageAndTheBuildIdFileAreNotPublished`                              | Fact   | The index page and the build identifier file are left out of the published set, so neither can be fetched from the store.                                                        |
| `AnUnhashedFileThatChangedBetweenBuildsIsServedAsEachBuildHadIt`            | Fact   | A file whose name carries no content hash and whose content changed between two builds is returned with each build's own content.                                                |
| `AnUnchangedFileIsStoredOnceAcrossBuilds`                                   | Fact   | Publishing a second build adds a stored blob only for the file that changed, so identical content is held once across builds.                                                    |
| `TheTargetIsOnlyAdvertisedOncePublishingFinishesAndTheLastBuildToStartWins` | Fact   | The target build is the caller's fallback until a publish completes, and when two builds publish in turn the one that published last is the advertised target.                   |
| `ABadOrUnknownRequestIsNeverServed`                                         | Theory | A path with traversal or illegal characters, a malformed or empty build identifier, and a build that was never published all return nothing.                                     |
| `AMissingBuildIdPublishesNothingAndDoesNotThrow`                            | Fact   | Publishing from a web root with no build identifier file writes nothing to the cache and raises no error.                                                                        |
| `OnlyContentHashedNamesAreTreatedAsImmutable`                               | Theory | Only file names carrying a content hash are recognised as immutable; plain names such as an image or the favicon are not.                                                        |

### ClientBuildAssetMiddlewareTests

`Jube.Test.Code.ClientBuildAssetMiddlewareTests` — 6 tests. Category: Unit.

| Test                                                     | Kind   | Verifies                                                                                                                                                                                      |
|----------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `ATabOnAnOlderBuildGetsThatBuildsFileFromTheStore`       | Fact   | A request whose build cookie names an older build is answered from the store with that build's file, the right content type and a no-cache header, without reaching the rest of the pipeline. |
| `ATabOnThisReplicasOwnBuildIsLeftToTheNormalStaticFiles` | Fact   | A request whose build cookie matches the replica's own build is passed on to the normal static file handling.                                                                                 |
| `ARequestWithNoBuildCookieIsLeftToTheNormalStaticFiles`  | Fact   | A request carrying no build cookie is passed on to the normal static file handling.                                                                                                           |
| `AFileTheOlderBuildNeverHadFallsThroughToThisReplica`    | Fact   | A request from an older build for a file that build never published is passed on to the replica rather than refused.                                                                          |
| `ApiCallsAndWritesAreNeverServedFromTheStore`            | Theory | API paths and non-read methods are always passed on, even when the build cookie names an older build.                                                                                         |
| `ABadCookieIsIgnored`                                    | Fact   | A build cookie that is not a valid build identifier is ignored and the request is passed on.                                                                                                  |

## Svirfd

Tests of the Svirfd IP intelligence client: the member catalogue, Directory and Remote hosts, drift, geo extensions,
settings, cache, circuit and narrative.

### SvirfdMembersTests

`Jube.Test.Svirfd.SvirfdMembersTests` — 6 tests. Category: Unit.

| Test                                                    | Kind | Verifies                                                                      |
|---------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `EveryScalarOfTheRecordGraphIsCatalogued`               | Fact | Every Scalar Of The Record Graph Is Catalogued.                               |
| `TheTimezoneAndTheOperatorDomainMatchFlagAreCatalogued` | Fact | The timezone members and the operator domain match flag are in the catalogue. |
| `PathsFollowTheWireShape`                               | Fact | Paths Follow The Wire Shape.                                                  |
| `MethodsAndTokensAreRecognised`                         | Fact | Methods And Tokens Are Recognised.                                            |
| `SplitSeparatesEntityFromMember`                        | Fact | Split Separates Entity From Member.                                           |
| `ReadsNumbersAndTextFromARecord`                        | Fact | Reads numbers and text from a record.                                         |

### SvirfdDirectoryHostTests

`Jube.Test.Svirfd.SvirfdDirectoryHostTests` — 3 tests. Category: Unit.

| Test                                          | Kind | Verifies                                            |
|-----------------------------------------------|------|-----------------------------------------------------|
| `LoadsTheFixtureSnapshotAndAnswersFromMemory` | Fact | Loads The Fixture Snapshot And Answers From Memory. |
| `EveryExpectedAddressResolves`                | Fact | Every Expected Address Resolves.                    |
| `DisabledSettingsNeverStartAClient`           | Fact | Disabled Settings Never Start AClient.              |

### SvirfdResilienceTests

`Jube.Test.Svirfd.SvirfdResilienceTests` — 7 tests. Category: Unit.

| Test                                                                              | Kind | Verifies                                                                                                                 |
|-----------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------------------------|
| `TheLookupPipelineUsesTheSharedDefaultsWithTheConfiguredTimeoutAndNoRetry`        | Fact | The Svirfd pipeline takes the shared breaker defaults, the configured lookup timeout and no retries.                     |
| `ATimeoutBelowTheFloorIsRaisedSoThePipelineCanBeBuilt`                            | Fact | A lookup timeout under 50 ms is raised to 50 ms.                                                                         |
| `AServerFailureIsNeverRetriedBecauseTheCallerIsInsideATransactionAsync`           | Fact | A 500 reaches the service once and pauses lookups for about five seconds.                                                |
| `RepeatedNetworkFailuresStopReachingTheServiceAsync`                              | Fact | After the breaker's minimum throughput of failures the service is no longer called.                                      |
| `CallerCancellationIsNotAFailureAndDoesNotOpenAnyCircuitAsync`                    | Fact | Cancelling the caller rethrows and opens no pause.                                                                       |
| `EachHostKeepsItsOwnBreakerSoOneInstallationNeverTripsAnotherAsync`               | Fact | Breaker state is per host instance.                                                                                      |
| `TheLookupRunsOnTheSuppliedExecutorSoProductionSharesTheProcessWideBreakersAsync` | Fact | The lookup builds its pipeline on the executor passed in, which the application supplies as the shared process-wide one. |

### SvirfdTenantSwitchTests

`Jube.Test.Svirfd.SvirfdTenantSwitchTests` — 6 tests. Category: Unit.

| Test                                                                   | Kind | Verifies                                                                                                                                 |
|------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------------------------------------------|
| `ARemoteLookupForATenantWithTheSwitchOffSendsNothingAsync`             | Fact | With the tenant switch off, plain and fresh lookups send nothing.                                                                        |
| `SwitchedOffLookupsNeverTripTheBreakerOrTheCircuitOrFillTheCacheAsync` | Fact | Switched-off lookups never reach the pipeline, so no breaker, pause or cache entry results and an allowed lookup afterwards still works. |
| `ASwitchedOffTenantDoesNotReadTheCacheAnotherTenantFilledAsync`        | Fact | A cached answer from another tenant is not served when the switch is off.                                                                |
| `TheDefaultOverloadsStillAllowTheRemoteLookupAsync`                    | Fact | The overloads without the flag still allow the remote lookup.                                                                            |
| `ADirectoryLookupIsUnaffectedByTheSwitchAsync`                         | Fact | Directory lookups answer regardless of the switch.                                                                                       |
| `TheNullLookupIgnoresTheSwitchAsync`                                   | Fact | The null lookup answers nothing either way.                                                                                              |

### SvirfdPrivacyTests

`Jube.Test.Svirfd.SvirfdPrivacyTests` — 5 tests (33 cases). Category: Unit.

| Test                                                        | Kind   | Verifies                                                                                 |
|-------------------------------------------------------------|--------|------------------------------------------------------------------------------------------|
| `OnlyAnHttpsServiceWithoutCredentialsIsTrustedWithTheKey`   | Theory | SvirfdBaseUrl must be https, no user info; http only for loopback.                       |
| `AnInsecureBaseUrlTurnsLookupsOffAndSendsNothing`           | Fact   | An insecure base URL disables the client and sends no request.                           |
| `NonPublicAddressesAreNotRoutable`                          | Theory | Private, loopback, link-local, CGNAT, multicast, unspecified addresses are never public. |
| `PublicAddressesAreRoutable`                                | Theory | Public IPv4 and IPv6 addresses, including range boundaries, are routable.                |
| `ARemoteLookupSendsOnlyThePublicAddressAndNeverAPrivateOne` | Fact   | One request, the address only in the path, no body; private addresses are not sent.      |

### SvirfdRemoteHostTests

`Jube.Test.Svirfd.SvirfdRemoteHostTests` — 10 tests. Category: Unit.

| Test                                                                    | Kind | Verifies                                                                                                                          |
|-------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------------------------------------------|
| `RemoteLookupsAreCachedAndFreshLookupsBypassTheCache`                   | Fact | Remote Lookups Are Cached And Fresh Lookups Bypass The Cache.                                                                     |
| `RateLimitOpensTheCircuitAndLookupsAnswerNullMeanwhile`                 | Fact | Rate Limit Opens The Circuit And Lookups Answer Null Meanwhile.                                                                   |
| `RetryAfterFromTheServiceSetsTheExactPause`                             | Fact | A Retry-After on a 429 sets the exact circuit pause.                                                                              |
| `EveryRequestNamesJubeInTheUserAgent`                                   | Fact | Every request carries a Svirfd-dotnet user agent ending in -Jube.                                                                 |
| `AutoModeFallsBackToTheServiceWhenTheSnapshotIsRefusedAndKeepsRetrying` | Fact | In Auto mode a refused snapshot falls back to the API, reports FellBackToRemote with a scheduled retry, and lookups still answer. |
| `RefusedKeyOpensTheCircuitUntilRestart`                                 | Fact | Refused Key Opens The Circuit Until Restart.                                                                                      |
| `IndexLoadingPausesBriefly`                                             | Fact | Index Loading Pauses Briefly.                                                                                                     |
| `SlowServiceIsCutOffByTheLookupTimeout`                                 | Fact | Slow Service Is Cut Off By The Lookup Timeout.                                                                                    |
| `NonAddressesNeverReachTheService`                                      | Fact | Non Addresses Never Reach The Service.                                                                                            |
| `TimeoutOpensTheCircuitSoTheNextLookupDoesNotWaitAgain`                 | Fact | Timeout opens the circuit so the next lookup does not wait again.                                                                 |

### SvirfdDriftTests

`Jube.Test.Svirfd.SvirfdDriftTests` — 3 tests. Category: Unit.

| Test                                                       | Kind | Verifies                                                           |
|------------------------------------------------------------|------|--------------------------------------------------------------------|
| `IdenticalRecordsHaveNoDrift`                              | Fact | Identical Records Have No Drift.                                   |
| `ChangedScalarsAreReportedByPathWithVolatileFieldsIgnored` | Fact | Changed Scalars Are Reported By Path With Volatile Fields Ignored. |
| `MissingRecordsCompareAsNoDrift`                           | Fact | Missing Records Compare As No Drift.                               |

### SvirfdGeoExtensionsTests

`Jube.Test.Svirfd.SvirfdGeoExtensionsTests` — 3 tests. Category: Unit.

| Test                                           | Kind | Verifies                                               |
|------------------------------------------------|------|--------------------------------------------------------|
| `DistanceUsesTheLocatedCoordinates`            | Fact | Distance Uses The Located Coordinates.                 |
| `UnlocatedRecordsAnswerNull`                   | Fact | Unlocated Records Answer Null.                         |
| `HubCoordinatesStandInWhenTheCityIsNotClaimed` | Fact | Hub Coordinates Stand In When The City Is Not Claimed. |

### SvirfdRttChartTests

`Jube.Test.Svirfd.SvirfdRttChartTests` — 2 tests. Category: Unit.

| Test                                                              | Kind | Verifies                                                                   |
|-------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `ARecordWithoutProbesHasNoChart`                                  | Fact | A record without probes has no chart.                                      |
| `LocatedProbesAreChartedAgainstTheirDistanceWithThePhysicalBound` | Fact | Located probes are charted against their distance with the physical bound. |

### SvirfdSupportTests

`Jube.Test.Svirfd.SvirfdSupportTests` — 6 tests. Category: Unit.

| Test                                       | Kind | Verifies                                        |
|--------------------------------------------|------|-------------------------------------------------|
| `SettingsReadEveryKeyWithDefaults`         | Fact | Settings Read Every Key With Defaults.          |
| `CacheExpiresAndEvictsWhenFull`            | Fact | Cache Expires And Evicts When Full.             |
| `CircuitBacksOffOnRepeatedRateLimits`      | Fact | Circuit Backs Off On Repeated Rate Limits.      |
| `NarrativeAdapterKeepsGroupOrderAndTitles` | Fact | Narrative Adapter Keeps Group Order And Titles. |
| `JsonRoundTripsTheWireShape`               | Fact | Json Round Trips The Wire Shape.                |
| `NullLookupIsInert`                        | Fact | Null Lookup Is Inert.                           |

### SvirfdServiceTests

`Jube.Test.Svirfd.SvirfdServiceTests` — 6 tests. Category: Service. Collection: Database.

| Test                                                                            | Kind | Verifies                                                                                                                |
|---------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------------------------|
| `LooksUpAnAddressWithTheRecordNarrativeAndCreditAsync`                          | Fact | The lookup returns the record JSON, the narrative groups, the credit and the mode, and the status reports ready.        |
| `RefusesWithoutThePermissionAndOnBadInputAsync`                                 | Fact | The lookup refuses a caller without permission 72, an unauthenticated caller and a value that is not an address.        |
| `ADisabledInstallationAnswersNotFoundAsync`                                     | Fact | A disabled installation answers not found and reports Svirfd off.                                                       |
| `RefusalMessagesComeFromTheLocaliserInTheCallersCultureAsync`                   | Fact | Refusal messages come from `SvirfdResources` in the caller's culture for all nine locales.                              |
| `ATenantWithRemoteLookupsSwitchedOffIsRefusedInRemoteModeAndNothingIsSentAsync` | Fact | With the tenant switch off a Remote mode lookup answers not found, nothing is sent and the status shows the switch off. |
| `ATenantWithRemoteLookupsSwitchedOffStillGetsDirectoryLookupsAsync`             | Fact | With the tenant switch off a Directory mode lookup still answers.                                                       |

### WatcherStreamFrameTests

`Jube.Test.App.Code.Watcher.WatcherStreamFrameTests` — 4 tests. Category: Unit.

| Test                                                    | Kind   | Verifies                                                         |
|---------------------------------------------------------|--------|------------------------------------------------------------------|
| `AJsonPayloadBecomesOneNamedEventFrame`                 | Fact   | A JSON payload becomes one named event frame.                    |
| `LineBreaksInThePayloadCannotInjectAnotherEventOrField` | Fact   | Line breaks in the payload cannot inject another event or field. |
| `AnEmptyPayloadIsDropped`                               | Theory | An empty payload is dropped.                                     |
| `AnOversizePayloadIsDroppedAndOneAtTheLimitIsKept`      | Fact   | An oversize payload is dropped and one at the limit is kept.     |

### WatcherStreamRegistryTests

`Jube.Test.App.Code.Watcher.WatcherStreamRegistryTests` — 4 tests. Category: Unit.

| Test                                                                            | Kind | Verifies                                                                                       |
|---------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `DispatchReachesOnlyTheConnectionsOfThatTenant`                                 | Fact | Dispatch reaches only the connections of that tenant.                                          |
| `DispatchToATenantWithNoConnectionsIsANoOpAndARemovedConnectionReceivesNothing` | Fact | Dispatch to a tenant with no connections is a no op and a removed connection receives nothing. |
| `ASlowClientLosesTheOldestMessagesAndNeverBlocksTheDispatcher`                  | Fact | A slow client loses the oldest messages and never blocks the dispatcher.                       |
| `AbortingAUserCancelsOnlyThatUsersStreamsAndMarksThemRevoked`                   | Fact | Aborting a user cancels only that user's streams and marks them revoked.                       |
