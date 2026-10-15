---
layout: default
title: Ask Jooby Agent
nav_order: 1
parent: Testing
---

# Ask Jooby Agent

Tests of the Ask Jooby conversational agent layer: agent routing, tool registration and permissions, conversation and
memory services, guards, dynamic SQL table access and the streaming endpoints.

## General

### AdvancedAnalyticsAgentToolBinderTests

`Jube.Test.AskJooby.AdvancedAnalyticsAgentToolBinderTests` — 7 tests. Category: AskJooby.

| Test                                                   | Kind | Verifies                                                       |
|--------------------------------------------------------|------|----------------------------------------------------------------|
| `TheCatalogueDiscoversTheGroupsTheLayerWasBuiltAround` | Fact | The catalogue discovers the groups the layer was built around. |
| `EveryDiscoveredGroupBindsAtLeastOneLiveFunction`      | Fact | Every discovered group binds at least one live function.       |
| `EveryBoundToolCarriesAPermission`                     | Fact | Every bound tool carries a permission.                         |
| `EveryBoundToolIsNamedAndDescribed`                    | Fact | Every bound tool is named and described.                       |
| `ToolNamesAreUniqueAcrossTheWholeSurface`              | Fact | Tool names are unique across the whole surface.                |
| `TheTrainingRunFamiliesAreHeldBackForTheServicePath`   | Fact | The training run families are held back for the service path.  |
| `ABoundToolActuallyComputesWhenInvokedAsync`           | Fact | A bound tool actually computes when invoked.                   |

### AdvancedAnalyticsCachedTableReaderTests

`Jube.Test.AskJooby.AdvancedAnalyticsCachedTableReaderTests` — 3 tests. Category: AskJooby. Collection: Database.

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `GetAsyncConvertsPackedDoublesIntoAGenericallyNamedCachedTableAsync` | Fact | Get async converts packed doubles into a generically named cached table. |
| `GetAsyncReturnsNullForAnUnknownGuidAsync`                           | Fact | Get async returns null for an unknown guid.                              |
| `GetAsyncReturnsNullForAForeignTenantsPointerAsync`                  | Fact | Get async returns null for a foreign tenants pointer.                    |

### AdvancedAnalyticsTrainingAgentCoverageTests

`Jube.Test.AskJooby.AdvancedAnalyticsTrainingAgentCoverageTests` — 9 tests. Category: Unit.

| Test                                                                     | Kind | Verifies                                                                                |
|--------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `EveryTrainingToolIsEitherOwnedByOneAgentOrUnboundForAStatedReason`      | Fact | Every training tool is either owned by one agent or unbound for a stated reason.        |
| `NoTrainingToolIsOwnedByTwoAgentsExceptTheRunControlEveryOneOfThemNeeds` | Fact | No training tool is owned by two agents except the run control every one of them needs. |
| `EveryClaimedToolActuallyExistsInTheAnalyticsCatalogue`                  | Fact | Every claimed tool actually exists in the analytics catalogue.                          |
| `NoTrainingAgentHoldsMoreThanTheHouseLimit`                              | Fact | No training agent holds more than the house limit.                                      |
| `EveryTrainingAgentIsRegisteredAsALeafUnderModelRisk`                    | Fact | Every training agent is registered as a leaf under model risk.                          |
| `EveryTrainingAgentIsReachableFromTheRootWithinTheDelegationDepthBudget` | Fact | Every training agent is reachable from the root within the delegation depth budget.     |
| `EveryTrainingAgentDeclaresThePermissionsItsOwnToolsRequire`             | Fact | Every training agent declares the permissions its own tools require.                    |
| `EveryToolClassResolvesToATrainingServiceTheBinderCanConstruct`          | Fact | Every tool class resolves to a training service the binder can construct.               |
| `NoAgentMixesKindsBeyondTheOneThatDeliberatelySpansTwo`                  | Fact | No agent mixes kinds beyond the one that deliberately spans two.                        |

### AdvancedAnalyticsTrainingWorkflowCoverageTests

`Jube.Test.AskJooby.AdvancedAnalyticsTrainingWorkflowCoverageTests` — 7 tests. Category: Unit.

| Test                                                                          | Kind | Verifies                                                                                     |
|-------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `EveryWorkflowCarriesEnoughSemanticsToBeChosenWithoutThePasture`              | Fact | Every workflow carries enough semantics to be chosen without the pasture.                    |
| `EveryWorkflowIdAndNodeIdIsUnique`                                            | Fact | Every workflow id and node id is unique.                                                     |
| `NoNodeRequiresAnArtefactThatNoEarlierNodeProduces`                           | Fact | No node requires an artefact that no earlier node produces.                                  |
| `EveryNodeIsOwnedByAnAgentThatExists`                                         | Fact | Every node is owned by an agent that exists.                                                 |
| `EveryNodeIsOwnedByAnAgentThatActuallyHoldsTheToolsItNames`                   | Fact | Every node is owned by an agent that actually holds the tools it names.                      |
| `EveryNodeNamesCataloguedToolsThatActuallyExist`                              | Fact | Every node names catalogued tools that actually exist.                                       |
| `EveryWorkflowDeclaresTheOrderingRuleThatMakesItAGraphRatherThanAListOfSteps` | Fact | Every workflow declares the ordering rule that makes it a graph rather than a list of steps. |

### AgentFactoryPermissionTests

`Jube.Test.AskJooby.AgentFactoryPermissionTests` — 18 tests. Category: AskJooby.

| Test                                                                             | Kind | Verifies                                                                                     |
|----------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------|
| `IsPermittedReturnsTrueWhenNoPermissionsAreRequired`                             | Fact | Is permitted returns true when no permissions are required.                                  |
| `IsPermittedDefersToHasPermissionWhenPermissionsAreRequired`                     | Fact | Is permitted defers to has permission when permissions are required.                         |
| `AvailableExcludesAgentTypesTheCallerIsNotPermittedToUse`                        | Fact | Available excludes agent types the caller is not permitted to use.                           |
| `AvailableExcludesInternalAgentTypesLikeTheValidatorEvenWhenPermitted`           | Fact | Available excludes internal agent types like the validator even when permitted.              |
| `CreateAsyncStillBuildsAnInternalAgentTypeByNameForServiceCodeAsync`             | Fact | Create async still builds an internal agent type by name for service code.                   |
| `CreateAsyncThrowsWhenTheCallerLacksTheAgentTypesRequiredPermissionAsync`        | Fact | Create async throws when the caller lacks the agent types required permission.               |
| `CreateAsyncBuildsOnlyThePermittedToolsWhenTheCallerIsPermittedAsync`            | Fact | Create async builds only the permitted tools when the caller is permitted.                   |
| `OnlyTheGeneralAgentIsOfferedAskClarifyingQuestion`                              | Fact | Only the general agent is offered ask clarifying question.                                   |
| `CountAvailableToolsIncludesTheDatabaseToolsOnlyWhenDatabaseAccessIsAvailable`   | Fact | Count available tools includes the database tools only when database access is available.    |
| `TheFourRScriptToolsAreOfferedToEveryAgentOnlyWhenScriptExecutionIsWiredUp`      | Fact | The four r script tools are offered to every agent only when script execution is wired up.   |
| `CountAvailableToolsAlwaysCountsThink`                                           | Fact | Count available tools always counts think.                                                   |
| `CountAvailableToolsStillCountsThePermissionFreeToolsWhenEverythingElseIsDenied` | Fact | Count available tools still counts the permission free tools when everything else is denied. |
| `CountAvailableJoobriksExcludesGeneralAndIsInternalAgentTypes`                   | Fact | Count available joobriks excludes general and is internal agent types.                       |
| `CountAvailableJoobriksIsZeroWhenPermissionIsDenied`                             | Fact | Count available joobriks is zero when permission is denied.                                  |
| `CreateAsyncStillBuildsAnAgentThatHasReachedTheSpawnDepthCeilingAsync`           | Fact | Create async still builds an agent that has reached the spawn depth ceiling.                 |
| `CreateAsyncThrowsWhenTheCallerIsNotLandlordForALandlordOnlyAgentTypeAsync`      | Fact | Create async throws when the caller is not landlord for a landlord only agent type.          |
| `CreateAsyncSucceedsWhenTheCallerIsLandlordForALandlordOnlyAgentTypeAsync`       | Fact | Create async succeeds when the caller is landlord for a landlord only agent type.            |
| `UnknownAgentTypeRemainsAPlainArgumentExceptionAsync`                            | Fact | Unknown agent type remains a plain argument exception.                                       |

### AgentFactorySpecialistDescriptionTests

`Jube.Test.AskJooby.AgentFactorySpecialistDescriptionTests` — 4 tests. Category: AskJooby.

