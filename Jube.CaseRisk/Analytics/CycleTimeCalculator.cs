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
    public static class CycleTimeCalculator
    {
        public static CycleTimeReport Compute(IReadOnlyCollection<CaseFact> facts, DateTime now)
        {
            static double Minutes(DateTime from, DateTime to) => Math.Max(0, (to - from).TotalMinutes);

            var firstView = facts.Where(f => f.FirstViewedDate.HasValue)
                .Select(f => Minutes(f.CreatedDate, f.FirstViewedDate!.Value)).ToList();
            var ownership = facts.Where(f => f.FirstOwnedDate.HasValue)
                .Select(f => Minutes(f.CreatedDate, f.FirstOwnedDate!.Value)).ToList();
            var resolution = facts.Where(f => f is { Closed: true, ClosedDate: not null })
                .Select(f => Minutes(f.CreatedDate, f.ClosedDate!.Value)).ToList();
            var waiting = facts.Where(f => !f.Closed && !f.FirstViewedDate.HasValue)
                .Select(f => Minutes(f.CreatedDate, now)).ToList();

            return new CycleTimeReport(facts.Count, DistributionStats.Of(firstView), DistributionStats.Of(ownership),
                DistributionStats.Of(resolution), DistributionStats.Of(waiting),
                facts.Count(f => !f.FirstViewedDate.HasValue), facts.Count(f => !f.FirstOwnedDate.HasValue),
                Histograms.Build(firstView, Histograms.MinuteEdges, Histograms.Minutes),
                Histograms.Build(resolution, Histograms.MinuteEdges, Histograms.Minutes));
        }
    }
}
