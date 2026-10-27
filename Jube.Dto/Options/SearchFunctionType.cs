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

    public enum SearchFunctionType
    {
        [Description("Count")] Count = 1,
        [Description("Distinct count")] DistinctCount = 2,
        [Description("Sum")] Sum = 3,
        [Description("Average")] Average = 4,
        [Description("Median")] Median = 5,
        [Description("Kurtosis")] Kurtosis = 6,
        [Description("Skew")] Skew = 7,
        [Description("Standard deviation")] StandardDeviation = 8,
        [Description("Mode")] Mode = 11,
        [Description("Same count")] SameCount = 12,
        [Description("Actual value")] ActualValue = 13,
        [Description("Max")] Max = 14,
        [Description("Min")] Min = 15,
        [Description("Since")] Since = 16
    }
}
