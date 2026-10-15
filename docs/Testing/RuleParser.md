---
layout: default
title: Rule Parsing and Compilation
nav_order: 11
parent: Testing
---

# Rule Parsing and Compilation

Tests of the rule parser and compiler that turn the rule DSL into executable code, including hardening against hostile
rule text.

## General

### AbstractionCalculationParityTests

`Jube.Test.RuleParser.AbstractionCalculationParityTests` — 10 tests. Category: Unit. Collection: EvalExpressionRegistry.

| Test                                                              | Kind   | Verifies                                                                     |
|-------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `TheCalculationWrapperMatchesTheDelegateTheEngineInvokes`         | Fact   | The calculation wrapper matches the delegate the engine invokes.             |
| `EveryUpstreamSourceCanBeReadByACoderRule`                        | Theory | Every upstream source can be read by a coder rule.                           |
| `AnInlineScriptPropertyAndAnInlineFunctionFeedTheSameExpression`  | Fact   | An inline script property and an inline function feed the same expression.   |
| `EveryUpstreamSourceMixesInOneRule`                               | Fact   | Every upstream source mixes in one rule.                                     |
| `AFluentCalculationOverCountersAndPayloadCompilesAndRuns`         | Fact   | A fluent calculation over counters and payload compiles and runs.            |
| `AFluentCalculationMixesAPayloadFieldAnInlineFunctionAndACounter` | Fact   | A fluent calculation mixes a payload field an inline function and a counter. |
| `AFluentCalculationOverAnEarlierCalculationAndASanctionRuns`      | Fact   | A fluent calculation over an earlier calculation and a sanction runs.        |
| `AnUndefinedFluentCalculationIsNaNAndNotAnException`              | Fact   | An undefined fluent calculation is na n and not an exception.                |
| `APayloadReferenceToAnUnknownFieldIsRefusedAtParseTime`           | Fact   | A payload reference to an unknown field is refused at parse time.            |
| `ATtlCounterReferenceToAnUnknownCounterIsRefusedAtParseTime`      | Fact   | A ttl counter reference to an unknown counter is refused at parse time.      |

### ApprovalPreflightTests

`Jube.Test.RuleParser.ApprovalPreflightTests` — 9 tests. Category: Unit.

| Test                                                                          | Kind | Verifies                                                                                    |
|-------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `ApprovingAnEntityWhoseTargetsAreAlreadyApprovedIsAllowed`                    | Fact | Approving an entity whose targets are already approved is allowed.                          |
| `ApprovingARuleThatUsesAnUnapprovedListRefusesAndNamesTheListAsAPrerequisite` | Fact | Approving a rule that uses an unapproved list refuses and names the list as a prerequisite. |
| `PrerequisitesAreListedDependenciesFirstSoTheyCanBeApprovedInThatOrder`       | Fact | Prerequisites are listed dependencies first so they can be approved in that order.          |
| `ApprovingAnEntityThatNamesSomethingThatDoesNotExistIsRefused`                | Fact | Approving an entity that names something that does not exist is refused.                    |
| `ApprovingTheDeletionOfAnEntityAnActiveRuleUsesIsRefused`                     | Fact | Approving the deletion of an entity an active rule uses is refused.                         |
| `ApprovingTheDeletionOfAnEntityNothingUsesIsAllowed`                          | Fact | Approving the deletion of an entity nothing uses is allowed.                                |
| `ADeletionUsedOnlyByAnInactiveRuleIsNotRefused`                               | Fact | A deletion used only by an inactive rule is not refused.                                    |
| `AnActiveDependentOfAnUnapprovedTargetIsReportedAsDependencyOnUnapproved`     | Fact | An active dependent of an unapproved target is reported as dependency on unapproved.        |
| `AnInactiveDependentOfAnUnapprovedTargetIsNotReported`                        | Fact | An inactive dependent of an unapproved target is not reported.                              |

### BuilderFilterTests

