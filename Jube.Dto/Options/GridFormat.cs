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

    public enum GridFormat
    {
        [Description("Drawn as the type suggests")] Default = 0,
        [Description("A whole number with thousands separators")] Integer = 1,
        [Description("A number to two decimal places")] Decimal = 2,
        [Description("A fraction shown as a percentage")] Percent = 3,
        [Description("A UTC date and time")] DateTime = 4,
        [Description("A UTC date")] Date = 5,
        [Description("A code or identifier in a fixed-width face")] Code = 6
    }
}
