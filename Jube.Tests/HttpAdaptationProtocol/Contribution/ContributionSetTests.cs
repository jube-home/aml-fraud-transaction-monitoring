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
using Jube.HttpAdaptationProtocol.Contribution;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Contribution
{
    [Trait("Category", "Unit")]
    public sealed class ContributionSetTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var items = new List<ContributionItem>
            {
                new() { Name = "Feature1", Weight = 0.6 },
                new() { Name = "Feature2", Weight = 0.4 }
            };

            var set = new ContributionSet
            {
                Space = "Relative",
                Method = "Coefficient",
                Exact = true,
                BaseValue = 0.1,
                Items = items
            };

            set.Space.Should().Be("Relative");
            set.Method.Should().Be("Coefficient");
            set.Exact.Should().BeTrue();
            set.BaseValue.Should().Be(0.1);
            set.Items.Should().BeSameAs(items);
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var set = new ContributionSet();

            set.Space.Should().BeNull();
            set.Method.Should().BeNull();
            set.Exact.Should().BeNull();
            set.BaseValue.Should().BeNull();
            set.Items.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJsonIncludingNestedItems()
        {
            var original = new ContributionSet
            {
                Space = "Relative",
                Method = "ArcStrength",
                Exact = false,
                BaseValue = 0.05,
                Items =
                [
                    new ContributionItem
                    {
                        Name = "Feature1", Weight = 0.6, Direction = 1.0, Significance = 0.01,
                        Source = "Payload", HumanLabel = "Feature One"
                    },
                    new ContributionItem
                    {
                        Name = "Feature2", Weight = -0.4, Direction = -1.0, Significance = 0.2,
                        Source = "Dictionary", HumanLabel = "Feature Two"
                    }
                ]
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ContributionSet>(json);

            deserialized.Should().NotBeNull();
            deserialized!.Space.Should().Be(original.Space);
            deserialized.Method.Should().Be(original.Method);
            deserialized.Exact.Should().Be(original.Exact);
            deserialized.BaseValue.Should().Be(original.BaseValue);
            deserialized.Items.Should().BeEquivalentTo(original.Items);
        }

        [Fact]
        public void EmptyItemsListRoundTripsAsEmptyRatherThanNull()
        {
            var original = new ContributionSet { Items = [] };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ContributionSet>(json);

            deserialized!.Items.Should().NotBeNull();
            deserialized.Items.Should().BeEmpty();
        }
    }
}