`Jube.Test.RuleParser.BuilderFilterTests` — 17 tests. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                                       |
|----------------------------------------------------------------------|--------|--------------------------------------------------------------------------------|
| `TheCatalogueIsEveryReadableScalarPropertyWithItsTypeAndDescription` | Fact   | The catalogue is every readable scalar property with its type and description. |
| `StringOperatorsAreOrdinalAndNeverMatchANullValue`                   | Theory | String operators are ordinal and never match a null value.                     |
| `IntegerOperatorsCompareNumerically`                                 | Theory | Integer operators compare numerically.                                         |
| `ANullableNumberWithNoValueNeverMatches`                             | Fact   | A nullable number with no value never matches.                                 |
| `BooleansCompareWithTrueOrFalseAndNullMatchesNeither`                | Fact   | Booleans compare with true or false and null matches neither.                  |
| `ACharIsFilteredAsText`                                              | Fact   | A char is filtered as text.                                                    |
| `DatesCompareAsInstantsInUtc`                                        | Fact   | Dates compare as instants in utc.                                              |
| `GuidsCompareByValue`                                                | Fact   | Guids compare by value.                                                        |
| `GroupsCombineWithOrAndNot`                                          | Fact   | Groups combine with or and not.                                                |
| `TheFilterFormatRefusesWhatDoesNotFitTheField`                       | Fact   | The filter format refuses what does not fit the field.                         |
| `DtoFilterPagesByIdAndSaysWhenThereIsMore`                           | Fact   | Dto filter pages by id and says when there is more.                            |
| `DtoFilterWithNoJsonSelectsEverythingAndClampsTake`                  | Fact   | Dto filter with no json selects everything and clamps take.                    |
| `DtoFilterReturnsErrorsWithPathsInsteadOfThrowing`                   | Fact   | Dto filter returns errors with paths instead of throwing.                      |
| `DtoCountGroupsByAFieldLargestFirstWithNullAsItsOwnGroup`            | Fact   | Dto count groups by a field largest first with null as its own group.          |
| `DtoCountFiltersBeforeCountingAndRefusesAnUnknownGroupBy`            | Fact   | Dto count filters before counting and refuses an unknown group by.             |
| `DtoFilterFieldsListTheOperatorsForEachType`                         | Fact   | Dto filter fields list the operators for each type.                            |
| `TheCatalogueReadsEachFieldsValueFromTheRowAndLeavesOutLists`        | Fact   | The catalogue reads each fields value from the row and leaves out lists.       |

### BuilderOperatorCatalogueTests

`Jube.Test.RuleParser.BuilderOperatorCatalogueTests` — 5 tests. Category: Unit.

| Test                                                                 | Kind   | Verifies                                                                        |
|----------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `EveryPipelineMatchStepOnAPlainValueIsAnOperator`                    | Fact   | Every pipeline match step on a plain value is an operator.                      |
| `KeysAreUniqueAndGroupsAreInOrderWithLabelsAlphabeticalOutsideBasic` | Fact   | Keys are unique and groups are in order with labels alphabetical outside basic. |
| `TheOperatorsSavedRulesAlreadyUseKeepTheirRuleText`                  | Theory | The operators saved rules already use keep their rule text.                     |
| `EveryOperatorCompilesInTheEngineWrapperForEveryTypeItAppliesTo`     | Theory | Every operator compiles in the engine wrapper for every type it applies to.     |
| `EveryOperatorBehavesInTheEngineExactlyAsTheLibraryDoesAsync`        | Theory | Every operator behaves in the engine exactly as the library does.               |

### BuilderRuleTranslatorTests

`Jube.Test.RuleParser.BuilderRuleTranslatorTests` — 10 tests. Category: Unit.

