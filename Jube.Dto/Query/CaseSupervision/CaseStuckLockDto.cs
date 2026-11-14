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
using MessagePack;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Dto.Query.CaseSupervision
{
    [Description("An open case that has stayed locked to someone with no activity on it since.")]
    [MessagePackObject]
    public class CaseStuckLockDto
    {
        [Description("Identifier of the case.")]
        [Key(0)]
        public int CaseId { get; set; }

        [Description("The case key, such as AccountId.")]
        [Key(1)]
        public string? CaseKey { get; set; }

        [Description("The case key value.")]
        [Key(2)]
        public string? CaseKeyValue { get; set; }

        [Description("Name of the status the case is in.")]
        [Key(3)]
        public string? StatusName { get; set; }

        [Description("The user the case is locked to.")]
        [Key(4)]
        public string? LockedUser { get; set; }

        [Description("When the lock was taken (UTC).")]
        [Key(5)]
        public DateTimeOffset? LockedDate { get; set; }

        [Description("When anyone last did anything on the case, or when it was locked if that is later (UTC).")]
        [Key(6)]
        public DateTimeOffset LastActivityDate { get; set; }

        [Description("Minutes since the last activity.")]
        [Key(7)]
        public double IdleMinutes { get; set; }

        [Description("Latest ranking Priority, 0 when the case is not yet assessed.")]
        [Key(8)]
        public int Priority { get; set; }

        [Description("Latest urgency lane, 1 Live money, 2 Mandatory, 3 Deadline near, 4 Standard, 0 when not yet " +
                     "assessed.")]
        [Key(9)]
        public int Lane { get; set; }
    }
}
