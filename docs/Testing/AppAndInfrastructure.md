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

| Test                                                                 | Kind | Verifies                                                                        |
|----------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `AnUnspecifiedDateTimeSerialisesWithTheUtcSuffix`                    | Fact | An unspecified date time serialises with the utc suffix.                        |
| `AUtcDateTimeSerialisesUnchangedWithTheUtcSuffix`                    | Fact | A utc date time serialises unchanged with the utc suffix.                       |
| `ALocalDateTimeSerialisesAsItsUtcInstant`                            | Fact | A local date time serialises as its utc instant.                                |
| `ANullableDateTimeSerialisesWithTheUtcSuffixAndNullStaysNull`        | Fact | A nullable date time serialises with the utc suffix and null stays null.        |
| `ADateTimeBoxedAsObjectInsideADictionarySerialisesWithTheUtcSuffix`  | Fact | A date time boxed as object inside a dictionary serialises with the utc suffix. |
| `AnOffsetlessStringDeserialisesAsThatWallClockTimeInUtc`             | Fact | An offsetless string deserialises as that wall clock time in utc.               |
| `AnOffsetStringDeserialisesAsTheSameInstantInUtc`                    | Fact | An offset string deserialises as the same instant in utc.                       |
| `AZuluStringDeserialisesUnchangedInUtc`                              | Fact | A zulu string deserialises unchanged in utc.                                    |

## Case

### SendHttpEndpointTests

`Jube.Test.Case.SendHttpEndpointTests` — 2 tests. Category: Unit.

| Test                                           | Kind | Verifies                                      |
|------------------------------------------------|------|-----------------------------------------------|
| `PostAsync_OnFailure_NeverLogsThePayloadAsync` | Fact | Post async on failure never logs the payload. |
| `GetAsync_OnFailure_NeverLogsTheFullUrlAsync`  | Fact | Get async on failure never logs the full url. |

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

| Test                                                  | Kind | Verifies                                                     |
|-------------------------------------------------------|------|--------------------------------------------------------------|
| `InvalidEncryptionExceptionExposesTheGivenMessage`    | Fact | Invalid encryption exception exposes the given message.      |
| `InvalidDecryptionExceptionExposesTheGivenMessage`    | Fact | Invalid decryption exception exposes the given message.      |
| `InvalidHmacExceptionCanBeConstructedWithoutAMessage` | Fact | Invalid hmac exception can be constructed without a message. |

### JempAesEncryptionTests

`Jube.Test.Cryptography.JempAesEncryptionTests` — 15 tests. Category: Unit.

| Test                                                                                  | Kind | Verifies                                                                                         |
|---------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `EncryptThenDecryptRoundTripsTheOriginalData`                                         | Fact | Encrypt then decrypt round trips the original data.                                              |
| `EncryptRoundTripsAnEmptyPayload`                                                     | Fact | Encrypt round trips an empty payload.                                                            |
| `EncryptProducesDifferentCipherTextOnEachCallDueToTheRandomIv`                        | Fact | Encrypt produces different cipher text on each call due to the random iv.                        |
| `EncryptPrependsA32ByteHmacAndA16ByteIvBeforeThePkcs7PaddedCipherText`                | Fact | Encrypt prepends a 32 byte hmac and a 16 byte iv before the pkcs7 padded cipher text.            |
| `EncryptOfNullDataThrowsInvalidEncryptionException`                                   | Fact | Encrypt of null data throws invalid encryption exception.                                        |
| `DecryptWithAWrongPasswordThrowsInvalidDecryptionException`                           | Fact | Decrypt with a wrong password throws invalid decryption exception.                               |
| `DecryptWithAWrongSaltThrowsInvalidDecryptionException`                               | Fact | Decrypt with a wrong salt throws invalid decryption exception.                                   |
| `DecryptOfTamperedCipherTextThrowsInvalidDecryptionException`                         | Fact | Decrypt of tampered cipher text throws invalid decryption exception.                             |
| `DecryptOfTamperedHmacThrowsInvalidDecryptionException`                               | Fact | Decrypt of tampered hmac throws invalid decryption exception.                                    |
| `DecryptOfTooShortDataThrowsInvalidDecryptionException`                               | Fact | Decrypt of too short data throws invalid decryption exception.                                   |
| `DecryptOfNullDataThrowsInvalidDecryptionException`                                   | Fact | Decrypt of null data throws invalid decryption exception.                                        |
| `DecryptWithoutLegacyFallbackDoesNotRecoverLegacyFormattedData`                       | Fact | Decrypt without legacy fallback does not recover legacy formatted data.                          |
| `DecryptWithLegacyFallbackEnabledRecoversLegacyFormattedData`                         | Fact | Decrypt with legacy fallback enabled recovers legacy formatted data.                             |
| `DecryptWithLegacyFallbackEnabledStillRecoversCurrentFormatData`                      | Fact | Decrypt with legacy fallback enabled still recovers current format data.                         |
| `DecryptWithLegacyFallbackEnabledAndInvalidDataStillThrowsInvalidDecryptionException` | Fact | Decrypt with legacy fallback enabled and invalid data still throws invalid decryption exception. |

## Documentation

### CiCategoryCoverageTests

`Jube.Test.Documentation.CiCategoryCoverageTests` — 1 tests. Category: Unit.

| Test                                                         | Kind | Verifies                                                          |
|--------------------------------------------------------------|------|--------------------------------------------------------------------|
| `EveryTestCategoryIsNamedByAWorkflowFilterSoEveryTestGates`  | Fact | Every test category is named by a workflow filter so every test gates. |

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

`Jube.Test.HttpAdaptationProtocol.Parsing.AdaptationResponseParserTests` — 16 tests. Category: Unit.

| Test                                                                               | Kind   | Verifies                                                                                           |
|------------------------------------------------------------------------------------|--------|----------------------------------------------------------------------------------------------------|
| `NullBodyReturnsAnEmptyResponseError`                                              | Fact   | Null body returns an empty response error.                                                         |
| `EmptyBodyReturnsAnEmptyResponseError`                                             | Fact   | Empty body returns an empty response error.                                                        |
| `WhitespaceOnlyBodyReturnsAnEmptyResponseError`                                    | Fact   | Whitespace only body returns an empty response error.                                              |
| `BareNumericBodyIsParsedDirectlyAsTheValue`                                        | Theory | Bare numeric body is parsed directly as the value.                                                 |
| `BracketedBareNumericBodyIsUnwrappedAndParsedAsTheValue`                           | Theory | Bracketed bare numeric body is unwrapped and parsed as the value.                                  |
| `CommaDecimalSeparatorIsNotTreatedAsANumberAndFallsThroughToAnUnparsableError`     | Fact   | Comma decimal separator is not treated as a number and falls through to an unparsable error.       |
| `MalformedJsonReturnsAnUnparsableErrorAndLogsAWarning`                             | Fact   | Malformed json returns an unparsable error and logs a warning.                                     |
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
