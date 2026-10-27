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
using System.Linq;
using FluentAssertions;
using Jube.Data.Query.GetCaseWorkflowXPathByCaseWorkflowIdQuery;
using Jube.Service.Query.CaseKeyJournal;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class JournalTransactionReaderTests
    {
        private static readonly JObject archived = JObject.Parse("""
            {
              "entityAnalysisModelName": "Retail",
              "tag": ["mule"],
              "activation": {
                "Velocity": { "forced": false, "visible": true },
                "Sanctioned": { "forced": true, "visible": true },
                "Internal": { "forced": false, "visible": false }
              },
              "abstraction": { "Volume": 12.5 },
              "abstractionCalculation": { "Ratio": 0.5 },
              "ttlCounter": { "All": 3 },
              "dictionary": { "Watch": 1 },
              "sanction": { "Fuzzy": 0.82 },
              "payload": { "AccountId": "A1", "Amount": 120.5, "Device": "D-9" },
              "exhaustiveAdaptation": { "StructNet": 0.91 },
              "exhaustiveAdaptationDetail": {
                "StructNet": {
                  "value": 0.91,
                  "humanLabel": "Very likely",
                  "narrative": "Large cash in, then out.",
                  "model": { "name": "StructNet", "family": "NeuralNetwork", "version": "7", "artifactHash": "abc",
                             "trainedDate": "2026-09-01T00:00:00Z", "featureCount": 4 },
                  "result": { "threshold": 0.8, "activated": true, "expectedPositiveRate": 0.31 },
                  "calibration": { "space": "Probability", "calibrated": true, "method": "Platt", "sample": 212, "brier": 0.12,
                                   "intercept": -0.1, "slope": 1.02,
                                   "band": [ { "lower": 0, "upper": 0.5, "expected": 0.2, "observed": 0.18, "count": 100 },
                                             { "lower": 0.5, "upper": 1, "expected": 0.8, "observed": 0.7, "count": 112 } ] },
                  "contribution": { "space": "LogOdds", "method": "TreePathContribution", "exact": true, "baseValue": -3.18,
                                    "items": [ { "name": "Amount", "weight": 2.2, "source": "Payload" },
                                               { "name": "Secret", "weight": 1.1, "source": "Payload" },
                                               { "name": "Volume", "weight": 0.5, "direction": -1, "source": "Abstraction",
                                                 "humanLabel": "Cash volume" } ] },
                  "journey": { "path": [ { "feature": "Amount", "operator": ">", "threshold": 5000, "source": "Payload" },
                                         { "feature": "Secret", "operator": "<=", "threshold": 9, "source": "Payload" },
                                         { "feature": "Volume", "operator": ">", "threshold": 10, "source": "Abstraction",
                                           "humanLabel": "Cash volume" } ] }
                }
              },
              "advancedAnalyticsAdaptation": { "Gradient": 0.4 },
              "advancedAnalyticsAdaptationDetail": { "Gradient": { "value": 0.4, "error": "partial" } },
              "httpAdaptation": { "Bureau": { "value": 640, "humanLabel": "Score" } }
            }
            """);

        private static readonly HashSet<string> visible = new(["Amount", "Device", "AccountId"],
            System.StringComparer.OrdinalIgnoreCase);

        private static Jube.Dto.Query.CaseKeyJournal.JournalAdaptationDto Structnet(ISet<string>? names = null)
        {
            return JournalTransactionReader.Adaptations(archived, names ?? visible)
                .Single(adaptation => adaptation.Name == "StructNet");
        }

        [Fact]
        public void EveryModelSourceIsReadAndTheValueComesFromItsDetailWhenThereIsOne()
        {
            var adaptations = JournalTransactionReader.Adaptations(archived, visible);

            adaptations.Select(a => (a.Source, a.Name)).Should().BeEquivalentTo(new[]
            {
                ("exhaustive", "StructNet"), ("advanced", "Gradient"), ("http", "Bureau")
            });
            adaptations.Single(a => a.Name == "Bureau").Value.Should().Be(640);
            adaptations.Single(a => a.Name == "Gradient").Error.Should().Be("partial");
        }

        [Fact]
        public void TheModelTheResultAndTheNarrativeAreCarriedAsArchived()
        {
            var adaptation = Structnet();

            adaptation.HumanLabel.Should().Be("Very likely");
            adaptation.Narrative.Should().Be("Large cash in, then out.");
            adaptation.Model!.Family.Should().Be("NeuralNetwork");
            adaptation.Model.Version.Should().Be("7");
            adaptation.Model.ArtifactHash.Should().Be("abc");
            adaptation.Model.TrainedDate!.Value.Year.Should().Be(2026);
            adaptation.Model.FeatureCount.Should().Be(4);
            adaptation.Result!.Threshold.Should().Be(0.8);
            adaptation.Result.Activated.Should().BeTrue();
            adaptation.Result.ExpectedPositiveRate.Should().Be(0.31);
        }

        [Fact]
        public void CalibrationKeepsItsBandsInOrder()
        {
            var calibration = Structnet().Calibration!;

            calibration.Method.Should().Be("Platt");
            calibration.Sample.Should().Be(212);
            calibration.Brier.Should().Be(0.12);
            calibration.Bands.Select(band => band.Count).Should().Equal(100, 112);
            calibration.Bands[1].Observed.Should().Be(0.7);
        }

        [Fact]
        public void ContributionsAreSignedByTheirDirectionAndExactnessIsKept()
        {
            var contribution = Structnet().Contribution!;

            contribution.Exact.Should().BeTrue();
            contribution.BaseValue.Should().Be(-3.18);
            contribution.Items.Single(item => item.Name == "Amount").Signed.Should().Be(2.2);
            contribution.Items.Single(item => item.Name == "Volume").Signed.Should().Be(-0.5);
            contribution.Items.Single(item => item.Name == "Volume").HumanLabel.Should().Be("Cash volume");
        }

        [Fact]
        public void APayloadInputTheCallerCannotSeeIsHiddenAndNeverNamedOrCounted()
        {
            var contribution = Structnet().Contribution!;

            contribution.HiddenItems.Should().Be(1);

            var hidden = contribution.Items.Single(item => item.Hidden);

            hidden.Name.Should().BeNull();
            hidden.Weight.Should().BeNull();
            hidden.Signed.Should().BeNull();
            hidden.HumanLabel.Should().BeNull();
            contribution.Items.Where(item => !item.Hidden).Select(item => item.Name).Should()
                .Equal("Amount", "Volume");
        }

        [Fact]
        public void AFeatureOrAbstractionInputIsNeverHiddenWhateverTheXPathsAre()
        {
            var contribution = Structnet(new HashSet<string>()).Contribution!;

            contribution.Items.Single(item => item.Name == "Volume").Hidden.Should().BeFalse();
            contribution.HiddenItems.Should().Be(2);
        }

        [Fact]
        public void TheDecisionPathIsInOrderAndHidesTheNodesOnInputsTheCallerCannotSee()
        {
            var journey = Structnet().Journey;

            journey.Should().HaveCount(3);
            journey[0].Feature.Should().Be("Amount");
            journey[0].Operator.Should().Be(">");
            journey[0].Threshold.Should().Be(5000);
            journey[1].Hidden.Should().BeTrue();
            journey[1].Feature.Should().BeNull();
            journey[1].Threshold.Should().BeNull();
            journey[1].Operator.Should().BeNull();
            journey[2].Feature.Should().Be("Volume");
            journey[2].HumanLabel.Should().Be("Cash volume");
        }

        [Fact]
        public void VisibilityIsNotCaseSensitive()
        {
            var names = new HashSet<string>(["amount", "SECRET"], System.StringComparer.OrdinalIgnoreCase);

            Structnet(names).Journey.Should().OnlyContain(node => !node.Hidden);
        }

        [Fact]
        public void TheNamesAXPathMakesVisibleIncludeItsLastPathSegment()
        {
            var names = JournalTransactionReader.VisibleNames(
            [
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = "Amt", XPath = "$.payload.Amount" },
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = "Dev", XPath = "$.payload['Device']" }
            ]);

            names.Should().Contain(["Amt", "Amount", "Dev", "Device"]);
            names.Contains("amount").Should().BeTrue();
        }

        [Fact]
        public void ValuesCarryTheirGroupAndAnHttpAdaptationIsReadByItsValue()
        {
            var values = JournalTransactionReader.Values(archived);

            values.Select(v => (v.Group, v.Name)).Should().BeEquivalentTo(new[]
            {
                ("abstraction", "Volume"), ("calculation", "Ratio"), ("counter", "All"),
                ("dictionary", "Watch"), ("sanction", "Fuzzy"), ("http", "Bureau")
            });
            values.Single(v => v.Name == "Bureau").Value.Should().Be(640L);
        }

        [Fact]
        public void OnlyVisibleActivationsAreListedWithTheirForcedFlag()
        {
            var activations = JournalTransactionReader.Activations(archived);

            activations.Select(a => (a.Name, a.Forced)).Should().BeEquivalentTo(new[]
            {
                ("Velocity", false), ("Sanctioned", true)
            });
        }

        [Fact]
        public void KeysAreTheDrillAndIdentifierXPathsWithTheirValues()
        {
            var keys = JournalTransactionReader.Keys(archived,
            [
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = "Device", XPath = "$.payload.Device", Drill = true },
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = "Account", XPath = "$.payload.AccountId", RoleId = 4 },
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = "Amount", XPath = "$.payload.Amount" },
                new GetCaseWorkflowXPathByCaseWorkflowIdQueryDto { Name = "Gone", XPath = "$.payload.Gone", Drill = true }
            ]);

            keys.Select(key => (key.Name, key.Value, key.Drill)).Should().Equal(
                ("Device", "D-9", true), ("Account", "A1", false), ("Gone", null, true));
            keys.Select(key => key.PayloadKey).Should().Equal("Device", "AccountId", "Gone");
        }

        [Theory]
        [InlineData("$.payload.Device", "Device")]
        [InlineData("payload.Device", "Device")]
        [InlineData("$.payload['Device']", "Device")]
        [InlineData("$.payload[\"Device\"]", "Device")]
        [InlineData("$.payload.Nested.Device", null)]
        [InlineData("$.activation.Velocity", null)]
        [InlineData("$..payload.Device", null)]
        [InlineData("", null)]
        [InlineData(null, null)]
        public void OnlyAnIdentifierReadStraightFromThePayloadCanBePivotedTo(string? xPath, string? expected)
        {
            JournalTransactionReader.PayloadKey(xPath).Should().Be(expected);
        }

        [Theory]
        [InlineData(2.0, 1.0, 2.0)]
        [InlineData(2.0, -1.0, -2.0)]
        [InlineData(-2.0, 1.0, 2.0)]
        [InlineData(-2.0, -1.0, -2.0)]
        [InlineData(2.0, 0.0, 2.0)]
        [InlineData(-2.0, null, -2.0)]
        public void TheSignedContributionAppliesTheDirectionToTheMagnitude(double weight, double? direction,
            double expected)
        {
            JournalTransactionReader.Signed(weight, direction).Should().Be(expected);
        }

        [Fact]
        public void NoWeightIsNoSignedContribution()
        {
            JournalTransactionReader.Signed(null, 1).Should().BeNull();
        }

        [Fact]
        public void AnAdaptationWithNoDetailStillHasItsValueAndNothingElse()
        {
            var json = JObject.Parse("{\"exhaustiveAdaptation\":{\"Plain\":0.2}}");

            var adaptation = JournalTransactionReader.Adaptations(json, visible).Single();

            adaptation.Value.Should().Be(0.2);
            adaptation.Model.Should().BeNull();
            adaptation.Contribution.Should().BeNull();
            adaptation.Journey.Should().BeEmpty();
        }

        [Fact]
        public void GarbageInTheArchiveIsToleratedRatherThanFailing()
        {
            var json = JObject.Parse("""
                {
                  "exhaustiveAdaptationDetail": {
                    "A": { "value": "x", "model": 5, "result": [], "calibration": { "band": "no" },
                           "contribution": { "items": [ 1, "two", { "name": 3, "weight": "heavy" } ] },
                           "journey": { "path": 7 } },
                    "B": "text",
                    "C": null
                  },
                  "activation": [],
                  "httpAdaptation": 5
                }
                """);

            var adaptations = JournalTransactionReader.Adaptations(json, visible);

            adaptations.Select(a => a.Name).Should().BeEquivalentTo("A", "B", "C");
            adaptations.Single(a => a.Name == "A").Value.Should().BeNull();
            adaptations.Single(a => a.Name == "A").Journey.Should().BeEmpty();
            JournalTransactionReader.Activations(json).Should().BeEmpty();
            JournalTransactionReader.Values(json).Should().BeEmpty();
        }
    }
}
