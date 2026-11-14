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
using System.Reflection;

namespace Jube.Charting.Maps
{
    internal static class WorldMap
    {
        public const double MinimumLatitude = -58;
        public const double MaximumLatitude = 84;

        private const string ResourceName = "Jube.Charting.Maps.world-110m.txt";

        private static readonly Lazy<IReadOnlyList<MapCountry>> countries = new(Load);

        private static readonly Lazy<IReadOnlyDictionary<string, MapCountry>> index = new(Index);

        private static readonly Dictionary<string, string> aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["UK"] = "GBR", ["Great Britain"] = "GBR", ["USA"] = "USA", ["United States"] = "USA",
            ["Czech Republic"] = "CZE", ["Ivory Coast"] = "CIV", ["Cote d'Ivoire"] = "CIV", ["Burma"] = "MMR",
            ["Swaziland"] = "SWZ", ["Macedonia"] = "MKD", ["East Timor"] = "TLS", ["Timor Leste"] = "TLS",
            ["Russian Federation"] = "RUS", ["South Korea"] = "KOR", ["Republic of Korea"] = "KOR",
            ["North Korea"] = "PRK", ["Vietnam"] = "VNM", ["Viet Nam"] = "VNM", ["Laos"] = "LAO",
            ["Iran, Islamic Republic of"] = "IRN", ["Syrian Arab Republic"] = "SYR", ["Tanzania"] = "TZA",
            ["Bolivia"] = "BOL", ["Venezuela"] = "VEN", ["Moldova"] = "MDA", ["Congo (Kinshasa)"] = "COD",
            ["Democratic Republic of the Congo"] = "COD", ["Congo (Brazzaville)"] = "COG"
        };

        public static IReadOnlyList<MapCountry> Countries => countries.Value;

        public static MapCountry? Find(string? code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var key = code.Trim();

            if (aliases.TryGetValue(key, out var alias))
            {
                key = alias;
            }

            return index.Value.GetValueOrDefault(key.ToUpperInvariant());
        }

        public static (double X, double Y) Project(double longitude, double latitude)
        {
            var lambda = longitude * Math.PI / 180;
            var phi = latitude * Math.PI / 180;
            var p2 = phi * phi;
            var p4 = p2 * p2;
            var p6 = p4 * p2;
            var p8 = p4 * p4;
            var p10 = p8 * p2;
            var p12 = p6 * p6;

            var x = lambda * (0.870700 - 0.131979 * p2 - 0.013791 * p4 + 0.003971 * p10 - 0.001529 * p12);
            var y = phi * (1.007226 + 0.015085 * p2 - 0.044475 * p6 + 0.028874 * p8 - 0.005916 * p10);

            return (x, -y);
        }

        public static (double MinX, double MinY, double MaxX, double MaxY) Bounds()
        {
            var west = Project(-180, 0);
            var east = Project(180, 0);
            var north = Project(0, MaximumLatitude);
            var south = Project(0, MinimumLatitude);

            return (west.X, north.Y, east.X, south.Y);
        }

        private static IReadOnlyDictionary<string, MapCountry> Index()
        {
            var map = new Dictionary<string, MapCountry>(StringComparer.Ordinal);

            foreach (var country in Countries)
            {
                map[country.Iso3.ToUpperInvariant()] = country;
            }

            foreach (var country in Countries)
            {
                if (country.Iso2.Length > 0)
                {
                    map.TryAdd(country.Iso2.ToUpperInvariant(), country);
                }

                map.TryAdd(country.Name.ToUpperInvariant(), country);
            }

            return map;
        }

        private static IReadOnlyList<MapCountry> Load()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
                               ?? throw new InvalidOperationException("The world outline resource is missing.");

            using var reader = new StreamReader(stream);
            var result = new List<MapCountry>();

            while (reader.ReadLine() is { Length: > 0 } line)
            {
                var parts = line.Split('\t');
                var rings = new List<(double Lon, double Lat)[]>();

                foreach (var ring in parts[3].Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var numbers = ring.Split(' ');
                    var points = new (double Lon, double Lat)[numbers.Length / 2];

                    for (var i = 0; i < points.Length; i++)
                    {
                        points[i] = (
                            int.Parse(numbers[i * 2], CultureInfo.InvariantCulture) / 10.0,
                            int.Parse(numbers[i * 2 + 1], CultureInfo.InvariantCulture) / 10.0);
                    }

                    rings.Add(points);
                }

                result.Add(new MapCountry(parts[0], parts[1], parts[2], rings));
            }

            return result;
        }
    }
}