| Test                                                                    | Kind   | Verifies                                                                             |
|-------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------|
| `TheTranslationIsExactlyWhatTheBrowserBuilderProduces`                  | Theory | The translation is exactly what the browser builder produces.                        |
| `TheBrowsersOwnJsonShapeIsAcceptedIncludingFieldTypeInputAndValidKeys`  | Fact   | The browsers own json shape is accepted including field type input and valid keys.   |
| `AnythingOutsideTheProfileIsRefusedWithACodeAndPath`                    | Theory | Anything outside the profile is refused with a code and path.                        |
| `NestingDeeperThanTheLimitIsRefused`                                    | Fact   | Nesting deeper than the limit is refused.                                            |
| `TranslatedSeedJsonBehavesLikeTheStoredSeedScriptAsync`                 | Theory | Translated seed json behaves like the stored seed script.                            |
| `EveryTranslationParsesAndCompilesAgainstTheModel`                      | Fact   | Every translation parses and compiles against the model.                             |
| `BeginsWithAndEndsWithCompileOnADefaultInstallation`                    | Fact   | Begins with and ends with compile on a default installation.                         |
| `TheBrowsersJsonForEveryKindOfArgumentTranslatesToTheBrowsersRuleText`  | Fact   | The browsers json for every kind of argument translates to the browsers rule text.   |
| `AFieldReferenceIsOnlyAcceptedWhereTheArgumentAllowsItAndNeverInMemory` | Fact   | A field reference is only accepted where the argument allows it and never in memory. |
| `AQuoteInATextValueIsDoubledAndAControlCharacterIsRefused`              | Fact   | A quote in a text value is doubled and a control character is refused.               |

### EngineRuleWrapperTests

`Jube.Test.RuleParser.EngineRuleWrapperTests` — 6 tests. Category: Unit.

| Test                                                 | Kind | Verifies                                                      |
|------------------------------------------------------|------|---------------------------------------------------------------|
| `TheGatewayWrapperIsTheEnginesTextExactly`           | Fact | The gateway wrapper is the engines text exactly.              |
| `TheAbstractionWrapperIsTheEnginesTextExactly`       | Fact | The abstraction wrapper is the engines text exactly.          |
| `TheActivationWrapperIsTheEnginesTextExactly`        | Fact | The activation wrapper is the engines text exactly.           |
| `TheInlineFunctionWrapperIsTheEnginesTextExactly`    | Fact | The inline function wrapper is the engines text exactly.      |
| `TheReprocessingWrapperIsTheEnginesTextExactly`      | Fact | The reprocessing wrapper is the engines text exactly.         |
| `WithoutTryCatchTheBodyIsBareSoRuntimeErrorsSurface` | Fact | Without try catch the body is bare so runtime errors surface. |

### FlowEntryCompilationTests

`Jube.Test.RuleParser.FlowEntryCompilationTests` — 41 tests. Category: Unit. Collection: EvalExpressionRegistry.

