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

using System;
using Jube.CaseRisk.Analytics;

namespace Jube.Test.CaseRisk
{
    internal static class FactBuilder
    {
        public static readonly DateTime Start = new(2026, 10, 5, 9, 0, 0, DateTimeKind.Utc);
        public static readonly Guid Status = Guid.Parse("00000000-0000-0000-0000-0000000000a1");

        public static CaseFact Fact(int id, double createdMinutes = 0, double? closedMinutes = null,
            int outcome = 0, Guid? rule = null, string? ruleName = null, double? viewedMinutes = null,
            double? ownedMinutes = null, int reopens = 0, int handoffs = 0, int? lane = null, int? priority = null,
            double? likelihood = null, string? closedUser = null, string? lockedUser = null,
            int? firstViewTarget = null, int? ownershipTarget = null, int? resolutionTarget = null,
            double? stayMinutes = null, int? maxDwell = null, DateTime? created = null) => new(id, Status,
            created ?? Start.AddMinutes(createdMinutes),
            closedMinutes.HasValue ? Start.AddMinutes(closedMinutes.Value) : null, closedMinutes.HasValue, outcome,
            rule, ruleName, viewedMinutes.HasValue ? Start.AddMinutes(viewedMinutes.Value) : null,
            ownedMinutes.HasValue ? Start.AddMinutes(ownedMinutes.Value) : null, reopens, handoffs, lane, priority,
            likelihood, null, firstViewTarget, ownershipTarget, resolutionTarget,
            stayMinutes.HasValue ? Start.AddMinutes(stayMinutes.Value) : null, maxDwell, closedUser, lockedUser);
    }
}
