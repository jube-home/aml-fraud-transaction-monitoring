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

using FluentAssertions;
using Jube.Service.Agent.RuleExtensionTools;
using Xunit;

namespace Jube.Test.AskJooby
{
    [Trait("Category", "Unit")]
    public sealed class RuleExtensionChainValidatorTests
    {
        [Fact]
        public void AWellFormedChainResolvesAndCarriesTheTypeThroughIt()
        {
            var result = RuleExtensionChainValidator.Validate(
                "Payload.Narrative.RemoveDiacritics().ContainsAny(\"crypto\",\"wallet\")");

            result.Valid.Should().BeTrue(string.Join("; ", result.Problems));
            result.Receiver.Should().Be("Payload.Narrative");
            result.ResultType.Should().Be("Boolean");
            result.Steps.Should().HaveCount(2);
        }

        [Fact]
        public void AMisspeltCallIsReportedRatherThanAbsorbedIntoTheThingBeingOperatedOn()
        {
            var result = RuleExtensionChainValidator.Validate(
                "Payload.Narrative.RemoveDiacritic().ContainsAny(\"a\")");

            result.Valid.Should().BeFalse(
                "a chain that was silently accepted because the broken link looked like part of the "
                + "payload path is the worst answer this tool could give");

            result.Problems.Should().ContainSingle()
                .Which.Should().Contain("RemoveDiacritics",
                    "the near miss is the whole value of the message");
        }

        [Fact]
        public void AMethodCalledOnTheWrongTypeSaysWhichTypeItDoesLiveOn()
        {
            var result = RuleExtensionChainValidator.Validate("Payload.Narrative.IsJustBelowThreshold(10000)");

            result.Valid.Should().BeFalse();
            result.Problems.Should().ContainSingle().Which.Should().Contain("Double").And.Contain("String");
        }

        [Fact]
        public void TheWrongNumberOfArgumentsIsAnsweredWithTheShapesThatDoExist()
        {
            var result = RuleExtensionChainValidator.Validate(
                "Payload.Narrative.LevenshteinDistance(\"a\",\"b\",\"c\")");

            result.Valid.Should().BeFalse();

            result.Problems.Should().ContainSingle()
                .Which.Should().Contain("<String>.LevenshteinDistance(other As String) As Integer");
        }

        [Fact]
        public void AnOptionalArgumentMayBeOmittedAndAParamArrayMayBeRepeated()
        {
            RuleExtensionChainValidator.Validate("Payload.Amount.Round()", "Double")
                .Valid.Should().BeTrue();
            RuleExtensionChainValidator.Validate("Payload.Amount.Round(2)", "Double")
                .Valid.Should().BeTrue();

            RuleExtensionChainValidator.Validate("Payload.Narrative.ContainsAny(\"a\",\"b\",\"c\",\"d\")")
                .Valid.Should().BeTrue();
        }

        [Fact]
        public void ADecimalPointInsideAnArgumentIsNotMistakenForALinkInTheChain()
        {
            var result = RuleExtensionChainValidator.Validate(
                "Payload.Amount.IsJustBelowThreshold(10000.0, 2.5)", "Double");

            result.Valid.Should().BeTrue(string.Join("; ", result.Problems));
            result.Steps.Should().ContainSingle();
        }

        [Fact]
        public void AQuotedDotIsNotMistakenForALinkEither()
        {
            var result = RuleExtensionChainValidator.Validate("Payload.Email.EmailDomain()");

            result.Valid.Should().BeTrue(string.Join("; ", result.Problems));

            RuleExtensionChainValidator.Validate("Payload.Email.Contains(\"a.b.c\")")
                .Valid.Should().BeTrue();
        }

        [Fact]
        public void AnExpressionNamingNothingAtAllSaysSoRatherThanPassing()
        {
            var result = RuleExtensionChainValidator.Validate("Payload.Narrative");

            result.Valid.Should().BeFalse(
                "an expression with no call in it cannot be a pipeline, and reporting it as valid "
                + "would send a model on to embed it in a rule");

            RuleExtensionChainValidator.Validate("").Valid.Should().BeFalse();
        }
    }
}