| Test                                                             | Kind | Verifies                                                                 |
|------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AStableWithItsOwnSpecialistsHasThemAppendedByName`              | Fact | A stable with its own specialists has them appended by name.             |
| `AnUnrestrictedAgentWithNullAllowedDelegatesGetsNothingAppended` | Fact | An unrestricted agent with null allowed delegates gets nothing appended. |
| `ALeafWithNoDelegatesGetsNothingAppended`                        | Fact | A leaf with no delegates gets nothing appended.                          |
| `FallsBackToInstructionsWhenThereIsNoDelegationSummary`          | Fact | Falls back to instructions when there is no delegation summary.          |

### AgentGroundingTextTests

`Jube.Test.AskJooby.AgentGroundingTextTests` — 3 tests. Category: AskJooby.

| Test                                                                    | Kind | Verifies                                                                          |
|-------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `CollectReturnsTheValidatorsInstructionsAndItsToolsUnderItsDisplayName` | Fact | Collect returns the validators instructions and its tools under its display name. |
| `CollectSkipsAgentTypesThatAreNotRegistered`                            | Fact | Collect skips agent types that are not registered.                                |
| `CollectDeduplicatesRepeatedAgentTypes`                                 | Fact | Collect deduplicates repeated agent types.                                        |

### AgentGuidanceExchangeTests

`Jube.Test.AskJooby.AgentGuidanceExchangeTests` — 7 tests. Category: Unit.

| Test                                                             | Kind | Verifies                                                                     |
|------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `EveryJoobrikStillWorkingHearsItAndEachOfThemOnlyOnce`           | Fact | Every joobrik still working hears it and each of them only once.             |
| `AJoobrikSpawnedAfterTheCallerSpokeStillHearsIt`                 | Fact | A joobrik spawned after the caller spoke still hears it.                     |
| `OneTurnsGuidanceNeverReachesAnother`                            | Fact | One turns guidance never reaches another.                                    |
| `ReleasingTheTurnStopsStaleGuidanceReachingTheNextOne`           | Fact | Releasing the turn stops stale guidance reaching the next one.               |
| `TheOldestIsDroppedFirstBecauseTheNewestIsWhatTheyMostWantHeard` | Fact | The oldest is dropped first because the newest is what they most want heard. |
| `NothingPendingIsTheCommonCaseAndCostsNothing`                   | Fact | Nothing pending is the common case and costs nothing.                        |
| `BlankGuidanceIsNotDelivered`                                    | Fact | Blank guidance is not delivered.                                             |

### AgentIdleWatchdogTests

`Jube.Test.AskJooby.AgentIdleWatchdogTests` — 6 tests. Category: AskJooby.

| Test                                                                     | Kind | Verifies                                                                    |
|--------------------------------------------------------------------------|------|-----------------------------------------------------------------------------|
| `AnAgentThatKeepsTalkingIsNeverCutOffHoweverLongItRunsAsync`             | Fact | An agent that keeps talking is never cut off however long it runs.          |
| `AnAgentThatGoesSilentIsRetiredRatherThanWaitedOnForeverAsync`           | Fact | An agent that goes silent is retired rather than waited on forever.         |
| `TheStalledAgentIsActuallyCancelledAndNotLeftRunningAsync`               | Fact | The stalled agent is actually cancelled and not left running.               |
| `ANonPositiveTimeoutLetsEverythingThroughUnwatchedAsync`                 | Fact | A non positive timeout lets everything through unwatched.                   |
| `CallerCancellationStillPropagatesRatherThanBeingTreatedAsIdlenessAsync` | Fact | Caller cancellation still propagates rather than being treated as idleness. |
| `AnEmptyRunCompletesWithoutBeingCalledIdleAsync`                         | Fact | An empty run completes without being called idle.                           |

### AgentRouterEmbeddingBatchTests

`Jube.Test.AskJooby.AgentRouterEmbeddingBatchTests` — 5 tests. Category: AskJooby.

| Test                                                                                  | Kind | Verifies                                                                                         |
|---------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------|
| `TheWholeRoutingCorpusIsEmbeddedInOneRequestAndNotOnePerExemplarAsync`                | Fact | The whole routing corpus is embedded in one request and not one per exemplar.                    |
| `WarmingTheCacheTakesOneRequestAndLeavesTheFirstTurnNothingToEmbedButItsOwnTaskAsync` | Fact | Warming the cache takes one request and leaves the first turn nothing to embed but its own task. |
| `AnEmbeddingProviderThatRejectsTheRequestFallsThroughAndSaysSoAsync`                  | Fact | An embedding provider that rejects the request falls through and says so.                        |
| `TheLedgerSaysWhichTierDecidedAndWhetherEmbeddingsWereReachedForAsync`                | Fact | The ledger says which tier decided and whether embeddings were reached for.                      |
| `ASecondRoutingCallEmbedsOnlyTheNewTaskBecauseTheExemplarsAreCachedAsync`             | Fact | A second routing call embeds only the new task because the exemplars are cached.                 |

### AgentRouterPermissionTests

`Jube.Test.AskJooby.AgentRouterPermissionTests` — 4 tests. Category: AskJooby.

| Test                                                                              | Kind | Verifies                                                                                   |
|-----------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `RouteAsyncNeverReturnsAnAgentTypeNotInTheAvailableSetAsync`                      | Fact | Route async never returns an agent type not in the available set.                          |
| `RouteAsyncRegexMatchesAnObviousResearchTaskAsync`                                | Fact | Route async regex matches an obvious research task.                                        |
| `RouteAsyncFallsBackToGeneralWhenNothingMatchesAsync`                             | Fact | Route async falls back to general when nothing matches.                                    |
| `RouteAsyncUsesTheLlmFallbackWhenNoEmbedderIsConfiguredAndRegexDoesNotMatchAsync` | Fact | Route async uses the llm fallback when no embedder is configured and regex does not match. |

### AgentStreamNarratorTests

`Jube.Test.AskJooby.AgentStreamNarratorTests` — 47 tests. Category: AskJooby.

| Test                                                                             | Kind   | Verifies                                                                                       |
|----------------------------------------------------------------------------------|--------|------------------------------------------------------------------------------------------------|
| `DescribeIncludesTheExceptionMessageWhenASpawnedAgentFails`                      | Fact   | Describe includes the exception message when a spawned agent fails.                            |
| `DescribeIncludesTheExceptionMessageForAnyOtherFailedTool`                       | Fact   | Describe includes the exception message for any other failed tool.                             |
| `DescribeEchoesRememberSSuccessTextRatherThanAGenericLine`                       | Fact   | Describe echoes remember s success text rather than a generic line.                            |
| `DescribeCountsRecallHitsRatherThanShowingRawContent`                            | Fact   | Describe counts recall hits rather than showing raw content.                                   |
| `DescribeCountsSearchHistoryHitsRatherThanShowingRawContent`                     | Fact   | Describe counts search history hits rather than showing raw content.                           |
| `DescribeReportsNoMatchesForSearchHistory`                                       | Fact   | Describe reports no matches for search history.                                                |
| `DescribeStartUsesTheAnimalPersonaVerbsForEachTool`                              | Fact   | Describe start uses the animal persona verbs for each tool.                                    |
| `DescribeStartAnnouncesHerdingTheJoobriksForSpawnAgents`                         | Fact   | Describe start announces herding the joobriks for spawn agents.                                |
| `ASurfacedFailureStillReadsAsAFailureRatherThanAnOddLookingAnswer`               | Fact   | A surfaced failure still reads as a failure rather than an odd looking answer.                 |
| `AToolPillCarriesTheExchangeBehindItSoTheCallerCanSeeWhatWasPassedAndReturned`   | Fact   | A tool pill carries the exchange behind it so the caller can see what was passed and returned. |
| `AFailedToolSaysSoInTheExchangeRatherThanShowingAnEmptyReturn`                   | Fact   | A failed tool says so in the exchange rather than showing an empty return.                     |
| `AToolCalledWithNothingAndReturningNothingCarriesNoExchangeAtAll`                | Fact   | A tool called with nothing and returning nothing carries no exchange at all.                   |
| `ADelegationEndsWithTheJoobriksCompletionWordInsteadOfAGistOfItsReport`          | Fact   | A delegation ends with the joobriks completion word instead of a gist of its report.           |
| `ALongReportNoLongerLeaksAnyOfItsTextIntoTheActivityLine`                        | Fact   | A long report no longer leaks any of its text into the activity line.                          |
| `AReportThatCameBackEmptyStillCompletesWithItsWord`                              | Fact   | A report that came back empty still completes with its word.                                   |
| `AFanOutAttributesEachGistToTheJoobrikThatGaveIt`                                | Fact   | A fan out attributes each gist to the joobrik that gave it.                                    |
| `EveryAgentInTheHierarchyHasAPersonaOfItsOwnAndNoTwoShareOne`                    | Fact   | Every agent in the hierarchy has a persona of its own and no two share one.                    |
| `EveryNamedAgentOpensItsPromptWithItsOwnNameAndGenderAndHasACompletionWord`      | Fact   | Every named agent opens its prompt with its own name and gender and has a completion word.     |
| `DescribeAgentLabelIsTheAgentsNameAloneAndNotItsJobTitle`                        | Fact   | Describe agent label is the agents name alone and not its job title.                           |
| `ASelfSpawnIsAnnouncedAsAFurtherStrandRatherThanAnotherAgent`                    | Fact   | A self spawn is announced as a further strand rather than another agent.                       |
| `ASelfSpawnsStepsAreDistinguishableFromItsParents`                               | Fact   | A self spawns steps are distinguishable from its parents.                                      |
| `AFanOutNumbersSelfSpawnsAsStrandsAndEveryoneElseByPosition`                     | Fact   | A fan out numbers self spawns as strands and everyone else by position.                        |
| `TheCheckerIsNamedFromTheValidatorDefinitionRatherThanWrittenOutAgain`           | Fact   | The checker is named from the validator definition rather than written out again.              |
| `TheRetiredReasonerIsNoLongerNarrated`                                           | Fact   | The retired reasoner is no longer narrated.                                                    |
| `DescribeStartNamesTheAgentsAFanOutIsCallingIn`                                  | Fact   | Describe start names the agents a fan out is calling in.                                       |
| `DescribeStartNamesEverySpawnedLeafSeparatelyRatherThanCollapsingThem`           | Fact   | Describe start names every spawned leaf separately rather than collapsing them.                |
| `DescribeStartFallsBackToTheHerdingLineWhenTheFanOutNamesNoAgent`                | Fact   | Describe start falls back to the herding line when the fan out names no agent.                 |
| `DescribeReportsBackForSpawnAgentsOnSuccessAndFailure`                           | Fact   | Describe reports back for spawn agents on success and failure.                                 |
| `DescribeReportsBackSilentlyOnSuccessWithNoExceptionNoise`                       | Fact   | Describe reports back silently on success with no exception noise.                             |
| `DescribeReadsTheActualBiteCountBackOutOfTheSearchRagResult`                     | Fact   | Describe reads the actual bite count back out of the search rag result.                        |
| `DescribeUsesSingularBiteForExactlyOneResult`                                    | Fact   | Describe uses singular bite for exactly one result.                                            |
| `DescribeReportsNoBitesFoundWhenSearchRagFindsNothing`                           | Fact   | Describe reports no bites found when search rag finds nothing.                                 |
| `DescribeStartSuppressesTheGenericStepPillForAskClarifyingQuestion`              | Fact   | Describe start suppresses the generic step pill for ask clarifying question.                   |
| `DescribeTurnsAskClarifyingQuestionIntoAClarificationItemCarryingTheRealOptions` | Fact   | Describe turns ask clarifying question into a clarification item carrying the real options.    |
| `DescribeFallsBackToAStepLineIfAskClarifyingQuestionsPayloadIsUnparseable`       | Fact   | Describe falls back to a step line if ask clarifying questions payload is unparseable.         |
| `DescribeReportsTheExceptionMessageWhenAskClarifyingQuestionFails`               | Fact   | Describe reports the exception message when ask clarifying question fails.                     |
| `DescribeStartNamesADomainJoobrikRatherThanPrintingItsSlug`                      | Fact   | Describe start names a domain joobrik rather than printing its slug.                           |
| `DescribeNamesADomainJoobrikOnSuccessAndOnFailure`                               | Fact   | Describe names a domain joobrik on success and on failure.                                     |
| `DescribeStartFallsBackToTheSlugForAnAgentThatCarriesNoName`                     | Fact   | Describe start falls back to the slug for an agent that carries no name.                       |
| `DescribeAgentLabelDecoratesOnlyTheAgentsThatCarryAName`                         | Theory | Describe agent label decorates only the agents that carry a name.                              |
| `DescribeStartHumanisesABoundServiceOperationName`                               | Fact   | Describe start humanises a bound service operation name.                                       |
| `DescribeHumanisesABoundServiceOperationNameOnSuccessAndOnFailure`               | Fact   | Describe humanises a bound service operation name on success and on failure.                   |
| `RoutingPassesTheYokeRatherThanCallingSomeoneIn`                                 | Fact   | Routing passes the yoke rather than calling someone in.                                        |
| `TheSpawnBannerAndTheHandoffBannerDoNotReadAsTheSameEvent`                       | Fact   | The spawn banner and the handoff banner do not read as the same event.                         |
| `TheRoutingDetailNamesTheTierThatDecidedAndTheAgentThatNowHoldsTheTurn`          | Fact   | The routing detail names the tier that decided and the agent that now holds the turn.          |
| `TheRoutingDetailLeavesOutAConfidenceTheTierNeverProduced`                       | Fact   | The routing detail leaves out a confidence the tier never produced.                            |
| `EveryTierReadsAsProseRatherThanAsItsEnumName`                                   | Theory | Every tier reads as prose rather than as its enum name.                                        |

### AgentToolIconsTests

`Jube.Test.AskJooby.AgentToolIconsTests` — 7 tests. Category: Unit.

| Test                                            | Kind   | Verifies                                               |
|-------------------------------------------------|--------|--------------------------------------------------------|
| `EveryCoreToolHasItsOwnIcon`                    | Fact   | Every core tool has its own icon.                      |
| `EveryAnalyticsToolResolvesToItsGroupIcon`      | Fact   | Every analytics tool resolves to its group icon.       |
| `EveryServiceOperationResolvesToADomainIcon`    | Fact   | Every service operation resolves to a domain icon.     |
| `TreeChildrenNavigationTakesTheIconOfItsEntity` | Theory | Tree children navigation takes the icon of its entity. |
| `EveryIconInTheEnumHasABrowserBody`             | Fact   | Every icon in the enum has a browser body.             |
| `EveryFunctionToolGroupHasItsOwnIcon`           | Fact   | Every function tool group has its own icon.            |
| `StepForAToolCarriesItsIcon`                    | Fact   | Step for a tool carries its icon.                      |

### AgentToolNarrationTests

`Jube.Test.AskJooby.AgentToolNarrationTests` — 7 tests. Category: Unit.

| Test                                                                          | Kind   | Verifies                                                                                   |
|-------------------------------------------------------------------------------|--------|--------------------------------------------------------------------------------------------|
| `DescribeStartingTurnsACataloguedNameIntoAParticiplePhrase`                   | Theory | Describe starting turns a catalogued name into a participle phrase.                        |
| `DescribeFinishedTurnsACataloguedNameIntoAPastTensePhrase`                    | Theory | Describe finished turns a catalogued name into a past tense phrase.                        |
| `DescribeStartingNeverMangesAnAcronymOrAProductName`                          | Theory | Describe starting never manges an acronym or a product name.                               |
| `DescribeStartingTakesTheOperationFromBeforeTheQualifierRatherThanInsideIt`   | Theory | Describe starting takes the operation from before the qualifier rather than inside it.     |
| `DescribeStartingCollapsesAWordTheOperationSuffixRepeats`                     | Theory | Describe starting collapses a word the operation suffix repeats.                           |
| `DescribeStartingOnlyArticlesASingularSubjectAndPicksTheRightArticle`         | Theory | Describe starting only articles a singular subject and picks the right article.            |
| `DescribeReturnsNothingWhenNoOperationIsRecognisedSoTheCallerKeepsTheRawName` | Theory | Describe returns nothing when no operation is recognised so the caller keeps the raw name. |

### AskJoobyChatAuditAiAnalysisTests

`Jube.Test.AskJooby.AskJoobyChatAuditAiAnalysisTests` — 9 tests. Category: AskJooby.

| Test                                                                         | Kind | Verifies                                                                            |
|------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `BuildPromptIncludesTheTitleAndEveryReport`                                  | Fact | Build prompt includes the title and every report.                                   |
| `BuildPromptGroundsInTheInvolvedAgentsOwnInstructionsWhenKnown`              | Fact | Build prompt grounds in the involved agents own instructions when known.            |
| `BuildPromptOmitsGroundingWhenNoAgentTypesAreKnown`                          | Fact | Build prompt omits grounding when no agent types are known.                         |
| `BuildPromptIncludesCodeGroundingSectionsWhenGiven`                          | Fact | Build prompt includes code grounding sections when given.                           |
| `BuildPromptOmitsCodeGroundingSectionsWhenNotGiven`                          | Fact | Build prompt omits code grounding sections when not given.                          |
| `AnalyzeAsyncReturnsTheTrimmedResponseTextOnSuccessAsync`                    | Fact | Analyze async returns the trimmed response text on success.                         |
| `AnalyzeAsyncReportsTheFailureReasonRatherThanThrowingWhenTheCallFailsAsync` | Fact | Analyze async reports the failure reason rather than throwing when the call fails.  |
| `AnalyzeAsyncReportsATimeoutReasonRatherThanHangingWhenTheCallTimesOutAsync` | Fact | Analyze async reports a timeout reason rather than hanging when the call times out. |
| `AnalyzeAsyncReportsAFailureReasonForAnEmptyResponseAsync`                   | Fact | Analyze async reports a failure reason for an empty response.                       |

### AskJoobyChatAuditBundleBuilderTests

`Jube.Test.AskJooby.AskJoobyChatAuditBundleBuilderTests` — 3 tests. Category: AskJooby.

| Test                                                                 | Kind | Verifies                                                                           |
|----------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `AToolCallsFullResultIsFiledNotInlinedButIsStillReachableFromTheLog` | Fact | A tool calls full result is filed not inlined but is still reachable from the log. |
| `ADataCacheTableOverTheRowLimitIsPreviewedInTheLogButFullInTheCsv`   | Fact | A data cache table over the row limit is previewed in the log but full in the csv. |
| `TheManifestIndexesEveryArtifactByItsActualPath`                     | Fact | The manifest indexes every artifact by its actual path.                            |

### AskJoobyChatAuditCodeGroundingTests

`Jube.Test.AskJooby.AskJoobyChatAuditCodeGroundingTests` — 4 tests. Category: AskJooby.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `BuildAgentCodeAsyncKeepsOnlyCodeCategoryHitsAsync`            | Fact | Build agent code async keeps only code category hits.              |
| `BuildAgentCodeAsyncDedupesTheSameChunkAcrossAgentTypesAsync`  | Fact | Build agent code async dedupes the same chunk across agent types.  |
| `BuildAgentCodeAsyncReturnsNullWhenNothingIsCodeAsync`         | Fact | Build agent code async returns null when nothing is code.          |
| `BuildAgentCodeAsyncStopsOnceTheCharacterBudgetIsReachedAsync` | Fact | Build agent code async stops once the character budget is reached. |

### AskJoobyChatAuditContextEfficiencyTests

`Jube.Test.AskJooby.AskJoobyChatAuditContextEfficiencyTests` — 4 tests. Category: AskJooby.

| Test                                                                 | Kind | Verifies                                                                        |
|----------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `NoModelCallsProducesAnEmptySection`                                 | Fact | No model calls produces an empty section.                                       |
| `TheLargestModelCallByInputTokensIsSurfacedWithItsAgentType`         | Fact | The largest model call by input tokens is surfaced with its agent type.         |
| `TheLargestToolResultAcrossAnyToolIsSurfacedNotJustRag`              | Fact | The largest tool result across any tool is surfaced not just rag.               |
| `CompactionPressureReportsHowMuchHadAlreadyBuiltUpBeforeTheFirstOne` | Fact | Compaction pressure reports how much had already built up before the first one. |

### AskJoobyChatAuditCoverageTests

`Jube.Test.AskJooby.AskJoobyChatAuditCoverageTests` — 3 tests. Category: AskJooby.

| Test                                                          | Kind | Verifies                                                              |
|---------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `EveryAskJoobyRelationIsEitherRenderedOrDeliberatelyExcluded` | Fact | Every ask jooby relation is either rendered or deliberately excluded. |
| `NothingIsClaimedAsRenderedThatTheContextDoesNotHave`         | Fact | Nothing is claimed as rendered that the context does not have.        |
| `EveryExclusionCarriesAReason`                                | Fact | Every exclusion carries a reason.                                     |

### AskJoobyChatAuditHealthSignalsTests

`Jube.Test.AskJooby.AskJoobyChatAuditHealthSignalsTests` — 5 tests. Category: AskJooby.

| Test                                                                     | Kind | Verifies                                                                               |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `AQuietConversationWithNothingUnusualFlagsNothing`                       | Fact | A quiet conversation with nothing unusual flags nothing.                               |
| `ThreeIdenticalToolCallsByTheSameRunAreFlaggedAsARetryLoop`              | Fact | Three identical tool calls by the same run are flagged as a retry loop.                |
| `ARunThatBouncesBackToItsGrandparentsAgentTypeIsFlaggedAsAHandoffBounce` | Fact | A run that bounces back to its grandparents agent type is flagged as a handoff bounce. |
| `ARunWithManyModelCallsAndNoResultIsFlaggedAsEffortWithoutOutput`        | Fact | A run with many model calls and no result is flagged as effort without output.         |
| `TheHealthJsonCarriesTheSameFlagCountAsTheMarkdownOverallLine`           | Fact | The health json carries the same flag count as the markdown overall line.              |

### AskJoobyChatAuditMarkdownBuilderGuidanceNudgeTests

`Jube.Test.AskJooby.AskJoobyChatAuditMarkdownBuilderGuidanceNudgeTests` — 3 tests. Category: AskJooby.

| Test                                                                  | Kind | Verifies                                                                            |
|-----------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `ANudgeThatWasPickedUpShowsWhenAndTheAgentsImmediateReaction`         | Fact | A nudge that was picked up shows when and the agents immediate reaction.            |
| `ANudgeThatWasNeverDrainedSaysSoRatherThanHidingIt`                   | Fact | A nudge that was never drained says so rather than hiding it.                       |
| `SeveralNudgesDrainedByOneStepShareOnePickupAndAreNotAssumedOneToOne` | Fact | Several nudges drained by one step share one pickup and are not assumed one to one. |

### AskJoobyChatAuditRagGapSignalsTests

`Jube.Test.AskJooby.AskJoobyChatAuditRagGapSignalsTests` — 5 tests. Category: AskJooby.

| Test                                                            | Kind | Verifies                                                                  |
|-----------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `AConversationWithNoGapsFlagsNothing`                           | Fact | A conversation with no gaps flags nothing.                                |
| `AnEmptySearchRagResultIsFlaggedWithItsQuery`                   | Fact | An empty search rag result is flagged with its query.                     |
| `AValidationOmissionEntryIsFlaggedAsAPastureCoverageGap`        | Fact | A validation omission entry is flagged as a pasture coverage gap.         |
| `AValidationThatNeverPickedUpIsFlaggedSeparatelyFromAnOmission` | Fact | A validation that never picked up is flagged separately from an omission. |
| `SqlFallbackIsCountedButNeverCountedAsAFlaggedGap`              | Fact | Sql fallback is counted but never counted as a flagged gap.               |

### AskJoobyChatAuditRagPerformanceTests

`Jube.Test.AskJooby.AskJoobyChatAuditRagPerformanceTests` — 3 tests. Category: AskJooby.

| Test                                        | Kind | Verifies                                         |
|---------------------------------------------|------|--------------------------------------------------|
| `NoSearchRagCallsProducesAnEmptySection`    | Fact | No search rag calls produces an empty section.   |
| `SummarisesHitCountResponseSizeAndDuration` | Fact | Summarises hit count response size and duration. |
| `NonSearchRagToolCallsAreIgnored`           | Fact | Non search rag tool calls are ignored.           |

### AskJoobyChatAuditServiceTests

`Jube.Test.AskJooby.AskJoobyChatAuditServiceTests` — 4 tests. Category: AskJooby. Collection: Database.

| Test                                                                | Kind | Verifies                                                                 |
|---------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `GenerateAsyncSucceedsForTheConversationsOwnCreatorAsync`           | Fact | Generate async succeeds for the conversations own creator.               |
| `GenerateAsyncIsForbiddenForADifferentUserInTheSameTenantAsync`     | Fact | Generate async is forbidden for a different user in the same tenant.     |
| `GenerateAsyncIsForbiddenForANonLandlordUserEvenHoldingAccessAsync` | Fact | Generate async is forbidden for a non landlord user even holding access. |
| `GenerateAsyncIsNotFoundForAUserInADifferentTenantAsync`            | Fact | Generate async is not found for a user in a different tenant.            |

### AskJoobyChatTranscriptServiceTests

`Jube.Test.AskJooby.AskJoobyChatTranscriptServiceTests` — 7 tests. Category: AskJooby. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                     |
|-------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `GenerateAsyncSucceedsForANonLandlordUserHoldingAccessAsync`            | Fact | Generate async succeeds for a non landlord user holding access.              |
| `GenerateAsyncSucceedsForALandlordUserWithNoOrdinaryPermissionsAsync`   | Fact | Generate async succeeds for a landlord user with no ordinary permissions.    |
| `GenerateAsyncIsForbiddenForAUserWithNeitherLandlordNorAccessAsync`     | Fact | Generate async is forbidden for a user with neither landlord nor access.     |
| `GenerateAsyncIsForbiddenForADifferentUserInTheSameTenantAsync`         | Fact | Generate async is forbidden for a different user in the same tenant.         |
| `GenerateAsyncIsNotFoundForAUserInADifferentTenantAsync`                | Fact | Generate async is not found for a user in a different tenant.                |
| `GenerateAsyncStatesHistoryWasNeverFoldedWhenThereWasNoCompactionAsync` | Fact | Generate async states history was never folded when there was no compaction. |
| `GenerateAsyncStatesHistoryWasFoldedButNeverLostWhenCompactionRanAsync` | Fact | Generate async states history was folded but never lost when compaction ran. |

### AskJoobyContentSafetyTests

`Jube.Test.AskJooby.AskJoobyContentSafetyTests` — 1 test. Category: AskJooby. Collection: Database.

| Test                                                                                           | Kind | Verifies                                                                                              |
|------------------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------------|
| `AnUnsupportedClarificationOptionIsCaughtAndCaveatedRatherThanReachingTheCallerUnflaggedAsync` | Fact | An unsupported clarification option is caught and caveated rather than reaching the caller unflagged. |

### AskJoobyConversationCompactionTests

`Jube.Test.AskJooby.AskJoobyConversationCompactionTests` — 1 test. Category: AskJooby. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                      |
|--------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `SendingAMessageOnceTheTurnCountThresholdIsReachedActuallyCompactsAsync` | Fact | Sending a message once the turn count threshold is reached actually compacts. |

### AskJoobyConversationFeedbackTests

`Jube.Test.AskJooby.AskJoobyConversationFeedbackTests` — 6 tests. Category: AskJooby. Collection: Database.

| Test                                                                   | Kind | Verifies                                                                      |
|------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `ThumbsUpStoresAUserScopedMemoryTaggedPositiveAsync`                   | Fact | Thumbs up stores a user scoped memory tagged positive.                        |
| `ThumbsDownStoresAUserScopedMemoryTaggedNegativeAsync`                 | Fact | Thumbs down stores a user scoped memory tagged negative.                      |
| `ThumbsUpLeavesAStepEntryInTheTranscriptAsync`                         | Fact | Thumbs up leaves a step entry in the transcript.                              |
| `ThumbsUpOnAReplyHoldingPersonalDataStoresARedactedMemoryAsync`        | Fact | Thumbs up on a reply holding personal data stores a redacted memory.          |
| `ThumbsUpOnAReplyHoldingASecretIsRefusedWithTheGuardsExplanationAsync` | Fact | Thumbs up on a reply holding a secret is refused with the guards explanation. |
| `RejectsFeedbackForAMessageThatIsNotAnAssistantReplyAsync`             | Fact | Rejects feedback for a message that is not an assistant reply.                |

### AskJoobyConversationHistoryQueryTests

`Jube.Test.AskJooby.AskJoobyConversationHistoryQueryTests` — 4 tests. Category: AskJooby. Collection: Database.

| Test                                                                      | Kind | Verifies                                                                         |
|---------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `SearchFindsAConversationByTitleEvenWhenTheBodyNeverRepeatsTheWordsAsync` | Fact | Search finds a conversation by title even when the body never repeats the words. |
| `UserScopeNeverSurfacesAnotherUsersConversationAsync`                     | Fact | User scope never surfaces another users conversation.                            |
| `SearchReportsIsCompactForACompactionSummaryRowAsync`                     | Fact | Search reports is compact for a compaction summary row.                          |
| `SearchExcludesNarrationEntriesLikeStepsAndThoughtsAsync`                 | Fact | Search excludes narration entries like steps and thoughts.                       |

### AskJoobyConversationNudgeTests

`Jube.Test.AskJooby.AskJoobyConversationNudgeTests` — 4 tests. Category: AskJooby. Collection: Database.

| Test                                                                   | Kind | Verifies                                                                      |
|------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `SendingWhileAnotherTurnIsRunningPersistsANudgeAndStartsNoNewRunAsync` | Fact | Sending while another turn is running persists a nudge and starts no new run. |
| `SendingWhileIdleNeverSetsIsNudgeAsync`                                | Fact | Sending while idle never sets is nudge.                                       |
| `SendNudgeAlwaysPersistsEvenWhenNothingIsActuallyRunningAsync`         | Fact | Send nudge always persists even when nothing is actually running.             |
| `PendingGuidanceIsInjectedIntoTheRunningTurnsNextModelCallAsync`       | Fact | Pending guidance is injected into the running turns next model call.          |

### AskJoobyConversationReviseStreamingTests

`Jube.Test.AskJooby.AskJoobyConversationReviseStreamingTests` — 1 test. Category: AskJooby. Collection: Database.

| Test                                                              | Kind | Verifies                                                                 |
|-------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `AStepFromTheReviseSubAgentsOwnToolCallReachesTheLiveStreamAsync` | Fact | A step from the revise sub agents own tool call reaches the live stream. |

### AskJoobyConversationStartTests

`Jube.Test.AskJooby.AskJoobyConversationStartTests` — 5 tests. Category: AskJooby. Collection: Database.

| Test                                                                              | Kind | Verifies                                                                                |
|-----------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------|
| `StartConversationIsANoOpWhenTheConversationAlreadyHasMessagesAsync`              | Fact | Start conversation is a no op when the conversation already has messages.               |
| `StartConversationUsesTheStaticFallbackWhenNoInferenceBackendIsConfiguredAsync`   | Fact | Start conversation uses the static fallback when no inference backend is configured.    |
| `StartConversationNeverCallsTheChatClientWhenNoInferenceBackendIsConfiguredAsync` | Fact | Start conversation never calls the chat client when no inference backend is configured. |
| `StartConversationSendsTheEmptyReplyFallbackWhenTheModelProducesNoTextAsync`      | Fact | Start conversation sends the empty reply fallback when the model produces no text.      |
| `StatsQueryCountsBitesGloballyButTrailsAndSaltlicksPerTenantAsync`                | Fact | Stats query counts bites globally but trails and saltlicks per tenant.                  |

### AskJoobyConversationValidationBypassTests

`Jube.Test.AskJooby.AskJoobyConversationValidationBypassTests` — 2 tests. Category: AskJooby. Collection: Database.

| Test                                                      | Kind | Verifies                                                      |
|-----------------------------------------------------------|------|---------------------------------------------------------------|
| `AReplyBuiltEntirelyFromRunSqlQueryStillGetsCheckedAsync` | Fact | A reply built entirely from run sql query still gets checked. |
| `AReplyWithNoBypassToolCallStillRunsValidationAsync`      | Fact | A reply with no bypass tool call still runs validation.       |

### AskJoobyDataCacheClientTests

`Jube.Test.AskJooby.AskJoobyDataCacheClientTests` — 6 tests. Category: AskJooby. Collection: Database.

| Test                                                                         | Kind | Verifies                                                                          |
|------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------|
| `StoreThenGetRoundTripsColumnsAndNormalisesRowValuesAsync`                   | Fact | Store then get round trips columns and normalises row values.                     |
| `GetReturnsNullForAnUnknownPointerAsync`                                     | Fact | Get returns null for an unknown pointer.                                          |
| `StoreThrowsWhenRowCountExceedsTheConfiguredCapAsync`                        | Fact | Store throws when row count exceeds the configured cap.                           |
| `GetDispatchesAGuidShapedPointerToTheAdvancedAnalyticsReaderAsync`           | Fact | Get dispatches a guid shaped pointer to the advanced analytics reader.            |
| `GetWithoutAnAdvancedAnalyticsReaderLeavesAGuidShapedPointerUnresolvedAsync` | Fact | Get without an advanced analytics reader leaves a guid shaped pointer unresolved. |
| `StoreAppliesTheDefaultTtlWhenNoneIsSuppliedAsync`                           | Fact | Store applies the default ttl when none is supplied.                              |

### AskJoobyDataCacheRepositoryTests

`Jube.Test.AskJooby.AskJoobyDataCacheRepositoryTests` — 4 tests. Category: AskJooby. Collection: Database.

| Test                                                        | Kind | Verifies                                                        |
|-------------------------------------------------------------|------|-----------------------------------------------------------------|
| `InsertAndGetRoundTripTheStoredJsonAsync`                   | Fact | Insert and get round trip the stored json.                      |
| `GetReturnsNullForAnExpiredEntryAsync`                      | Fact | Get returns null for an expired entry.                          |
| `GetReturnsNullForAPointerBelongingToADifferentTenantAsync` | Fact | Get returns null for a pointer belonging to a different tenant. |
| `GetReturnsNullForAnUnknownPointerAsync`                    | Fact | Get returns null for an unknown pointer.                        |

### AskJoobyErrataPromptTests

`Jube.Test.AskJooby.AskJoobyErrataPromptTests` — 3 tests. Category: AskJooby.

| Test                                                               | Kind | Verifies                                                                     |
|--------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `NoErrataMeansNoErrataBlockAtAll`                                  | Fact | No errata means no errata block at all.                                      |
| `EveryErratumIsListedUnderTheHeaderInTheOrderGiven`                | Fact | Every erratum is listed under the header in the order given.                 |
| `ErrataSitAfterTheSharedPreambleAndBeforeTheAgentsOwnInstructions` | Fact | Errata sit after the shared preamble and before the agents own instructions. |

### AskJoobyFileExportClientTests

`Jube.Test.AskJooby.AskJoobyFileExportClientTests` — 2 tests. Category: AskJooby. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                        |
|--------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `ExportAsyncCreatesAFileRowWithTheGivenBytesAndMetadataAsync`            | Fact | Export async creates a file row with the given bytes and metadata.              |
| `AttachToMessageAsyncLinksAnExportedFileToTheMessageThatProducedItAsync` | Fact | Attach to message async links an exported file to the message that produced it. |

### AskJoobyInjectionGuardTests

`Jube.Test.AskJooby.AskJoobyInjectionGuardTests` — 19 tests. Category: AskJooby.

| Test                                                                                                     | Kind   | Verifies                                                                                            |
|----------------------------------------------------------------------------------------------------------|--------|-----------------------------------------------------------------------------------------------------|
| `AHostileTopicJudgeWouldRejectADecontextualisedTabularRowUnderTheHumanInputPipelineAsync`                | Fact   | A hostile topic judge would reject a decontextualised tabular row under the human input pipeline.   |
| `DescribeScreening_categorises_the_firing_rule_by_what_it_actually_is`                                   | Theory | Describe screening categorises the firing rule by what it actually is.                              |
| `CheckToolContentAsync_passes_the_same_benign_row_with_no_topic_judgment_at_allAsync`                    | Fact   | Check tool content async passes the same benign row with no topic judgment at all.                  |
| `CheckToolContentAsync_still_flags_a_genuine_instruction_override_in_tool_contentAsync`                  | Fact   | Check tool content async still flags a genuine instruction override in tool content.                |
| `CheckAsync_blocks_a_textbook_injection_pattern_by_default_via_the_regex_floorAsync`                     | Fact   | Check async blocks a textbook injection pattern by default via the regex floor.                     |
| `TheDefaultSettings_ShipChatInputTopicBoundaryEnabled`                                                   | Fact   | The default settings ship chat input topic boundary enabled.                                        |
| `BuildPipeline_gives_the_input_side_judge_its_own_permissive_system_promptAsync`                         | Fact   | Build pipeline gives the input side judge its own permissive system prompt.                         |
| `TheDefaultSettings_KeepTheLiberalPromptInjectionThreshold`                                              | Fact   | The default settings keep the liberal prompt injection threshold.                                   |
| `ResolveChatInputPromptInjectionMainThreshold_returns_the_configured_liberal_default`                    | Fact   | Resolve chat input prompt injection main threshold returns the configured liberal default.          |
| `ResolveChatInputPromptInjectionMainThreshold_returns_null_once_explicitly_disabled`                     | Fact   | Resolve chat input prompt injection main threshold returns null once explicitly disabled.           |
| `CheckAsync_still_detects_secrets_even_with_ChatInputPromptInjection_explicitly_disabledAsync`           | Fact   | Check async still detects secrets even with chat input prompt injection explicitly disabled.        |
| `FindAsync_no_longer_reliably_catches_a_subtler_override_phrasing_at_the_liberal_default_thresholdAsync` | Fact   | Find async no longer reliably catches a subtler override phrasing at the liberal default threshold. |
| `CheckToolContentAsync_can_be_disabled_through_the_ToolContentScreening_ruleAsync`                       | Fact   | Check tool content async can be disabled through the tool content screening rule.                   |
| `FindAsync_flags_a_message_that_tries_to_override_the_instructionsAsync`                                 | Theory | Find async flags a message that tries to override the instructions.                                 |
| `FindAsync_lets_an_ordinary_question_throughAsync`                                                       | Theory | Find async lets an ordinary question through.                                                       |
| `FindAsync_flags_a_message_that_contains_an_aws_access_keyAsync`                                         | Fact   | Find async flags a message that contains an aws access key.                                         |
| `FindAsync_flags_a_message_that_contains_a_credential_even_when_some_other_rule_catches_it_firstAsync`   | Fact   | Find async flags a message that contains a credential even when some other rule catches it first.   |
| `FindAsync_still_lets_through_a_question_containing_ordinary_jube_domain_dataAsync`                      | Fact   | Find async still lets through a question containing ordinary jube domain data.                      |
| `CheckOutputAsync_lets_an_ordinary_reply_through_when_no_judge_is_configuredAsync`                       | Fact   | Check output async lets an ordinary reply through when no judge is configured.                      |

### AskJoobyMemoryTests

`Jube.Test.AskJooby.AskJoobyMemoryTests` — 11 tests. Category: AskJooby. Collection: Database.

| Test                                                                | Kind | Verifies                                                               |
|---------------------------------------------------------------------|------|------------------------------------------------------------------------|
| `RecallNeverSurfacesAnotherUsersUserScopedMemoryAsync`              | Fact | Recall never surfaces another users user scoped memory.                |
| `RecallSurfacesSystemScopedMemoriesRegardlessOfWhoCreatedThemAsync` | Fact | Recall surfaces system scoped memories regardless of who created them. |
| `RecallRanksByVectorSimilarityWhenAnEmbeddingIsSuppliedAsync`       | Fact | Recall ranks by vector similarity when an embedding is supplied.       |
| `InsertAsyncPersistsTheEmbeddingColumnAsync`                        | Fact | Insert async persists the embedding column.                            |
| `InsertAsyncAndSearchBothPreserveTheFeedbackStatusAsync`            | Fact | Insert async and search both preserve the feedback status.             |
| `RecallExcludesAMemoryWhoseExpiryHasAlreadyPassedAsync`             | Fact | Recall excludes a memory whose expiry has already passed.              |
| `RecallSinceExcludesMemoriesRecordedBeforeTheCutoffAsync`           | Fact | Recall since excludes memories recorded before the cutoff.             |
| `RecallRanksAFresherMemoryAboveAnEquallyMatchingOlderOneAsync`      | Fact | Recall ranks a fresher memory above an equally matching older one.     |
| `InsertAsyncDefaultsToFactKindAsync`                                | Fact | Insert async defaults to fact kind.                                    |
| `RecallExcludesReflectionsByDefaultButIncludesFactsAsync`           | Fact | Recall excludes reflections by default but includes facts.             |
| `ConversationScopeIsVisibleWithinItsOwnConversationOnlyAsync`       | Fact | Conversation scope is visible within its own conversation only.        |

### AskJoobyOpeningInstructionTests

`Jube.Test.AskJooby.AskJoobyOpeningInstructionTests` — 6 tests. Category: AskJooby.

| Test                                                                                    | Kind | Verifies                                                                                             |
|-----------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------|
| `WithPageContext_StillCarriesTheFullInstructionBody`                                    | Fact | With page context still carries the full instruction body.                                           |
| `WithoutPageContext_ReturnsTheInstructionBodyUnprefixed`                                | Fact | Without page context returns the instruction body unprefixed.                                        |
| `ParsePageContext_WithJsonPageNameStateAndRecord_ResolvesCatalogueTextAndRecordSummary` | Fact | Parse page context with json page name state and record resolves catalogue text and record summary.  |
| `ParsePageContext_WithPageNameNotInTheCatalogue_StillReturnsPageNameWithoutPurposeText` | Fact | Parse page context with page name not in the catalogue still returns page name without purpose text. |
| `ParsePageContext_WithNonJsonText_FallsBackToTreatingItAsThePageName`                   | Fact | Parse page context with non json text falls back to treating it as the page name.                    |
| `ParsePageContext_WithNullOrWhitespace_ReturnsNull`                                     | Fact | Parse page context with null or whitespace returns null.                                             |

### AskJoobyOutputTopicBoundaryTests

`Jube.Test.AskJooby.AskJoobyOutputTopicBoundaryTests` — 5 tests. Category: AskJooby.

| Test                                                                                         | Kind | Verifies                                                                                   |
|----------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `A_bare_validator_pass_verdict_is_off_topic_under_the_plain_default_topic_promptAsync`       | Fact | A bare validator pass verdict is off topic under the plain default topic prompt.           |
| `AskJoobys_own_output_pipeline_lets_the_same_validator_verdict_throughAsync`                 | Fact | Ask joobys own output pipeline lets the same validator verdict through.                    |
| `A_genuinely_unrelated_reply_is_still_blocked_through_AskJoobys_output_pipelineAsync`        | Fact | A genuinely unrelated reply is still blocked through ask joobys output pipeline.           |
| `The_landlord_topic_list_is_the_base_list_plus_exactly_one_platform_ops_topic`               | Fact | The landlord topic list is the base list plus exactly one platform ops topic.              |
| `CheckAsync_and_CheckOutputAsync_accept_an_isLandlord_flag_without_throwing_either_wayAsync` | Fact | Check async and check output async accept an is landlord flag without throwing either way. |

### AskJoobyRagClientPartitionTests

`Jube.Test.AskJooby.AskJoobyRagClientPartitionTests` — 4 tests. Category: AskJooby.

| Test                                                                              | Kind | Verifies                                                                                          |
|-----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------|
| `ABiteOverTheLimitIsDroppedAndNeverReachesTheKeptList`                            | Fact | A bite over the limit is dropped and never reaches the kept list.                                 |
| `ABiteExactlyAtTheLimitIsKept`                                                    | Fact | A bite exactly at the limit is kept.                                                              |
| `AZeroOrNegativeLimitDisablesFilteringEntirely`                                   | Fact | A zero or negative limit disables filtering entirely.                                             |
| `WhenEveryCandidateIsOversizedTheKeptListIsEmptyRatherThanFallingBackToAllOfThem` | Fact | When every candidate is oversized the kept list is empty rather than falling back to all of them. |

### AskJoobyRepositoryTests

`Jube.Test.AskJooby.AskJoobyRepositoryTests` — 14 tests. Category: Service. Collection: Database.

| Test                                                                             | Kind | Verifies                                                                              |
|----------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------|
| `ListMessagesByConversationAsyncExcludesNarrationEntriesAsync`                   | Fact | List messages by conversation async excludes narration entries.                       |
| `InsertAsyncDefaultsEntryTypeToMessageWhenNotSpecifiedAsync`                     | Fact | Insert async defaults entry type to message when not specified.                       |
| `ListEntriesByConversationAsyncExcludesCompactsButListMessagesIncludesThemAsync` | Fact | List entries by conversation async excludes compacts but list messages includes them. |
| `InsertWithEmbeddingAsyncPersistsTheEmbeddingAndCompactFlagAsync`                | Fact | Insert with embedding async persists the embedding and compact flag.                  |
| `InsertWithEmbeddingAsyncAcceptsAnEmptyEmbeddingWithoutThrowingAsync`            | Fact | Insert with embedding async accepts an empty embedding without throwing.              |
| `GetTotalsByRunIdsAsyncSumsTokensOnlyAcrossTheGivenRunTreeAsync`                 | Fact | Get totals by run ids async sums tokens only across the given run tree.               |
| `GetTotalsByRunIdsAsyncReturnsZeroForAnEmptyRunIdCollectionAsync`                | Fact | Get totals by run ids async returns zero for an empty run id collection.              |
| `ListByConversationAsyncReturnsAllRunsInTheRunTreeAsync`                         | Fact | List by conversation async returns all runs in the run tree.                          |
| `AttachToMessageAsyncLinksAnUploadedFileToTheMessageItWasSentWithAsync`          | Fact | Attach to message async links an uploaded file to the message it was sent with.       |
| `ListByMessageIdsAsyncReturnsAnEmptyListForAnEmptyMessageIdCollectionAsync`      | Fact | List by message ids async returns an empty list for an empty message id collection.   |
| `DeleteAsyncSoftDeletesAConversationSoItNoLongerLoadsAsync`                      | Fact | Delete async soft deletes a conversation so it no longer loads.                       |
| `DeleteAsyncThrowsKeyNotFoundForAConversationThatDoesNotExistAsync`              | Fact | Delete async throws key not found for a conversation that does not exist.             |
| `ListAsyncOrdersByMostRecentlyActiveRatherThanCreationOrderAsync`                | Fact | List async orders by most recently active rather than creation order.                 |
| `ListAsyncFallsBackToCreatedDateWhenUpdatedDateIsNullAsync`                      | Fact | List async falls back to created date when updated date is null.                      |

### AskJoobySensitiveDataGuardTests

`Jube.Test.AskJooby.AskJoobySensitiveDataGuardTests` — 2 tests. Category: AskJooby.

| Test                                                                   | Kind   | Verifies                                                          |
|------------------------------------------------------------------------|--------|-------------------------------------------------------------------|
| `FindAsync_refuses_a_memory_that_holds_a_secret_or_personal_dataAsync` | Theory | Find async refuses a memory that holds a secret or personal data. |
| `FindAsync_lets_an_ordinary_fact_throughAsync`                         | Theory | Find async lets an ordinary fact through.                         |

### AskJoobySqlQueryClientTests

`Jube.Test.AskJooby.AskJoobySqlQueryClientTests` — 12 tests. Category: AskJooby. Collection: Database.

| Test                                                                    | Kind | Verifies                                                                   |
|-------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `RejectsAQueryReferencingAnUnregisteredTableAsync`                      | Fact | Rejects a query referencing an unregistered table.                         |
| `RejectsANonSelectStatementAsync`                                       | Fact | Rejects a non select statement.                                            |
| `LogsARejectedQueryWithTheFailureDetailsAsync`                          | Fact | Logs a rejected query with the failure details.                            |
| `ValidateReportsTheColumnTheDatabaseCannotResolveWithoutExecutingAsync` | Fact | Validate reports the column the database cannot resolve without executing. |
| `ValidateAcceptsAStatementThePlannerCanResolveAsync`                    | Fact | Validate accepts a statement the planner can resolve.                      |
| `ValidateRefusesAnUnregisteredTableExactlyAsExecutionWouldAsync`        | Fact | Validate refuses an unregistered table exactly as execution would.         |
| `ExplainReturnsAPlanAndACostWithoutReadingAnyRowAsync`                  | Fact | Explain returns a plan and a cost without reading any row.                 |
| `TableStatisticsNamesOnlyTheTablesTheStatementActuallyTouchesAsync`     | Fact | Table statistics names only the tables the statement actually touches.     |
| `TableStatisticsSizesNothingForATableTheCallerMayNotQueryAsync`         | Fact | Table statistics sizes nothing for a table the caller may not query.       |
| `NoCallerCanReachARawCatalogueViewThroughDynamicSqlAsync`               | Fact | No caller can reach a raw catalogue view through dynamic sql.              |
| `QueryHistoryReturnsAggregatesOrSaysThereIsNoneAsync`                   | Fact | Query history returns aggregates or says there is none.                    |
| `LoggingAFailedLookupNeverMasksTheOriginalExceptionAsync`               | Fact | Logging a failed lookup never masks the original exception.                |

### AskJoobyStepIconReplayTests

`Jube.Test.AskJooby.AskJoobyStepIconReplayTests` — 4 tests. Category: Unit.

| Test                                                  | Kind | Verifies                                                      |
|-------------------------------------------------------|------|---------------------------------------------------------------|
| `AStoredStepReplaysWithTheIconItWasStreamedWith`      | Fact | A stored step replays with the icon it was streamed with.     |
| `AStepRecordedBeforeIconsExistedReplaysWithNoIcon`    | Fact | A step recorded before icons existed replays with no icon.    |
| `AStoredStepReplaysWithTheDetailItWasStreamedWith`    | Fact | A stored step replays with the detail it was streamed with.   |
| `AStepRecordedBeforeDetailExistedReplaysWithNoDetail` | Fact | A step recorded before detail existed replays with no detail. |

### AskJoobyTurnTrackerTests

`Jube.Test.AskJooby.AskJoobyTurnTrackerTests` — 5 tests. Category: Unit.

| Test                                                                       | Kind | Verifies                                                                                 |
|----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `ABegunConversationIsRunningUntilItIsDisposed`                             | Fact | A begun conversation is running until it is disposed.                                    |
| `TwoOverlappingTurnsOnTheSameConversationBothHaveToEndBeforeItIsIdleAgain` | Fact | Two overlapping turns on the same conversation both have to end before it is idle again. |
| `DisposingTwiceIsHarmless`                                                 | Fact | Disposing twice is harmless.                                                             |
| `OneConversationRunningNeverReportsAnotherAsBusy`                          | Fact | One conversation running never reports another as busy.                                  |
| `AConversationNeverBegunIsNotRunning`                                      | Fact | A conversation never begun is not running.                                               |

### AskJoobyValidationModelCoherenceTests

`Jube.Test.AskJooby.AskJoobyValidationModelCoherenceTests` — 2 tests. Category: AskJoobyLive.

| Test                                                                   | Kind | Verifies                                                                   |
|------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `ValidationModelFollowsASimpleDirectInstructionAsync`                  | Fact | Validation model follows a simple direct instruction.                      |
| `ValidationModelProducesNonDegenerateProseForAnOpenEndedQuestionAsync` | Fact | Validation model produces non degenerate prose for an open ended question. |

### BasicAgentRunnableThinkingTests

`Jube.Test.AskJooby.BasicAgentRunnableThinkingTests` — 2 tests. Category: AskJooby.

| Test                                                                 | Kind | Verifies                                                                 |
|----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `SurfacesReasoningContentAsThinkingItemsWhenEnableThinkingIsOnAsync` | Fact | Surfaces reasoning content as thinking items when enable thinking is on. |
| `NeverSurfacesReasoningContentWhenEnableThinkingIsOffAsync`          | Fact | Never surfaces reasoning content when enable thinking is off.            |

### BgeRerankerTests

`Jube.Test.AskJooby.BgeRerankerTests` — 11 tests. Category: AskJooby.

| Test                                                                   | Kind | Verifies                                                                     |
|------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `TheCohereShapeIsReadAsync`                                            | Fact | The cohere shape is read.                                                    |
| `TheTextEmbeddingsInferenceShapeIsReadAsync`                           | Fact | The text embeddings inference shape is read.                                 |
| `CandidatesComeBackBestFirstWhateverOrderTheProviderSentAsync`         | Fact | Candidates come back best first whatever order the provider sent.            |
| `AnIndexOutsideTheCandidatesIsDiscardedRatherThanThrowingAsync`        | Fact | An index outside the candidates is discarded rather than throwing.           |
| `ARefusalIsReportedAsNotHavingRunRatherThanAsRejectingEverythingAsync` | Fact | A refusal is reported as not having run rather than as rejecting everything. |
| `AnUnreachableEndpointIsReportedAsNotHavingRunAsync`                   | Fact | An unreachable endpoint is reported as not having run.                       |
| `AnAnswerInNoRecognisedShapeIsReportedAsNotHavingRunAsync`             | Fact | An answer in no recognised shape is reported as not having run.              |
| `NothingToRankIsNotACallAsync`                                         | Fact | Nothing to rank is not a call.                                               |
| `AUsageTokenCountInTheResponseIsCapturedAsync`                         | Fact | A usage token count in the response is captured.                             |
| `NoUsageFieldLeavesTokensComparedNullAsync`                            | Fact | No usage field leaves tokens compared null.                                  |
| `TheBareArrayShapeNeverCarriesUsageAsync`                              | Fact | The bare array shape never carries usage.                                    |

### BoundToolSurfaceDecorationTests

`Jube.Test.AskJooby.BoundToolSurfaceDecorationTests` — 6 tests. Category: Unit.

| Test                                                                            | Kind | Verifies                                                                                      |
|---------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `EveryAgentHandedTheRuleExtensionOperationsIsToldWhatTheyAreFor`                | Fact | Every agent handed the rule extension operations is told what they are for.                   |
| `NoAgentIsToldAboutTheRuleExtensionOperationsWithoutHoldingThem`                | Fact | No agent is told about the rule extension operations without holding them.                    |
| `EveryAgentHoldingARuleExtensionValidationIsToldToValidate`                     | Fact | Every agent holding a rule extension validation is told to validate.                          |
| `EveryComputationAnalystIsToldItsOperationsTakeTheNumbersThemselves`            | Fact | Every computation analyst is told its operations take the numbers themselves.                 |
| `EveryTrainingAgentIsToldItCanCancelARunItOpened`                               | Fact | Every training agent is told it can cancel a run it opened.                                   |
| `TheDelegationSummaryOfAnAgentWithAnAddedSurfaceSaysSoRatherThanUnderstatingIt` | Fact | The delegation summary of an agent with an added surface says so rather than understating it. |

### CacheAssembledTableToolTests

`Jube.Test.AskJooby.CacheAssembledTableToolTests` — 4 tests. Category: AskJooby.

| Test                                                      | Kind | Verifies                                                    |
|-----------------------------------------------------------|------|-------------------------------------------------------------|
| `CacheAssembledTableReturnsAPointerAndAPreviewAsync`      | Fact | Cache assembled table returns a pointer and a preview.      |
| `CacheAssembledTableRoundTripsThroughCachedDataToolAsync` | Fact | Cache assembled table round trips through cached data tool. |
| `CacheAssembledTableRejectsRowsWithDifferingKeysAsync`    | Fact | Cache assembled table rejects rows with differing keys.     |
| `CacheAssembledTableRejectsAnEmptyArrayAsync`             | Fact | Cache assembled table rejects an empty array.               |

### CachedDataToolAdvancedAnalyticsPointerTests

`Jube.Test.AskJooby.CachedDataToolAdvancedAnalyticsPointerTests` — 2 tests. Category: AskJooby. Collection: Database.

| Test                                                                     | Kind | Verifies                                                                         |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `InspectPreviewAndQueryAllWorkAgainstAnAdvancedAnalyticsPointerAsync`    | Fact | Inspect preview and query all work against an advanced analytics pointer.        |
| `InspectPreviewAndQueryAllStillWorkAgainstATblPointerOnTheSameToolAsync` | Fact | Inspect preview and query all still work against a tbl pointer on the same tool. |

### CachedDataToolTests

`Jube.Test.AskJooby.CachedDataToolTests` — 19 tests. Category: AskJooby.

| Test                                                                            | Kind | Verifies                                                                                 |
|---------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `InspectDataSchemaReportsRowCountColumnCountAndNullCountsAsync`                 | Fact | Inspect data schema reports row count column count and null counts.                      |
| `InspectDataSchemaReportsNotFoundForAMissingPointerAsync`                       | Fact | Inspect data schema reports not found for a missing pointer.                             |
| `PreviewDataRowsClampsAboveTheCeilingAsync`                                     | Fact | Preview data rows clamps above the ceiling.                                              |
| `PreviewDataRowsClampsBelowOneAsync`                                            | Fact | Preview data rows clamps below one.                                                      |
| `InspectDataSchemaReportsWhatReadingTheWholeTableWouldCostAsync`                | Fact | Inspect data schema reports what reading the whole table would cost.                     |
| `InspectDataSchemaScalesTheReportedCostWithTheSizeOfTheTableAsync`              | Fact | Inspect data schema scales the reported cost with the size of the table.                 |
| `PreviewDataRowsWalksPastTheStartOfTheTableWithAnOffsetAsync`                   | Fact | Preview data rows walks past the start of the table with an offset.                      |
| `PreviewDataRowsSaysSoRatherThanReturningNothingWhenTheOffsetIsPastTheEndAsync` | Fact | Preview data rows says so rather than returning nothing when the offset is past the end. |
| `PreviewDataRowsTreatsANegativeOffsetAsTheStartAsync`                           | Fact | Preview data rows treats a negative offset as the start.                                 |
| `QueryCachedDataFiltersOnAnEqualityConditionAsync`                              | Fact | Query cached data filters on an equality condition.                                      |
| `QueryCachedDataFiltersOnANumericGreaterThanConditionAsync`                     | Fact | Query cached data filters on a numeric greater than condition.                           |
| `QueryCachedDataCombinesConditionsWithAndAsync`                                 | Fact | Query cached data combines conditions with and.                                          |
| `QueryCachedDataSupportsContainsAsync`                                          | Fact | Query cached data supports contains.                                                     |
| `QueryCachedDataReportsNoMatchesDistinctlyAsync`                                | Fact | Query cached data reports no matches distinctly.                                         |
| `QueryCachedDataThrowsForAnUnknownColumnAsync`                                  | Fact | Query cached data throws for an unknown column.                                          |
| `QueryCachedDataThrowsForAnUnparseableConditionAsync`                           | Fact | Query cached data throws for an unparseable condition.                                   |
| `QueryCachedDataReportsNotFoundForAMissingPointerAsync`                         | Fact | Query cached data reports not found for a missing pointer.                               |
| `PreviewDataRowsOrdersColumnsByTheTableNotTheRowAsync`                          | Fact | Preview data rows orders columns by the table not the row.                               |
| `QueryCachedDataOrdersColumnsByTheTableNotTheRowAsync`                          | Fact | Query cached data orders columns by the table not the row.                               |

### CachedRowTextTests

`Jube.Test.AskJooby.CachedRowTextTests` — 7 tests. Category: AskJooby.

| Test                                                               | Kind | Verifies                                                                        |
|--------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `FormatTakesItsOrderFromTheColumnsAndNotFromTheRow`                | Fact | Format takes its order from the columns and not from the row.                   |
| `FormatReportsAColumnTheRowDoesNotCarryAsNullRatherThanDroppingIt` | Fact | Format reports a column the row does not carry as null rather than dropping it. |
| `FormatIgnoresAKeyThatIsNotAColumn`                                | Fact | Format ignores a key that is not a column.                                      |
| `FormatAppliesTheCallersValueRenderer`                             | Fact | Format applies the callers value renderer.                                      |
| `FormatFirstTakesTheLeadingColumnsInTableOrder`                    | Fact | Format first takes the leading columns in table order.                          |
| `FormatFirstReturnsEveryColumnWhenAskedForMoreThanThereAre`        | Fact | Format first returns every column when asked for more than there are.           |
| `FormatReturnsAnEmptyStringForATableWithNoColumns`                 | Fact | Format returns an empty string for a table with no columns.                     |

### ConversationHistoryToolTests

`Jube.Test.AskJooby.ConversationHistoryToolTests` — 1 test. Category: AskJooby.

| Test                                                                    | Kind | Verifies                                                                   |
|-------------------------------------------------------------------------|------|----------------------------------------------------------------------------|
| `SearchHistoryTruncatesAHoofprintLongerThanFourThousandCharactersAsync` | Fact | Search history truncates a hoofprint longer than four thousand characters. |

### ConversationWindowTests

`Jube.Test.AskJooby.ConversationWindowTests` — 5 tests. Category: Unit.

| Test                                                       | Kind | Verifies                                                             |
|------------------------------------------------------------|------|----------------------------------------------------------------------|
| `WithNoCompactEverythingIsCarried`                         | Fact | With no compact everything is carried.                               |
| `ACompactStandsInForWhatItCoveredAndTheRestIsCarried`      | Fact | A compact stands in for what it covered and the rest is carried.     |
| `AMessageWrittenWhileCompactionWasStillRunningIsNotLost`   | Fact | A message written while compaction was still running is not lost.    |
| `OnlyTheNewestCompactIsCarriedBecauseItSubsumesTheOlder`   | Fact | Only the newest compact is carried because it subsumes the older.    |
| `ACompactWrittenBeforeTheAnchorExistedStillBehavesAsItDid` | Fact | A compact written before the anchor existed still behaves as it did. |

### DelimitedTextTableTests

`Jube.Test.AskJooby.DelimitedTextTableTests` — 7 tests. Category: Unit.

| Test                                                            | Kind | Verifies                                                                     |
|-----------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `AHeaderAndRowsBecomeColumnsAndRows`                            | Fact | A header and rows become columns and rows.                                   |
| `ADelimiterInsideAQuotedFieldDoesNotStartANewColumn`            | Fact | A delimiter inside a quoted field does not start a new column.               |
| `ADoubledQuoteIsOneQuoteAndANewlineInsideQuotesStaysInTheField` | Fact | A doubled quote is one quote and a newline inside quotes stays in the field. |
| `TabsAndSemicolonsAreRecognisedAsDelimitersToo`                 | Fact | Tabs and semicolons are recognised as delimiters too.                        |
| `AShortRowIsPaddedRatherThanRejected`                           | Fact | A short row is padded rather than rejected.                                  |
| `DuplicateHeaderNamesAreMadeUniqueSoNoColumnIsLost`             | Fact | Duplicate header names are made unique so no column is lost.                 |
| `ProseIsNotMistakenForATable`                                   | Fact | Prose is not mistaken for a table.                                           |

### DemoDatasetToolTests

`Jube.Test.AskJooby.DemoDatasetToolTests` — 6 tests. Category: AskJooby.

| Test                                                                     | Kind | Verifies                                                                         |
|--------------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `TheEmbeddedDemoDatasetLoadsFromTheAssemblyRatherThanAFilePath`          | Fact | The embedded demo dataset loads from the assembly rather than a file path.       |
| `EveryRowCarriesEveryColumnSoTheSampleIsRectangular`                     | Fact | Every row carries every column so the sample is rectangular.                     |
| `TheLabelColumnHoldsBothOutcomesAndNothingElse`                          | Fact | The label column holds both outcomes and nothing else.                           |
| `ValuesParseAsNumbersRatherThanStayingAsText`                            | Fact | Values parse as numbers rather than staying as text.                             |
| `GetDemoDatasetHandsBackAPointerAndNotTheRowsAsync`                      | Fact | Get demo dataset hands back a pointer and not the rows.                          |
| `GetDemoDatasetNamesTheDatasetsThatExistWhenAskedForOneThatDoesNotAsync` | Fact | Get demo dataset names the datasets that exist when asked for one that does not. |

### DiffToolTests

`Jube.Test.AskJooby.DiffToolTests` — 11 tests. Category: Unit.

| Test                                                                       | Kind | Verifies                                                                                 |
|----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `IdenticalInputsReturnAnEmptyArray`                                        | Fact | Identical inputs return an empty array.                                                  |
| `AChangedLineIsReportedAsReplacedWithBothLineNumbersAndBothTexts`          | Fact | A changed line is reported as replaced with both line numbers and both texts.            |
| `APureDeletionHasNoNewSideAndNoLineNew`                                    | Fact | A pure deletion has no new side and no line new.                                         |
| `APureInsertionHasNoOldSideAndNoLineOld`                                   | Fact | A pure insertion has no old side and no line old.                                        |
| `AnUnequalSizedChangedBlockPairsPositionallyThenSpillsIntoInserts`         | Fact | An unequal sized changed block pairs positionally then spills into inserts.              |
| `ASingleTrailingNewlineOnOneSideIsNotReportedAsAChange`                    | Fact | A single trailing newline on one side is not reported as a change.                       |
| `ASecondTrailingNewlineIsStillARealBlankLineAndIsReported`                 | Fact | A second trailing newline is still a real blank line and is reported.                    |
| `ComparisonIsCaseAndWhitespaceExact`                                       | Fact | Comparison is case and whitespace exact.                                                 |
| `OversizedInputReturnsAPlainRefusalNotJson`                                | Fact | Oversized input returns a plain refusal not json.                                        |
| `NullInputsAreTreatedAsEmptyRatherThanThrowing`                            | Fact | Null inputs are treated as empty rather than throwing.                                   |
| `MoreChangedLinesThanTheCapEndsWithATruncatedEntryInsteadOfGoingOnForever` | Fact | More changed lines than the cap ends with a truncated entry instead of going on forever. |

### DynamicSqlCommonTableExpressionTests

`Jube.Test.AskJooby.DynamicSqlCommonTableExpressionTests` — 8 tests. Category: AskJooby.

| Test                                                                          | Kind | Verifies                                                                                    |
|-------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------|
| `ACommonTableExpressionNameIsNotJudgedAgainstTheAllowList`                    | Fact | A common table expression name is not judged against the allow list.                        |
| `TheExactStatementTheAgentWasRefusedNowRewrites`                              | Fact | The exact statement the agent was refused now rewrites.                                     |
| `OneExpressionCanBuildOnAnEarlierOne`                                         | Fact | One expression can build on an earlier one.                                                 |
| `ARecursiveExpressionCanReferenceItself`                                      | Fact | A recursive expression can reference itself.                                                |
| `ANonRecursiveExpressionCannotSeeItselfSoAnInnerReferenceIsStillTheRealTable` | Fact | A non recursive expression cannot see itself so an inner reference is still the real table. |
| `AnUnregisteredTableInsideAnExpressionBodyIsStillRejected`                    | Fact | An unregistered table inside an expression body is still rejected.                          |
| `AnExpressionMayNotTakeTheNameOfARegisteredTable`                             | Fact | An expression may not take the name of a registered table.                                  |
| `CollectedTableReferencesExcludeExpressionNames`                              | Fact | Collected table references exclude expression names.                                        |

### DynamicSqlNamedParameterRewriteTests

`Jube.Test.AskJooby.DynamicSqlNamedParameterRewriteTests` — 7 tests. Category: Unit.

| Test                                                     | Kind | Verifies                                                         |
|----------------------------------------------------------|------|------------------------------------------------------------------|
| `TheSeededPieChartCommandRewritesWithItsParameterIntact` | Fact | The seeded pie chart command rewrites with its parameter intact. |
| `ParameterCaseAndUnderscoresSurviveTheRoundTrip`         | Fact | Parameter case and underscores survive the round trip.           |
| `MoreThanNineParametersRestoreWithoutPrefixCollision`    | Fact | More than nine parameters restore without prefix collision.      |
| `AnAtSignInsideAStringLiteralIsRestoredVerbatim`         | Fact | An at sign inside a string literal is restored verbatim.         |
| `PositionalParametersAreRefused`                         | Fact | Positional parameters are refused.                               |
| `AnUnregisteredTableIsRefusedEvenWithParameters`         | Fact | An unregistered table is refused even with parameters.           |
| `TheRegistryTemplatesAllRewriteTheirOwnTable`            | Fact | The registry templates all rewrite their own table.              |

### DynamicSqlQueryLogRepositoryTests

`Jube.Test.AskJooby.DynamicSqlQueryLogRepositoryTests` — 2 tests. Category: AskJooby. Collection: Database.

| Test                                                    | Kind | Verifies                                                   |
|---------------------------------------------------------|------|------------------------------------------------------------|
| `InsertAsyncPersistsASuccessfulRunAsync`                | Fact | Insert async persists a successful run.                    |
| `InsertAsyncPersistsAFailedRunWithTheErrorMessageAsync` | Fact | Insert async persists a failed run with the error message. |

### DynamicSqlRoleFilteringTests

`Jube.Test.AskJooby.DynamicSqlRoleFilteringTests` — 2 tests. Category: AskJooby. Collection: Database.

| Test                                                                                                 | Kind | Verifies                                                                                                           |
|------------------------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------------------------------|
| `EntityAnalysisModelWithARoleAssignedIsOnlyVisibleToAUserHoldingThatRoleNotJustAnyTenantMemberAsync` | Fact | Entity analysis model with a role assigned is only visible to a user holding that role not just any tenant member. |
| `EntityAnalysisModelWithNoRolesAssignedAtAllIsVisibleToAnyTenantMemberAsync`                         | Fact | Entity analysis model with no roles assigned at all is visible to any tenant member.                               |

### DynamicSqlTableAccessTests

`Jube.Test.AskJooby.DynamicSqlTableAccessTests` — 19 tests. Category: AskJooby.

| Test                                                                                 | Kind | Verifies                                                                                       |
|--------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `ScopedSqlSurvivesTheValidatorTheAgentPathActuallyRunsItThrough`                     | Fact | Scoped sql survives the validator the agent path actually runs it through.                     |
| `TheProtectedRelationGateStillBlocksScopedSqlAndIsWhyTheAgentPathMustNotUseIt`       | Fact | The protected relation gate still blocks scoped sql and is why the agent path must not use it. |
| `TheCredentialColumnsAreProjectedAwaySoReachingTheTableCannotReachTheSecret`         | Fact | The credential columns are projected away so reaching the table cannot reach the secret.       |
| `TheRawGateRefusesCredentialTablesEvenThoughTheAllowListRegistersThem`               | Fact | The raw gate refuses credential tables even though the allow list registers them.              |
| `TheRawGateStillAdmitsAnOrdinaryTableTheCallerIsEntitledTo`                          | Fact | The raw gate still admits an ordinary table the caller is entitled to.                         |
| `BuildAllowedTablesIncludesAKnownRegisteredTableWhenPermissionGranted`               | Fact | Build allowed tables includes a known registered table when permission granted.                |
| `BuildAllowedTablesIncludesCaseFamilyTablesWhenPermission1IsGranted`                 | Fact | Build allowed tables includes case family tables when permission1 is granted.                  |
| `BuildAllowedTablesIncludesCaseCreationObservabilityTablesWhenPermission27IsGranted` | Fact | Build allowed tables includes case creation observability tables when permission27 is granted. |
| `BuildAllowedTablesOmitsCaseFamilyTablesWhenNeitherPermissionIsGranted`              | Fact | Build allowed tables omits case family tables when neither permission is granted.              |
| `BuildAllowedTablesLooksUpByTableNameCaseInsensitively`                              | Fact | Build allowed tables looks up by table name case insensitively.                                |
| `BuildAllowedTablesAddsARoleExistsCheckOnTopOfTenantForARoleGatedTable`              | Fact | Build allowed tables adds a role exists check on top of tenant for a role gated table.         |
| `BuildAllowedTablesOmitsEverythingWhenPermissionDenied`                              | Fact | Build allowed tables omits everything when permission denied.                                  |
| `BuildAllowedTablesThrowsForAnEmptyUsername`                                         | Fact | Build allowed tables throws for an empty username.                                             |
| `BuildAllowedTablesThrowsForANullUsername`                                           | Fact | Build allowed tables throws for a null username.                                               |
| `ThePointerCacheIsNotQueryableWithoutAConversationToBindItTo`                        | Fact | The pointer cache is not queryable without a conversation to bind it to.                       |
| `ThePointerCacheIsBoundToTheCallersOwnConversation`                                  | Fact | The pointer cache is bound to the callers own conversation.                                    |
| `ThePointerCacheHonoursTheSameExpiryTheToolPathEnforces`                             | Fact | The pointer cache honours the same expiry the tool path enforces.                              |
| `TheConversationIdIsSplicedIntoExactlyTheConversationOwnedTables`                    | Fact | The conversation id is spliced into exactly the conversation owned tables.                     |
| `NoTemplateIsLeftHoldingAnUnresolvedToken`                                           | Fact | No template is left holding an unresolved token.                                               |

### DynamicSqlTablePermissionParityTests

`Jube.Test.AskJooby.DynamicSqlTablePermissionParityTests` — 4 tests. Category: Unit.

| Test                                                                           | Kind | Verifies                                                                                 |
|--------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------|
| `EveryRegisteredTableWithAnOwningServiceDemandsThatServicesOwnPermissions`     | Fact | Every registered table with an owning service demands that services own permissions.     |
| `TheCredentialTablesAreNotQueryableOnTheBareAssistantPermission`               | Fact | The credential tables are not queryable on the bare assistant permission.                |
| `TheExhaustiveFamilyDemandsTheExhaustivePagePermission`                        | Fact | The exhaustive family demands the exhaustive page permission.                            |
| `TheTablesWithDifferentlyNamedOwningServicesDemandThoseServicesOwnPermissions` | Fact | The tables with differently named owning services demand those services own permissions. |

### DynamicSqlTemplateSchemaSweepTests

`Jube.Test.AskJooby.DynamicSqlTemplateSchemaSweepTests` — 2 tests. Category: AskJooby. Collection: Database.

| Test                                                            | Kind | Verifies                                                            |
|-----------------------------------------------------------------|------|---------------------------------------------------------------------|
| `EveryRegisteredTemplateExplainsAgainstTheLiveSchemaAsync`      | Fact | Every registered template explains against the live schema.         |
| `EveryLiveTableIsEitherRegisteredOrBlockedInProtectedModeAsync` | Fact | Every live table is either registered or blocked in protected mode. |

### ExportCachedTableToolTests

`Jube.Test.AskJooby.ExportCachedTableToolTests` — 5 tests. Category: AskJooby.

| Test                                                                    | Kind | Verifies                                                                      |
|-------------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `ExportCachedTableToCsvWritesRealCsvBytesAndRecordsTheFileAsync`        | Fact | Export cached table to csv writes real csv bytes and records the file.        |
| `ExportCachedTableToCsvQuotesFieldsContainingCommasAndQuotesAsync`      | Fact | Export cached table to csv quotes fields containing commas and quotes.        |
| `ExportCachedTableToCsvReturnsAFriendlyMessageForAnUnknownPointerAsync` | Fact | Export cached table to csv returns a friendly message for an unknown pointer. |
| `ExportCachedTableToCsvRefusesAnEmptyTableAsync`                        | Fact | Export cached table to csv refuses an empty table.                            |
| `ExportCachedTableToCsvSanitizesAnUnsafeFileNameAsync`                  | Fact | Export cached table to csv sanitizes an unsafe file name.                     |

### GetAskJoobyConversationRetrievedDocsQueryTests

`Jube.Test.AskJooby.GetAskJoobyConversationRetrievedDocsQueryTests` — 5 tests. Category: AskJooby. Collection: Database.

| Test                                                          | Kind | Verifies                                                          |
|---------------------------------------------------------------|------|-------------------------------------------------------------------|
| `DeduplicatesTheSameBiteTextRetrievedByMultipleSearchesAsync` | Fact | Deduplicates the same bite text retrieved by multiple searches.   |
| `ExcludesNoMatchingBitesFoundResultsAsync`                    | Fact | Excludes no matching bites found results.                         |
| `NeverReturnsAnotherConversationsSearchResultsAsync`          | Fact | Never returns another conversations search results.               |
| `CapsTheResultCountEvenWhenFarMoreDistinctSearchesExistAsync` | Fact | Caps the result count even when far more distinct searches exist. |
| `PrefersTheMostRecentDistinctSearchesWhenCappingAsync`        | Fact | Prefers the most recent distinct searches when capping.           |

### GraphWorkflowToolTests

`Jube.Test.AskJooby.GraphWorkflowToolTests` — 9 tests. Category: Unit.

| Test                                                                          | Kind | Verifies                                                                                   |
|-------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `ATaskInTheCallersOwnWordsFindsTheWorkflowForIt`                              | Fact | A task in the callers own words finds the workflow for it.                                 |
| `ATrainingTaskInTheCallersOwnWordsFindsTheTrainingWorkflow`                   | Fact | A training task in the callers own words finds the training workflow.                      |
| `EveryWorkflowCanBeFetchedByTheIdTheSearchReturns`                            | Fact | Every workflow can be fetched by the id the search returns.                                |
| `TheStepsCarryTheEdgesAndNotJustTheProse`                                     | Fact | The steps carry the edges and not just the prose.                                          |
| `TheTtlCounterReferenceConventionIsInTheGraphWhereAnAgentWillSeeIt`           | Fact | The ttl counter reference convention is in the graph where an agent will see it.           |
| `AnOperatorStepSaysSoRatherThanNamingAnAgentThatCouldActOnIt`                 | Fact | An operator step says so rather than naming an agent that could act on it.                 |
| `AnUnknownIdAnswersWithTheIdsThatDoExist`                                     | Fact | An unknown id answers with the ids that do exist.                                          |
| `ATaskMatchingNothingSaysSoRatherThanGuessing`                                | Fact | A task matching nothing says so rather than guessing.                                      |
| `AQuestionMadeEntirelyOfCommonWordsGetsTheCatalogueRatherThanAConfidentMatch` | Fact | A question made entirely of common words gets the catalogue rather than a confident match. |

### InstructionsBuilderTests

`Jube.Test.AskJooby.InstructionsBuilderTests` — 5 tests. Category: AskJooby.

| Test                                                                    | Kind | Verifies                                                                               |
|-------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `ReachedSpawnDepthLimitAddsAnExplicitNoteNamingTheConfiguredCeiling`    | Fact | Reached spawn depth limit adds an explicit note naming the configured ceiling.         |
| `NoSpawnDepthLimitMeansNoCeilingNoteAtAll`                              | Fact | No spawn depth limit means no ceiling note at all.                                     |
| `TheCeilingNoteSitsBeforeTheGenericRecapTextSoItReadsAsAnExceptionToIt` | Fact | The ceiling note sits before the generic recap text so it reads as an exception to it. |
| `PageContextAddsPurposeAndLikelyAsksToEveryAgentsOwnInstructions`       | Fact | Page context adds purpose and likely asks to every agents own instructions.            |
| `NoPageContextMeansNoPageNoteAtAll`                                     | Fact | No page context means no page note at all.                                             |

### LargeResultPointerTests

`Jube.Test.AskJooby.LargeResultPointerTests` — 4 tests. Category: AskJooby.

| Test                                                                   | Kind | Verifies                                                                        |
|------------------------------------------------------------------------|------|---------------------------------------------------------------------------------|
| `APayloadWrappingItsRowsIsCachedAsThoseRowsAndNotAsOneOpaqueCellAsync` | Fact | A payload wrapping its rows is cached as those rows and not as one opaque cell. |
| `TheModelIsToldTheRealRowCountRatherThanOneAsync`                      | Fact | The model is told the real row count rather than one.                           |
| `ASmallResultIsStillReturnedWholeRatherThanPointerisedAsync`           | Fact | A small result is still returned whole rather than pointerised.                 |
| `APayloadWithNoRowsArrayFallsBackToASingleReadableCellAsync`           | Fact | A payload with no rows array falls back to a single readable cell.              |

### ListQueryableTablesFilterTests

`Jube.Test.AskJooby.ListQueryableTablesFilterTests` — 4 tests. Category: Unit.

| Test                                                              | Kind | Verifies                                                             |
|-------------------------------------------------------------------|------|----------------------------------------------------------------------|
| `AFilteredListingCarriesOnlyWhatWasAskedForAsync`                 | Fact | A filtered listing carries only what was asked for.                  |
| `AFilteredListingCarriesTheDocumentationCommentForEachMatchAsync` | Fact | A filtered listing carries the documentation comment for each match. |
| `AnUnfilteredListingStillWorksAndCarriesCommentsTooAsync`         | Fact | An unfiltered listing still works and carries comments too.          |
| `AFilterMatchingNothingSaysSoRatherThanInvitingAGuessAsync`       | Fact | A filter matching nothing says so rather than inviting a guess.      |

### MemoryToolTests

`Jube.Test.AskJooby.MemoryToolTests` — 18 tests. Category: AskJooby.

| Test                                                         | Kind | Verifies                                                        |
|--------------------------------------------------------------|------|-----------------------------------------------------------------|
| `RecallLabelsAThumbsUpMemoryAsACorrectExampleAsync`          | Fact | Recall labels a thumbs up memory as a correct example.          |
| `RecallLabelsAThumbsDownMemoryAsAnIncorrectExampleAsync`     | Fact | Recall labels a thumbs down memory as an incorrect example.     |
| `RecallLabelsAnUnreviewedMemoryAsAgentGeneratedAsync`        | Fact | Recall labels an unreviewed memory as agent generated.          |
| `RememberPassesNoExpiryByDefaultAsync`                       | Fact | Remember passes no expiry by default.                           |
| `RememberConvertsExpiresInMinutesToAFutureUtcTimestampAsync` | Fact | Remember converts expires in minutes to a future utc timestamp. |
| `RememberRejectsAnExpiryOnSystemScopeMemoriesAsync`          | Fact | Remember rejects an expiry on system scope memories.            |
| `RememberRejectsAnUnrecognisedScopeAsync`                    | Fact | Remember rejects an unrecognised scope.                         |
| `RememberAllowsAnExpiryOnConversationScopeMemoriesAsync`     | Fact | Remember allows an expiry on conversation scope memories.       |
| `RememberRejectsANonPositiveExpiryAsync`                     | Fact | Remember rejects a non positive expiry.                         |
| `RecallPassesNoSinceFilterByDefaultAsync`                    | Fact | Recall passes no since filter by default.                       |
| `RecallConvertsSinceDaysAgoToAPastUtcTimestampAsync`         | Fact | Recall converts since days ago to a past utc timestamp.         |
| `RecallRejectsANonPositiveSinceDaysAgoAsync`                 | Fact | Recall rejects a non positive since days ago.                   |
| `RememberDefaultsToFactKindAsync`                            | Fact | Remember defaults to fact kind.                                 |
| `RememberPassesReflectionKindThroughAsync`                   | Fact | Remember passes reflection kind through.                        |
| `RememberRejectsAnUnrecognisedKindAsync`                     | Fact | Remember rejects an unrecognised kind.                          |
| `RecallExcludesReflectionsByDefaultAsync`                    | Fact | Recall excludes reflections by default.                         |
| `RecallPassesIncludeReflectionsThroughWhenRequestedAsync`    | Fact | Recall passes include reflections through when requested.       |
| `RecallTagsAReflectionHitInItsOutputAsync`                   | Fact | Recall tags a reflection hit in its output.                     |

### MemoryVolatilityGuardTests

`Jube.Test.AskJooby.MemoryVolatilityGuardTests` — 2 tests. Category: Unit.

| Test                                                      | Kind   | Verifies                                                        |
|-----------------------------------------------------------|--------|-----------------------------------------------------------------|
| `ALiveValueIsRefusedAndTheModelIsToldToQueryInsteadAsync` | Theory | A live value is refused and the model is told to query instead. |
| `AGenericPlanOrParameterisedPatternIsAcceptedAsync`       | Theory | A generic plan or parameterised pattern is accepted.            |

### ModelAffinityPolicyTests

`Jube.Test.AskJooby.ModelAffinityPolicyTests` — 5 tests. Category: AskJooby.

| Test                                                   | Kind | Verifies                                                       |
|--------------------------------------------------------|------|----------------------------------------------------------------|
| `TheHeaderCarriesTheConversationWheneverOneIsInScope`  | Fact | The header carries the conversation whenever one is in scope.  |
| `NoConversationInScopeLeavesTheRequestToRouteNormally` | Fact | No conversation in scope leaves the request to route normally. |
| `TheConversationReachesWorkDoneOnAnotherThreadAsync`   | Fact | The conversation reaches work done on another thread.          |
| `EveryBranchOfAFanOutCarriesTheSameValueAsync`         | Fact | Every branch of a fan out carries the same value.              |
| `LeavingAConversationRestoresWhateverWasThereBefore`   | Fact | Leaving a conversation restores whatever was there before.     |

### OpenCorporatesClientTests

`Jube.Test.AskJooby.OpenCorporatesClientTests` — 13 tests. Category: AskJooby.

| Test                                                                  | Kind   | Verifies                                                                  |
|-----------------------------------------------------------------------|--------|---------------------------------------------------------------------------|
| `SearchCompanySendsTheQueryAndApiTokenAsync`                          | Fact   | Search company sends the query and api token.                             |
| `SearchCompanyParsesTheResultsAsync`                                  | Fact   | Search company parses the results.                                        |
| `SearchCompanyCarriesTheRawResponseVerbatimAsync`                     | Fact   | Search company carries the raw response verbatim.                         |
| `GetCompanySendsTheJurisdictionAndCompanyNumberInThePathAsync`        | Fact   | Get company sends the jurisdiction and company number in the path.        |
| `GetCompanyParsesTheCompanyAsync`                                     | Fact   | Get company parses the company.                                           |
| `GetCompanyReturnsNullRatherThanThrowingOnNotFoundAsync`              | Fact   | Get company returns null rather than throwing on not found.               |
| `SearchCompanyThrowsWithTheStatusCodeOnEveryDocumentedErrorAsync`     | Theory | Search company throws with the status code on every documented error.     |
| `SearchBeneficialOwnersSendsTheControlledEntityNameAndUboFilterAsync` | Fact   | Search beneficial owners sends the controlled entity name and ubo filter. |
| `SearchBeneficialOwnersOmitsTheOwnerFilterWhenNoneIsGivenAsync`       | Fact   | Search beneficial owners omits the owner filter when none is given.       |
| `SearchBeneficialOwnersParsesEachStatementAsync`                      | Fact   | Search beneficial owners parses each statement.                           |
| `SearchBeneficialOwnersParsesCleanlyWhenNoStatementsComeBackAsync`    | Fact   | Search beneficial owners parses cleanly when no statements come back.     |
| `RecordsEachOperationAgainstTheGivenRunWhenARecorderIsSuppliedAsync`  | Fact   | Records each operation against the given run when a recorder is supplied. |
| `RecordsNothingWhenNoRecorderIsSuppliedAsync`                         | Fact   | Records nothing when no recorder is supplied.                             |

### OpenCorporatesToolTests

`Jube.Test.AskJooby.OpenCorporatesToolTests` — 19 tests. Category: AskJooby.

| Test                                                                                 | Kind | Verifies                                                                                   |
|--------------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------------|
| `SearchCompanyByNameReportsAMachineParseableCountLineAsync`                          | Fact | Search company by name reports a machine parseable count line.                             |
| `SearchCompanyByNameReportsNoMatchesAsync`                                           | Fact | Search company by name reports no matches.                                                 |
| `SearchCompanyByNamePassesTheConfiguredDefaultMaxResultsThroughAsync`                | Fact | Search company by name passes the configured default max results through.                  |
| `SearchCompanyByNameUsesTheModelSuppliedMaxResultsOverTheConfiguredDefaultAsync`     | Fact | Search company by name uses the model supplied max results over the configured default.    |
| `SearchCompanyByNameClampsAModelSuppliedMaxResultsAboveTheCeilingAsync`              | Fact | Search company by name clamps a model supplied max results above the ceiling.              |
| `SearchCompanyByNameClampsAModelSuppliedMaxResultsBelowOneAsync`                     | Fact | Search company by name clamps a model supplied max results below one.                      |
| `SearchCompanyByNameDoesNotSendWhenTheQueryMatchesPromptInjectionScreeningAsync`     | Fact | Search company by name does not send when the query matches prompt injection screening.    |
| `SearchCompanyByNameRecordsARequestSideGuardRejectionUnderItsOwnPipelineNameAsync`   | Fact | Search company by name records a request side guard rejection under its own pipeline name. |
| `SearchCompanyByNameWithholdsACompanyWhoseNameMatchesPromptInjectionScreeningAsync`  | Fact | Search company by name withholds a company whose name matches prompt injection screening.  |
| `SearchCompanyByNameRecordsAnAggregateGuardRejectionWhenWithholdingCompaniesAsync`   | Fact | Search company by name records an aggregate guard rejection when withholding companies.    |
| `GetCompanyDetailsReportsNotFoundAsync`                                              | Fact | Get company details reports not found.                                                     |
| `GetCompanyDetailsReturnsTheRecordAsync`                                             | Fact | Get company details returns the record.                                                    |
| `GetCompanyDetailsDoesNotSendWhenAnArgumentMatchesPromptInjectionScreeningAsync`     | Fact | Get company details does not send when an argument matches prompt injection screening.     |
| `GetCompanyDetailsWithholdsARecordWhoseAddressMatchesPromptInjectionScreeningAsync`  | Fact | Get company details withholds a record whose address matches prompt injection screening.   |
| `SearchBeneficialOwnersReportsNoStatementsAsync`                                     | Fact | Search beneficial owners reports no statements.                                            |
| `SearchBeneficialOwnersReturnsEachStatementAsync`                                    | Fact | Search beneficial owners returns each statement.                                           |
| `SearchBeneficialOwnersDoesNotSendWhenTheQueryMatchesPromptInjectionScreeningAsync`  | Fact | Search beneficial owners does not send when the query matches prompt injection screening.  |
| `SearchBeneficialOwnersWithholdsAStatementWhoseControllingNameMatchesScreeningAsync` | Fact | Search beneficial owners withholds a statement whose controlling name matches screening.   |
| `PassesTheRecorderAndRunIdThroughToTheClientAsync`                                   | Fact | Passes the recorder and run id through to the client.                                      |

### PagePurposeCatalogueTests

`Jube.Test.AskJooby.PagePurposeCatalogueTests` — 3 tests. Category: Unit.

| Test                                                        | Kind | Verifies                                                              |
|-------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `EveryPageNameSentByTheClientIsCatalogedOrExplicitlyExempt` | Fact | Every page name sent by the client is cataloged or explicitly exempt. |
| `NoCataloguedPageNameIsAlsoListedAsExempt`                  | Fact | No catalogued page name is also listed as exempt.                     |
| `NoCataloguedPageNameHasGoneStaleAgainstTheClient`          | Fact | No catalogued page name has gone stale against the client.            |

### PlanToolTests

`Jube.Test.AskJooby.PlanToolTests` — 9 tests. Category: AskJooby.

| Test                                                        | Kind | Verifies                                                              |
|-------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `WritePlanRecordsTheRenderedPlanOnTheTurn`                  | Fact | Write plan records the rendered plan on the turn.                     |
| `WritePlanRejectsAnEmptyPlan`                               | Fact | Write plan rejects an empty plan.                                     |
| `WritePlanRejectsAPlanLongerThanTheCeiling`                 | Fact | Write plan rejects a plan longer than the ceiling.                    |
| `WritePlanRejectsAStepWithNoCheck`                          | Fact | Write plan rejects a step with no check.                              |
| `WritePlanFillsTheStableFromTheSpecialistAndRecordsIt`      | Fact | Write plan fills the stable from the specialist and records it.       |
| `WritePlanRefusesAStableThatDoesNotOwnTheSpecialist`        | Fact | Write plan refuses a stable that does not own the specialist.         |
| `AStablePlanIsRecordedAgainstThatStableAndNotTheRootPlan`   | Fact | A stable plan is recorded against that stable and not the root plan.  |
| `AStablePlanRefusesASpecialistThatBelongsToAnotherStable`   | Fact | A stable plan refuses a specialist that belongs to another stable.    |
| `WritePlanRefusesAnUnknownStableForASpecialistWithNoStable` | Fact | Write plan refuses an unknown stable for a specialist with no stable. |

### PlatformOpsWorkflowCoverageTests

`Jube.Test.AskJooby.PlatformOpsWorkflowCoverageTests` — 10 tests. Category: Unit.

| Test                                                             | Kind | Verifies                                                                  |
|------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `EveryWorkflowCarriesEnoughSemanticsToBeChosenWithoutThePasture` | Fact | Every workflow carries enough semantics to be chosen without the pasture. |
| `AnAgentNodeNamesARegisteredAgentAndAnOperatorNodeNamesNobody`   | Fact | An agent node names a registered agent and an operator node names nobody. |
| `EveryToolNamedAnywhereIsOneTheCatalogueCarries`                 | Fact | Every tool named anywhere is one the catalogue carries.                   |
| `EveryNodeIsOwnedByAnAgentThatActuallyHoldsTheToolsItNames`      | Fact | Every node is owned by an agent that actually holds the tools it names.   |
| `NoPlatformOperationsNodeClaimsToActOnThePlatform`               | Fact | No platform operations node claims to act on the platform.                |
| `EveryWorkflowHandsTheActOverWhereThereIsAnActToHandOver`        | Fact | Every workflow hands the act over where there is an act to hand over.     |
| `NoNodeRequiresAnArtefactNothingBeforeItProduces`                | Fact | No node requires an artefact nothing before it produces.                  |
| `EveryObservationalWorkflowTakesTheWideSampleBeforeTheRecentOne` | Fact | Every observational workflow takes the wide sample before the recent one. |
| `TheTriageWorkflowReadsTheStackFromTheBottom`                    | Fact | The triage workflow reads the stack from the bottom.                      |
| `TheBackupWorkflowStatesItsOwnBlindnessBeforeAnythingElse`       | Fact | The backup workflow states its own blindness before anything else.        |

### PreservationSnapshotCoverageTests

`Jube.Test.AskJooby.PreservationSnapshotCoverageTests` — 2 tests. Category: Unit.

| Test                                                            | Kind | Verifies                                                                 |
|-----------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `TheRequestExposesEverySwitchTheExportItselfSupports`           | Fact | The request exposes every switch the export itself supports.             |
| `EverythingButTheLargestIsIncludedUnlessTheCallerSaysOtherwise` | Fact | Everything but the largest is included unless the caller says otherwise. |

### PromptCacheUsageKeyTests

`Jube.Test.AskJooby.PromptCacheUsageKeyTests` — 5 tests. Category: AskJooby.

| Test                                                           | Kind | Verifies                                                                |
|----------------------------------------------------------------|------|-------------------------------------------------------------------------|
| `TheHitCountIsReadAndTheMissCountBesideItIsNot`                | Fact | The hit count is read and the miss count beside it is not.              |
| `AMissCountOnItsOwnIsNeverMistakenForACachedFigure`            | Fact | A miss count on its own is never mistaken for a cached figure.          |
| `ACacheWriteIsNotACacheRead`                                   | Fact | A cache write is not a cache read.                                      |
| `AProviderReportingNothingYieldsNothingRatherThanAZeroReading` | Fact | A provider reporting nothing yields nothing rather than a zero reading. |
| `TheOpenAiCompatibleDetailSpellingIsRead`                      | Fact | The open ai compatible detail spelling is read.                         |

### PromptCacheUsageTests

`Jube.Test.AskJooby.PromptCacheUsageTests` — 6 tests. Category: AskJooby.

| Test                                                               | Kind | Verifies                                                                       |
|--------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `TheProvidersOwnCacheReadCountIsTakenWhateverItCallsIt`            | Fact | The providers own cache read count is taken whatever it calls it.              |
| `AReportedMissIsZeroAndNotSilenceBecauseTheTwoMeanDifferentThings` | Fact | A reported miss is zero and not silence because the two mean different things. |
| `ACacheWriteIsNeverMistakenForARead`                               | Fact | A cache write is never mistaken for a read.                                    |
| `AReadIsStillFoundAlongsideAWrite`                                 | Fact | A read is still found alongside a write.                                       |
| `AnUnrelatedCountIsNotReadAsACacheFigure`                          | Fact | An unrelated count is not read as a cache figure.                              |
| `NoUsageAtAllIsReportedAsNothingRatherThanThrowing`                | Fact | No usage at all is reported as nothing rather than throwing.                   |

### PromptPrefixStabilityTests

`Jube.Test.AskJooby.PromptPrefixStabilityTests` — 3 tests. Category: AskJooby.

| Test                                                                       | Kind | Verifies                                                                               |
|----------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------|
| `TwoValidationsShareEveryByteOfTheCriteriaBeforeAnythingVolatileAppears`   | Fact | Two validations share every byte of the criteria before anything volatile appears.     |
| `TheValidationFormatReminderStillComesLastSoItIsTheModelsFinalInstruction` | Fact | The validation format reminder still comes last so it is the models final instruction. |
| `TwoRevisionsShareEveryByteOfTheMethodBeforeAnythingVolatileAppears`       | Fact | Two revisions share every byte of the method before anything volatile appears.         |

### PromptPrefixTests

`Jube.Test.AskJooby.PromptPrefixTests` — 5 tests. Category: AskJooby.

| Test                                                              | Kind | Verifies                                                                      |
|-------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `TheSameSystemPromptHashesTheSameHoweverTheRestOfTheTurnDiffers`  | Fact | The same system prompt hashes the same however the rest of the turn differs.  |
| `OneCharacterAtTheStartChangesTheHashBecauseItAlsoBreaksTheCache` | Fact | One character at the start changes the hash because it also breaks the cache. |
| `TheSystemMessageIsTakenEvenWhenItIsNotFirstInTheList`            | Fact | The system message is taken even when it is not first in the list.            |
| `WithNoSystemMessageTheFirstMessageIsHashedInstead`               | Fact | With no system message the first message is hashed instead.                   |
| `NothingToHashIsReportedAsNothingRatherThanThrowing`              | Fact | Nothing to hash is reported as nothing rather than throwing.                  |

### ProviderMarkupLeakTests

`Jube.Test.AskJooby.ProviderMarkupLeakTests` — 7 tests. Category: AskJooby.

| Test                                                             | Kind | Verifies                                                            |
|------------------------------------------------------------------|------|---------------------------------------------------------------------|
| `ASwallowedToolCallIsCutOutOfTheArgumentItLeakedIntoAsync`       | Fact | A swallowed tool call is cut out of the argument it leaked into.    |
| `TheModelIsToldTheCallDidNotRunAndWhichOneItWasAsync`            | Fact | The model is told the call did not run and which one it was.        |
| `DeepSeeksOwnSeparatorFormIsRecognisedTooAsync`                  | Fact | Deep seeks own separator form is recognised too.                    |
| `MarkupThatNamesNothingStillWarnsRatherThanPassingSilentlyAsync` | Fact | Markup that names nothing still warns rather than passing silently. |
| `AnOrdinaryArgumentIsUntouchedAndUnannotatedAsync`               | Fact | An ordinary argument is untouched and unannotated.                  |
| `AnAsciiPipeIsNotMistakenForAControlTokenAsync`                  | Fact | An ascii pipe is not mistaken for a control token.                  |
| `StripAndNameReadTheLeakWithoutExecutingAnythingFromIt`          | Fact | Strip and name read the leak without executing anything from it.    |

### ProviderMarkupTruncationTests

`Jube.Test.AskJooby.ProviderMarkupTruncationTests` — 9 tests. Category: AskJooby.

| Test                                                                          | Kind   | Verifies                                                                              |
|-------------------------------------------------------------------------------|--------|---------------------------------------------------------------------------------------|
| `LooksTruncatedMatchesOnlyADanglingTrailingWord`                              | Theory | Looks truncated matches only a dangling trailing word.                                |
| `LooksTruncatedIsNullSafe`                                                    | Fact   | Looks truncated is null safe.                                                         |
| `ANonStreamingReplyThatStopsOnADanglingWordIsReaskedAndStitchedTogetherAsync` | Fact   | A non streaming reply that stops on a dangling word is reasked and stitched together. |
| `ANonStreamingReplyThatEndsCleanlyIsNeverReaskedAsync`                        | Fact   | A non streaming reply that ends cleanly is never reasked.                             |
| `ADanglingWordAtLengthBudgetIsNotTreatedAsTruncationAsync`                    | Fact   | A dangling word at length budget is not treated as truncation.                        |
| `ADanglingWordAlongsideARealToolCallIsNotTreatedAsTruncationAsync`            | Fact   | A dangling word alongside a real tool call is not treated as truncation.              |
| `AStreamingReplyThatStopsOnADanglingWordIsReaskedAndContinuedAsync`           | Fact   | A streaming reply that stops on a dangling word is reasked and continued.             |
| `AStreamingReplyThatEndsCleanlyIsNeverReaskedAsync`                           | Fact   | A streaming reply that ends cleanly is never reasked.                                 |
| `AStreamingReplyStillDanglingAfterOneReaskSaysSoRatherThanLoopingAsync`       | Fact   | A streaming reply still dangling after one reask says so rather than looping.         |

### RagSearchToolTests

`Jube.Test.AskJooby.RagSearchToolTests` — 12 tests. Category: AskJooby.

| Test                                                           | Kind | Verifies                                                           |
|----------------------------------------------------------------|------|--------------------------------------------------------------------|
| `ResultStartsWithAMachineParseableCountLineAsync`              | Fact | Result starts with a machine parseable count line.                 |
| `ReportsNoMatchingBitesWhenNothingComesBackAsync`              | Fact | Reports no matching bites when nothing comes back.                 |
| `PassesTheConfiguredTopKThroughToTheRagClientAsync`            | Fact | Passes the configured top k through to the rag client.             |
| `DefaultsToATopKOfFiveWhenNoneIsSuppliedAsync`                 | Fact | Defaults to a top k of five when none is supplied.                 |
| `UsesTheModelSuppliedMaxBitesOverTheConfiguredTopKAsync`       | Fact | Uses the model supplied max bites over the configured top k.       |
| `ClampsAModelSuppliedMaxBitesAboveTheCeilingAsync`             | Fact | Clamps a model supplied max bites above the ceiling.               |
| `ClampsAModelSuppliedMaxBitesBelowOneAsync`                    | Fact | Clamps a model supplied max bites below one.                       |
| `FreshBitesStillComeBackInFullAlongsideTheNamedRepeatsAsync`   | Fact | Fresh bites still come back in full alongside the named repeats.   |
| `CandidatesAreForwardedToTheSearchToBeRerankedAsync`           | Fact | Candidates are forwarded to the search to be reranked.             |
| `CandidatesAreClampedToTheCeilingWhateverTheModelAsksForAsync` | Fact | Candidates are clamped to the ceiling whatever the model asks for. |
| `UnsetCandidatesDefersToTheDeploymentDefaultAsync`             | Fact | Unset candidates defers to the deployment default.                 |
| `MaxBitesStillCapsWhatComesBackAtEightAsync`                   | Fact | Max bites still caps what comes back at eight.                     |

### RenderChartToolTests

`Jube.Test.AskJooby.RenderChartToolTests` — 16 tests. Category: AskJooby.

| Test                                                              | Kind   | Verifies                                                                |
|-------------------------------------------------------------------|--------|-------------------------------------------------------------------------|
| `RenderChartNeverReturnsTheChartItselfOnlyThatItWasDrawnAsync`    | Fact   | Render chart never returns the chart itself only that it was drawn.     |
| `RenderChartPutsRealSvgOntoTheReplyAsAnAttachmentAsync`           | Fact   | Render chart puts real svg onto the reply as an attachment.             |
| `RenderChartDrawsSeveralSeriesWhenGivenSeveralValueColumnsAsync`  | Fact   | Render chart draws several series when given several value columns.     |
| `RenderChartReportsAMissingPointerRatherThanThrowingAsync`        | Fact   | Render chart reports a missing pointer rather than throwing.            |
| `RenderChartNamesTheRealColumnsWhenGivenOneThatDoesNotExistAsync` | Fact   | Render chart names the real columns when given one that does not exist. |
| `RenderChartListsTheTypesItKnowsWhenGivenOneItDoesNotAsync`       | Fact   | Render chart lists the types it knows when given one it does not.       |
| `RenderChartRefusesAColumnThatHoldsNothingNumericAsync`           | Fact   | Render chart refuses a column that holds nothing numeric.               |
| `EveryCategoricalChartTypeTheToolAdvertisesActuallyDrawsAsync`    | Theory | Every categorical chart type the tool advertises actually draws.        |
| `ScatterPlotsOneNumericColumnAgainstAnotherAsync`                 | Fact   | Scatter plots one numeric column against another.                       |
| `RenderChartRefusesATableTooLargeToReadAsAChartAsync`             | Fact   | Render chart refuses a table too large to read as a chart.              |
| `TheRendererAdvertisesExactlyTheTypesTheToolDescriptionNames`     | Fact   | The renderer advertises exactly the types the tool description names.   |
| `AHistogramNeedsNoCategoryColumnBecauseItLabelsItsOwnAxisAsync`   | Fact   | A histogram needs no category column because it labels its own axis.    |
| `AHistogramBinsTheRawValuesRatherThanDrawingOneMarkPerRow`        | Fact   | A histogram bins the raw values rather than drawing one mark per row.   |
| `ABinCountIsHonouredBecauseAThresholdHasToLandOnAnEdge`           | Fact   | A bin count is honoured because a threshold has to land on an edge.     |
| `AnOrdinaryChartStillDemandsItsCategoryColumn`                    | Fact   | An ordinary chart still demands its category column.                    |
| `TheRendererDrawsWithInlineStylesSoAnImageTagCanShowIt`           | Fact   | The renderer draws with inline styles so an image tag can show it.      |

### RepeatedToolLedgerTests

`Jube.Test.AskJooby.RepeatedToolLedgerTests` — 9 tests. Category: AskJooby.

| Test                                                                        | Kind | Verifies                                                                                  |
|-----------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------|
| `TheSameReadWithTheSameArgumentsIsAnsweredFromTheFirstOne`                  | Fact | The same read with the same arguments is answered from the first one.                     |
| `DifferentArgumentsAreADifferentQuestionAndRunAgain`                        | Fact | Different arguments are a different question and run again.                               |
| `ALiveReadingIsNeverRepeatedBecauseAskingTwiceMayBeThePoint`                | Fact | A live reading is never repeated because asking twice may be the point.                   |
| `AWriteIsNeverRepeatedHoweverIdenticalTheArgumentsAre`                      | Fact | A write is never repeated however identical the arguments are.                            |
| `RememberingAnUnrepeatableToolIsIgnoredRatherThanStored`                    | Fact | Remembering an unrepeatable tool is ignored rather than stored.                           |
| `TheSearchesThatWentNowhereAreTheOnesWorthNotRepeating`                     | Fact | The searches that went nowhere are the ones worth not repeating.                          |
| `ARepeatableToolIsAnnouncedOnceAcrossTheWholeTurnHoweverManyAgentsAskForIt` | Fact | A repeatable tool is announced once across the whole turn however many agents ask for it. |
| `TheSameToolAskedADifferentQuestionIsStillAnnounced`                        | Fact | The same tool asked a different question is still announced.                              |
| `AToolThatIsNotRepeatableIsAlwaysAnnounced`                                 | Fact | A tool that is not repeatable is always announced.                                        |

### ReplyDraftTests

`Jube.Test.AskJooby.ReplyDraftTests` — 6 tests. Category: AskJooby.

| Test                                                               | Kind | Verifies                                                                         |
|--------------------------------------------------------------------|------|----------------------------------------------------------------------------------|
| `NarrationWrittenBeforeAToolCallIsNotPartOfTheAnswer`              | Fact | Narration written before a tool call is not part of the answer.                  |
| `FilingAMemoryAfterTheAnswerDoesNotDiscardTheAnswer`               | Fact | Filing a memory after the answer does not discard the answer.                    |
| `AnAnswerDiscardedByAToolCallIsRecoveredWhenOnlyASignOffFollowsIt` | Fact | An answer discarded by a tool call is recovered when only a sign off follows it. |
| `AForwardedJoobriksActivityNeverTruncatesTheAnswerBeingWritten`    | Fact | A forwarded joobriks activity never truncates the answer being written.          |
| `ThinkingAndThoughtLeaveTheAnswerAlone`                            | Fact | Thinking and thought leave the answer alone.                                     |
| `AnAnswerWithNoToolCallAtAllSurvivesWhole`                         | Fact | An answer with no tool call at all survives whole.                               |

### RerankingToggleTests

`Jube.Test.AskJooby.RerankingToggleTests` — 3 tests. Category: AskJooby.

| Test                                            | Kind | Verifies                                                  |
|-------------------------------------------------|------|-----------------------------------------------------------|
| `RerankingIsOnByDefaultSoAnOperatorHasToOptOut` | Fact | Reranking is on by default so an operator has to opt out. |
| `SettingTheFlagToFalseTurnsRerankingOff`        | Fact | Setting the flag to false turns reranking off.            |
| `OnlyTheLiteralFalseTurnsRerankingOff`          | Fact | Only the literal false turns reranking off.               |

### ResponseReviserTests

`Jube.Test.AskJooby.ResponseReviserTests` — 5 tests. Category: AskJooby.

| Test                                                                | Kind | Verifies                                                                      |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `BuildTaskIncludesTheQuestionDraftFeedbackAndDocs`                  | Fact | Build task includes the question draft feedback and docs.                     |
| `BuildTaskTellsTheReviserToSearchRatherThanDefendTheOriginalGuess`  | Fact | Build task tells the reviser to search rather than defend the original guess. |
| `BuildTaskAsksForSeveralDifferentSearchAttemptsRatherThanOneRepeat` | Fact | Build task asks for several different search attempts rather than one repeat. |
| `BuildTaskTellsTheReviserCodeOutranksDocsOnConflict`                | Fact | Build task tells the reviser code outranks docs on conflict.                  |
| `BuildTaskNotesWhenTheOriginalHadNoDocsAtAll`                       | Fact | Build task notes when the original had no docs at all.                        |

### ResponseValidatorTests

`Jube.Test.AskJooby.ResponseValidatorTests` — 14 tests. Category: AskJooby.

| Test                                                                    | Kind | Verifies                                                                            |
|-------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------|
| `IsApprovedWhenStatusIsPassed`                                          | Fact | Is approved when status is passed.                                                  |
| `TrimsAndIgnoresCaseWhenMatchingPassed`                                 | Fact | Trims and ignores case when matching passed.                                        |
| `ReturnsTheReasonAsFeedbackWhenStatusIsFailed`                          | Fact | Returns the reason as feedback when status is failed.                               |
| `TreatsAnUnparseableResponseAsFailedUsingTheRawTextAsFeedback`          | Fact | Treats an unparseable response as failed using the raw text as feedback.            |
| `ParsesKindOmissionWhenTheValidatorFoundNothingEitherWay`               | Fact | Parses kind omission when the validator found nothing either way.                   |
| `ParsesKindErrorWhenABiteActivelyContradictsTheClaim`                   | Fact | Parses kind error when a bite actively contradicts the claim.                       |
| `DefaultsToErrorWhenKindIsMissingOrUnparseable`                         | Fact | Defaults to error when kind is missing or unparseable.                              |
| `ApprovedResultsHaveNoFailureKind`                                      | Fact | Approved results have no failure kind.                                              |
| `BuildTaskIncludesTheQuestionDocsAndReply`                              | Fact | Build task includes the question docs and reply.                                    |
| `BuildTaskTellsTheValidatorToSearchRatherThanTrustTheSuppliedDocsAlone` | Fact | Build task tells the validator to search rather than trust the supplied docs alone. |
| `BuildTaskFlagsEverythingAsUnconfirmedWhenNoDocsWereEverRetrieved`      | Fact | Build task flags everything as unconfirmed when no docs were ever retrieved.        |
| `BuildTaskTellsTheValidatorCodeOutranksDocsOnConflict`                  | Fact | Build task tells the validator code outranks docs on conflict.                      |
| `BuildTaskAsksForAKindFieldDistinguishingOmissionFromError`             | Fact | Build task asks for a kind field distinguishing omission from error.                |
| `BuildTaskTellsTheValidatorNotToKeepRephrasingTheSameSearch`            | Fact | Build task tells the validator not to keep rephrasing the same search.              |

### RetrievedBiteLedgerTests

`Jube.Test.AskJooby.RetrievedBiteLedgerTests` — 5 tests. Category: Unit.

| Test                                                                  | Kind | Verifies                                                                     |
|-----------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `ASecondAgentIsGivenTheBiteInFullRatherThanTheSuppressionNoticeAsync` | Fact | A second agent is given the bite in full rather than the suppression notice. |
| `TheSameAgentAskingTwiceIsToldToScrollUpRatherThanSentItAgainAsync`   | Fact | The same agent asking twice is told to scroll up rather than sent it again.  |
| `TheSearchAllowanceIsSpentByTheTurnAndNotByEachAgentSeparately`       | Fact | The search allowance is spent by the turn and not by each agent separately.  |
| `WhetherTheTurnRetrievedAnythingIsAnsweredAcrossEveryAgentInIt`       | Fact | Whether the turn retrieved anything is answered across every agent in it.    |
| `AnAgentWithNoIdentityFallsBackToTheOneUnattributedBucket`            | Fact | An agent with no identity falls back to the one unattributed bucket.         |

### RoutingRulesCoverageTests

`Jube.Test.AskJooby.RoutingRulesCoverageTests` — 4 tests. Category: Unit.

| Test                                                                     | Kind | Verifies                                                                             |
|--------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `EveryDomainIsReachableFromRouting`                                      | Fact | Every domain is reachable from routing.                                              |
| `EveryRoutingRuleNamesAnAgentThatActuallyExists`                         | Fact | Every routing rule names an agent that actually exists.                              |
| `EveryRoutingRuleCarriesExemplarsAndADescriptionForTheStagesThatUseThem` | Fact | Every routing rule carries exemplars and a description for the stages that use them. |
| `RoutingIsOfferedPerDomainRatherThanPerLeaf`                             | Fact | Routing is offered per domain rather than per leaf.                                  |

### RoutingTierAndHandBackTests

`Jube.Test.AskJooby.RoutingTierAndHandBackTests` — 16 tests. Category: Unit.

| Test                                                                                      | Kind | Verifies                                                                                                 |
|-------------------------------------------------------------------------------------------|------|----------------------------------------------------------------------------------------------------------|
| `ARoutingTierKeepsFreeFormSqlAsTheSameUniversalFallbackEveryAgentHolds`                   | Fact | A routing tier keeps free form sql as the same universal fallback every agent holds.                     |
| `TheVisualisationDefinitionRoutingTierCanFallBackToSqlRatherThanFabricateAHandBackTarget` | Fact | The visualisation definition routing tier can fall back to sql rather than fabricate a hand back target. |
| `ARoutingTierKeepsTheCachedResultReadersSoItCanReadWhatItsJoobriksProduced`               | Fact | A routing tier keeps the cached result readers so it can read what its joobriks produced.                |
| `ALeafKeepsFreeFormSqlBecauseAnAggregateAcrossSlicesIsARealNeed`                          | Fact | A leaf keeps free form sql because an aggregate across slices is a real need.                            |
| `EveryAgentWithoutOperationsOfItsOwnIsTreatedAsARoutingTier`                              | Fact | Every agent without operations of its own is treated as a routing tier.                                  |
| `HandBackIsOfferedOnlyWhereSomeoneIsWaitingOnTheAnswer`                                   | Fact | Hand back is offered only where someone is waiting on the answer.                                        |
| `HandBackRecordsWhatIsNeededAndWhoOwnsIt`                                                 | Fact | Hand back records what is needed and who owns it.                                                        |
| `AnUnnamedOwnerIsCarriedAsUnknownRatherThanAsAnEmptyString`                               | Fact | An unnamed owner is carried as unknown rather than as an empty string.                                   |
| `AFabricatedOwningAgentNameIsDroppedRatherThanPassedOn`                                   | Fact | A fabricated owning agent name is dropped rather than passed on.                                         |
| `ARealAgentNameStillPassesEvenWhenItIsNotTheRightOneForThisQuestion`                      | Fact | A real agent name still passes even when it is not the right one for this question.                      |
| `TheFirstHandBackWins`                                                                    | Fact | The first hand back wins.                                                                                |
| `ARoutingTierHoldsTheReadsOfItsDelegatesButNeverTheirWrites`                              | Fact | A routing tier holds the reads of its delegates but never their writes.                                  |
| `AWorkerIsNotHandedAWriteThroughTheTierEvenWhenItsDelegateHoldsOne`                       | Fact | A worker is not handed a write through the tier even when its delegate holds one.                        |
| `StaticGraphAndFieldLookupsAreAnsweredOncePerTurn`                                        | Fact | Static graph and field lookups are answered once per turn.                                               |
| `TheWorkerIsNeverToldItHoldsNoSql`                                                        | Fact | The worker is never told it holds no sql.                                                                |
| `EveryLeafIsAHierarchyAgentThatCannotDelegateExceptToTheSharedWebResearchSpecialist`      | Fact | Every leaf is a hierarchy agent that cannot delegate except to the shared web research specialist.       |

### RuleExtensionCatalogueTests

`Jube.Test.AskJooby.RuleExtensionCatalogueTests` — 8 tests. Category: Unit.

| Test                                                                             | Kind | Verifies                                                                                       |
|----------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------|
| `TheCatalogueFindsTheWholeExtensionSurfaceAndNotAHandfulOfIt`                    | Fact | The catalogue finds the whole extension surface and not a handful of it.                       |
| `EverySignatureIsRenderedInVisualBasicAndNotInTheLanguageItWasWrittenIn`         | Fact | Every signature is rendered in visual basic and not in the language it was written in.         |
| `AParameterlessExtensionRendersAsTheCallARuleActuallyWrites`                     | Fact | A parameterless extension renders as the call a rule actually writes.                          |
| `AParamArrayIsAdvertisedAsOneRatherThanAsAnArrayArgument`                        | Fact | A param array is advertised as one rather than as an array argument.                           |
| `TheEvalMechanismIsNotOfferedAsSomethingToCall`                                  | Fact | The eval mechanism is not offered as something to call.                                        |
| `EveryAgentGivenTheseToolsIsOneThatActuallyWritesAFragment`                      | Fact | Every agent given these tools is one that actually writes a fragment.                          |
| `WhatTheModelActuallyReceivesIsTheVisualBasicDeclarationAndNotTheSameThingTwice` | Fact | What the model actually receives is the visual basic declaration and not the same thing twice. |
| `EveryToolNameBindsToARealMethodOnTheToolClass`                                  | Fact | Every tool name binds to a real method on the tool class.                                      |

### RuleExtensionChainValidatorTests

`Jube.Test.AskJooby.RuleExtensionChainValidatorTests` — 8 tests. Category: Unit.

| Test                                                                   | Kind | Verifies                                                                           |
|------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `AWellFormedChainResolvesAndCarriesTheTypeThroughIt`                   | Fact | A well formed chain resolves and carries the type through it.                      |
| `AMisspeltCallIsReportedRatherThanAbsorbedIntoTheThingBeingOperatedOn` | Fact | A misspelt call is reported rather than absorbed into the thing being operated on. |
| `AMethodCalledOnTheWrongTypeSaysWhichTypeItDoesLiveOn`                 | Fact | A method called on the wrong type says which type it does live on.                 |
| `TheWrongNumberOfArgumentsIsAnsweredWithTheShapesThatDoExist`          | Fact | The wrong number of arguments is answered with the shapes that do exist.           |
| `AnOptionalArgumentMayBeOmittedAndAParamArrayMayBeRepeated`            | Fact | An optional argument may be omitted and a param array may be repeated.             |
| `ADecimalPointInsideAnArgumentIsNotMistakenForALinkInTheChain`         | Fact | A decimal point inside an argument is not mistaken for a link in the chain.        |
| `AQuotedDotIsNotMistakenForALinkEither`                                | Fact | A quoted dot is not mistaken for a link either.                                    |
| `AnExpressionNamingNothingAtAllSaysSoRatherThanPassing`                | Fact | An expression naming nothing at all says so rather than passing.                   |

### RuleTaxonomyWorkflowCoverageTests

`Jube.Test.AskJooby.RuleTaxonomyWorkflowCoverageTests` — 12 tests. Category: Unit.

| Test                                                                | Kind | Verifies                                                                      |
|---------------------------------------------------------------------|------|-------------------------------------------------------------------------------|
| `EveryDocumentedTaxonomyTypeHasAWorkflowAndNothingElseDoes`         | Fact | Every documented taxonomy type has a workflow and nothing else does.          |
| `EveryWorkflowKeepsTheTitleTheTaxonomyGaveIt`                       | Fact | Every workflow keeps the title the taxonomy gave it.                          |
| `EveryWorkflowCarriesEnoughSemanticsToBeChosenWithoutTheePasture`   | Fact | Every workflow carries enough semantics to be chosen without thee pasture.    |
| `EveryRuleTaxonomyNodeIsPerformableByAnAgent`                       | Fact | Every rule taxonomy node is performable by an agent.                          |
| `EveryWorkflowIdAndNodeIdIsUnique`                                  | Fact | Every workflow id and node id is unique.                                      |
| `NoNodeRequiresAnArtefactThatNoEarlierNodeProduces`                 | Fact | No node requires an artefact that no earlier node produces.                   |
| `EveryWorkflowLooksForSomethingItCouldReuseBeforeItCreatesAnything` | Fact | Every workflow looks for something it could reuse before it creates anything. |
| `EveryWorkflowEndsBySynchronisingAndCheckingTheFragmentCompiled`    | Fact | Every workflow ends by synchronising and checking the fragment compiled.      |
| `EveryNodeIsOwnedByAnAgentThatExists`                               | Fact | Every node is owned by an agent that exists.                                  |
| `EveryNodeIsOwnedByAnAgentThatActuallyHoldsTheToolsItNames`         | Fact | Every node is owned by an agent that actually holds the tools it names.       |
| `EveryNodeNamesCataloguedToolsThatActuallyExist`                    | Fact | Every node names catalogued tools that actually exist.                        |
| `TheTwoTypesWhoseOrderingIsOppositeBothSaySoOnTheWorkflow`          | Fact | The two types whose ordering is opposite both say so on the workflow.         |

### RunScriptToolTests

`Jube.Test.AskJooby.RunScriptToolTests` — 6 tests. Category: AskJooby.

| Test                                                       | Kind | Verifies                                                     |
|------------------------------------------------------------|------|--------------------------------------------------------------|
| `CheckRServerHealthReportsStatusAndScriptRulesAsync`       | Fact | Check r server health reports status and script rules.       |
| `ListRPackagesReportsEachPackageAsync`                     | Fact | List r packages reports each package.                        |
| `ValidateRScriptReportsErrorsWhenNotOkAsync`               | Fact | Validate r script reports errors when not ok.                |
| `RunScriptReportsConsoleOutputOnSuccessAsync`              | Fact | Run script reports console output on success.                |
| `RunScriptReportsTheErrorOnFailureAsync`                   | Fact | Run script reports the error on failure.                     |
| `RunScriptReturnsAFriendlyRejectionInsteadOfThrowingAsync` | Fact | Run script returns a friendly rejection instead of throwing. |

### RunSqlQueryToolTests

`Jube.Test.AskJooby.RunSqlQueryToolTests` — 7 tests. Category: AskJooby.

| Test                                                                                   | Kind | Verifies                                                                                        |
|----------------------------------------------------------------------------------------|------|-------------------------------------------------------------------------------------------------|
| `RunSqlQueryReturnsAPointerAndAPreviewOnSuccessAsync`                                  | Fact | Run sql query returns a pointer and a preview on success.                                       |
| `RunSqlQueryReturnsAFriendlyRejectionInsteadOfThrowingAsync`                           | Fact | Run sql query returns a friendly rejection instead of throwing.                                 |
| `RunSqlQueryHandlesAnEmptyResultSetAsync`                                              | Fact | Run sql query handles an empty result set.                                                      |
| `RunSqlQueryDoesNotStoreAnythingWhenTheQueryIsRejectedAsync`                           | Fact | Run sql query does not store anything when the query is rejected.                               |
| `RunSqlQueryWithholdsARowThatMatchesPromptInjectionScreeningButKeepsTheCleanRowsAsync` | Fact | Run sql query withholds a row that matches prompt injection screening but keeps the clean rows. |
| `RunSqlQueryRecordsAGuardRejectionWhenWithholdingARowAsync`                            | Fact | Run sql query records a guard rejection when withholding a row.                                 |
| `RunSqlQueryRecordsNothingWhenNoRowIsWithheldAsync`                                    | Fact | Run sql query records nothing when no row is withheld.                                          |

### SearchAskJoobyRagChunkQueryTests

`Jube.Test.AskJooby.SearchAskJoobyRagChunkQueryTests` — 3 tests. Category: AskJooby. Collection: Database.

| Test                                                                        | Kind | Verifies                                                                           |
|-----------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `KeywordOnlySearchMatchesAChunkThatOnlyContainsOneOfSeveralQueryTermsAsync` | Fact | Keyword only search matches a chunk that only contains one of several query terms. |
| `KeywordOnlySearchStillHonoursTheMinScoreFloorAsync`                        | Fact | Keyword only search still honours the min score floor.                             |
| `HybridSearchDoesNotApplyTheMinScoreFloorToTheFusedRankScoreAsync`          | Fact | Hybrid search does not apply the min score floor to the fused rank score.          |

### SnapshotOnWriteAiFunctionTests

`Jube.Test.AskJooby.SnapshotOnWriteAiFunctionTests` — 5 tests. Category: Unit.

| Test                                                        | Kind | Verifies                                                         |
|-------------------------------------------------------------|------|------------------------------------------------------------------|
| `ASnapshotIsTakenBeforeTheChangeRunsAsync`                  | Fact | A snapshot is taken before the change runs.                      |
| `EveryChangeGetsItsOwnRestorePointRatherThanOnePerRunAsync` | Fact | Every change gets its own restore point rather than one per run. |
| `TheChangeDoesNotRunIfItsRestorePointCouldNotBeTakenAsync`  | Fact | The change does not run if its restore point could not be taken. |
| `TheRestorePointIsAnnouncedWithTheIdNeededToUseItAsync`     | Fact | The restore point is announced with the id needed to use it.     |
| `AToolWithNoChannelStillRunsAndStillSnapshotsAsync`         | Fact | A tool with no channel still runs and still snapshots.           |

### SpawnAgentToolTests

`Jube.Test.AskJooby.SpawnAgentToolTests` — 19 tests. Category: AskJooby.

| Test                                                                               | Kind | Verifies                                                                                      |
|------------------------------------------------------------------------------------|------|-----------------------------------------------------------------------------------------------|
| `SpawnAgentIsRefusedWhenThePlanHasNoStepForTheSpecialistsStableAsync`              | Fact | Spawn agent is refused when the plan has no step for the specialists stable.                  |
| `AStableIsRefusedAFanOutUntilItHasItsOwnPlanAsync`                                 | Fact | A stable is refused a fan out until it has its own plan.                                      |
| `SpawnAgentThrowsWhenTheCallerLacksThePermissionForTheRequestedSpecialistAsync`    | Fact | Spawn agent throws when the caller lacks the permission for the requested specialist.         |
| `SpawnAgentThrowsForAnUnknownAgentTypeAsync`                                       | Fact | Spawn agent throws for an unknown agent type.                                                 |
| `SpawnAgentDelegatesToAPermittedSpecialistAndReturnsItsFinalAnswerAsync`           | Fact | Spawn agent delegates to a permitted specialist and returns its final answer.                 |
| `SpawnAgentTellsTheCallerToSpeakUpWhenAHandBackLeavesNowhereFurtherToRouteItAsync` | Fact | Spawn agent tells the caller to speak up when a hand back leaves nowhere further to route it. |
| `SpawnAgentForwardsTheSpawnedJoobriksOwnStepsLiveLabelledWithItsAgentTypeAsync`    | Fact | Spawn agent forwards the spawned joobriks own steps live labelled with its agent type.        |
| `SpawnAgentForwardsTheSpawnedJoobriksOwnReasoningLabelledWithItsAgentTypeAsync`    | Fact | Spawn agent forwards the spawned joobriks own reasoning labelled with its agent type.         |
| `SpawnAgentAnnouncesTheAgentItIsPickingUpBeforeItStartsWorkingAsync`               | Fact | Spawn agent announces the agent it is picking up before it starts working.                    |
| `SpawnAgentsThrowsForFewerThanTwoSubTasksAsync`                                    | Fact | Spawn agents throws for fewer than two sub tasks.                                             |
| `SpawnAgentsThrowsWhenExceedingMaxFanOutAsync`                                     | Fact | Spawn agents throws when exceeding max fan out.                                               |
| `SpawnAgentsRunsIndependentSubTasksConcurrentlyAndLabelsEachWithAnOrdinalAsync`    | Fact | Spawn agents runs independent sub tasks concurrently and labels each with an ordinal.         |
| `ACutOffChildIsRecordedAsCancelledRatherThanLeftRunningAsync`                      | Fact | A cut off child is recorded as cancelled rather than left running.                            |
| `ASingleHandoverNeedsNoPlanButIsCountedAsUnplannedAsync`                           | Fact | A single handover needs no plan but is counted as unplanned.                                  |
| `GeneralCannotJumpStraightToASpecialistOutsideItsOwnStablesAsync`                  | Fact | General cannot jump straight to a specialist outside its own stables.                         |
| `GeneralCanStillReachOneOfItsOwnDeclaredStablesAsync`                              | Fact | General can still reach one of its own declared stables.                                      |
| `SpawnAgentsAcrossTwoStablesIsRefusedUntilAPlanExistsAsync`                        | Fact | Spawn agents across two stables is refused until a plan exists.                               |
| `SpawnAgentIsRefusedWhenTheTurnHasNoLedgerAtAllAsync`                              | Fact | Spawn agent is refused when the turn has no ledger at all.                                    |
| `ANestedJoobrikIsNotAskedToPlanAgainAsync`                                         | Fact | A nested joobrik is not asked to plan again.                                                  |

### SpawnAgentsConcurrencyDbTests

`Jube.Test.AskJooby.SpawnAgentsConcurrencyDbTests` — 1 test. Category: AskJooby. Collection: Database.

| Test                                                                  | Kind | Verifies                                                                 |
|-----------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `SpawnAgentsRunningConcurrentlyDoesNotRaceTheDatabaseConnectionAsync` | Fact | Spawn agents running concurrently does not race the database connection. |

### SqlPreFlightToolTests

`Jube.Test.AskJooby.SqlPreFlightToolTests` — 9 tests. Category: AskJooby.

| Test                                                                    | Kind | Verifies                                                                     |
|-------------------------------------------------------------------------|------|------------------------------------------------------------------------------|
| `ValidateSqlQueryReportsTheDatabaseErrorRatherThanAGenericFailureAsync` | Fact | Validate sql query reports the database error rather than a generic failure. |
| `ValidateSqlQuerySaysNothingAboutCostWhenTheStatementIsValidAsync`      | Fact | Validate sql query says nothing about cost when the statement is valid.      |
| `ExplainSqlQueryNamesTheRelationsScannedSequentiallyAsync`              | Fact | Explain sql query names the relations scanned sequentially.                  |
| `InspectTableStatisticsWarnsWhenTheTableIsLargeAsync`                   | Fact | Inspect table statistics warns when the table is large.                      |
| `InspectTableStatisticsStaysQuietForASmallTableAsync`                   | Fact | Inspect table statistics stays quiet for a small table.                      |
| `AnalyseQueryHistoryFallsThroughToExplainWhenNothingWasRecordedAsync`   | Fact | Analyse query history falls through to explain when nothing was recorded.    |
| `AnalyseQueryHistoryCallsOutSkewWhenTheWorstCaseDwarfsTheAverageAsync`  | Fact | Analyse query history calls out skew when the worst case dwarfs the average. |
| `AnalyseQueryHistoryCallsOutDiskReadingAndWideReturnsTogetherAsync`     | Fact | Analyse query history calls out disk reading and wide returns together.      |
| `AnalyseQueryHistoryStaysQuietOnABusyButWellBehavedShapeAsync`          | Fact | Analyse query history stays quiet on a busy but well behaved shape.          |

### SurfacedFailureAiFunctionTests

`Jube.Test.AskJooby.SurfacedFailureAiFunctionTests` — 4 tests. Category: Unit.

| Test                                                                            | Kind | Verifies                                                                             |
|---------------------------------------------------------------------------------|------|--------------------------------------------------------------------------------------|
| `TheServicesOwnValidationMessageReachesTheModelRatherThanAGenericFailureAsync`  | Fact | The services own validation message reaches the model rather than a generic failure. |
| `TheInnermostMessageIsTakenBecauseAWrappedOneSaysNothingUsefulAsync`            | Fact | The innermost message is taken because a wrapped one says nothing useful.            |
| `CancellationStillPropagatesBecauseSomethingDeliberatelyAskedTheRunToStopAsync` | Fact | Cancellation still propagates because something deliberately asked the run to stop.  |
| `AToolThatSucceedsIsPassedStraightThroughUntouchedAsync`                        | Fact | A tool that succeeds is passed straight through untouched.                           |

### TavilyWebSearchClientTests

`Jube.Test.AskJooby.TavilyWebSearchClientTests` — 12 tests. Category: AskJooby.

| Test                                                               | Kind   | Verifies                                                                 |
|--------------------------------------------------------------------|--------|--------------------------------------------------------------------------|
| `SendsTheApiKeyAsABearerTokenAsync`                                | Fact   | Sends the api key as a bearer token.                                     |
| `MapsSearchDepthOntoTheProviderOwnStringAsync`                     | Theory | Maps search depth onto the provider own string.                          |
| `MapsAnswerModeOntoTheProviderOwnIncludeAnswerValueAsync`          | Theory | Maps answer mode onto the provider own include answer value.             |
| `MapsRawContentModeOntoTheProviderOwnIncludeRawContentValueAsync`  | Theory | Maps raw content mode onto the provider own include raw content value.   |
| `SendsTheQueryAndMaxResultsAsync`                                  | Fact   | Sends the query and max results.                                         |
| `ParsesTheAnswerAndResultsAsync`                                   | Fact   | Parses the answer and results.                                           |
| `ARegularResultWithNoRawContentFieldLeavesItNullAsync`             | Fact   | A regular result with no raw content field leaves it null.               |
| `CarriesTheRawResponseVerbatimAsync`                               | Fact   | Carries the raw response verbatim.                                       |
| `MissingAnswerAndEmptyResultsParseCleanlyAsync`                    | Fact   | Missing answer and empty results parse cleanly.                          |
| `ThrowsWithTheStatusCodeAndBodyWhenTheProviderAnswersAnErrorAsync` | Fact   | Throws with the status code and body when the provider answers an error. |
| `RecordsTheCallAgainstTheGivenRunWhenARecorderIsSuppliedAsync`     | Fact   | Records the call against the given run when a recorder is supplied.      |
| `RecordsNothingWhenNoRecorderIsSuppliedAsync`                      | Fact   | Records nothing when no recorder is supplied.                            |

### ThinkingEffortLadderTests

`Jube.Test.AskJooby.ThinkingEffortLadderTests` — 3 tests. Category: AskJooby.

| Test                                                                | Kind | Verifies                                                                       |
|---------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `TheCheckSitsAtTheCheapestBandRatherThanInheritingAProviderDefault` | Fact | The check sits at the cheapest band rather than inheriting a provider default. |
| `TheShortUtilityCallsDoNotReasonAtAllByDefault`                     | Fact | The short utility calls do not reason at all by default.                       |
| `EveryModelRoleCarriesItsOwnSwitchRatherThanInheritingAnother`      | Fact | Every model role carries its own switch rather than inheriting another.        |

### ThinkingOptionsTests

`Jube.Test.AskJooby.ThinkingOptionsTests` — 5 tests. Category: AskJooby.

| Test                                                       | Kind   | Verifies                                                             |
|------------------------------------------------------------|--------|----------------------------------------------------------------------|
| `RawOptionsDefaultsToHighWhenNoEffortIsSupplied`           | Fact   | Raw options defaults to high when no effort is supplied.             |
| `RawOptionsMapsTheOrdinalToTheConfirmedDeepSeekEffortWord` | Theory | Raw options maps the ordinal to the confirmed deep seek effort word. |
| `TurningThinkingOffSendsNoEffortAtAll`                     | Fact   | Turning thinking off sends no effort at all.                         |
| `EveryEffortWordOfferedIsOneTheProviderWillAccept`         | Fact   | Every effort word offered is one the provider will accept.           |
| `RawOptionsFallsBackToHighForAnOutOfRangeOrdinal`          | Theory | Raw options falls back to high for an out of range ordinal.          |

### TokenBudgetCacheWeightingTests

`Jube.Test.AskJooby.TokenBudgetCacheWeightingTests` — 5 tests. Category: AskJooby.

| Test                                                | Kind | Verifies                                                      |
|-----------------------------------------------------|------|---------------------------------------------------------------|
| `ACachedPrefixIsChargedAtItsWeightRatherThanInFull` | Fact | A cached prefix is charged at its weight rather than in full. |
| `NothingReportedAsCachedIsStillChargedInFull`       | Fact | Nothing reported as cached is still charged in full.          |
| `AWeightOfOneChargesCachedAndUncachedAlike`         | Fact | A weight of one charges cached and uncached alike.            |
| `AReportedCacheLargerThanTheInputIsNotPriced`       | Fact | A reported cache larger than the input is not priced.         |
| `TheDiscountDelaysExhaustionRatherThanRemovingIt`   | Fact | The discount delays exhaustion rather than removing it.       |

### TokenBudgetTests

`Jube.Test.AskJooby.TokenBudgetTests` — 11 tests. Category: AskJooby.

| Test                                                                | Kind | Verifies                                                                           |
|---------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ABudgetOfZeroIsNoBudgetAtAllRatherThanOneAlreadySpent`             | Fact | A budget of zero is no budget at all rather than one already spent.                |
| `SpendAccumulatesInputAndOutputTogetherUntilTheLimitIsReached`      | Fact | Spend accumulates input and output together until the limit is reached.            |
| `NegativeOrAbsentCountsAreIgnoredRatherThanCreditedBack`            | Fact | Negative or absent counts are ignored rather than credited back.                   |
| `ConcurrentJoobriksSpendingAtOnceAreAllCounted`                     | Fact | Concurrent joobriks spending at once are all counted.                              |
| `TheHardCeilingSitsAboveTheSoftOneSoAgentsGetAChanceToWriteUpFirst` | Fact | The hard ceiling sits above the soft one so agents get a chance to write up first. |
| `ABudgetOfZeroIsNeverOverrunEither`                                 | Fact | A budget of zero is never overrun either.                                          |
| `AnOverrunMultipleOfOneCollapsesBothThresholdsOntoTheLimit`         | Fact | An overrun multiple of one collapses both thresholds onto the limit.               |
| `AToolRunsNormallyWhileThereIsBudgetLeftAsync`                      | Fact | A tool runs normally while there is budget left.                                   |
| `AnExhaustedBudgetStopsTheToolRunningAndTellsTheModelToAnswerAsync` | Fact | An exhausted budget stops the tool running and tells the model to answer.          |
| `UsageReportedByTheProviderIsCountedAgainstTheBudgetAsync`          | Fact | Usage reported by the provider is counted against the budget.                      |
| `UsageArrivingMidStreamIsCountedTooAsync`                           | Fact | Usage arriving mid stream is counted too.                                          |

