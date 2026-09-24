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
    internal static class UnitSupport
    {
        internal static readonly IReadOnlyDictionary<string, (string Dimension, double Factor)> Units =
            new Dictionary<string, (string Dimension, double Factor)>(StringComparer.OrdinalIgnoreCase)
            {
                ["m"] = ("length", 1.0),
                ["km"] = ("length", 1000.0),
                ["cm"] = ("length", 0.01),
                ["mm"] = ("length", 0.001),
                ["mi"] = ("length", 1609.344),
                ["yd"] = ("length", 0.9144),
                ["ft"] = ("length", 0.3048),
                ["in"] = ("length", 0.0254),
                ["nmi"] = ("length", 1852.0),
                ["kg"] = ("mass", 1.0),
                ["g"] = ("mass", 0.001),
                ["mg"] = ("mass", 1e-06),
                ["t"] = ("mass", 1000.0),
                ["lb"] = ("mass", 0.45359237),
                ["oz"] = ("mass", 0.028349523125),
                ["st"] = ("mass", 6.35029318),
                ["ozt"] = ("mass", 0.0311034768),
                ["m2"] = ("area", 1.0),
                ["km2"] = ("area", 1000000.0),
                ["ha"] = ("area", 10000.0),
                ["acre"] = ("area", 4046.8564224),
                ["ft2"] = ("area", 0.09290304),
                ["mi2"] = ("area", 2589988.110336),
                ["l"] = ("volume", 1.0),
                ["ml"] = ("volume", 0.001),
                ["m3"] = ("volume", 1000.0),
                ["gal"] = ("volume", 3.785411784),
                ["ukgal"] = ("volume", 4.54609),
                ["qt"] = ("volume", 0.946352946),
                ["pt"] = ("volume", 0.473176473),
                ["floz"] = ("volume", 0.0295735295625),
                ["m/s"] = ("speed", 1.0),
                ["km/h"] = ("speed", 0.2777777777777778),
                ["mph"] = ("speed", 0.44704),
                ["kn"] = ("speed", 0.5144444444444445),
                ["s"] = ("time", 1.0),
                ["ms"] = ("time", 0.001),
                ["min"] = ("time", 60.0),
                ["h"] = ("time", 3600.0),
                ["d"] = ("time", 86400.0),
                ["wk"] = ("time", 604800.0),
                ["b"] = ("data", 1.0),
                ["kb"] = ("data", 1000.0),
                ["mb"] = ("data", 1000000.0),
                ["gb"] = ("data", 1000000000.0),
                ["tb"] = ("data", 1000000000000.0),
                ["kib"] = ("data", 1024.0),
                ["mib"] = ("data", 1048576.0),
                ["gib"] = ("data", 1073741824.0),
                ["tib"] = ("data", 1099511627776.0)
            };

        internal static readonly string[] Temperatures = ["c", "f", "k"];

        internal static double ToKelvin(double value, string unit)
        {
            return unit switch
            {
                "c" => value + 273.15,
                "f" => (value - 32) * 5 / 9 + 273.15,
                _ => value
            };
        }

        internal static double FromKelvin(double kelvin, string unit)
        {
            return unit switch
            {
                "c" => kelvin - 273.15,
                "f" => (kelvin - 273.15) * 9 / 5 + 32,
                _ => kelvin
            };
        }
    }
}