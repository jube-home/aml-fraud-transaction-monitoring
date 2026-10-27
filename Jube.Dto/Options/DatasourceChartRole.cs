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

namespace Jube.Dto.Options
{
    using System.ComponentModel;

    public enum DatasourceChartRole
    {
        [Description("Category or label along the axis")] Category = 1,
        [Description("Numeric value, one series per column")] Value = 2,
        [Description("Column whose distinct values split the data into series")] Series = 3,
        [Description("Horizontal numeric or time value")] X = 4,
        [Description("Vertical numeric value")] Y = 5,
        [Description("Numeric value that sizes a point")] Size = 6,
        [Description("Opening value")] Open = 7,
        [Description("Highest value")] High = 8,
        [Description("Lowest value")] Low = 9,
        [Description("Closing value")] Close = 10,
        [Description("Minimum value")] Minimum = 11,
        [Description("First quartile")] FirstQuartile = 12,
        [Description("Median value")] Median = 13,
        [Description("Third quartile")] ThirdQuartile = 14,
        [Description("Maximum value")] Maximum = 15,
        [Description("Latitude in degrees")] Latitude = 16,
        [Description("Longitude in degrees")] Longitude = 17,
        [Description("ISO country code or country name")] Country = 18,
        [Description("Text label")] Label = 19,
        [Description("Parent group")] Parent = 20,
        [Description("Start time")] Start = 21,
        [Description("End time")] End = 22,
        [Description("Target value")] Target = 23,
        [Description("Row category")] Row = 24,
        [Description("Column category")] Column = 25,
        [Description("Numeric value drawn as a line")] Line = 26,
        [Description("Numeric value drawn as a line on the secondary axis")] SecondaryLine = 27
    }
}
