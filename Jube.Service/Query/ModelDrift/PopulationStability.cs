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
using System.Collections.Generic;
using System.Linq;

namespace Jube.Service.Query.ModelDrift
{
    public static class PopulationStability
    {
        public const double ModerateThreshold = 0.1;
        public const double SignificantThreshold = 0.25;
        public const double ShareFloor = 0.0001;

        public static double Compute(IReadOnlyList<double> baseline, IReadOnlyList<double> current, int bins = 10)
        {
            if (baseline.Count == 0 || current.Count == 0)
            {
                throw new ArgumentException("Both windows need at least one score.");
            }

            var cuts = CutPoints(baseline, Math.Max(2, bins));
            var baselineShares = Shares(baseline, cuts);
            var currentShares = Shares(current, cuts);
            var psi = 0d;

            for (var i = 0; i < baselineShares.Length; i++)
            {
                psi += (currentShares[i] - baselineShares[i]) * Math.Log(currentShares[i] / baselineShares[i]);
            }

            return Math.Max(psi, 0d);
        }

        public static short Status(double psi)
        {
            return psi >= SignificantThreshold ? (short)3 : psi >= ModerateThreshold ? (short)2 : (short)1;
        }

        private static double[] CutPoints(IReadOnlyList<double> baseline, int bins)
        {
            var sorted = baseline.OrderBy(v => v).ToArray();
            var cuts = new SortedSet<double>();

            for (var i = 1; i < bins; i++)
            {
                cuts.Add(sorted[Math.Min(sorted.Length - 1, (int)((long)sorted.Length * i / bins))]);
            }

            return [.. cuts];
        }

        private static double[] Shares(IReadOnlyList<double> values, double[] cuts)
        {
            var counts = new double[cuts.Length + 1];

            foreach (var value in values)
            {
                var index = Array.BinarySearch(cuts, value);
                counts[index >= 0 ? index : ~index]++;
            }

            return counts.Select(c => Math.Max(c / values.Count, ShareFloor)).ToArray();
        }
    }
}
