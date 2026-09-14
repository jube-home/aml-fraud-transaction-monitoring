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
        public static double HaversineDistanceMiles(this double latitude1, double longitude1,
            double latitude2, double longitude2)
        {
            const double earthRadiusMiles = 3958.7613;

            var latitude1Radians = latitude1 * Math.PI / 180.0;
            var latitude2Radians = latitude2 * Math.PI / 180.0;
            var deltaLatitudeRadians = (latitude2 - latitude1) * Math.PI / 180.0;
            var deltaLongitudeRadians = (longitude2 - longitude1) * Math.PI / 180.0;

            var a = Math.Sin(deltaLatitudeRadians / 2) * Math.Sin(deltaLatitudeRadians / 2) +
                    Math.Cos(latitude1Radians) * Math.Cos(latitude2Radians) *
                    Math.Sin(deltaLongitudeRadians / 2) * Math.Sin(deltaLongitudeRadians / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return earthRadiusMiles * c;
        }
    }
}