| Test                                                                        | Kind   | Verifies                                                                                   |
|-----------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `ADoubleFieldStartsAPipelineWithNoStart`                                    | Theory | A double field starts a pipeline with no start.                                            |
| `AnIntegerFieldStartsAPipelineWithNoStart`                                  | Theory | An integer field starts a pipeline with no start.                                          |
| `AStringFieldStartsAPipelineWithNoStart`                                    | Theory | A string field starts a pipeline with no start.                                            |
| `ADateTimeFieldStartsAPipelineWithNoStart`                                  | Theory | A date time field starts a pipeline with no start.                                         |
| `ABooleanFieldStartsAPipelineWithNoStart`                                   | Theory | A boolean field starts a pipeline with no start.                                           |
| `APlainTransformerCanBeTheFirstStepAndTheNextStepContinuesTheChain`         | Theory | A plain transformer can be the first step and the next step continues the chain.           |
| `ATextLengthStepCanFeedAnIntegerStep`                                       | Theory | A text length step can feed an integer step.                                               |
| `TheBarePropertyNameLengthIsRefusedAtParseTimeSoItCannotBypassTheAllowList` | Fact   | The bare property name length is refused at parse time so it cannot bypass the allow list. |
| `TheBareMethodNameTrimIsRefusedAtParseTimeSoItCannotBypassTheAllowList`     | Fact   | The bare method name trim is refused at parse time so it cannot bypass the allow list.     |
| `AFlowTransformerCanBeTheFirstStep`                                         | Theory | A flow transformer can be the first step.                                                  |
| `ARoundWithDigitsOnARawDoubleBindsToTheStaticDoubleRoundInVbSoStartFirst`   | Fact   | A round with digits on a raw double binds to the static double round in vb so start first. |
| `ADateTransformerThatAlsoExistsAsAPlainMethodStillChains`                   | Fact   | A date transformer that also exists as a plain method still chains.                        |
| `AZoneConversionCanBeTheFirstStepAndFeedsAnHourStep`                        | Fact   | A zone conversion can be the first step and feeds an hour step.                            |
| `ParsedDoubleErrorsRatherThanThrowingOnBadText`                             | Fact   | Parsed double errors rather than throwing on bad text.                                     |
| `AgainstSwitchesFieldAfterAnImplicitStart`                                  | Theory | Against switches field after an implicit start.                                            |
| `AnExplicitStartStillWorksAndIsHarmlessToRepeat`                            | Theory | An explicit start still works and is harmless to repeat.                                   |
| `AListTokenCanBeAnArgumentOfAnImplicitStart`                                | Theory | A list token can be an argument of an implicit start.                                      |
| `ATtlCounterTokenCanBeTheReceiver`                                          | Fact   | A ttl counter token can be the receiver.                                                   |
| `AnAbstractionTokenCanBeTheReceiver`                                        | Fact   | An abstraction token can be the receiver.                                                  |
| `AnAbstractionCalculationTokenCanBeTheReceiver`                             | Fact   | An abstraction calculation token can be the receiver.                                      |
| `ADictionaryTokenCanBeTheReceiver`                                          | Fact   | A dictionary token can be the receiver.                                                    |
| `ACounterGuardThenAPayloadStructuringTestReadsAsOneChain`                   | Fact   | A counter guard then a payload structuring test reads as one chain.                        |
| `ADeclineRatioIsARatioOfTwoCountersComparedInTheSameChain`                  | Fact   | A decline ratio is a ratio of two counters compared in the same chain.                     |
| `AShareOfSumIsTheDeclineRatioOverDeclinedPlusApproved`                      | Fact   | A share of sum is the decline ratio over declined plus approved.                           |
| `ACalculationCanBeChainedIntoAnother`                                       | Fact   | A calculation can be chained into another.                                                 |
| `APredicateOnACalculationTakesTheBoundsAfterTheOtherValues`                 | Fact   | A predicate on a calculation takes the bounds after the other values.                      |
| `AParamsCalculationTakesTheBoundFirst`                                      | Fact   | A params calculation takes the bound first.                                                |
| `ADivisionByAZeroFieldIsUndefinedAndFailsClosed`                            | Fact   | A division by a zero field is undefined and fails closed.                                  |
| `ACalculationComposesWithGuardsAndAnotherFieldThroughAgainst`               | Fact   | A calculation composes with guards and another field through against.                      |
| `OrBetweenTwoPipelinesOfTheSameTypeCompiles`                                | Theory | Or between two pipelines of the same type compiles.                                        |
| `AndBetweenTwoPipelinesOfTheSameTypeCompiles`                               | Theory | And between two pipelines of the same type compiles.                                       |
| `OrAndAndBetweenPipelinesOfDifferentTypesCompile`                           | Theory | Or and and between pipelines of different types compile.                                   |
| `APipelineCombinesWithAPlainBooleanExpression`                              | Theory | A pipeline combines with a plain boolean expression.                                       |
| `APlainBooleanOnTheLeftOfAPipelineDoesNotCompileSoThePipelineGoesFirst`     | Fact   | A plain boolean on the left of a pipeline does not compile so the pipeline goes first.     |
| `NotAndGroupingWorkOnPipelines`                                             | Theory | Not and grouping work on pipelines.                                                        |
| `AnUndecidedPipelineIsFalseInACombination`                                  | Theory | An undecided pipeline is false in a combination.                                           |
| `APipelineCanBeUsedDirectlyInAnIfWithOr`                                    | Theory | A pipeline can be used directly in an if with or.                                          |
| `TheIsMatchedFormStillWorks`                                                | Fact   | The is matched form still works.                                                           |
| `ANaNInputFailsClosed`                                                      | Theory | A na n input fails closed.                                                                 |
| `ANullStringFailsClosedRatherThanThrowing`                                  | Theory | A null string fails closed rather than throwing.                                           |
| `ATerminalAloneOnAFieldIsNotAnEntryPoint`                                   | Theory | A terminal alone on a field is not an entry point.                                         |

