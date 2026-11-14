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
using Jube.Dictionary.Extensions;
using Svirfd;

namespace Jube.Svirfd
{
    public static class SvirfdGeoExtensions
    {
        public static double? DistanceKm(this IpResponse? record, double latitude, double longitude)
        {
            return TryCoordinates(record, out var recordLatitude, out var recordLongitude)
                ? recordLatitude.HaversineDistanceKilometers(recordLongitude, latitude, longitude)
                : null;
        }

        public static double? DistanceMiles(this IpResponse? record, double latitude, double longitude)
        {
            return TryCoordinates(record, out var recordLatitude, out var recordLongitude)
                ? recordLatitude.HaversineDistanceMiles(recordLongitude, latitude, longitude)
                : null;
        }

        public static bool? WithinKm(this IpResponse? record, double latitude, double longitude, double kilometres)
        {
            var distance = record.DistanceKm(latitude, longitude);

            return distance.HasValue ? distance.Value <= kilometres : null;
        }

        public static OriginBlock? FirstOrigin(this RoutingBlock? routing)
        {
            return routing?.Origins is { Length: > 0 } origins ? origins[0] : null;
        }

        public static bool TryCoordinates(this IpResponse? record, out double latitude, out double longitude)
        {
            latitude = 0;
            longitude = 0;

            if (record?.Location == null)
            {
                return false;
            }

            if (record.Location is { Lat: { } lat, Lon: { } lon })
            {
                latitude = lat;
                longitude = lon;

                return true;
            }

            if (record.Location.Hub is { } hub)
            {
                latitude = hub.Lat;
                longitude = hub.Lon;

                return true;
            }

            if (record.Location.Prior is { } prior)
            {
                latitude = prior.Lat;
                longitude = prior.Lon;

                return true;
            }

            return false;
        }
    }
}
