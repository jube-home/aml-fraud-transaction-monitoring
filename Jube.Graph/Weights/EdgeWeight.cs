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

namespace Jube.Graph.Weights
{
    public static class EdgeWeight
    {
        public static double Decay(double value, DateTimeOffset from, DateTimeOffset to, double halfLifeDays)
        {
            if (halfLifeDays <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(halfLifeDays));
            }

            return value * Math.Pow(2d, -(to - from).TotalDays / halfLifeDays);
        }

        public static double Specificity(int distinctCounterparts)
        {
            if (distinctCounterparts < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(distinctCounterparts));
            }

            return 1d / Math.Log(2d + distinctCounterparts);
        }

        public static double Weight(double kindPrior, double confidence, double specificity, double decayedCount,
            double decayedAmount)
        {
            return kindPrior * confidence * specificity * (1d + Math.Log(1d + decayedCount)) * (1d + decayedAmount);
        }

        public static double LogAmount(double amount, double referenceAmount)
        {
            if (referenceAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(referenceAmount));
            }

            return Math.Log(1d + Math.Max(0d, amount) / referenceAmount);
        }

        public static (double DecayedCount, double DecayedAmount) Fold(
            IEnumerable<(DateTimeOffset At, double Amount)> events, double halfLifeDays, double referenceAmount,
            DateTimeOffset asOf)
        {
            ArgumentNullException.ThrowIfNull(events);

            var ordered = events.OrderBy(e => e.At).ToArray();

            if (ordered.Length == 0)
            {
                return (0d, 0d);
            }

            var anchor = ordered[^1].At > asOf ? ordered[^1].At : asOf;
            var decayedCount = 0d;
            var decayedAmount = 0d;

            foreach (var (at, amount) in ordered)
            {
                decayedCount += Decay(1d, at, anchor, halfLifeDays);
                decayedAmount += Decay(LogAmount(amount, referenceAmount), at, anchor, halfLifeDays);
            }

            return (decayedCount, decayedAmount);
        }
    }
}