### FlowRuleCompilationTests

`Jube.Test.RuleParser.FlowRuleCompilationTests` — 18 tests. Category: Unit. Collection: EvalExpressionRegistry.

| Test                                                              | Kind   | Verifies                                                                   |
|-------------------------------------------------------------------|--------|----------------------------------------------------------------------------|
| `AFlowAssignedDirectlyToMatchedCompilesAndRuns`                   | Theory | A flow assigned directly to matched compiles and runs.                     |
| `AGuardChainWithAParamsArrayAndAnAgainstSwitchOfValueRuns`        | Theory | A guard chain with a params array and an against switch of value runs.     |
| `AStepThatTakesAListTokenRuns`                                    | Theory | A step that takes a list token runs.                                       |
| `AnExistingBooleanExtensionCanGuardAFlow`                         | Theory | An existing boolean extension can guard a flow.                            |
| `AScoreCanBeAccumulatedAndThresholded`                            | Theory | A score can be accumulated and thresholded.                                |
| `AFlowCanBeUsedDirectlyAsAnIfCondition`                           | Theory | A flow can be used directly as an if condition.                            |
| `AGuardClauseIfSelectsBetweenFlows`                               | Theory | A guard clause if selects between flows.                                   |
| `AParsedStringFlowFailsClosedOnBadInput`                          | Theory | A parsed string flow fails closed on bad input.                            |
| `TheUserFacingFuzzyListExtensionCompilesAndRuns`                  | Theory | The user facing fuzzy list extension compiles and runs.                    |
| `AnOptionsStringWithCommasAndEqualsSurvivesTranslationAndParsing` | Theory | An options string with commas and equals survives translation and parsing. |
| `AFuzzyScoreCanBeComparedInsideARule`                             | Theory | A fuzzy score can be compared inside a rule.                               |
| `AFuzzyPipelineStepWithOptionsCompilesAndRuns`                    | Theory | A fuzzy pipeline step with options compiles and runs.                      |
| `AFuzzyEntryFoundInsideFreeTextCompilesAndRuns`                   | Theory | A fuzzy entry found inside free text compiles and runs.                    |
| `AnInvalidOptionsStringCompilesAndFailsClosedAtRuntime`           | Fact   | An invalid options string compiles and fails closed at runtime.            |
| `ALambdaIsStillRefusedByTheTokenAllowList`                        | Fact   | A lambda is still refused by the token allow list.                         |
| `EveryFlowMethodIsAcceptedAsARuleToken`                           | Fact   | Every flow method is accepted as a rule token.                             |
| `NoFlowMethodAcceptsADelegateSoRulesCannotSmuggleInCode`          | Fact   | No flow method accepts a delegate so rules cannot smuggle in code.         |
| `TheFlowStructAndItsHelpersAreNotRuleTokens`                      | Fact   | The flow struct and its helpers are not rule tokens.                       |

### ModelDependencyGraphTests

`Jube.Test.RuleParser.ModelDependencyGraphTests` — 11 tests. Category: Unit.

