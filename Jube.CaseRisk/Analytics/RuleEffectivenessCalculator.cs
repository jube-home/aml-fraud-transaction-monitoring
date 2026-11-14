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
    public static class RuleEffectivenessCalculator
    {
        public static List<RuleEffectiveness> Compute(IReadOnlyCollection<CaseFact> facts)
        {
            double Handling(CaseFact f) => f.ClosedDate.HasValue
                ? Math.Max(0, (f.ClosedDate.Value - f.CreatedDate).TotalMinutes)
                : 0;

            var groups = facts.GroupBy(f => f.RuleGuid).Select(g =>
            {
                var confirmed = g.Count(f => f is { Closed: true, OutcomeId: 1 });
                var notConfirmed = g.Count(f => f is { Closed: true, OutcomeId: 2 });
                var decided = confirmed + notConfirmed;
                var closed = g.Where(f => f.Closed).ToList();

                return new
                {
                    g.Key,
                    Name = g.Select(f => f.RuleName).FirstOrDefault(n => !string.IsNullOrEmpty(n))
                           ?? (g.Key.HasValue ? "An activation rule" : "Not created by a rule"),
                    Cases = g.Count(),
                    Open = g.Count(f => !f.Closed),
                    Confirmed = confirmed,
                    NotConfirmed = notConfirmed,
                    NoOutcome = closed.Count - decided,
                    Precision = decided > 0 ? (double?)confirmed / decided : null,
                    Mean = closed.Count > 0 ? closed.Average(Handling) : 0,
                    Wasted = g.Where(f => f is { Closed: true, OutcomeId: 2 }).Sum(Handling)
                };
            }).ToList();

            var totalWasted = groups.Sum(g => g.Wasted);

            return groups.Select(g => new RuleEffectiveness(g.Key, g.Name, g.Cases, g.Open, g.Confirmed,
                    g.NotConfirmed, g.NoOutcome, g.Precision, g.Mean, g.Wasted,
                    totalWasted > 0 ? g.Wasted / totalWasted : 0))
                .OrderByDescending(r => r.WastedMinutes).ThenByDescending(r => r.Cases).ToList();
        }
    }
}
