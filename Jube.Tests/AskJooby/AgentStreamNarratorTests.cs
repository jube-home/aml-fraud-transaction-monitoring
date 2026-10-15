/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Jube.AskJooby.Agents;
using Jube.AskJooby.Agents.Models;
using Jube.AskJooby.Agents.Basic;
using Jube.AskJooby.Observability;
using Jube.AskJooby.Observability.Models;
using Jube.AskJooby.Tools.Clarification;
using Microsoft.Extensions.AI;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "AskJooby")]
    public sealed class AgentStreamNarratorTests
    {
        [Fact]
        public void DescribeIncludesTheExceptionMessageWhenASpawnedAgentFails()
        {
            var call = new FunctionCallContent(
                "call-1", "SpawnAgent", new Dictionary<string, object?> { ["agentType"] = "researcher" });

            var result = new FunctionResultContent("call-1", null)
            {
                Exception = new InvalidOperationException("the researcher's chat client rejected the request")
            };

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Contain($"{Named("researcher")} from Researcher hit an error");
            item.Text.Should().Contain("the researcher's chat client rejected the request");
        }

        [Fact]
        public void DescribeIncludesTheExceptionMessageForAnyOtherFailedTool()
        {
            var call = new FunctionCallContent("call-2", "SomeOtherTool", new Dictionary<string, object?>());

            var result = new FunctionResultContent("call-2", null)
            {
                Exception = new InvalidOperationException("embedding generator unavailable")
            };

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Contain("SomeOtherTool hit an error");
            item.Text.Should().Contain("embedding generator unavailable");
        }

        [Fact]
        public void DescribeEchoesRememberSSuccessTextRatherThanAGenericLine()
        {
            var call = new FunctionCallContent("call-3", "Remember", new Dictionary<string, object?>());
            var result = new FunctionResultContent("call-3", "Remembered at the Salt Lick (System scope).");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Remembered at the Salt Lick (System scope).");
        }

        [Fact]
        public void DescribeCountsRecallHitsRatherThanShowingRawContent()
        {
            var call = new FunctionCallContent("call-4", "Recall",
                new Dictionary<string, object?> { ["query"] = "velocity rule" });

            var result = new FunctionResultContent("call-4", "[System] This is an example of a correct response.\n" +
                                                             "content one\n\n" +
                                                             "[User] This is an example of an agent generated memory.\n" +
                                                             "content two\n");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Enjoyed the Salt Lick for \"velocity rule\" -- found 2 memories.");
        }

        [Fact]
        public void DescribeCountsSearchHistoryHitsRatherThanShowingRawContent()
        {
            var call = new FunctionCallContent("call-history", "SearchHistory",
                new Dictionary<string, object?> { ["query"] = "case management" });

            var result = new FunctionResultContent("call-history",
                "[Conversation #1] hit one\n[Conversation #2] (summary) hit two\n");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Traced back Hoofprints for \"case management\" -- found 2 prints.");
        }

        [Fact]
        public void DescribeReportsNoMatchesForSearchHistory()
        {
            var call = new FunctionCallContent("call-history-2", "SearchHistory",
                new Dictionary<string, object?> { ["query"] = "case management" });

            var result = new FunctionResultContent("call-history-2", "No matching Hoofprints found.");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Traced back Hoofprints for \"case management\" -- nothing matched.");
        }

        [Fact]
        public void DescribeStartUsesTheAnimalPersonaVerbsForEachTool()
        {
            var ragStart = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-a", "SearchRag", new Dictionary<string, object?> { ["query"] = "q" }));
            var recallStart = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-b", "Recall", new Dictionary<string, object?> { ["query"] = "q" }));
            var historyStart = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-c", "SearchHistory", new Dictionary<string, object?> { ["query"] = "q" }));

            ragStart!.Text.Should().Be("Grazing the Pasture for \"q\"...");
            recallStart!.Text.Should().Be("Enjoying the Salt Lick for \"q\"...");
            historyStart!.Text.Should().Be("Tracing back Hoofprints for \"q\"...");
        }

        [Fact]
        public void DescribeStartAnnouncesHerdingTheJoobriksForSpawnAgents()
        {
            var start = AgentStreamNarrator.DescribeStart(
                new FunctionCallContent("call-fanout", "SpawnAgents", new Dictionary<string, object?>()));

            start!.Text.Should().Be("Herding the Joobriks and scaring a stampede...");
        }

        [Fact]
        public void ASurfacedFailureStillReadsAsAFailureRatherThanAnOddLookingAnswer()
        {
            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-f1", "EntityAnalysisModelActivationRuleCreate",
                    new Dictionary<string, object?> { ["name"] = "TransactionVelocityThreshold" }),
                new FunctionResultContent("call-f1", new ToolInvocationFailure(
                    "EntityAnalysisModelActivationRuleCreate",
                    "CoderRuleScript: Line 1: Abstraction does not exist.")));

            item.Text.Should().Contain("hit an error");

            item.Text.Should().Contain("Abstraction does not exist",
                "the pill must carry the reason now that the tool no longer throws");

            item.Detail.Should().Contain("Failed: CoderRuleScript: Line 1: Abstraction does not exist.");
        }

        [Fact]
        public void AToolPillCarriesTheExchangeBehindItSoTheCallerCanSeeWhatWasPassedAndReturned()
        {
            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-d1", "ListQueryableTables",
                    new Dictionary<string, object?> { ["nameContains"] = "exhaustive" }),
                new FunctionResultContent("call-d1", "3 tables you may query:\nExhaustiveSearchInstance"));

            item.Detail.Should().StartWith("Passed: nameContains: exhaustive");
            item.Detail.Should().Contain("Returned: 3 tables you may query");
        }

        [Fact]
        public void AFailedToolSaysSoInTheExchangeRatherThanShowingAnEmptyReturn()
        {
            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-d2", "RunSqlQuery",
                    new Dictionary<string, object?> { ["sql"] = "select 1" }),
                new FunctionResultContent("call-d2", null)
                {
                    Exception = new InvalidOperationException("relation does not exist")
                });

            item.Detail.Should().Contain("Passed: sql: select 1");
            item.Detail.Should().Contain("Failed: relation does not exist");
        }

        [Fact]
        public void AToolCalledWithNothingAndReturningNothingCarriesNoExchangeAtAll()
        {
            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-d3", "ListQueryableTables",
                    new Dictionary<string, object?>()),
                new FunctionResultContent("call-d3", string.Empty));

            (item.Detail is null).Should().BeTrue(
                "a pill with nothing behind it must not offer a door onto emptiness");
        }

        [Fact]
        public void ADelegationEndsWithTheJoobriksCompletionWordInsteadOfAGistOfItsReport()
        {
            var definition = AgentDefinitions.All["advanced-analytics-model-inspector"];

            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-1", "SpawnAgent",
                    new Dictionary<string, object?> { ["agentType"] = "advanced-analytics-model-inspector" }),
                new FunctionResultContent("call-1",
                    "## Findings\n\nVersion 4 beats version 3 on the held-out split.\n\nMore detail follows."));

            item.Text.Should().StartWith(definition.DisplayName!);

            item.Text.Should()
                .Be(
                    $"{definition.DisplayName} from Advanced Analytics Model Inspector has completed: {definition.CompletionWord}.");

            item.Text.Should().NotContain("Findings",
                "the activity line carries the word, the report stays in the detail");
        }

        [Fact]
        public void ALongReportNoLongerLeaksAnyOfItsTextIntoTheActivityLine()
        {
            var report = string.Join(" ", Enumerable.Repeat("elaborate", 60));
            var definition = AgentDefinitions.All["advanced-analytics-instance-inspector"];

            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-2", "SpawnAgent",
                    new Dictionary<string, object?> { ["agentType"] = "advanced-analytics-instance-inspector" }),
                new FunctionResultContent("call-2", report));

            item.Text.Should().EndWith($": {definition.CompletionWord}.");
            item.Text.Should().NotContain("elaborate", "the summary that was truncated mid-report is gone");
        }

        [Fact]
        public void AReportThatCameBackEmptyStillCompletesWithItsWord()
        {
            var definition = AgentDefinitions.All["advanced-analytics-instance-inspector"];

            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-3", "SpawnAgent",
                    new Dictionary<string, object?> { ["agentType"] = "advanced-analytics-instance-inspector" }),
                new FunctionResultContent("call-3", "   "));

            item.Text.Should().EndWith($": {definition.CompletionWord}.");
        }

        [Fact]
        public void AFanOutAttributesEachGistToTheJoobrikThatGaveIt()
        {
            var joined =
                "[advanced-analytics-instance-inspector #1] list the instances\n"
                + "There are four instances, two of them allocated.\n\n"
                + "[advanced-analytics-model-inspector #2] list the models\n"
                + "Nine models, newest trained on Tuesday.\n\n";

            var item = AgentStreamNarrator.Describe(
                new FunctionCallContent("call-4", "SpawnAgents", new Dictionary<string, object?>()),
                new FunctionResultContent("call-4", joined));

            item.Text.Should()
                .Contain(
                    $"{Named("advanced-analytics-instance-inspector")}: {Word("advanced-analytics-instance-inspector")}");
            item.Text.Should()
                .Contain(
                    $"{Named("advanced-analytics-model-inspector")}: {Word("advanced-analytics-model-inspector")}");

            item.Text.Should().Contain(" / ", "two Joobriks reporting must not read as one voice");
        }

        [Fact]
        public void EveryAgentInTheHierarchyHasAPersonaOfItsOwnAndNoTwoShareOne()
        {
            var named = AgentDefinitions.All
                .Where(entry => AgentDefinitions.HierarchyDomainOf.ContainsKey(entry.Key)
                                || entry.Value.DisplayName is not null)
                .ToList();

            named.Where(entry => entry.Value.DisplayName is null).Select(entry => entry.Key)
                .Should().BeEmpty("a colleague you can ask for by name is the point, and an agent with "
                                  + "no name of its own cannot be asked for");

            named.GroupBy(entry => entry.Value.DisplayName)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .Should().BeEmpty("one name, one agent -- a name shared between two of them reads as a "
                                  + "single agent repeating itself, which is exactly what made a fan out "
                                  + "of six specialists all announce themselves as the same person");
        }

        private static string Named(string agentType)
        {
            return AgentDefinitions.All[agentType].DisplayName!;
        }

        private static string Word(string agentType)
        {
            return AgentDefinitions.All[agentType].CompletionWord!;
        }

        [Fact]
        public void EveryNamedAgentOpensItsPromptWithItsOwnNameAndGenderAndHasACompletionWord()
        {
            var named = AgentDefinitions.All
                .Where(entry => entry.Key != "validator" && entry.Value.DisplayName is not null)
                .ToList();

            named.Should().HaveCount(220,
                "every hierarchy agent plus the researcher and the web research specialist carries a persona");

            foreach (var (agentType, definition) in named)
            {
                definition.Instructions.Should().MatchRegex(
                    "^Your name is " + Regex.Escape(definition.DisplayName!)
                                     + @", and you are a (man|woman), so use (he/him|she/her) for yourself\. ",
                    $"{agentType} must open with its own name and gender");

                definition.CompletionWord.Should().NotBeNullOrWhiteSpace(
                    $"{agentType} needs a completion word for its activity line");
            }
        }

        [Fact]
        public void DescribeAgentLabelIsTheAgentsNameAloneAndNotItsJobTitle()
        {
            AgentStreamNarrator.DescribeAgentLabel("redis-observability")
                .Should().NotContain("Redis",
                    "the label is who turned up, not what they do -- the transcript already carries "
                    + "the work in the line beside it")
                .And.NotBe(Named("platform-ops"),
                    "a specialist carries its own name, never the one belonging to the domain above it");

            AgentStreamNarrator.DescribeAgentLabel("telemetry-log-counters-and-runtime-metrics")
                .Should().NotBe(AgentStreamNarrator.DescribeAgentLabel(
                        "containers-load-balancer-and-overlay-network"),
                    "two leaves under one domain must never share a label");

            AgentStreamNarrator.DescribeAgentLabel("platform-ops")
                .Should().Be(Named("platform-ops"), "a domain is named the same way as everybody else");

            AgentStreamNarrator.DescribeAgentLabel("general")
                .Should().Be("general", "an agent with no name of its own is left exactly as it was");
        }

        [Fact]
        public void ASelfSpawnIsAnnouncedAsAFurtherStrandRatherThanAnotherAgent()
        {
            AgentStreamNarrator.DescribeSpawnAnnouncement("model-admin", "model-admin")
                .Should().Be($"{Named("model-admin")} opening strand 2 of its thinking...",
                    "an agent spawning its own type is thinking along a second strand, not calling a colleague");

            AgentStreamNarrator.DescribeSpawnAnnouncement("model-admin", "platform-ops")
                .Should().Be($"{Named("model-admin")} picking up the yoke...");
        }

        [Fact]
        public void ASelfSpawnsStepsAreDistinguishableFromItsParents()
        {
            AgentStreamNarrator.DescribeSpawnLabel("model-admin", "model-admin")
                .Should().Be($"{Named("model-admin")} strand 2",
                    "the second strand still needs its own label, or its steps interleave "
                    + "indistinguishably with the agent that started it");

            AgentStreamNarrator.DescribeSpawnLabel("model-admin", "platform-ops")
                .Should().Be(Named("model-admin"));
        }

        [Fact]
        public void AFanOutNumbersSelfSpawnsAsStrandsAndEveryoneElseByPosition()
        {
            AgentStreamNarrator.DescribeSpawnLabel("model-admin", "model-admin", 0)
                .Should().Be($"{Named("model-admin")} strand 2",
                    "the agent that fanned out is strand one, so the first child it spawns of its own type is strand two");

            AgentStreamNarrator.DescribeSpawnLabel("model-admin", "model-admin", 2)
                .Should().Be($"{Named("model-admin")} strand 4");

            AgentStreamNarrator.DescribeSpawnLabel("model-admin", "platform-ops", 1)
                .Should().Be($"{Named("model-admin")} #2",
                    "a colleague called in alongside others is still numbered by its place in the fan out");
        }

        [Fact]
        public void TheCheckerIsNamedFromTheValidatorDefinitionRatherThanWrittenOutAgain()
        {
            AgentDefinitions.All["validator"].DisplayName.Should().Be("Jadek");
        }

        [Fact]
        public void TheRetiredReasonerIsNoLongerNarrated()
        {
            AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                    "call-reason", "ReasonAboutHardProblem", new Dictionary<string, object?>()))!
                .Text.Should().NotContain("Jadek");
        }

        [Fact]
        public void DescribeStartNamesTheAgentsAFanOutIsCallingIn()
        {
            var item = AgentStreamNarrator.DescribeStart(
                new FunctionCallContent("call-fanout", "SpawnAgents", new Dictionary<string, object?>
                {
                    ["subTasks"] = "[{\"agentType\":\"platform-ops\",\"task\":\"x\"},"
                                   + "{\"agentType\":\"model-risk\",\"task\":\"y\"}]"
                }));

            item!.Text.Should().Contain(Named("platform-ops"), "a named domain is announced by its name")
                .And.Contain(Named("model-risk"))
                .And.StartWith("Herding the Joobriks");
        }

        [Fact]
        public void DescribeStartNamesEverySpawnedLeafSeparatelyRatherThanCollapsingThem()
        {
            var item = AgentStreamNarrator.DescribeStart(
                new FunctionCallContent("call-fanout", "SpawnAgents", new Dictionary<string, object?>
                {
                    ["subTasks"] =
                        "[{\"agentType\":\"redis-observability\",\"task\":\"a\"},"
                        + "{\"agentType\":\"postgres-observability\",\"task\":\"b\"},"
                        + "{\"agentType\":\"containers-load-balancer-and-overlay-network\",\"task\":\"c\"}]"
                }));

            item!.Text.Should().Contain(AgentStreamNarrator.DescribeAgentLabel("redis-observability"))
                .And.Contain(AgentStreamNarrator.DescribeAgentLabel("postgres-observability"))
                .And.Contain(AgentStreamNarrator.DescribeAgentLabel(
                        "containers-load-balancer-and-overlay-network"),
                    "three leaves of one domain used to collapse into a single name, so a fan-out of "
                    + "seven announced one agent and looked like no spawn had happened at all");
        }

        [Fact]
        public void DescribeStartFallsBackToTheHerdingLineWhenTheFanOutNamesNoAgent()
        {
            var item = AgentStreamNarrator.DescribeStart(
                new FunctionCallContent("call-fanout", "SpawnAgents", new Dictionary<string, object?>()));

            item!.Text.Should().Be("Herding the Joobriks and scaring a stampede...");
        }

        [Fact]
        public void DescribeReportsBackForSpawnAgentsOnSuccessAndFailure()
        {
            var call = new FunctionCallContent("call-fanout", "SpawnAgents", new Dictionary<string, object?>());
            var success = AgentStreamNarrator.Describe(call, new FunctionResultContent("call-fanout", "reports"));

            var failure = AgentStreamNarrator.Describe(call, new FunctionResultContent("call-fanout", null)
            {
                Exception = new InvalidOperationException("one Joobrik never came back")
            });

            success.Text.Should().Be("The Joobriks reported back: reports");
            failure.Text.Should().Be("The Joobriks hit an error. (one Joobrik never came back)");
        }

        [Fact]
        public void DescribeReportsBackSilentlyOnSuccessWithNoExceptionNoise()
        {
            var call = new FunctionCallContent(
                "call-3", "SpawnAgent", new Dictionary<string, object?> { ["agentType"] = "researcher" });

            var result = new FunctionResultContent("call-3", "some final answer");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should()
                .Be(
                    $"{AgentDefinitions.All["researcher"].DisplayName} from Researcher has completed: {AgentDefinitions.All["researcher"].CompletionWord}.");
        }

        [Fact]
        public void DescribeReadsTheActualBiteCountBackOutOfTheSearchRagResult()
        {
            var call = new FunctionCallContent(
                "call-4", "SearchRag", new Dictionary<string, object?> { ["query"] = "abstraction rule syntax" });

            var result = new FunctionResultContent("call-4", "Found 6 Bites.\n\n[Document/Concept] ...");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Grazed the Pasture for \"abstraction rule syntax\" -- returned 6 Bites.");
        }

        [Fact]
        public void DescribeUsesSingularBiteForExactlyOneResult()
        {
            var call = new FunctionCallContent(
                "call-5", "SearchRag", new Dictionary<string, object?> { ["query"] = "velocity rule" });

            var result = new FunctionResultContent("call-5", "Found 1 Bite.\n\n[Document/Concept] ...");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Grazed the Pasture for \"velocity rule\" -- returned 1 Bite.");
        }

        [Fact]
        public void DescribeReportsNoBitesFoundWhenSearchRagFindsNothing()
        {
            var call = new FunctionCallContent(
                "call-6", "SearchRag", new Dictionary<string, object?> { ["query"] = "nonexistent thing" });

            var result = new FunctionResultContent("call-6", "No matching Bites found.");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Text.Should().Be("Grazed the Pasture for \"nonexistent thing\" -- no Bites found.");
        }

        [Fact]
        public void DescribeStartSuppressesTheGenericStepPillForAskClarifyingQuestion()
        {
            var start = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-7", "AskClarifyingQuestion", new Dictionary<string, object?>()));

            start.Should().BeNull();
        }

        [Fact]
        public void DescribeTurnsAskClarifyingQuestionIntoAClarificationItemCarryingTheRealOptions()
        {
            var call = new FunctionCallContent("call-7", "AskClarifyingQuestion", new Dictionary<string, object?>());

            var toolResult = AskClarifyingQuestionTool.AskClarifyingQuestion(
                "Which rule type do you mean?", ["Activation rules", "Gateway rules"], false);

            var result = new FunctionResultContent("call-7", toolResult);

            var item = AgentStreamNarrator.Describe(call, result);

            item.Kind.Should().Be(AgentStreamItemKind.Clarification);
            item.Text.Should().Be("Which rule type do you mean?");
            item.Options.Should().Equal("Activation rules", "Gateway rules");
            item.AllowFreeText.Should().BeFalse();
        }

        [Fact]
        public void DescribeFallsBackToAStepLineIfAskClarifyingQuestionsPayloadIsUnparseable()
        {
            var call = new FunctionCallContent("call-8", "AskClarifyingQuestion", new Dictionary<string, object?>());
            var result = new FunctionResultContent("call-8", "not valid json");

            var item = AgentStreamNarrator.Describe(call, result);

            item.Kind.Should().Be(AgentStreamItemKind.Step);
            item.Text.Should().Be("Called AskClarifyingQuestion.");
        }

        [Fact]
        public void DescribeReportsTheExceptionMessageWhenAskClarifyingQuestionFails()
        {
            var call = new FunctionCallContent("call-9", "AskClarifyingQuestion", new Dictionary<string, object?>());

            var result = new FunctionResultContent("call-9", null)
            {
                Exception = new InvalidOperationException("boom")
            };

            var item = AgentStreamNarrator.Describe(call, result);

            item.Kind.Should().Be(AgentStreamItemKind.Step);
            item.Text.Should().Contain("AskClarifyingQuestion hit an error").And.Contain("boom");
        }

        [Fact]
        public void DescribeStartNamesADomainJoobrikRatherThanPrintingItsSlug()
        {
            var start = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-named", "SpawnAgent", new Dictionary<string, object?> { ["agentType"] = "platform-ops" }));

            start!.Text.Should().Be($"Calling in {Named("platform-ops")} from Platform Ops...");
        }

        [Fact]
        public void DescribeNamesADomainJoobrikOnSuccessAndOnFailure()
        {
            var call = new FunctionCallContent(
                "call-named", "SpawnAgent", new Dictionary<string, object?> { ["agentType"] = "platform-ops" });

            var success = AgentStreamNarrator.Describe(call, new FunctionResultContent("call-named", "done"));

            var failure = AgentStreamNarrator.Describe(call, new FunctionResultContent("call-named", null)
            {
                Exception = new InvalidOperationException("etcd unreachable")
            });

            var definition = AgentDefinitions.All["platform-ops"];

            success.Text.Should()
                .Be($"{definition.DisplayName} from Platform Ops has completed: {definition.CompletionWord}.");

            failure.Text.Should().Be($"{definition.DisplayName} from Platform Ops hit an error. (etcd unreachable)");
        }

        [Fact]
        public void DescribeStartFallsBackToTheSlugForAnAgentThatCarriesNoName()
        {
            var start = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-plain", "SpawnAgent", new Dictionary<string, object?> { ["agentType"] = "general" }));

            start!.Text.Should().Be("Calling in a Joobrik (general)...");
        }

        [Theory]
        [InlineData("platform-ops")]
        [InlineData("model-risk")]
        [InlineData("researcher")]
        [InlineData("general")]
        [InlineData("platform-ops.database")]
        public void DescribeAgentLabelDecoratesOnlyTheAgentsThatCarryAName(string agentType)
        {
            var expected = AgentDefinitions.All.TryGetValue(agentType, out var definition) &&
                           definition.DisplayName is not null
                ? definition.DisplayName
                : agentType;

            AgentStreamNarrator.DescribeAgentLabel(agentType).Should().Be(expected);
        }

        [Fact]
        public void DescribeStartHumanisesABoundServiceOperationName()
        {
            var start = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-tool", "PostgresActivityList", new Dictionary<string, object?>()));

            start!.Text.Should().Be("Listing Postgres activity...");
        }

        [Fact]
        public void DescribeHumanisesABoundServiceOperationNameOnSuccessAndOnFailure()
        {
            var call = new FunctionCallContent(
                "call-tool", "EntityAnalysisModelTagCreate", new Dictionary<string, object?>());

            var success = AgentStreamNarrator.Describe(call, new FunctionResultContent("call-tool", "ok"));

            var failure = AgentStreamNarrator.Describe(call, new FunctionResultContent("call-tool", null)
            {
                Exception = new InvalidOperationException("permission denied")
            });

            success.Text.Should().Be("Created an entity analysis model tag.");
            failure.Text.Should().Be("Creating an entity analysis model tag hit an error. (permission denied)");
        }

        [Fact]
        public void RoutingPassesTheYokeRatherThanCallingSomeoneIn()
        {
            var announcement = AgentStreamNarrator.DescribeRoutingAnnouncement("platform-ops");

            announcement.Should().StartWith("Passing the yoke to ");
            announcement.Should().EndWith("...");
            announcement.Should().NotContain("Calling in");
        }

        [Fact]
        public void TheSpawnBannerAndTheHandoffBannerDoNotReadAsTheSameEvent()
        {
            var spawn = AgentStreamNarrator.DescribeStart(new FunctionCallContent(
                "call-named", "SpawnAgent", new Dictionary<string, object?> { ["agentType"] = "platform-ops" }));

            var routed = AgentStreamNarrator.DescribeRoutingAnnouncement("platform-ops");

            spawn!.Kind.Should().Be(AgentStreamItemKind.Delegation);
            spawn.Text.Should().StartWith("Calling in ");
            routed.Should().StartWith("Passing the yoke to ");
            routed.Should().NotBe(spawn.Text);
        }

        [Fact]
        public void TheRoutingDetailNamesTheTierThatDecidedAndTheAgentThatNowHoldsTheTurn()
        {
            var detail = AgentStreamNarrator.DescribeRoutingDetail("platform-ops", AgentRoutingStage.Continuity,
                0.82, 134);

            detail.Should().Contain("`platform-ops`");
            detail.Should().Contain("unfinished");
            detail.Should().Contain("0.82");
            detail.Should().Contain("134ms");
        }

        [Fact]
        public void TheRoutingDetailLeavesOutAConfidenceTheTierNeverProduced()
        {
            var detail = AgentStreamNarrator.DescribeRoutingDetail("platform-ops", AgentRoutingStage.Pattern,
                null, 2);

            detail.Should().NotContain("Confidence");
            detail.Should().Contain("a wording rule");
        }

        [Theory]
        [InlineData(AgentRoutingStage.Pattern)]
        [InlineData(AgentRoutingStage.Continuity)]
        [InlineData(AgentRoutingStage.Embedding)]
        [InlineData(AgentRoutingStage.Model)]
        [InlineData(AgentRoutingStage.Default)]
        [InlineData(AgentRoutingStage.NoCandidates)]
        public void EveryTierReadsAsProseRatherThanAsItsEnumName(AgentRoutingStage stage)
        {
            var detail = AgentStreamNarrator.DescribeRoutingDetail("general", stage, null, 1);

            detail.Should().Contain("Decided by: ");
            detail.Should().NotContain("Decided by: " + stage);
        }
    }
}