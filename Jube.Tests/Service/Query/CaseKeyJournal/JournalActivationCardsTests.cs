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

using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Jube.Dto.Options;
using Jube.Dto.Query.CaseKeyJournal;
using Jube.Service.Query.CaseKeyJournal;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Service.Query.CaseKeyJournal
{
    [Trait("Category", "Unit")]
    public sealed class JournalActivationCardsTests
    {
        private static readonly HashSet<string> visible = new(StringComparer.Ordinal) { "Amount", "Deposits" };

        private static readonly IReadOnlyDictionary<string, IReadOnlyList<JournalRuleInput>> none =
            new Dictionary<string, IReadOnlyList<JournalRuleInput>>();

        private static JObject Archived(double score, bool activated, bool velocity = true, bool forced = false,
            bool calibrated = true)
        {
            return JObject.Parse($$"""
                {
                  "activation": { "Velocity": { "forced": {{forced.ToString().ToLowerInvariant()}}, "visible": {{velocity.ToString().ToLowerInvariant()}} },
                                  "Internal": { "forced": false, "visible": false } },
                  "payload": { "Amount": 250, "Deposits": 11, "Secret": 9 },
                  "ttlCounter": { "Deposits9d": 11 },
                  "exhaustiveAdaptation": { "StructNet": {{score}} },
                  "exhaustiveAdaptationDetail": {
                    "StructNet": {
                      "value": {{score}},
                      "model": { "name": "StructNet", "version": "v7" },
                      "result": { "threshold": 0.8, "activated": {{activated.ToString().ToLowerInvariant()}} },
                      "calibration": { "space": "Probability", "calibrated": {{calibrated.ToString().ToLowerInvariant()}}, "sample": 212,
                        "band": [ { "lower": 0, "upper": 0.5, "expected": 0.1, "observed": 0.08, "count": 100 },
                                  { "lower": 0.5, "upper": 1.0, "expected": 0.7, "observed": 0.31, "count": 112 } ] },
                      "contribution": { "space": "LogOdds", "exact": true, "baseValue": -3.18,
                        "items": [ { "name": "Amount", "weight": 2.2, "source": "Payload" },
                                   { "name": "Deposits", "weight": 1.55, "source": "Payload" },
                                   { "name": "Volume", "weight": 1.1, "source": "Abstraction" },
                                   { "name": "Speed", "weight": 0.8, "source": "Abstraction" },
                                   { "name": "Tenure", "weight": 0.5, "direction": -1, "source": "Abstraction" },
                                   { "name": "Secret", "weight": 3, "source": "Payload" } ] }
                    }
                  }
                }
                """);
        }

        private static JournalActivationRow Row(long id, JObject json)
        {
            return new JournalActivationRow(id, new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc).AddDays(id), json);
        }

        [Fact]
        public void ARuleAndAModelAreAnchoredOnTheLatestRowTheyFiredOn()
        {
            var cards = JournalActivationCards.Build(
                [Row(1, Archived(0.9, true)), Row(3, Archived(0.95, true)), Row(2, Archived(0.2, false))], visible,
                none);

            var model = cards.Single(card => card.Kind == "model");
            var rule = cards.Single(card => card.Kind == "rule");

            model.AnchorId.Should().Be(3);
            model.Fired.Should().Be(2);
            model.Value.Should().Be(0.95);
            model.Threshold.Should().Be(0.8);
            model.ModelVersion.Should().Be("v7");
            rule.Name.Should().Be("Velocity");
            rule.Fired.Should().Be(3);
            rule.AnchorId.Should().Be(3);
        }

        [Fact]
        public void AModelThatNeverActivatedGetsNoCardAndAHiddenRuleNeverAppears()
        {
            var cards = JournalActivationCards.Build([Row(1, Archived(0.3, false))], visible, none);

            cards.Should().ContainSingle(card => card.Kind == "rule" && card.Name == "Velocity");
            cards.Should().NotContain(card => card.Kind == "model");
            cards.Should().NotContain(card => card.Name == "Internal");
        }

        [Fact]
        public void ACalibratedProbabilityCarriesItsBandAndTheRateObservedInItsReliabilityBand()
        {
            var card = JournalActivationCards.Build([Row(1, Archived(0.91, true))], visible, none)
                .Single(item => item.Kind == "model");

            card.Calibrated.Should().BeTrue();
            card.BandId.Should().Be((int)JournalScoreBand.VeryLikely);
            card.ObservedRate.Should().Be(0.31);
            card.ExpectedRate.Should().Be(0.7);
            card.BandSample.Should().Be(112);
        }

        [Fact]
        public void AScoreThatIsNotCalibratedIsNeverGivenABandOrCalledAProbability()
        {
            var card = JournalActivationCards.Build([Row(1, Archived(0.91, true, calibrated: false))], visible, none)
                .Single(item => item.Kind == "model");

            card.Calibrated.Should().BeFalse();
            card.BandId.Should().BeNull();
            card.ObservedRate.Should().BeNull();
        }

        [Theory]
        [InlineData(0.1, JournalScoreBand.Unlikely)]
        [InlineData(0.25, JournalScoreBand.Possible)]
        [InlineData(0.5, JournalScoreBand.Likely)]
        [InlineData(0.75, JournalScoreBand.VeryLikely)]
        [InlineData(1.0, JournalScoreBand.VeryLikely)]
        public void TheBandWordFollowsTheCalibratedProbability(double value, JournalScoreBand expected)
        {
            var adaptation = new JournalAdaptationDto
            {
                Value = value, Calibration = new JournalCalibrationDto { Calibrated = true, Space = "Probability" }
            };

            JournalScoreBands.Of(adaptation).Should().Be(expected);
        }

        [Fact]
        public void TheFourLargestDriversAreKeptAndANegativeOneIsNeverDroppedForAPositive()
        {
            var card = JournalActivationCards.Build([Row(1, Archived(0.91, true))], visible, none)
                .Single(item => item.Kind == "model");

            card.Drivers.Should().HaveCount(JournalActivationCards.TopDrivers);
            card.Drivers.Select(driver => driver.Label).Should().Equal("Amount", "Deposits", "Volume", "Tenure");
            card.Drivers.Should().Contain(driver => driver.Signed < 0);
            card.OtherDrivers.Should().Be(1);
        }

        [Fact]
        public void AnInputTheCallersRolesCannotSeeIsNeverNamedAsADriver()
        {
            var card = JournalActivationCards.Build([Row(1, Archived(0.91, true))], visible, none)
                .Single(item => item.Kind == "model");

            Newtonsoft.Json.JsonConvert.SerializeObject(card).Should().NotContain("Secret");
        }

        [Fact]
        public void ARuleReadsItsInputsFromTheAnchorAndNeverNamesAHiddenPayloadField()
        {
            IReadOnlyDictionary<string, IReadOnlyList<JournalRuleInput>> inputs =
                new Dictionary<string, IReadOnlyList<JournalRuleInput>>
                {
                    ["Velocity"] =
                    [
                        new JournalRuleInput("TTLCounter", "Deposits9d"), new JournalRuleInput("Payload", "Amount"),
                        new JournalRuleInput("Payload", "Secret"), new JournalRuleInput("List", "Watch"),
                        new JournalRuleInput("TTLCounter", "Missing")
                    ]
                };

            var card = JournalActivationCards.Build([Row(1, Archived(0.3, false))], visible, inputs).Single();

            card.Path.Select(path => (path.Group, path.Name)).Should()
                .Equal(("counter", "Deposits9d"), ("payload", "Amount"));
            card.Path.Single(path => path.Name == "Deposits9d").Value.Should().Be(11L);
            card.HiddenPath.Should().Be(1);
            Newtonsoft.Json.JsonConvert.SerializeObject(card).Should().NotContain("Secret");
        }

        [Fact]
        public void AForcedActivationIsMarkedForcedOnTheAnchor()
        {
            var card = JournalActivationCards.Build([Row(1, Archived(0.3, false, forced: true))], visible, none)
                .Single();

            card.Forced.Should().BeTrue();
        }
    }
}
