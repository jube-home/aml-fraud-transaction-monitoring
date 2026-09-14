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
using FluentAssertions;
using Jube.HttpAdaptationProtocol.Calibration;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Calibration
{
    [Trait("Category", "Unit")]
    public sealed class CalibrationDescriptorTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var validatedDate = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
            var bands = new List<CalibrationBand> { new() { Lower = 0, Upper = 1, Count = 10 } };

            var descriptor = new CalibrationDescriptor
            {
                Space = "Probability",
                Calibrated = true,
                Method = "Platt",
                ValidatedDate = validatedDate,
                Sample = 10000,
                Brier = 0.08,
                Intercept = -0.5,
                Slope = 1.02,
                Band = bands
            };

            descriptor.Space.Should().Be("Probability");
            descriptor.Calibrated.Should().BeTrue();
            descriptor.Method.Should().Be("Platt");
            descriptor.ValidatedDate.Should().Be(validatedDate);
            descriptor.Sample.Should().Be(10000);
            descriptor.Brier.Should().Be(0.08);
            descriptor.Intercept.Should().Be(-0.5);
            descriptor.Slope.Should().Be(1.02);
            descriptor.Band.Should().BeSameAs(bands);
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var descriptor = new CalibrationDescriptor();

            descriptor.Space.Should().BeNull();
            descriptor.Calibrated.Should().BeNull();
            descriptor.Method.Should().BeNull();
            descriptor.ValidatedDate.Should().BeNull();
            descriptor.Sample.Should().BeNull();
            descriptor.Brier.Should().BeNull();
            descriptor.Intercept.Should().BeNull();
            descriptor.Slope.Should().BeNull();
            descriptor.Band.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJsonIncludingNestedBands()
        {
            var original = new CalibrationDescriptor
            {
                Space = "Probability",
                Calibrated = true,
                Method = "Isotonic",
                ValidatedDate = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc),
                Sample = 5000,
                Brier = 0.11,
                Intercept = 0.0,
                Slope = 1.0,
                Band =
                [
                    new CalibrationBand { Lower = 0, Upper = 0.5, Expected = 0.2, Observed = 0.19, Count = 100 },
                    new CalibrationBand { Lower = 0.5, Upper = 1.0, Expected = 0.7, Observed = 0.72, Count = 80 }
                ]
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<CalibrationDescriptor>(json);

            deserialized.Should().NotBeNull();
            deserialized!.Space.Should().Be(original.Space);
            deserialized.Calibrated.Should().Be(original.Calibrated);
            deserialized.Method.Should().Be(original.Method);
            deserialized.ValidatedDate.Should().Be(original.ValidatedDate);
            deserialized.Sample.Should().Be(original.Sample);
            deserialized.Brier.Should().Be(original.Brier);
            deserialized.Intercept.Should().Be(original.Intercept);
            deserialized.Slope.Should().Be(original.Slope);
            deserialized.Band.Should().BeEquivalentTo(original.Band);
        }
    }
}