| Test                                                                  | Kind   | Verifies                                                                        |
|-----------------------------------------------------------------------|--------|---------------------------------------------------------------------------------|
| `APayloadNameResolvesToTheRequestXPathBeforeAnInlineScriptProperty`   | Fact   | A payload name resolves to the request x path before an inline script property. |
| `APayloadNameThatIsOnlyAnInlineFunctionResolvesToTheFunction`         | Fact   | A payload name that is only an inline function resolves to the function.        |
| `AReferenceToAMissingNameIsDanglingWithItsLine`                       | Fact   | A reference to a missing name is dangling with its line.                        |
| `OnlyActiveDependentsOfInactiveTargetsAreReported`                    | Fact   | Only active dependents of inactive targets are reported.                        |
| `DependentsAreFoundByNameOrById`                                      | Fact   | Dependents are found by name or by id.                                          |
| `EntitiesNothingReferencesAreUnreferenced`                            | Fact   | Entities nothing references are unreferenced.                                   |
| `NamesMatchCaseSensitivelyLikeTheParser`                              | Fact   | Names match case sensitively like the parser.                                   |
| `NamespacesMatchTheCompletionGroups`                                  | Theory | Namespaces match the completion groups.                                         |
| `TheParserRecordsEveryNamespacedReferenceItSeesIncludingUnknownNames` | Fact   | The parser records every namespaced reference it sees including unknown names.  |
| `TokensOutsideTheKnownNamespacesAreNotReferences`                     | Fact   | Tokens outside the known namespaces are not references.                         |
| `TheBuilderJoinsRuleTextAndStructuredReferencesIntoOneGraph`          | Fact   | The builder joins rule text and structured references into one graph.           |

### ParserDynamicEvalTests

`Jube.Test.RuleParser.ParserDynamicEvalTests` — 14 tests. Category: Unit. Collection: EvalExpressionRegistry.

| Test                                                                                 | Kind   | Verifies                                                                                            |
|--------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------|
| `ARegisteredNameIsAcceptedAndRewrittenToTheGenericEvalCallWhenEnabled`               | Fact   | A registered name is accepted and rewritten to the generic eval call when enabled.                  |
| `ARegisteredNameIsRejectedWhenDisabled`                                              | Fact   | A registered name is rejected when disabled.                                                        |
| `CallingEvalDirectlyIsAlwaysRejectedRegardlessOfTheSwitch`                           | Fact   | Calling eval directly is always rejected regardless of the switch.                                  |
| `CallingEvalDirectlyIsRejectedEvenWithNoNamesRegisteredAtAll`                        | Fact   | Calling eval directly is rejected even with no names registered at all.                             |
| `MultipleRegisteredNamesAreEachIndependentlyAcceptedAndRewritten`                    | Fact   | Multiple registered names are each independently accepted and rewritten.                            |
| `ARegisteredNameWithExplicitEmptyParenthesesIsAlsoRewritten`                         | Fact   | A registered name with explicit empty parentheses is also rewritten.                                |
| `ARegisteredNameThatIsAPrefixOfAnotherIdentifierIsNotIncorrectlyRewritten`           | Fact   | A registered name that is a prefix of another identifier is not incorrectly rewritten.              |
| `AnUnregisteredBareIdentifierStillFailsSoftParseNormally`                            | Fact   | An unregistered bare identifier still fails soft parse normally.                                    |
| `ARuleCombiningARealExtensionMethodAndACuratedNameBothCompile`                       | Fact   | A rule combining a real extension method and a curated name both compile.                           |
| `ANameRegisteredWhileEnabledIsNotSeededAsATokenIntoAParserConstructedAfterDisabling` | Fact   | A name registered while enabled is not seeded as a token into a parser constructed after disabling. |
| `ANameRegisteredAfterTheParserWasConstructedIsNotAvailableUntilANewParserIsBuilt`    | Fact   | A name registered after the parser was constructed is not available until a new parser is built.    |
| `ANameRegisteredBeforeConstructionIsAvailableOnANewlyBuiltParser`                    | Fact   | A name registered before construction is available on a newly built parser.                         |
| `RewriteProducesTheCorrectVbGenericSyntaxForEveryResultTypeId`                       | Theory | Rewrite produces the correct vb generic syntax for every result type id.                            |
| `ARuleWithNoCuratedNamesAtAllStillParsesNormallyWhenEnabled`                         | Fact   | A rule with no curated names at all still parses normally when enabled.                             |