### TokenReductionTests

`Jube.Test.AskJooby.TokenReductionTests` — 7 tests. Category: Unit.

| Test                                                              | Kind | Verifies                                                                  |
|-------------------------------------------------------------------|------|---------------------------------------------------------------------------|
| `ABiteAlreadyGivenThisTurnIsNamedRatherThanSentAgainAsync`        | Fact | A bite already given this turn is named rather than sent again.           |
| `AGenuinelyNewBiteStillComesBackInFullAsync`                      | Fact | A genuinely new bite still comes back in full.                            |
| `SearchingStopsOnceTheTurnsAllowanceIsSpentAsync`                 | Fact | Searching stops once the turns allowance is spent.                        |
| `ATurnThatRetrievedNothingIsDistinguishableFromOneThatDid`        | Fact | A turn that retrieved nothing is distinguishable from one that did.       |
| `ValidatorGroundingKeepsOneCopyOfEachBite`                        | Fact | Validator grounding keeps one copy of each bite.                          |
| `ValidatorGroundingNamesWhatItCouldNotAffordToQuote`              | Fact | Validator grounding names what it could not afford to quote.              |
| `ValidatorGroundingKeepsAnUnparseablePayloadRatherThanDroppingIt` | Fact | Validator grounding keeps an unparseable payload rather than dropping it. |

