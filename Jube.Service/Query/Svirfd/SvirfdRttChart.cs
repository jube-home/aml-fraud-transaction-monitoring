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
using Jube.Charting;
using Jube.Dictionary.Extensions;
using Jube.Svirfd;
using Svirfd;

namespace Jube.Service.Query.Svirfd
{
    public static class SvirfdRttChart
    {
        private const double KilometresPerMillisecond = 100;

        public static string? Render(IpResponse record)
        {
            var probes = record.Location.Measured?.ProbesByRtt;

            if (probes is not { Length: > 1 } || !record.TryCoordinates(out var latitude, out var longitude))
            {
                return null;
            }

            var reached = new List<(double X, double Y)>();
            var edge = new List<(double X, double Y)>();

            foreach (var probe in probes)
            {
                if (probe.Lat is not { } probeLatitude || probe.Lon is not { } probeLongitude)
                {
                    continue;
                }

                var distance = latitude.HaversineDistanceKilometers(longitude, probeLatitude, probeLongitude);

                (probe.Reached ? reached : edge).Add((distance, probe.Rtt));
            }

            if (reached.Count + edge.Count < 2)
            {
                return null;
            }

            var farthest = reached.Concat(edge).Max(p => p.X);
            var slowest = reached.Concat(edge).Max(p => p.Y);
            var bound = Math.Max(farthest, slowest * KilometresPerMillisecond);

            var builder = Chart.Scatter()
                .XAxisTitle("Distance from the placed location (km)")
                .YAxisTitle("Round trip (ms)")
                .Reference(0, 0, bound, bound / KilometresPerMillisecond, "Physical bound");

            if (reached.Count > 0)
            {
                builder.Series("Reached", reached.Select(p => p.X), reached.Select(p => p.Y));
            }

            if (edge.Count > 0)
            {
                builder.Series("Stopped at edge", edge.Select(p => p.X), edge.Select(p => p.Y));
            }

            return builder.Render();
        }
    }
}
