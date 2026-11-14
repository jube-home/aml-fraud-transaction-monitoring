/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.
 *
 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;
using FluentAssertions;
using Jube.Engine.Exhaustive.Extensions;
using Jube.Engine.Exhaustive.Models;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class ExhaustiveRecallTests
    {
        private static readonly List<ExhaustiveSearchInstancePromotedTrialInstanceVariable> variables =
        [
            new() { Name = "Abstraction.A", Mean = 10, Sd = 2, NormalisationTypeId = 2 },
            new() { Name = "Abstraction.B", Mean = 0.4, Sd = 0.5, NormalisationTypeId = 1 },
            new() { Name = "Abstraction.C", Mean = 5, Sd = 0, NormalisationTypeId = 2 }
        ];

        [Fact]
        public void BuildScoreInputs_AllPresent_StandardisesType2Only()
        {
            var body = JObject.Parse("{\"Abstraction.A\": 14, \"Abstraction.B\": 1, \"Abstraction.C\": 9}");

            ExhaustiveExtensions.BuildScoreInputs(variables, body).Should().Equal(2, 1, 0);
        }

        [Fact]
        public void BuildScoreInputs_MissingKeys_AreMeanImputed_NotZero()
        {
            var inputs = ExhaustiveExtensions.BuildScoreInputs(variables, JObject.Parse("{}"));

            inputs.Should().Equal(0, 0.4, 0);
        }

        [Fact]
        public void BuildScoreInputs_MissingMatchesInPipelineFallback()
        {
            var inputs = ExhaustiveExtensions.BuildScoreInputs(variables, JObject.Parse("{}"));

            for (var i = 0; i < variables.Count; i++)
            {
                inputs[i].Should().Be(variables[i].ZScore(variables[i].Mean),
                    "in-pipeline recall uses ZScore(Mean) for an absent value");
            }
        }

        [Fact]
        public void BuildScoreInputs_JsonNullAndUnconvertibleValues_AreTreatedAsMissing()
        {
            var body = JObject.Parse("{\"Abstraction.A\": null, \"Abstraction.B\": \"not a number\"}");

            ExhaustiveExtensions.BuildScoreInputs(variables, body).Should().Equal(0, 0.4, 0);
        }

        [Fact]
        public void BuildScoreInputs_MatchIsCaseSensitiveAndExact()
        {
            var body = JObject.Parse("{\"abstraction.a\": 14}");

            ExhaustiveExtensions.BuildScoreInputs(variables, body)[0].Should().Be(0);
        }

        [Fact]
        public void BuildScoreInputs_ExtremeContinuousValue_IsClampedToTheConfiguredBoundBothDirections()
        {
            var body = JObject.Parse("{\"Abstraction.A\": 110, \"Abstraction.C\": 5}");

            var inputs = ExhaustiveExtensions.BuildScoreInputs(variables, body);

            inputs[0].Should().Be(5);
        }

        [Fact]
        public void BuildScoreInputs_ExtremeNegativeContinuousValue_IsClampedToTheConfiguredBound()
        {
            var body = JObject.Parse("{\"Abstraction.A\": -90, \"Abstraction.C\": 5}");

            var inputs = ExhaustiveExtensions.BuildScoreInputs(variables, body);

            inputs[0].Should().Be(-5);
        }

        [Fact]
        public void ZScore_ExtremeValue_IsClampedToTheConfiguredBoundInBothDirections()
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Mean = 10, Sd = 2, NormalisationTypeId = 2 };

            variable.ZScore(10 + 2 * 50).Should().Be(5);
            variable.ZScore(10 - 2 * 50).Should().Be(-5);
        }

        [Fact]
        public void ZScore_ValueExactlyAtTheBound_IsUnaffected()
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Mean = 10, Sd = 2, NormalisationTypeId = 2 };

            variable.ZScore(10 + 2 * 5).Should().Be(5);
            variable.ZScore(10 - 2 * 5).Should().Be(-5);
        }

        [Fact]
        public void ZScore_ValueWithinTheBound_IsByteIdenticalToTheUnclampedZScore()
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Mean = 10, Sd = 2, NormalisationTypeId = 2 };

            variable.ZScore(10 + 2 * 4.9).Should().Be(4.9);
        }

        [Theory]
        [InlineData((byte)0)]
        [InlineData((byte)1)]
        public void ZScore_NonStandardisedVariable_IsNeverClampedEvenWhenLarge(byte normalisationTypeId)
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Mean = 0, Sd = 1, NormalisationTypeId = normalisationTypeId };

            variable.ZScore(999).Should().Be(999);
        }

        [Fact]
        public void ZScore_UsesTheVariablesConfiguredClipBoundNotAHardcodedFive()
        {
            var variable = new ExhaustiveSearchInstancePromotedTrialInstanceVariable
                { Mean = 10, Sd = 2, NormalisationTypeId = 2, ZScoreClipBound = 3 };

            variable.ZScore(10 + 2 * 50).Should().Be(3);
            variable.ZScore(10 - 2 * 50).Should().Be(-3);
        }
    }
}