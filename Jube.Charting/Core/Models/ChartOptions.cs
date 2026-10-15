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

using System.Text.RegularExpressions;
using Jube.Charting.Exceptions;

namespace Jube.Charting.Core.Models
{
    public sealed record ChartOptions
    {
        public string? Title { get; init; }
        public string? Subtitle { get; init; }
        public string? XAxisTitle { get; init; }
        public string? YAxisTitle { get; init; }
        public string? SecondaryYAxisTitle { get; init; }
        public string? Description { get; init; }
        public string? Id { get; init; }
        public double Width { get; init; } = 640;
        public double Height { get; init; } = 400;
        public ChartSizing Sizing { get; init; } = ChartSizing.Responsive;
        public ThemeMode Theme { get; init; } = ThemeMode.Auto;
        public LegendPosition Legend { get; init; } = LegendPosition.Auto;
        public bool TransparentBackground { get; init; }
        public bool Interactive { get; init; } = true;
        public int LegendMaxRows { get; init; }
        public ChartStyleMode StyleMode { get; init; } = ChartStyleMode.Inline;

        internal void Validate()
        {
            if (!(Width is >= 40 and <= 4000) || !(Height is >= 20 and <= 4000))
            {
                throw new ChartSpecificationException("Chart width must be 40 to 4000 and height 20 to 4000.");
            }

            if (Id is not null && !Regex.IsMatch(Id, "^[A-Za-z][A-Za-z0-9_-]{0,40}$"))
            {
                throw new ChartSpecificationException(
                    "Chart id must start with a letter and contain only letters, digits, hyphen or underscore.");
            }
        }
    }
}