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
        public static double InitialBearingDegrees(this double latitude1, double longitude1, double latitude2,
            double longitude2)
        {
            if (!latitude1.IsValidCoordinate(longitude1) || !latitude2.IsValidCoordinate(longitude2))
            {
                return double.NaN;
            }

            var phi1 = latitude1 * Math.PI / 180d;
            var phi2 = latitude2 * Math.PI / 180d;
            var deltaLambda = (longitude2 - longitude1) * Math.PI / 180d;
            var y = Math.Sin(deltaLambda) * Math.Cos(phi2);
            var x = Math.Cos(phi1) * Math.Sin(phi2) - Math.Sin(phi1) * Math.Cos(phi2) * Math.Cos(deltaLambda);
            var degrees = Math.Atan2(y, x) * 180d / Math.PI;
            return (degrees + 360d) % 360d;
        }
    }
}