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

using System.ComponentModel;
using Jube.Dictionary.Extensions;
using Jube.Service.Agent;
using Jube.Service.Agent.Models;
using Jube.Service.Extensions.Geospatial.Models;

namespace Jube.Service.Extensions.Geospatial
{
    [FunctionToolGroup("Geospatial",
        "Great-circle geometry on latitude and longitude: distances, radius checks, implied travel speed for impossible-travel analysis, bearings and geohashes.")]
    public static class GeospatialTools
    {
        [FunctionTool("DistanceBetweenPoints")]
        [Description(
            "Great-circle (haversine) distance between two points given as latitude and longitude in decimal degrees, in both kilometres and miles.")]
        public static GeoDistance DistanceBetweenPoints(
            [Description("Latitude of the first point, -90 to 90.")]
            double latitude1,
            [Description("Longitude of the first point, -180 to 180.")]
            double longitude1,
            [Description("Latitude of the second point, -90 to 90.")]
            double latitude2,
            [Description("Longitude of the second point, -180 to 180.")]
            double longitude2)
        {
            Points(latitude1, longitude1, latitude2, longitude2);

            return new GeoDistance(latitude1.HaversineDistanceKilometers(longitude1, latitude2, longitude2),
                latitude1.HaversineDistanceMiles(longitude1, latitude2, longitude2));
        }

        [FunctionTool("IsWithinRadius")]
        [Description(
            "Whether a point lies within a radius in kilometres of a centre point, by great-circle distance. Use it for geofences around a home address or merchant.")]
        public static bool IsWithinRadius(
            [Description("Latitude of the centre.")]
            double centreLatitude,
            [Description("Longitude of the centre.")]
            double centreLongitude,
            [Description("Latitude of the point being tested.")]
            double latitude,
            [Description("Longitude of the point being tested.")]
            double longitude,
            [Description("The radius in kilometres.")]
            double radiusKilometres)
        {
            Points(centreLatitude, centreLongitude, latitude, longitude);

            return centreLatitude.IsWithinRadiusKm(centreLongitude, latitude, longitude, radiusKilometres);
        }

        [FunctionTool("ImpliedTravelSpeed")]
        [Description(
            "Speed in km/h someone would need to travel between two locations in the time between two events. Over about 900 km/h (a commercial flight) suggests impossible travel, such as the same card used in two countries an hour apart.")]
        public static double ImpliedTravelSpeed(
            [Description("Latitude of the first event.")]
            double latitude1,
            [Description("Longitude of the first event.")]
            double longitude1,
            [Description("Latitude of the second event.")]
            double latitude2,
            [Description("Longitude of the second event.")]
            double longitude2,
            [Description("Hours between the two events; must be positive.")]
            double hoursElapsed)
        {
            Points(latitude1, longitude1, latitude2, longitude2);

            return FunctionToolInput.Defined(
                latitude1.ImpliedTravelSpeedKmh(longitude1, latitude2, longitude2, hoursElapsed),
                "hoursElapsed must be positive.");
        }

        [FunctionTool("InitialBearing")]
        [Description(
            "Initial compass bearing in degrees (0 north, 90 east) for the great-circle route from a first point to a second.")]
        public static double InitialBearing(
            [Description("Latitude of the start point.")]
            double latitude1,
            [Description("Longitude of the start point.")]
            double longitude1,
            [Description("Latitude of the destination.")]
            double latitude2,
            [Description("Longitude of the destination.")]
            double longitude2)
        {
            Points(latitude1, longitude1, latitude2, longitude2);

            return latitude1.InitialBearingDegrees(longitude1, latitude2, longitude2);
        }

        [FunctionTool("EncodeGeohash")]
        [Description(
            "Encodes a latitude and longitude as a geohash of 1 to 12 characters; longer is more precise (6 is about 1.2 km, 9 about 5 m). Points sharing a prefix are near each other, which makes geohashes good grouping keys.")]
        public static string EncodeGeohash(
            [Description("Latitude, -90 to 90.")] double latitude,
            [Description("Longitude, -180 to 180.")]
            double longitude,
            [Description("Number of characters, 1 to 12.")]
            int precision = 9)
        {
            return FunctionToolInput.Found(latitude.ToGeohash(longitude, precision),
                "coordinates must be in range and precision 1 to 12.");
        }

        [FunctionTool("DecodeGeohash")]
        [Description("The centre latitude and longitude of the cell a geohash names.")]
        public static GeoPoint DecodeGeohash([Description("The geohash, 1 to 12 characters.")] string geohash)
        {
            var value = FunctionToolInput.Required(geohash, nameof(geohash), 12);
            var latitude = value.GeohashLatitude();

            if (double.IsNaN(latitude))
            {
                throw new ArgumentException($"'{geohash}' is not a valid geohash.", nameof(geohash));
            }

            return new GeoPoint(latitude, value.GeohashLongitude());
        }

        [FunctionTool("IsValidCoordinate")]
        [Description(
            "Whether a latitude and longitude pair is in range: latitude -90 to 90 and longitude -180 to 180. Swapped coordinates often fail this check.")]
        public static bool IsValidCoordinate(
            [Description("The latitude in decimal degrees.")]
            double latitude,
            [Description("The longitude in decimal degrees.")]
            double longitude)
        {
            return latitude.IsValidCoordinate(longitude);
        }

        private static void Points(double latitude1, double longitude1, double latitude2, double longitude2)
        {
            if (!latitude1.IsValidCoordinate(longitude1) || !latitude2.IsValidCoordinate(longitude2))
            {
                throw new ArgumentException("Latitudes must be -90 to 90 and longitudes -180 to 180.");
            }
        }
    }
}