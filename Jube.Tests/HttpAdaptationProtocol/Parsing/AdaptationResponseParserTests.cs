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

using FluentAssertions;
using Jube.HttpAdaptationProtocol.Parsing;
using Jube.Test.Infrastructure;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Parsing
{
    [Trait("Category", "Unit")]
    public sealed class AdaptationResponseParserTests
    {
        [Fact]
        public void NullBodyReturnsAnEmptyResponseError()
        {
            var log = new TestLog();

            var adaptation = ((string?)null).ParseAdaptationResponse(log);

            adaptation.Error.Should().Be("The HTTP Adaptation endpoint returned an empty response body.");
            adaptation.Value.Should().BeNull();
            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void EmptyBodyReturnsAnEmptyResponseError()
        {
            var log = new TestLog();

            var adaptation = "".ParseAdaptationResponse(log);

            adaptation.Error.Should().Be("The HTTP Adaptation endpoint returned an empty response body.");
        }

        [Fact]
        public void WhitespaceOnlyBodyReturnsAnEmptyResponseError()
        {
            var log = new TestLog();

            var adaptation = "   \t  \n".ParseAdaptationResponse(log);

            adaptation.Error.Should().Be("The HTTP Adaptation endpoint returned an empty response body.");
        }

        [Theory]
        [InlineData("0.85", 0.85)]
        [InlineData("0", 0.0)]
        [InlineData("-3.5", -3.5)]
        [InlineData("1e3", 1000.0)]
        [InlineData("  0.42  ", 0.42)]
        public void BareNumericBodyIsParsedDirectlyAsTheValue(string rawBody, double expectedValue)
        {
            var log = new TestLog();

            var adaptation = rawBody.ParseAdaptationResponse(log);

            adaptation.Value.Should().Be(expectedValue);
            adaptation.Error.Should().BeNull();
            adaptation.IsSuppressed.Should().BeFalse();
        }

        [Theory]
        [InlineData("[0.85]", 0.85)]
        [InlineData("[ 0.85 ]", 0.85)]
        [InlineData("[-1.25]", -1.25)]
        public void BracketedBareNumericBodyIsUnwrappedAndParsedAsTheValue(string rawBody, double expectedValue)
        {
            var log = new TestLog();

            var adaptation = rawBody.ParseAdaptationResponse(log);

            adaptation.Value.Should().Be(expectedValue);
            adaptation.Error.Should().BeNull();
        }

        [Fact]
        public void CommaDecimalSeparatorIsNotTreatedAsANumberAndFallsThroughToAnUnparsableError()
        {
            var log = new TestLog();

            var adaptation = "3,5".ParseAdaptationResponse(log);

            adaptation.Value.Should().BeNull();
            adaptation.Error.Should()
                .Be(
                    "The HTTP Adaptation endpoint returned a response body that could not be parsed as a bare number or a protocol Adaptation object.");
        }

        [Fact]
        public void MalformedJsonReturnsAnUnparsableErrorAndLogsAWarning()
        {
            var log = new TestLog();

            var adaptation = "{ this is not valid json".ParseAdaptationResponse(log);

            adaptation.Error.Should()
                .Be(
                    "The HTTP Adaptation endpoint returned a response body that could not be parsed as a bare number or a protocol Adaptation object.");
            adaptation.Value.Should().BeNull();
            log.Entries.Should().ContainSingle(e => e.Level == "WARN");
        }

        [Fact]
        public void JsonArrayThatCannotDeserializeIntoAnAdaptationReturnsAnUnparsableError()
        {
            var log = new TestLog();

            var adaptation = "[1,2,3]".ParseAdaptationResponse(log);

            adaptation.Error.Should()
                .Be(
                    "The HTTP Adaptation endpoint returned a response body that could not be parsed as a bare number or a protocol Adaptation object.");
        }

        [Fact]
        public void WarningIsNotLoggedWhenWarnLevelIsDisabled()
        {
            var log = new TestLog(false);

            var adaptation = "{ not valid json".ParseAdaptationResponse(log);

            adaptation.Error.Should().NotBeNullOrEmpty();
            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public void JsonLiteralNullDeserializesToAFreshSuppressedAdaptation()
        {
            var log = new TestLog();

            var adaptation = "null".ParseAdaptationResponse(log);

            adaptation.Should().NotBeNull();
            adaptation.Value.Should().BeNull();
            adaptation.Error.Should().BeNull();
            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void SimpleJsonObjectWithOnlyAValueIsParsed()
        {
            var log = new TestLog();

            var adaptation = "{\"Value\": 0.73}".ParseAdaptationResponse(log);

            adaptation.Value.Should().Be(0.73);
            adaptation.Error.Should().BeNull();
            adaptation.IsSuppressed.Should().BeFalse();
        }

        [Fact]
        public void UnknownJsonMembersAreIgnoredRatherThanCausingAFailure()
        {
            var log = new TestLog();

            var adaptation =
                "{\"Value\": 0.5, \"SomeFutureProtocolField\": \"unexpected\"}".ParseAdaptationResponse(log);

            adaptation.Value.Should().Be(0.5);
            adaptation.Error.Should().BeNull();
            log.Entries.Should().BeEmpty();
        }

        [Fact]
        public void WhenBothErrorAndValueArePresentTheValueIsForcedToNullSoTheAdaptationIsSuppressed()
        {
            var log = new TestLog();

            var adaptation = "{\"Value\": 0.9, \"Error\": \"Model unavailable\"}".ParseAdaptationResponse(log);

            adaptation.Value.Should().BeNull();
            adaptation.Error.Should().Be("Model unavailable");
            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void ErrorOnlyResponseHasNoValueAndIsSuppressed()
        {
            var log = new TestLog();

            var adaptation = "{\"Error\": \"Timeout invoking downstream model\"}".ParseAdaptationResponse(log);

            adaptation.Value.Should().BeNull();
            adaptation.Error.Should().Be("Timeout invoking downstream model");
            adaptation.IsSuppressed.Should().BeTrue();
        }

        [Fact]
        public void FullProtocolResponseIsParsedIntoAllNestedDescriptors()
        {
            var log = new TestLog();

            const string json = """
                                {
                                  "Value": 0.87,
                                  "Narrative": "High risk of fraud based on transaction velocity.",
                                  "HumanLabel": "High Risk",
                                  "ProtocolVersion": "1.1",
                                  "Model": {
                                    "Name": "fraud-glm-v3",
                                    "Family": "GLM",
                                    "Version": "3.0.1",
                                    "ArtifactHash": "sha256:abc123",
                                    "TrainedDate": "2023-01-01T00:00:00Z",
                                    "FeatureCount": 42,
                                    "Validation": {
                                      "Date": "2023-02-01T00:00:00Z",
                                      "Sample": 25000,
                                      "Auc": 0.82,
                                      "Gini": 0.64,
                                      "Ks": 0.45,
                                      "Brier": 0.09,
                                      "PopulationStabilityIndex": 0.03,
                                      "NextReviewDate": "2024-02-01T00:00:00Z",
                                      "Stale": false
                                    }
                                  },
                                  "Result": {
                                    "Threshold": 0.75,
                                    "Activated": true,
                                    "ExpectedPositiveRate": 0.02
                                  },
                                  "Calibration": {
                                    "Space": "Probability",
                                    "Calibrated": true,
                                    "Method": "Platt",
                                    "ValidatedDate": "2023-03-01T00:00:00Z",
                                    "Sample": 10000,
                                    "Brier": 0.08,
                                    "Intercept": -0.5,
                                    "Slope": 1.02,
                                    "Band": [
                                      { "Lower": 0.0, "Upper": 0.5, "Expected": 0.2, "Observed": 0.19, "Count": 100 },
                                      { "Lower": 0.5, "Upper": 1.0, "Expected": 0.7, "Observed": 0.72, "Count": 80 }
                                    ]
                                  },
                                  "Contribution": {
                                    "Space": "Relative",
                                    "Method": "Coefficient",
                                    "Exact": true,
                                    "BaseValue": 0.1,
                                    "Items": [
                                      { "Name": "TransactionAmount", "Weight": 0.35, "Direction": 1.0, "Significance": 0.02, "Source": "Payload", "HumanLabel": "Transaction Amount" },
                                      { "Name": "AccountAge", "Weight": -0.12, "Direction": -1.0, "Significance": 0.5, "Source": "Abstraction", "HumanLabel": "Account Age" }
                                    ]
                                  },
                                  "Journey": {
                                    "Path": [
                                      { "Feature": "TransactionAmount", "Operator": ">=", "Threshold": 1000.0, "Source": "Payload", "HumanLabel": "Transaction amount at least 1000" },
                                      { "Feature": "CountryCode", "Operator": "in", "ThresholdCategory": "HighRiskCountryList", "Source": "Dictionary", "HumanLabel": "Country is high risk" }
                                    ]
                                  }
                                }
                                """;

            var adaptation = json.ParseAdaptationResponse(log);

            adaptation.Value.Should().Be(0.87);
            adaptation.Error.Should().BeNull();
            adaptation.Narrative.Should().Be("High risk of fraud based on transaction velocity.");
            adaptation.HumanLabel.Should().Be("High Risk");
            adaptation.ProtocolVersion.Should().Be("1.1");
            adaptation.IsSuppressed.Should().BeFalse();

            adaptation.Model.Name.Should().Be("fraud-glm-v3");
            adaptation.Model.Family.Should().Be("GLM");
            adaptation.Model.FeatureCount.Should().Be(42);
            adaptation.Model.Validation.Auc.Should().Be(0.82);
            adaptation.Model.Validation.Stale.Should().BeFalse();

            adaptation.Result.Threshold.Should().Be(0.75);
            adaptation.Result.Activated.Should().BeTrue();

            adaptation.Calibration.Method.Should().Be("Platt");
            adaptation.Calibration.Band.Should().HaveCount(2);
            adaptation.Calibration.Band[0].Count.Should().Be(100);
            adaptation.Calibration.Band[1].Observed.Should().Be(0.72);

            adaptation.Contribution.Method.Should().Be("Coefficient");
            adaptation.Contribution.Items.Should().HaveCount(2);
            adaptation.Contribution.Items[0].Name.Should().Be("TransactionAmount");
            adaptation.Contribution.Items[1].Weight.Should().Be(-0.12);

            adaptation.Journey.Path.Should().HaveCount(2);
            adaptation.Journey.Path[0].Feature.Should().Be("TransactionAmount");
            adaptation.Journey.Path[1].ThresholdCategory.Should().Be("HighRiskCountryList");
        }

        [Fact]
        public void LeadingAndTrailingWhitespaceAroundAFullJsonObjectIsTrimmedBeforeParsing()
        {
            var log = new TestLog();

            var adaptation = "\n\t {\"Value\": 0.6} \t\n".ParseAdaptationResponse(log);

            adaptation.Value.Should().Be(0.6);
            adaptation.Error.Should().BeNull();
        }
    }
}