### ToolCallAndResultGuardChatClientTests

`Jube.Test.AskJooby.ToolCallAndResultGuardChatClientTests` — 19 tests. Category: AskJooby.

| Test                                                                                                           | Kind | Verifies                                                                                                   |
|----------------------------------------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------------------------------|
| `GetResponseAsync_passes_through_a_benign_tool_callAsync`                                                      | Fact | Get response async passes through a benign tool call.                                                      |
| `GetResponseAsync_blocks_a_tool_call_with_a_shell_command_chaining_patternAsync`                               | Fact | Get response async blocks a tool call with a shell command chaining pattern.                               |
| `GetResponseAsync_does_not_block_a_legitimate_union_select_for_the_allow_listed_sql_toolAsync`                 | Fact | Get response async does not block a legitimate union select for the allow listed sql tool.                 |
| `GetResponseAsync_still_blocks_the_same_union_pattern_for_a_tool_that_is_not_allow_listedAsync`                | Fact | Get response async still blocks the same union pattern for a tool that is not allow listed.                |
| `GetResponseAsync_does_not_block_delegation_text_mentioning_executeAsync`                                      | Fact | Get response async does not block delegation text mentioning execute.                                      |
| `GetResponseAsync_does_not_block_a_search_query_mentioning_executeAsync`                                       | Fact | Get response async does not block a search query mentioning execute.                                       |
| `GetResponseAsync_does_not_block_a_web_search_query_mentioning_executeAsync`                                   | Fact | Get response async does not block a web search query mentioning execute.                                   |
| `GetResponseAsync_still_blocks_an_execute_pattern_outside_the_declared_provenanceAsync`                        | Fact | Get response async still blocks an execute pattern outside the declared provenance.                        |
| `GetResponseAsync_does_not_block_r_source_for_the_allow_listed_validate_r_script_toolAsync`                    | Fact | Get response async does not block r source for the allow listed validate r script tool.                    |
| `GetResponseAsync_does_not_block_r_source_for_the_allow_listed_run_script_toolAsync`                           | Fact | Get response async does not block r source for the allow listed run script tool.                           |
| `GetResponseAsync_still_blocks_the_same_r_source_for_a_tool_that_is_not_allow_listedAsync`                     | Fact | Get response async still blocks the same r source for a tool that is not allow listed.                     |
| `GetResponseAsync_blocks_an_instruction_override_hidden_in_a_tool_result_before_calling_the_inner_clientAsync` | Fact | Get response async blocks an instruction override hidden in a tool result before calling the inner client. |
| `GetResponseAsync_passes_through_AskClarifyingQuestions_own_instruction_scaffoldingAsync`                      | Fact | Get response async passes through ask clarifying questions own instruction scaffolding.                    |
| `GetResponseAsync_still_blocks_the_same_instruction_field_shape_from_an_unexempted_toolAsync`                  | Fact | Get response async still blocks the same instruction field shape from an unexempted tool.                  |
| `GetResponseAsync_passes_through_a_benign_tool_resultAsync`                                                    | Fact | Get response async passes through a benign tool result.                                                    |
| `GetResponseAsync_does_not_block_a_SearchRag_result_quoting_AgentGuards_own_documentationAsync`                | Fact | Get response async does not block a search rag result quoting agent guards own documentation.              |
| `GetResponseAsync_still_blocks_the_same_injection_vocabulary_from_a_tool_that_is_not_exemptedAsync`            | Fact | Get response async still blocks the same injection vocabulary from a tool that is not exempted.            |
| `GetResponseAsync_passes_through_a_plain_text_reply_with_no_tool_callsAsync`                                   | Fact | Get response async passes through a plain text reply with no tool calls.                                   |
| `GetStreamingResponseAsync_passes_through_a_plain_text_reply_with_no_tool_callsAsync`                          | Fact | Get streaming response async passes through a plain text reply with no tool calls.                         |

