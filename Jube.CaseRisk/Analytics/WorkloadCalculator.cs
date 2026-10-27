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
    public static class WorkloadCalculator
    {
        public const int MinimumPeopleToShowATeam = 3;

        public static WorkloadReport Compute(IReadOnlyCollection<CaseFact> facts, string user)
        {
            static double Minutes(CaseFact f) => Math.Max(0, (f.ClosedDate!.Value - f.CreatedDate).TotalMinutes);

            var closed = facts.Where(f => f is { Closed: true, ClosedDate: not null }
                                          && !string.IsNullOrEmpty(f.ClosedUser)).ToList();
            var mine = closed.Where(f => f.ClosedUser == user).ToList();

            var person = new PersonWorkload(mine.Count, DistributionStats.Of(mine.Select(Minutes)),
                facts.Count(f => !f.Closed && f.LockedUser == user));

            var perPerson = closed.GroupBy(f => f.ClosedUser).Select(g => (double)g.Count()).ToList();

            if (perPerson.Count < MinimumPeopleToShowATeam)
            {
                return new WorkloadReport(person, null);
            }

            return new WorkloadReport(person, new TeamWorkload(perPerson.Count, DistributionStats.Of(perPerson).P50,
                DistributionStats.Of(closed.Select(Minutes)).P50));
        }
    }
}