### RuleDocumentationFragmentTests

`Jube.Test.RuleParser.RuleDocumentationFragmentTests` — 1 test. Category: Unit. Collection: EvalExpressionRegistry.

| Test                                                  | Kind   | Verifies                                                    |
|-------------------------------------------------------|--------|-------------------------------------------------------------|
| `EveryDocumentedRuleFragmentCompilesThroughTheParser` | Theory | Every documented rule fragment compiles through the parser. |

### RuleParseNestingDepthTests

`Jube.Test.RuleParser.RuleParseNestingDepthTests` — 7 tests. Category: Unit.

| Test                                                              | Kind   | Verifies                                                                    |
|-------------------------------------------------------------------|--------|-----------------------------------------------------------------------------|
| `ADeeplyNestedRuleIsRefusedWithoutReachingTheCompiler`            | Theory | A deeply nested rule is refused without reaching the compiler.              |
| `AReasonablyNestedLegitimateRuleStillCompiles`                    | Theory | A reasonably nested legitimate rule still compiles.                         |
| `ManyConsecutiveDashesInsideAStringLiteralAreNotCountedAsNesting` | Fact   | Many consecutive dashes inside a string literal are not counted as nesting. |
| `ManyNotWordsInsideAStringLiteralAreNotCountedAsNesting`          | Fact   | Many not words inside a string literal are not counted as nesting.          |
| `ALongFlatArithmeticChainIsNotTreatedAsNesting`                   | Fact   | A long flat arithmetic chain is not treated as nesting.                     |
| `TheStructuredFuzzCorpusNeverReachesTheCompilerOrHangs`           | Theory | The structured fuzz corpus never reaches the compiler or hangs.             |
| `NotAsPartOfAnIdentifierIsNotCountedAsAUnaryOperator`             | Fact   | Not as part of an identifier is not counted as a unary operator.            |

### RuleParseSecurityCorpusTests

`Jube.Test.RuleParser.RuleParseSecurityCorpusTests` — 2 tests. Category: Unit.

| Test                                            | Kind   | Verifies                                              |
|-------------------------------------------------|--------|-------------------------------------------------------|
| `AHostileRuleNeverCompilesRegardlessOfSpelling` | Theory | A hostile rule never compiles regardless of spelling. |
| `AHostileRuleIsAlsoRefusedAsAnActivationRule`   | Theory | A hostile rule is also refused as an activation rule. |

### RuleParseTests

`Jube.Test.RuleParser.RuleParseTests` — 13 tests. Category: Unit.

| Test                                                          | Kind   | Verifies                                                                |
|---------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `ValidGatewayRuleCompilesAndCarriesTheClassName`              | Fact   | Valid gateway rule compiles and carries the class name.                 |
| `MatchedIsRegisteredByTheParserRatherThanSuppliedByTheCaller` | Fact   | Matched is registered by the parser rather than supplied by the caller. |
| `AnUnregisteredNameIsStillRefused`                            | Fact   | An unregistered name is still refused.                                  |
| `UnknownFieldReturnsLocatedErrorSpans`                        | Fact   | Unknown field returns located error spans.                              |
| `CompilerErrorIsReportedAgainstTheUserLineNotTheWrapper`      | Fact   | Compiler error is reported against the user line not the wrapper.       |
| `RuleTextOverTheMaximumLengthIsAnErrorWithoutParsing`         | Fact   | Rule text over the maximum length is an error without parsing.          |
| `WithoutReferencesTheRuleParsesButIsNotMarkedCompiled`        | Fact   | Without references the rule parses but is not marked compiled.          |
| `AbstractionRulesOnlySeeCachedPayloadFields`                  | Fact   | Abstraction rules only see cached payload fields.                       |
| `TtlCountersAreNotVisibleBeforeTheirStage`                    | Fact   | Ttl counters are not visible before their stage.                        |
| `TheEnvironmentTokenListIsNotMutatedByTheParser`              | Fact   | The environment token list is not mutated by the parser.                |
| `ClassNameFollowsTheParserWrapper`                            | Theory | Class name follows the parser wrapper.                                  |
| `CompilingWithoutLoadingKeepsTheBinaryAndLoadsNoAssembly`     | Fact   | Compiling without loading keeps the binary and loads no assembly.       |
| `CompilingWithAndWithoutLoadingReportsTheSameErrors`          | Fact   | Compiling with and without loading reports the same errors.             |