### TurnActivityLedgerTests

`Jube.Test.AskJooby.TurnActivityLedgerTests` — 6 tests. Category: Unit.

| Test                                                               | Kind | Verifies                                                                       |
|--------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `AQuietTurnSaysNothingAboutReasoningAtAll`                         | Fact | A quiet turn says nothing about reasoning at all.                              |
| `AModelThatReasonsWithTheSwitchOffIsReportedRatherThanOnlyCounted` | Fact | A model that reasons with the switch off is reported rather than only counted. |
| `HandBacksAreCountedAcrossTheTurn`                                 | Fact | Hand backs are counted across the turn.                                        |
| `ATurnHasNoPlanUntilOneIsRecorded`                                 | Fact | A turn has no plan until one is recorded.                                      |
| `UnplannedHandoversAreCountedAcrossTheTurn`                        | Fact | Unplanned handovers are counted across the turn.                               |
| `AStablePlanIsKeptAgainstThatStableAndNotTheRootPlan`              | Fact | A stable plan is kept against that stable and not the root plan.               |

### TurnCostPricingTests

`Jube.Test.AskJooby.TurnCostPricingTests` — 12 tests.

| Test                                                                                          | Kind | Verifies                                                                                                      |
|-----------------------------------------------------------------------------------------------|------|---------------------------------------------------------------------------------------------------------------|
| `PriceModelCalls_WithNoCachedTokens_ChargesAllInputAtTheFullRate`                             | Fact | Price model calls with no cached tokens charges all input at the full rate.                                   |
| `PriceModelCalls_PricesTheCachedSubsetSeparately_NotOnTopOfInputTokens`                       | Fact | Price model calls prices the cached subset separately not on top of input tokens.                             |
| `PriceModelCalls_WithNoCachedRateConfigured_FallsBackToTheFullInputRate`                      | Fact | Price model calls with no cached rate configured falls back to the full input rate.                           |
| `PriceModelCalls_SumsAcrossDifferentTimesOfDayInTheSameTurn_EachAtItsOwnBandsRate`            | Fact | Price model calls sums across different times of day in the same turn each at its own bands rate.             |
| `PriceModelCalls_WithATimeThatMatchesNoBand_ReturnsNullRatherThanAPartialFigure`              | Fact | Price model calls with a time that matches no band returns null rather than a partial figure.                 |
| `PriceModelCalls_WithABandRestrictedToWeekdays_DoesNotMatchASaturdayCall`                     | Fact | Price model calls with a band restricted to weekdays does not match a saturday call.                          |
| `PriceModelCalls_WithAMidnightWrappingBand_MatchesBothSidesOfMidnightButNotTheMiddleOfTheDay` | Fact | Price model calls with a midnight wrapping band matches both sides of midnight but not the middle of the day. |
| `PriceEmbeddingCalls_PricesByInputTokensOnly`                                                 | Fact | Price embedding calls prices by input tokens only.                                                            |
| `PriceRerankCalls_WithNoTokensCompared_FallsBackToDocumentsCompared`                          | Fact | Price rerank calls with no tokens compared falls back to documents compared.                                  |
| `PriceRerankCalls_PrefersTokensComparedWhenBothTheCallAndTheFeeHaveIt`                        | Fact | Price rerank calls prefers tokens compared when both the call and the fee have it.                            |
| `PriceRerankCalls_WithAnUnpricedModel_ReturnsNull`                                            | Fact | Price rerank calls with an unpriced model returns null.                                                       |
| `PriceRerankCalls_WithNeitherAUsableTokenCountNorADocumentRateInCommon_ReturnsNull`           | Fact | Price rerank calls with neither a usable token count nor a document rate in common returns null.              |

