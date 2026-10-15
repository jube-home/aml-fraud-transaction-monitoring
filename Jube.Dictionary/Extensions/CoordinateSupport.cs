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

using System.Globalization;

namespace Jube.Dictionary.Extensions
{
    internal static class CoordinateSupport
    {
        internal static bool TryParse(string? text, out double latitude, out double longitude)
        {
            latitude = 0;
            longitude = 0;

            var parts = text?.Split(',');

            if (parts is null || parts.Length != 2)
            {
                return false;
            }

            return double.TryParse(parts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out latitude) &&
                   double.TryParse(parts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out longitude);
        }

        internal static bool IsInsidePolygon(double latitude, double longitude, IEnumerable<string>? vertices)
        {
            if (vertices is null)
            {
                return false;
            }

            var points = new List<(double Latitude, double Longitude)>();

            foreach (var vertex in vertices)
            {
                if (!TryParse(vertex, out var vertexLatitude, out var vertexLongitude))
                {
                    return false;
                }

                points.Add((vertexLatitude, vertexLongitude));
            }

            if (points.Count < 3)
            {
                return false;
            }

            var inside = false;

            for (int i = 0, j = points.Count - 1; i < points.Count; j = i++)
            {
                var (yi, xi) = points[i];
                var (yj, xj) = points[j];

                if ((yi > latitude) != (yj > latitude) &&
                    longitude < (xj - xi) * (latitude - yi) / (yj - yi) + xi)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }
}