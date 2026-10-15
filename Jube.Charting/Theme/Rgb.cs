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
using Jube.Charting.Exceptions;

namespace Jube.Charting.Theme
{
    public readonly record struct Rgb(byte R, byte G, byte B)
    {
        public string Hex => string.Create(CultureInfo.InvariantCulture, $"#{R:x2}{G:x2}{B:x2}");

        public static bool TryParse(string? value, out Rgb color)
        {
            color = default;

            if (string.IsNullOrEmpty(value) || value[0] != '#')
            {
                return false;
            }

            var hex = value[1..];

            if (hex.Length == 3)
            {
                hex = string.Concat(hex.Select(c => new string(c, 2)));
            }

            if (hex.Length != 6 || !hex.All(Uri.IsHexDigit))
            {
                return false;
            }

            color = new Rgb(
                byte.Parse(hex[..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
                byte.Parse(hex[4..], NumberStyles.HexNumber, CultureInfo.InvariantCulture));

            return true;
        }

        public static Rgb Parse(string value)
        {
            return TryParse(value, out var color)
                ? color
                : throw new ChartSpecificationException("Colours must be #rgb or #rrggbb hexadecimal values.");
        }

        public static Rgb Lerp(Rgb a, Rgb b, double t)
        {
            return new Rgb(
                (byte)Math.Round(a.R + (b.R - a.R) * t),
                (byte)Math.Round(a.G + (b.G - a.G) * t),
                (byte)Math.Round(a.B + (b.B - a.B) * t));
        }

        public double Luminance()
        {
            static double Channel(byte v)
            {
                var s = v / 255.0;

                return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
            }

            return 0.2126 * Channel(R) + 0.7152 * Channel(G) + 0.0722 * Channel(B);
        }
    }
}