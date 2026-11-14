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
    public static class RankingQualityCalculator
    {
        public const int MinimumOutcomes = 30;
        public const int MinimumPerClass = 5;

        private sealed record Scored(int CaseId, bool Confirmed, double? Priority, double? Likelihood, DateTime Created);

        public static RankingQualityReport Compute(IReadOnlyCollection<CaseFact> facts)
        {
            var outcomes = facts.Where(f => f is { Closed: true } && f.OutcomeId is 1 or 2)
                .Select(f => new Scored(f.CaseId, f.OutcomeId == 1, f.Priority, f.Likelihood, f.CreatedDate)).ToList();

            var confirmed = outcomes.Count(o => o.Confirmed);
            var baseRate = outcomes.Count > 0 ? (double)confirmed / outcomes.Count : 0;
            var sufficient = outcomes.Count >= MinimumOutcomes && confirmed >= MinimumPerClass
                             && outcomes.Count - confirmed >= MinimumPerClass;

            var assessed = outcomes.Where(o => o.Priority.HasValue).ToList();
            var withLikelihood = outcomes.Where(o => o.Likelihood.HasValue).ToList();

            var priorityGains = Gains(assessed.OrderByDescending(o => o.Priority).ThenBy(o => o.CaseId)
                .Select(o => o.Confirmed).ToList());
            var likelihoodGains = Gains(withLikelihood.OrderByDescending(o => o.Likelihood).ThenBy(o => o.CaseId)
                .Select(o => o.Confirmed).ToList());
            var oldestGains = Gains(outcomes.OrderBy(o => o.Created).ThenBy(o => o.CaseId)
                .Select(o => o.Confirmed).ToList());

            var bins = Calibration(withLikelihood.Select(o => (o.Likelihood!.Value, o.Confirmed)).ToList());

            double? brier = withLikelihood.Count > 0
                ? withLikelihood.Average(o => Math.Pow(o.Likelihood!.Value - (o.Confirmed ? 1 : 0), 2))
                : null;

            double? ece = withLikelihood.Count > 0
                ? bins.Sum(b => b.Cases * Math.Abs(b.Predicted - b.Observed)) / withLikelihood.Count
                : null;

            return new RankingQualityReport(outcomes.Count, confirmed, baseRate, sufficient,
                Auc(assessed.Select(o => (o.Priority!.Value, o.Confirmed)).ToList()),
                Auc(withLikelihood.Select(o => (o.Likelihood!.Value, o.Confirmed)).ToList()),
                Auc(outcomes.Select(o => (-(double)o.Created.Ticks, o.Confirmed)).ToList()),
                Lift(assessed.OrderByDescending(o => o.Priority).ThenBy(o => o.CaseId).Select(o => o.Confirmed).ToList(),
                    0.1),
                Lift(assessed.OrderByDescending(o => o.Priority).ThenBy(o => o.CaseId).Select(o => o.Confirmed).ToList(),
                    0.2),
                Lift(assessed.OrderByDescending(o => o.Priority).ThenBy(o => o.CaseId).Select(o => o.Confirmed).ToList(),
                    0.3),
                priorityGains, likelihoodGains, oldestGains, bins, brier, ece);
        }

        public static List<GainsPoint> Gains(IReadOnlyList<bool> orderedConfirmed)
        {
            var points = new List<GainsPoint>();
            var total = orderedConfirmed.Count;
            var confirmed = orderedConfirmed.Count(c => c);

            if (total == 0)
            {
                return points;
            }

            var cumulative = new int[total + 1];

            for (var i = 0; i < total; i++)
            {
                cumulative[i + 1] = cumulative[i] + (orderedConfirmed[i] ? 1 : 0);
            }

            for (var step = 0; step <= 20; step++)
            {
                var fraction = step / 20.0;
                var worked = (int)Math.Ceiling(fraction * total);

                points.Add(new GainsPoint(fraction, confirmed > 0 ? (double)cumulative[worked] / confirmed : 0));
            }

            return points;
        }

        public static double? Auc(IReadOnlyList<(double Score, bool Confirmed)> items)
        {
            var positives = items.Count(i => i.Confirmed);
            var negatives = items.Count - positives;

            if (positives == 0 || negatives == 0)
            {
                return null;
            }

            double wins = 0;

            foreach (var positive in items.Where(i => i.Confirmed))
            {
                foreach (var negative in items.Where(i => !i.Confirmed))
                {
                    wins += positive.Score > negative.Score ? 1 : positive.Score == negative.Score ? 0.5 : 0;
                }
            }

            return wins / ((double)positives * negatives);
        }

        public static double? Lift(IReadOnlyList<bool> orderedConfirmed, double fraction)
        {
            if (orderedConfirmed.Count == 0)
            {
                return null;
            }

            var baseRate = (double)orderedConfirmed.Count(c => c) / orderedConfirmed.Count;

            if (baseRate == 0)
            {
                return null;
            }

            var top = Math.Max(1, (int)Math.Ceiling(fraction * orderedConfirmed.Count));
            var precision = (double)orderedConfirmed.Take(top).Count(c => c) / top;

            return precision / baseRate;
        }

        public static List<CalibrationBin> Calibration(IReadOnlyList<(double Likelihood, bool Confirmed)> items)
        {
            var bins = new List<CalibrationBin>();

            for (var i = 0; i < 10; i++)
            {
                var from = i / 10.0;
                var to = (i + 1) / 10.0;

                var inside = items.Where(x => x.Likelihood >= from && (x.Likelihood < to || i == 9)).ToList();

                if (inside.Count == 0)
                {
                    continue;
                }

                bins.Add(new CalibrationBin(from, to, inside.Count, inside.Average(x => x.Likelihood),
                    (double)inside.Count(x => x.Confirmed) / inside.Count));
            }

            return bins;
        }
    }
}
