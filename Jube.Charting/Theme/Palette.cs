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
    public static class Palette
    {
        private static readonly string[] sequentialLight =
            ["#cde2fb", "#9ec5f4", "#86b6ef", "#6da7ec", "#3987e5", "#2a78d6", "#1c5cab", "#184f95", "#0d366b"];

        private static string[] BuildDiverging(string negative, string middle, string positive)
        {
            var n = Rgb.Parse(negative);
            var m = Rgb.Parse(middle);
            var p = Rgb.Parse(positive);
            var result = new string[11];

            for (var i = 0; i < 5; i++)
            {
                result[i] = Rgb.Lerp(n, m, i / 5.0).Hex;
                result[10 - i] = Rgb.Lerp(p, m, i / 5.0).Hex;
            }

            result[5] = m.Hex;

            return result;
        }

        public const int CategoricalCount = 8;

        public static ThemeVariant Light { get; } = new()
        {
            Name = "light",
            Surface = "#fcfcfb",
            Text = "#0b0b0b",
            TextSecondary = "#52514e",
            TextMuted = "#66655f",
            Grid = "#e8e7e3",
            Axis = "#b3b2ab",
            Tooltip = "#ffffff",
            Categorical = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#4a3aa7", "#e34948"],
            Sequential = sequentialLight,
            Diverging = BuildDiverging("#1c5cab", "#f0efec", "#c23a39")
        };

        public static ThemeVariant Dark { get; } = new()
        {
            Name = "dark",
            Surface = "#1a1a19",
            Text = "#ffffff",
            TextSecondary = "#c3c2b7",
            TextMuted = "#a3a297",
            Grid = "#2e2e2c",
            Axis = "#57564f",
            Tooltip = "#2a2a28",
            Categorical = ["#3987e5", "#d95926", "#199e70", "#c98500", "#d55181", "#008300", "#9085e9", "#e66767"],
            Sequential = [.. sequentialLight.Reverse()],
            Diverging = BuildDiverging("#3987e5", "#383835", "#e66767")
        };
    }
}