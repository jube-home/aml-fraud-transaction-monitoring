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

    public enum CaseCloseReason
    {
        [Description("No reason recorded")] None = 0,
        [Description("False positive: known customer activity")] FalsePositiveKnownActivity = 1,
        [Description("False positive: rule too broad")] FalsePositiveRuleTooBroad = 2,
        [Description("Duplicate of another case")] Duplicate = 3,
        [Description("Insufficient evidence")] InsufficientEvidence = 4,
        [Description("Suspicion confirmed and reported")] SuspicionConfirmedAndReported = 5,
        [Description("Fraud confirmed, loss prevented")] FraudConfirmedLossPrevented = 6,
        [Description("Customer contacted and explained")] CustomerExplained = 7,
        [Description("Other")] Other = 8
    }
}
