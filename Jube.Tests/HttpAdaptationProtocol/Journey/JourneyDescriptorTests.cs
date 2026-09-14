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
using Jube.HttpAdaptationProtocol.Journey;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Journey
{
    [Trait("Category", "Unit")]
    public sealed class JourneyDescriptorTests
    {
        [Fact]
        public void HoldsAssignedPath()
        {
            var path = new List<JourneyNode>
            {
                new() { Feature = "TransactionAmount", Operator = ">=", Threshold = 1000.0 },
                new() { Feature = "CountryCode", Operator = "in", ThresholdCategory = "HighRiskCountryList" }
            };

            var descriptor = new JourneyDescriptor { Path = path };

            descriptor.Path.Should().BeSameAs(path);
        }

        [Fact]
        public void PathDefaultsToNullWhenNotAssigned()
        {
            var descriptor = new JourneyDescriptor();

            descriptor.Path.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJsonPreservingNodeOrder()
        {
            var original = new JourneyDescriptor
            {
                Path =
                [
                    new JourneyNode { Feature = "A", Operator = ">", Threshold = 1.0, Source = "Payload" },
                    new JourneyNode { Feature = "B", Operator = "<", Threshold = 2.0, Source = "Abstraction" },
                    new JourneyNode { Feature = "C", Operator = "==", ThresholdCategory = "X", Source = "Sanction" }
                ]
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<JourneyDescriptor>(json);

            deserialized!.Path.Should().HaveCount(3);
            deserialized.Path.Should().ContainInOrder(original.Path);
        }
    }
}