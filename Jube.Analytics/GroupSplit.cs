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

using Jube.Analytics.Models;

namespace Jube.Analytics
{
    public static class GroupSplit
    {
        public static SplitFold[] KFold<T>(T[] groups, int folds, int seed) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(groups);

            if (folds < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(folds));
            }

            var members = Members(groups, seed);

            if (members.Count < folds)
            {
                throw new ArgumentException("There are fewer groups than folds.");
            }

            var assignment = new List<int>[folds];
            var sizes = new int[folds];

            for (var f = 0; f < folds; f++)
            {
                assignment[f] = [];
            }

            foreach (var group in members.OrderByDescending(g => g.Length))
            {
                var target = 0;

                for (var f = 1; f < folds; f++)
                {
                    if (sizes[f] < sizes[target])
                    {
                        target = f;
                    }
                }

                assignment[target].AddRange(group);
                sizes[target] += group.Length;
            }

            var all = Enumerable.Range(0, groups.Length).ToArray();

            return assignment.Select(test =>
            {
                var testSet = test.OrderBy(i => i).ToArray();
                var held = new HashSet<int>(testSet);

                return new SplitFold(all.Where(i => !held.Contains(i)).ToArray(), testSet);
            }).ToArray();
        }

        public static DataSplitIndices Split<T>(T[] groups, double trainFraction, double validationFraction,
            double testFraction, int seed) where T : notnull
        {
            ArgumentNullException.ThrowIfNull(groups);
            var fractions = new[] { trainFraction, validationFraction, testFraction };

            if (fractions.Any(f => double.IsNaN(f) || f < 0 || f > 1) || fractions.Sum() > 1 + 1e-9)
            {
                throw new ArgumentException("Fractions must be between 0 and 1 and sum to at most 1.");
            }

            var total = fractions.Sum();

            if (total <= 0)
            {
                throw new ArgumentException("At least one fraction must be positive.");
            }

            var members = Members(groups, seed);
            var targets = fractions.Select(f => f / total * groups.Length).ToArray();
            var sets = new List<int>[3];
            var sizes = new double[3];

            for (var s = 0; s < 3; s++)
            {
                sets[s] = [];
            }

            foreach (var group in members.OrderByDescending(g => g.Length))
            {
                var best = -1;
                var bestDeficit = double.NegativeInfinity;

                for (var s = 0; s < 3; s++)
                {
                    if (targets[s] <= 0)
                    {
                        continue;
                    }

                    var deficit = (targets[s] - sizes[s]) / targets[s];

                    if (deficit > bestDeficit)
                    {
                        bestDeficit = deficit;
                        best = s;
                    }
                }

                sets[best].AddRange(group);
                sizes[best] += group.Length;
            }

            return new DataSplitIndices(sets[0].OrderBy(i => i).ToArray(), sets[1].OrderBy(i => i).ToArray(),
                sets[2].OrderBy(i => i).ToArray());
        }

        private static List<int[]> Members<T>(T[] groups, int seed) where T : notnull
        {
            var byGroup = new Dictionary<T, List<int>>();
            var firstSeen = new List<T>();

            for (var i = 0; i < groups.Length; i++)
            {
                if (!byGroup.TryGetValue(groups[i], out var list))
                {
                    list = [];
                    byGroup[groups[i]] = list;
                    firstSeen.Add(groups[i]);
                }

                list.Add(i);
            }

            var order = firstSeen.ToArray();
            var random = new Random(seed);

            for (var i = order.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            return order.Select(g => byGroup[g].ToArray()).ToList();
        }
    }
}