### UserQueryGuardChatClientTests

`Jube.Test.AskJooby.UserQueryGuardChatClientTests` — 3 tests. Category: AskJooby.

| Test                                                             | Kind | Verifies                                                              |
|------------------------------------------------------------------|------|-----------------------------------------------------------------------|
| `PassesThroughWhenANonEmptyUserMessageIsPresentAsync`            | Fact | Passes through when a non empty user message is present.              |
| `ThrowsBeforeCallingTheInnerClientWhenThereIsNoUserMessageAsync` | Fact | Throws before calling the inner client when there is no user message. |
| `ThrowsWhenTheOnlyUserMessageIsBlankAsync`                       | Fact | Throws when the only user message is blank.                           |

### WebSearchToolTests

`Jube.Test.AskJooby.WebSearchToolTests` — 20 tests. Category: AskJooby.

| Test                                                                         | Kind | Verifies                                                                           |
|------------------------------------------------------------------------------|------|------------------------------------------------------------------------------------|
| `ResultStartsWithAMachineParseableCountLineAsync`                            | Fact | Result starts with a machine parseable count line.                                 |
| `ReportsNoMatchingResultsWhenNothingComesBackAsync`                          | Fact | Reports no matching results when nothing comes back.                               |
| `IncludesTheSynthesisedAnswerWhenTheProviderGivesOneAsync`                   | Fact | Includes the synthesised answer when the provider gives one.                       |
| `PassesTheConfiguredDefaultMaxResultsThroughAsync`                           | Fact | Passes the configured default max results through.                                 |
| `UsesTheModelSuppliedMaxResultsOverTheConfiguredDefaultAsync`                | Fact | Uses the model supplied max results over the configured default.                   |
| `ClampsAModelSuppliedMaxResultsAboveTheCeilingAsync`                         | Fact | Clamps a model supplied max results above the ceiling.                             |
| `ClampsAModelSuppliedMaxResultsBelowOneAsync`                                | Fact | Clamps a model supplied max results below one.                                     |
| `PassesTheConfiguredDepthAndAnswerModeThroughAsync`                          | Fact | Passes the configured depth and answer mode through.                               |
| `PassesTheConfiguredRawContentModeThroughAsync`                              | Fact | Passes the configured raw content mode through.                                    |
| `DefaultsToMarkdownRawContentWhenNoneIsConfiguredAsync`                      | Fact | Defaults to markdown raw content when none is configured.                          |
| `IncludesTheFullExtractedPageWhenTheProviderReturnsOneAsync`                 | Fact | Includes the full extracted page when the provider returns one.                    |
| `OmitsTheFullExtractedPageSectionWhenNoneCameBackAsync`                      | Fact | Omits the full extracted page section when none came back.                         |
| `WithholdsAResultWhoseFullExtractedPageMatchesPromptInjectionScreeningAsync` | Fact | Withholds a result whose full extracted page matches prompt injection screening.   |
| `PassesTheRecorderAndRunIdThroughToTheClientAsync`                           | Fact | Passes the recorder and run id through to the client.                              |
| `WithholdsAResultThatMatchesPromptInjectionScreeningAsync`                   | Fact | Withholds a result that matches prompt injection screening.                        |
| `ReportsOnlyTheWithholdingWhenEveryResultIsWithheldAsync`                    | Fact | Reports only the withholding when every result is withheld.                        |
| `RecordsAnAggregateGuardRejectionWhenWithholdingResultsAsync`                | Fact | Records an aggregate guard rejection when withholding results.                     |
| `RecordsNothingWhenNoResultIsWithheldAsync`                                  | Fact | Records nothing when no result is withheld.                                        |
| `DoesNotSendTheSearchWhenTheQueryItselfMatchesPromptInjectionScreeningAsync` | Fact | Does not send the search when the query itself matches prompt injection screening. |
| `RecordsARequestSideGuardRejectionUnderItsOwnPipelineNameAsync`              | Fact | Records a request side guard rejection under its own pipeline name.                |

