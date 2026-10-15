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

namespace Jube.Charting.More.Models
{
    public sealed record SurvivalStep(
        double Time,
        double Survival,
        double Lower,
        double Upper,
        double StandardError,
        int AtRisk,
        int Events,
        int Censored);

    public sealed record SurvivalCurve(
        IReadOnlyList<SurvivalStep> Steps,
        IReadOnlyList<double> CensorTimes,
        IReadOnlyList<double> SortedTimes,
        int Count,
        int EventCount,
        double Confidence,
        // ReSharper disable once NotAccessedPositionalProperty.Global
        ConfidenceMethod Method)
    {
        public double? Median
        {
            get
            {
                foreach (var step in Steps)
                {
                    if (step is { Time: > 0, Survival: <= 0.5 })
                    {
                        return step.Time;
                    }
                }

                return null;
            }
        }

        public double MaxTime => SortedTimes.Count > 0 ? SortedTimes[^1] : Steps.Count > 0 ? Steps[^1].Time : 0;

        public SurvivalStep StepAt(double time)
        {
            var lo = 0;
            var hi = Steps.Count - 1;
            var best = 0;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                if (Steps[mid].Time <= time)
                {
                    best = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return Steps[best];
        }

        public int AtRiskAt(double time)
        {
            if (SortedTimes.Count > 0)
            {
                var lo = 0;
                var hi = SortedTimes.Count;

                while (lo < hi)
                {
                    var mid = (lo + hi) / 2;

                    if (SortedTimes[mid] < time)
                    {
                        lo = mid + 1;
                    }
                    else
                    {
                        hi = mid;
                    }
                }

                return SortedTimes.Count - lo;
            }

            var step = StepAt(time);

            return time <= step.Time ? step.AtRisk : Math.Max(step.AtRisk - step.Events - step.Censored, 0);
        }
    }

    public sealed record LogRankResult(double Statistic, int DegreesOfFreedom, double PValue);
}