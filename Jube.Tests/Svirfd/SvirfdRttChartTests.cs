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
using Jube.Service.Query.Svirfd;
using Jube.Svirfd;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.Svirfd
{
    [Trait("Category", "Unit")]
    public sealed class SvirfdRttChartTests
    {
        [Fact]
        public async Task ARecordWithoutProbesHasNoChart()
        {
            var warsaw = await SvirfdFixture.RecordAsync("37.30.56.54");

            SvirfdRttChart.Render(warsaw with { Location = warsaw.Location with { Measured = null } })
                .Should().BeNull();
        }

        [Fact]
        public async Task LocatedProbesAreChartedAgainstTheirDistanceWithThePhysicalBound()
        {
            var warsaw = await SvirfdFixture.RecordAsync("37.30.56.54");
            var json = JObject.Parse(SvirfdJson.Serialize(warsaw));

            json["location"]!["measured"] = JObject.Parse("""
                {
                  "prefix": "37.30.56.0/24", "rttMs": 9, "probes": 3, "targetAnswered": true, "edgeOnly": false,
                  "depth": 1, "method": "traceroute", "estimate": null,
                  "probesByRtt": [
                    { "city": "Berlin", "country": "DE", "rtt": 9.0, "reached": true, "depth": 1, "lat": 52.52, "lon": 13.4 },
                    { "city": "London", "country": "GB", "rtt": 24.0, "reached": false, "depth": 0, "lat": 51.5, "lon": -0.12 },
                    { "city": "Nowhere", "country": null, "rtt": 5.0, "reached": true, "depth": 1, "lat": null, "lon": null }
                  ]
                }
                """);

            var svg = SvirfdRttChart.Render(SvirfdJson.Deserialize(json.ToString())!);

            svg.Should().NotBeNull();
            svg.Should().StartWith("<svg").And.Contain("Round trip (ms)").And.Contain("Physical bound");
            svg.Should().Contain("Reached").And.Contain("Stopped at edge");
        }
    }
}