### WithRealChunks

`Jube.Test.AskJooby.AskJoobyChatAuditCodeGroundingTests+WithRealChunks` — 1 test. Category: AskJooby. Collection:
Database.

| Test                                                                   | Kind | Verifies                                                                 |
|------------------------------------------------------------------------|------|--------------------------------------------------------------------------|
| `BuildOrchestrationCodeAsyncRetrievesTheFixedOrchestrationChunksAsync` | Fact | Build orchestration code async retrieves the fixed orchestration chunks. |

### WorkflowProgressToolTests

`Jube.Test.AskJooby.WorkflowProgressToolTests` — 9 tests. Category: Unit.

| Test                                                                     | Kind | Verifies                                                                       |
|--------------------------------------------------------------------------|------|--------------------------------------------------------------------------------|
| `WithNothingDoneTheFirstStepIsTheNextOneAndNoneAreMarkedCompleteAsync`   | Fact | With nothing done the first step is the next one and none are marked complete. |
| `AStepWhoseOperationsHaveAllRunIsCountedDoneWithoutAnyoneSayingSoAsync`  | Fact | A step whose operations have all run is counted done without anyone saying so. |
| `AStepWithSomeOfItsOperationsRunIsNotCountedDoneAsync`                   | Fact | A step with some of its operations run is not counted done.                    |
| `AnAgentCanOverruleAnInferenceThatSaysAStepIsDoneAsync`                  | Fact | An agent can overrule an inference that says a step is done.                   |
| `AnOperatorStepIsOnlyEverCompletedByBeingStatedAsync`                    | Fact | An operator step is only ever completed by being stated.                       |
| `TheReasonIsCarriedWithTheStatementRatherThanDiscardedAsync`             | Fact | The reason is carried with the statement rather than discarded.                |
| `AStepIdThatIsNotInTheGraphIsRefusedAndTheRealOnesAreNamedAsync`         | Fact | A step id that is not in the graph is refused and the real ones are named.     |
| `AnUnknownWorkflowIsRefusedByBothOperationsRatherThanReportedEmptyAsync` | Fact | An unknown workflow is refused by both operations rather than reported empty.  |
| `EveryStepDoneSaysSoRatherThanOfferingANextStepAsync`                    | Fact | Every step done says so rather than offering a next step.                      |
