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

    public enum SanctionAggregationType
    {
        [Description("Sum")] Sum = 1,
        [Description("Average")] Average = 2,
        [Description("Count")] Count = 3,
        [Description("Max")] Max = 4,
        [Description("Min")] Min = 5,
        [Description("First")] First = 6,
        [Description("Last")] Last = 7,
        [Description("Confidence")] Confidence = 8
    }
}
