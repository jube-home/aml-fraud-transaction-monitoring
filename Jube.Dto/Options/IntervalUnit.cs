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
    using System.Runtime.Serialization;

    public enum IntervalUnit
    {
        [Description("Seconds"), EnumMember(Value = "s")] Second = 1,
        [Description("Minutes"), EnumMember(Value = "n")] Minute = 2,
        [Description("Hours"), EnumMember(Value = "h")] Hour = 3,
        [Description("Days"), EnumMember(Value = "d")] Day = 4,
        [Description("Months"), EnumMember(Value = "m")] Month = 5,
        [Description("Years"), EnumMember(Value = "y")] Year = 6
    }
}
