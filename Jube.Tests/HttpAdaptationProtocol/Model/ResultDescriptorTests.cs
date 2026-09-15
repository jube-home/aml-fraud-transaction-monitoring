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
using Jube.HttpAdaptationProtocol.Model;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Model
{
    [Trait("Category", "Unit")]
    public sealed class ResultDescriptorTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var descriptor = new ResultDescriptor
            {
                Threshold = 0.75,
                Activated = true,
                ExpectedPositiveRate = 0.02
            };

            descriptor.Threshold.Should().Be(0.75);
            descriptor.Activated.Should().BeTrue();
            descriptor.ExpectedPositiveRate.Should().Be(0.02);
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var descriptor = new ResultDescriptor();

            descriptor.Threshold.Should().BeNull();
            descriptor.Activated.Should().BeNull();
            descriptor.ExpectedPositiveRate.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJson()
        {
            var original = new ResultDescriptor { Threshold = 0.5, Activated = false, ExpectedPositiveRate = 0.1 };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<ResultDescriptor>(json);

            deserialized.Should().Be(original);
        }
    }
}