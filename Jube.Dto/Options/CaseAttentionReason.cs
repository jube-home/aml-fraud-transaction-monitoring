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

    public enum CaseAttentionReason
    {
        [Description("Reopened since you last looked")] Reopened = 1,
        [Description("Escalated to a higher priority status")] Escalated = 2,
        [Description("Moved to another status by someone else")] MovedByOther = 3,
        [Description("The case diary is due")] DiaryDue = 4,
        [Description("The risk has risen since you last looked")] RiskRose = 5,
        [Description("Others have changed the case since you last looked")] NewActivity = 6
    }
}
