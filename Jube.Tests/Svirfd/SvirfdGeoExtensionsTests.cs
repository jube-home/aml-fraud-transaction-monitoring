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
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Svirfd;
using Svirfd;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdGeoExtensionsTests
    {
        [Fact]
        public async Task DistanceUsesTheLocatedCoordinates()
        {
            var warsaw = await SvirfdFixture.RecordAsync("37.30.56.54");

            warsaw.Location.Lat.Should().NotBeNull();
            warsaw.DistanceKm(52.23, 21.01).Should().BeApproximately(0, 0.01);
            warsaw.DistanceKm(51.51, -0.13).Should().BeApproximately(1448, 10);
            warsaw.DistanceMiles(51.51, -0.13).Should().BeApproximately(900, 10);
            warsaw.WithinKm(52.23, 21.01, 5).Should().BeTrue();
            warsaw.WithinKm(51.51, -0.13, 5).Should().BeFalse();
        }

        [Fact]
        public async Task UnlocatedRecordsAnswerNull()
        {
            var tor = await SvirfdFixture.RecordAsync("185.220.101.151");
            var unlocated = tor with { Location = tor.Location with { Lat = null, Lon = null, Hub = null, Prior = null } };

            unlocated.DistanceKm(0, 0).Should().BeNull();
            unlocated.WithinKm(0, 0, 100).Should().BeNull();
            ((IpResponse?)null).DistanceKm(0, 0).Should().BeNull();
        }

        [Fact]
        public async Task HubCoordinatesStandInWhenTheCityIsNotClaimed()
        {
            var record = await SvirfdFixture.RecordAsync("37.30.56.54");

            var hubOnly = record with
            {
                Location = record.Location with
                {
                    Lat = null, Lon = null, Hub = new HubBlock("Ashburn", "US", 39.04, -77.49, 50, 1, "tracerouteAsn")
                }
            };

            hubOnly.DistanceKm(39.04, -77.49).Should().BeApproximately(0, 0.01);
        }
    }
}