### RuleReturnContractTests

`Jube.Test.RuleParser.RuleReturnContractTests` — 8 tests. Category: Unit.

| Test                                                                | Kind   | Verifies                                                                     |
|---------------------------------------------------------------------|--------|------------------------------------------------------------------------------|
| `BareExtensionEvaluationWithoutAssignmentIsRefusedForEveryRuleType` | Theory | Bare extension evaluation without assignment is refused for every rule type. |
| `TheRefusalPointsAtTheDiscardedExpressionWithItsOwnSpan`            | Fact   | The refusal points at the discarded expression with its own span.            |
| `ARuleWithNoStatementsAtAllIsRefused`                               | Fact   | A rule with no statements at all is refused.                                 |
| `ABareReturnCompiles`                                               | Fact   | A bare return compiles.                                                      |
| `APipelineAssignmentToMatchedCompiles`                              | Fact   | A pipeline assignment to matched compiles.                                   |
| `AConditionalPipelineAssignmentToMatchedCompiles`                   | Fact   | A conditional pipeline assignment to matched compiles.                       |
| `AnAssignmentToAnotherNameDoesNotSatisfyTheContract`                | Fact   | An assignment to another name does not satisfy the contract.                 |
| `AReturnInsideAStringLiteralDoesNotSatisfyTheContract`              | Fact   | A return inside a string literal does not satisfy the contract.              |

### RuleSandboxTests

`Jube.Test.RuleParser.RuleSandboxTests` — 7 tests. Category: Unit.

| Test                                                        | Kind   | Verifies                                                       |
|-------------------------------------------------------------|--------|----------------------------------------------------------------|
| `RunsAParsedRuleThroughTheEngineDelegateTypeAsync`          | Theory | Runs a parsed rule through the engine delegate type.           |
| `TheSameRuleIsLoadedOnceAndThenServedFromTheCacheAsync`     | Fact   | The same rule is loaded once and then served from the cache.   |
| `CapacityEvictsTheLeastRecentlyUsedRuleAsync`               | Fact   | Capacity evicts the least recently used rule.                  |
| `AnEvictedRuleAssemblyIsUnloadedByTheGarbageCollectorAsync` | Fact   | An evicted rule assembly is unloaded by the garbage collector. |
| `ARuleThatHasNotCompiledCannotBeLoadedAsync`                | Fact   | A rule that has not compiled cannot be loaded.                 |
| `ARunThatExceedsTheTimeoutIsAbandonedAsync`                 | Fact   | A run that exceeds the timeout is abandoned.                   |
| `ARuntimeExceptionSurfacesToTheCallerAsync`                 | Fact   | A runtime exception surfaces to the caller.                    |

### RuleVocabularyTests

`Jube.Test.RuleParser.RuleVocabularyTests` — 5 tests. Category: Unit.

| Test                                                         | Kind | Verifies                                                              |
|--------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `EveryWordTheParserAllowsIsListedOnceExceptCompilerNames`    | Fact | Every word the parser allows is listed once except compiler names.    |
| `BuiltInWordsAreKeywordsAndInstallationWordsAreAllowedWords` | Fact | Built in words are keywords and installation words are allowed words. |
| `ExtensionMethodsAreFunctionsWithVisualBasicSignatures`      | Fact | Extension methods are functions with visual basic signatures.         |
| `EveryFunctionHasAReceiverAndAReturnType`                    | Fact | Every function has a receiver and a return type.                      |
| `EvalIsNeverOffered`                                         | Fact | Eval is never offered.                                                |
