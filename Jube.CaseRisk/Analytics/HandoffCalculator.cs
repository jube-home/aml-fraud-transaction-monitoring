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

namespace Jube.CaseRisk.Analytics
{
    public static class HandoffCalculator
    {
        public const int PingPongThreshold = 3;

        public static HandoffReport Compute(IReadOnlyCollection<CaseFact> facts, int take = 10)
        {
            var distribution = new List<HandoffBucket>
            {
                new("None", facts.Count(f => f.Handoffs == 0)),
                new("1", facts.Count(f => f.Handoffs == 1)),
                new("2", facts.Count(f => f.Handoffs == 2)),
                new("3 or more", facts.Count(f => f.Handoffs >= 3))
            };

            var reopened = facts.Count(f => f.Reopens > 0);

            return new HandoffReport(distribution, reopened, facts.Count > 0 ? (double)reopened / facts.Count : 0,
                facts.Where(f => f.Handoffs >= PingPongThreshold || f.Reopens > 1)
                    .OrderByDescending(f => f.Handoffs + f.Reopens).ThenBy(f => f.CaseId).Take(take)
                    .Select(f => new HandoffCase(f.CaseId, f.Handoffs, f.Reopens)).ToList());
        }
    }
}
