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
using Jube.HttpAdaptationProtocol.Journey;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Journey
{
    [Trait("Category", "Unit")]
    public sealed class JourneyNodeTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var node = new JourneyNode
            {
                Feature = "TransactionAmount",
                Operator = ">=",
                Threshold = 1000.0,
                ThresholdCategory = null,
                Source = "Payload",
                HumanLabel = "Transaction amount at least 1000"
            };

            node.Feature.Should().Be("TransactionAmount");
            node.Operator.Should().Be(">=");
            node.Threshold.Should().Be(1000.0);
            node.ThresholdCategory.Should().BeNull();
            node.Source.Should().Be("Payload");
            node.HumanLabel.Should().Be("Transaction amount at least 1000");
        }

        [Fact]
        public void SupportsCategoricalThresholdsInsteadOfNumeric()
        {
            var node = new JourneyNode
            {
                Feature = "CountryCode",
                Operator = "in",
                Threshold = null,
                ThresholdCategory = "HighRiskCountryList",
                Source = "Dictionary",
                HumanLabel = "Country is high risk"
            };

            node.Threshold.Should().BeNull();
            node.ThresholdCategory.Should().Be("HighRiskCountryList");
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var node = new JourneyNode();

            node.Feature.Should().BeNull();
            node.Operator.Should().BeNull();
            node.Threshold.Should().BeNull();
            node.ThresholdCategory.Should().BeNull();
            node.Source.Should().BeNull();
            node.HumanLabel.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJson()
        {
            var original = new JourneyNode
            {
                Feature = "AccountAge",
                Operator = "<",
                Threshold = 30.0,
                ThresholdCategory = null,
                Source = "TtlCounter",
                HumanLabel = "Account newer than 30 days"
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<JourneyNode>(json);

            deserialized.Should().Be(original);
        }
    }
}