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

using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Jube.AskJooby.Agents.Basic;
using Jube.Service.Agent.RuleExtensionTools;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class RuleExtensionCatalogueTests
    {
        [Fact]
        public void TheCatalogueFindsTheWholeExtensionSurfaceAndNotAHandfulOfIt()
        {
            RuleExtensionCatalogue.All.Should().HaveCountGreaterThan(1000,
                "the rule language is over a thousand extension methods, and a catalogue that found "
                + "a few dozen would be worse than none: a model would search it, find nothing, and "
                + "conclude the operation does not exist");
        }

        [Fact]
        public void EverySignatureIsRenderedInVisualBasicAndNotInTheLanguageItWasWrittenIn()
        {
            var clrSpellings = new[] { "Int32", "Single", "Byte[]", "String[]", "Double[]" };

            foreach (var signature in RuleExtensionCatalogue.All)
            {
                foreach (var parameter in signature.Parameters)
                {
                    clrSpellings.Should().NotContain(parameter.Type,
                        $"{signature.Name} would be pasted into a Visual Basic fragment verbatim, and "
                        + "a CLR spelling there does not compile");
                }

                signature.Declaration.Should().StartWith("<" + signature.Instance + ">." + signature.Name);
            }
        }

        [Fact]
        public void AParameterlessExtensionRendersAsTheCallARuleActuallyWrites()
        {
            var distance = RuleExtensionCatalogue.All.Single(signature =>
                signature.Name == "LevenshteinDistance" && signature.Instance == "String");

            distance.Declaration.Should().Be("<String>.LevenshteinDistance(other As String) As Integer");
        }

        [Fact]
        public void AParamArrayIsAdvertisedAsOneRatherThanAsAnArrayArgument()
        {
            var containsAny = RuleExtensionCatalogue.All.First(signature =>
                signature.Name == "ContainsAny" && signature.Instance == "String"
                                                && signature.Parameters.Count == 1);

            containsAny.Declaration.Should().Contain("ParamArray",
                "a rule author passing one array where the method takes a list of values writes a "
                + "fragment that does not compile, and nothing else tells them which it is");
        }

        [Fact]
        public void TheEvalMechanismIsNotOfferedAsSomethingToCall()
        {
            RuleExtensionCatalogue.All.Should().NotContain(signature => signature.Name == "Eval",
                "the parser excludes it for the same reason: what a rule author writes is the "
                + "registered name, which the parser rewrites into the Eval call");
        }

        [Fact]
        public void EveryAgentGivenTheseToolsIsOneThatActuallyWritesAFragment()
        {
            foreach (var agent in RuleExtensionAgentToolNames.Agents)
            {
                AgentDefinitions.All.Should().ContainKey(agent,
                    "tools bound to an agent that does not exist are bound to nothing at all");
            }

            RuleExtensionAgentToolNames.Agents.Should().OnlyHaveUniqueItems();
        }

        [Fact]
        public void WhatTheModelActuallyReceivesIsTheVisualBasicDeclarationAndNotTheSameThingTwice()
        {
            var signature = RuleExtensionCatalogue.All.First(candidate =>
                candidate.Name == "LevenshteinDistance" && candidate.Instance == "String");

            var json = JsonSerializer.Serialize(signature);

            json.Should().Contain("Declaration",
                "the declaration is the only part of this a rule author can paste");

            json.Should().NotContain("Parameters",
                "the parameter breakdown says exactly what the declaration already says, and a "
                + "search returning sixty rows would pay for it sixty times");
            json.Should().Contain("\"Origin\":\"Extension\"",
                "an origin reaching the model as a bare 1 tells it nothing about whether the method "
                + "exists outside this process");
        }

        [Fact]
        public void EveryToolNameBindsToARealMethodOnTheToolClass()
        {
            var bound = RuleExtensionToolBinder.Bind(RuleExtensionAgentToolNames.Tools);

            bound.Select(tool => tool.Function.Name).Should()
                .BeEquivalentTo(RuleExtensionAgentToolNames.Tools,
                    "a name in the list that matches no method is dropped silently by the binder, so "
                    + "the agent simply never receives it");
        }
    }
}