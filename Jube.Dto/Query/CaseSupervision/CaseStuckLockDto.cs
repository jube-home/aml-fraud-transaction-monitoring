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

using System.ComponentModel;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSupervision
{
    [Description("An open case that has stayed locked to someone with no activity on it since.")]
    public class CaseStuckLockDto
    {
        [Description("Identifier of the case.")]
        public int CaseId { get; set; }

        [Description("The case key, such as AccountId.")]
        public string? CaseKey { get; set; }

        [Description("The case key value.")]
        public string? CaseKeyValue { get; set; }

        [Description("Name of the status the case is in.")]
        public string? StatusName { get; set; }

        [Description("The user the case is locked to.")]
        public string? LockedUser { get; set; }

        [Description("When the lock was taken (UTC).")]
        public DateTimeOffset? LockedDate { get; set; }

        [Description("When anyone last did anything on the case, or when it was locked if that is later (UTC).")]
        public DateTimeOffset LastActivityDate { get; set; }

        [Description("Minutes since the last activity.")]
        public double IdleMinutes { get; set; }

        [Description("Latest ranking Priority, 0 when the case is not yet assessed.")]
        public int Priority { get; set; }

        [Description("Latest urgency lane, 1 Live money, 2 Mandatory, 3 Deadline near, 4 Standard, 0 when not yet " +
                     "assessed.")]
        public int Lane { get; set; }
    }
}
