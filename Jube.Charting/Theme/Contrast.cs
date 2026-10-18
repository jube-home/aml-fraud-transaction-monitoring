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

namespace Jube.Charting.Theme
{
    public static class Contrast
    {
        public static double Ratio(string foreground, string background)
        {
            return Ratio(Rgb.Parse(foreground), Rgb.Parse(background));
        }

        public static double Ratio(Rgb foreground, Rgb background)
        {
            var a = foreground.Luminance();
            var b = background.Luminance();
            var lighter = Math.Max(a, b);
            var darker = Math.Min(a, b);

            return (lighter + 0.05) / (darker + 0.05);
        }

        public static Rgb BestText(Rgb fill, Rgb dark, Rgb light)
        {
            return Ratio(dark, fill) >= Ratio(light, fill) ? dark : light;
        }
    }
}