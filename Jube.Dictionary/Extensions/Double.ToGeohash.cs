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
        public static string ToGeohash(this double latitude, double longitude, int precision)
        {
            if (!latitude.IsValidCoordinate(longitude) || precision is < 1 or > 12)
            {
                return "";
            }

            const string alphabet = "0123456789bcdefghjkmnpqrstuvwxyz";
            double latitudeLow = -90, latitudeHigh = 90, longitudeLow = -180, longitudeHigh = 180;
            var builder = new System.Text.StringBuilder();
            var even = true;
            var bit = 0;
            var character = 0;
            while (builder.Length < precision)
            {
                if (even)
                {
                    var middle = (longitudeLow + longitudeHigh) / 2;
                    if (longitude >= middle)
                    {
                        character = (character << 1) | 1;
                        longitudeLow = middle;
                    }
                    else
                    {
                        character <<= 1;
                        longitudeHigh = middle;
                    }
                }
                else
                {
                    var middle = (latitudeLow + latitudeHigh) / 2;
                    if (latitude >= middle)
                    {
                        character = (character << 1) | 1;
                        latitudeLow = middle;
                    }
                    else
                    {
                        character <<= 1;
                        latitudeHigh = middle;
                    }
                }

                even = !even;
                if (++bit == 5)
                {
                    builder.Append(alphabet[character]);
                    bit = 0;
                    character = 0;
                }
            }

            return builder.ToString();
        }
    }
}