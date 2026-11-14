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

namespace Jube.Dictionary.Extensions
{
    public static partial class Extensions
    {
        [Description("Checks if two coordinates are within a given distance of each other, in kilometers, a geofence check.")]
        public static bool IsWithinRadiusKm(this double latitude1,
            [Description("Longitude of the first point.")] double longitude1,
            [Description("Latitude of the second point.")] double latitude2,
            [Description("Longitude of the second point.")] double longitude2,
            [Description("The radius to test against.")] double radiusKm)
        {
            return latitude1.HaversineDistanceKilometers(longitude1, latitude2, longitude2) <= radiusKm;
        }
    }
}
