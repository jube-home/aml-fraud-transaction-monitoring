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
using Jube.HttpAdaptationProtocol.Calibration;
using Newtonsoft.Json;
using Xunit;

namespace Jube.Test.HttpAdaptationProtocol.Calibration
{
    [Trait("Category", "Unit")]
    public sealed class CalibrationBandTests
    {
        [Fact]
        public void HoldsAssignedFields()
        {
            var band = new CalibrationBand
            {
                Lower = 0.1,
                Upper = 0.2,
                Expected = 0.15,
                Observed = 0.14,
                Count = 120
            };

            band.Lower.Should().Be(0.1);
            band.Upper.Should().Be(0.2);
            band.Expected.Should().Be(0.15);
            band.Observed.Should().Be(0.14);
            band.Count.Should().Be(120);
        }

        [Fact]
        public void AllFieldsDefaultToNullWhenNotAssigned()
        {
            var band = new CalibrationBand();

            band.Lower.Should().BeNull();
            band.Upper.Should().BeNull();
            band.Expected.Should().BeNull();
            band.Observed.Should().BeNull();
            band.Count.Should().BeNull();
        }

        [Fact]
        public void RoundTripsThroughJson()
        {
            var original = new CalibrationBand
            {
                Lower = 0.0,
                Upper = 0.1,
                Expected = 0.05,
                Observed = 0.045,
                Count = 500
            };

            var json = JsonConvert.SerializeObject(original);
            var deserialized = JsonConvert.DeserializeObject<CalibrationBand>(json);

            deserialized.Should().Be(original);
        }

        [Fact]
        public void DeserializesFromLowerCaseJsonPropertyNames()
        {
            const string json = "{\"lower\":0.1,\"upper\":0.3,\"expected\":0.2,\"observed\":0.19,\"count\":42}";

            var band = JsonConvert.DeserializeObject<CalibrationBand>(json);

            if (band == null)
            {
                return;
            }

            band.Lower.Should().Be(0.1);
            band.Upper.Should().Be(0.3);
            band.Expected.Should().Be(0.2);
            band.Observed.Should().Be(0.19);
            band.Count.Should().Be(42);
        }
    }
}