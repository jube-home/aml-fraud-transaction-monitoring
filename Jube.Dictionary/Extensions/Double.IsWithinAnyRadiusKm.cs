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
        [Description("True when: the point at the value as latitude and the argument as longitude is within the radius in kilometres of any of the centres given as latitude,longitude text.")]
        public static bool IsWithinAnyRadiusKm(this double latitude,
            [Description("The longitude of the point in degrees; the value is its latitude.")] double longitude,
            [Description("The centres as latitude,longitude text, usually a List token.")] IEnumerable<string>? centres,
            [Description("The radius in kilometres.")] double radiusKm)
        {
            if (centres is null)
            {
                return false;
            }

            foreach (var centre in centres)
            {
                if (CoordinateSupport.TryParse(centre, out var centreLatitude, out var centreLongitude) &&
                    latitude.IsWithinRadiusKm(longitude, centreLatitude, centreLongitude, radiusKm))
                {
                    return true;
                }
            }

            return false;
        }
    }
}