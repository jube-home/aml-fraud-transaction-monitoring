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

    public enum CaseSearchView
    {
        [Description("My cases")] Mine = 1,
        [Description("Returned to me")] Returned = 2,
        [Description("Free for all")] Free = 3,
        [Description("Urgent")] Urgent = 4,
        [Description("Overdue")] Overdue = 5,
        [Description("All open")] All = 6,
        [Description("Quality review samples")] QaSamples = 7
    }
}
