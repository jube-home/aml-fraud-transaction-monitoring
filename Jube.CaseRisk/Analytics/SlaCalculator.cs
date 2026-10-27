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
    public static class SlaCalculator
    {
        public static SlaReport Compute(IReadOnlyCollection<CaseFact> facts, DateTime now, int watchTake = 15)
        {
            var assessed = facts.Select(f => (Fact: f, Assessment: ServiceLevelCalculator.Assess(new ServiceLevelInputs(now,
                f.CreatedDate, f.Closed ? f.ClosedDate : null, f.FirstViewedDate, f.FirstOwnedDate,
                f.Closed ? null : f.StayEnteredDate, f.FirstViewTargetMinutes, f.OwnershipTargetMinutes,
                f.ResolutionTargetMinutes, f.MaxDwellMinutes)))).ToList();

            var measures = assessed.SelectMany(a => a.Assessment.Measures.Select(m => (a.Fact, Measure: m))).ToList();

            var dimensions = Enum.GetValues<ServiceLevelDimension>().Select(d =>
            {
                var of = measures.Where(m => m.Measure.Dimension == d).ToList();
                var met = of.Count(m => m.Measure.State == ServiceLevelState.Met);
                var breached = of.Count(m => m.Measure.State == ServiceLevelState.Breached);

                return new SlaDimensionSummary(d, met, breached,
                    of.Count(m => m.Measure.State == ServiceLevelState.AtRisk),
                    of.Count(m => m.Measure.State == ServiceLevelState.OnTrack),
                    met + breached > 0 ? (double)met / (met + breached) : null);
            }).Where(d => d.Met + d.Breached + d.AtRisk + d.OnTrack > 0).ToList();

            var weekly = measures.Where(m => m.Measure.State is ServiceLevelState.Met or ServiceLevelState.Breached)
                .GroupBy(m => (Week: WeekStart(m.Fact.CreatedDate), m.Measure.Dimension))
                .Select(g => new SlaWeek(g.Key.Week, g.Key.Dimension,
                    g.Count(x => x.Measure.State == ServiceLevelState.Met),
                    g.Count(x => x.Measure.State == ServiceLevelState.Breached)))
                .OrderBy(w => w.WeekStart).ThenBy(w => w.Dimension).ToList();

            var watch = measures.Where(m => m.Measure.State is ServiceLevelState.AtRisk or ServiceLevelState.Breached
                                            && !m.Fact.Closed && m.Measure.ActualMinutes == null)
                .OrderByDescending(m => m.Measure.State).ThenByDescending(m => m.Measure.ElapsedFraction)
                .Take(watchTake)
                .Select(m => new SlaWatch(m.Fact.CaseId, m.Measure.Dimension, m.Measure.State,
                    m.Measure.ElapsedFraction, m.Measure.DueDate)).ToList();

            var open = assessed.Where(a => !a.Fact.Closed).Select(a => a.Assessment.Measures
                .Where(m => m.ActualMinutes == null).Select(m => m.State).ToList()).ToList();

            return new SlaReport(dimensions, weekly, watch, open.Count(s => s.Contains(ServiceLevelState.Breached)),
                open.Count(s => !s.Contains(ServiceLevelState.Breached) && s.Contains(ServiceLevelState.AtRisk)));
        }

        public static DateTime WeekStart(DateTime date)
        {
            var diff = ((int)date.DayOfWeek + 6) % 7;

            return date.Date.AddDays(-diff);
        }
    }
}
