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
    internal static class GeohashSupport
    {
        internal static bool TryDecode(string? geohash, out double latitude, out double longitude)
        {
            const string alphabet = "0123456789bcdefghjkmnpqrstuvwxyz";
            latitude = double.NaN;
            longitude = double.NaN;
            var value = (geohash ?? "").Trim().ToLowerInvariant();
            if (value.Length is 0 or > 12)
            {
                return false;
            }

            double latitudeLow = -90, latitudeHigh = 90, longitudeLow = -180, longitudeHigh = 180;
            var even = true;
            foreach (var c in value)
            {
                var index = alphabet.IndexOf(c);
                if (index < 0)
                {
                    return false;
                }

                for (var mask = 16; mask > 0; mask >>= 1)
                {
                    var set = (index & mask) != 0;
                    if (even)
                    {
                        var middle = (longitudeLow + longitudeHigh) / 2;
                        if (set)
                        {
                            longitudeLow = middle;
                        }
                        else
                        {
                            longitudeHigh = middle;
                        }
                    }
                    else
                    {
                        var middle = (latitudeLow + latitudeHigh) / 2;
                        if (set)
                        {
                            latitudeLow = middle;
                        }
                        else
                        {
                            latitudeHigh = middle;
                        }
                    }

                    even = !even;
                }
            }

            latitude = (latitudeLow + latitudeHigh) / 2;
            longitude = (longitudeLow + longitudeHigh) / 2;
            return true;
        }
    }
}