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

    public enum NotificationKind
    {
        [Description("Case service level breached")] CaseServiceLevelBreached = 1,
        [Description("Sanctions rescreen found new matches")] SanctionRescreenHits = 2,
        [Description("Case digest")] CaseDigest = 3,
        [Description("Model drift")] ModelDrift = 4,
        [Description("Entity risk reviews due")] EntityRiskReviewDue = 5,
        [Description("Report awaiting approval")] CaseReportAwaitingApproval = 6,
        [Description("Report decided")] CaseReportDecided = 7,
        [Description("Report answered by the FIU")] CaseReportFeedback = 8
    }
}
