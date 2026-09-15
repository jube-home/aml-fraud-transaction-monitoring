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
using Jube.HttpAdaptationProtocol.Contribution;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Contribution
{
    [Trait("Category", "Unit")]
    public sealed class ContributionItemTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var item = new ContributionItem
            {
                Name = "TransactionAmount",
                Weight = 0.35,
                Direction = 1.0,
                Significance = 0.02,
                Source = "Payload",
                HumanLabel = "Transaction Amount"
            };

            item.Name.Should().Be("TransactionAmount");
            item.Weight.Should().Be(0.35);
            item.Direction.Should().Be(1.0);
            item.Significance.Should().Be(0.02);
            item.Source.Should().Be("Payload");
            item.HumanLabel.Should().Be("Transaction Amount");
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var item = new ContributionItem();

            item.Name.Should().BeNull();
            item.Weight.Should().BeNull();
            item.Direction.Should().BeNull();
            item.Significance.Should().BeNull();
            item.Source.Should().BeNull();
            item.HumanLabel.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJson()
        {
            var original = new ContributionItem
            {
                Name = "AccountAge",
                Weight = -0.12,
                Direction = -1.0,
                Significance = 0.5,
                Source = "Abstraction",
                HumanLabel = "Account Age"
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ContributionItem>(json);

            deserialized.Should().Be(original);
        }
    }
}