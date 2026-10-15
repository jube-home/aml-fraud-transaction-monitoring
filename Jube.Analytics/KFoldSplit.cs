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
    public static class KFoldSplit
    {
        public static SplitFold[] Split(int count, int folds, int seed)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            Validate(count, folds);
            var shuffled = Shuffle(Enumerable.Range(0, count).ToArray(), new Random(seed));
            var tests = new List<int>[folds];

            for (var f = 0; f < folds; f++)
            {
                tests[f] = [];
            }

            for (var i = 0; i < shuffled.Length; i++)
            {
                tests[i % folds].Add(shuffled[i]);
            }

            return Build(tests, count);
        }

        public static SplitFold[] Stratified(double[] labels, int folds, int seed)
        {
            ArgumentNullException.ThrowIfNull(labels);
            Validate(labels.Length, folds);
            var random = new Random(seed);
            var tests = new List<int>[folds];

            for (var f = 0; f < folds; f++)
            {
                tests[f] = [];
            }

            var offset = 0;

            foreach (var group in Enumerable.Range(0, labels.Length).GroupBy(i => labels[i]).OrderBy(g => g.Key))
            {
                foreach (var index in Shuffle(group.ToArray(), random))
                {
                    tests[offset % folds].Add(index);
                    offset++;
                }
            }

            return Build(tests, labels.Length);
        }

        private static void Validate(int count, int folds)
        {
            if (folds < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(folds));
            }

            if (count < folds)
            {
                throw new ArgumentException("There are fewer rows than folds.");
            }
        }

        private static int[] Shuffle(int[] values, Random random)
        {
            for (var i = values.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }

            return values;
        }

        private static SplitFold[] Build(List<int>[] tests, int count)
        {
            var all = Enumerable.Range(0, count).ToArray();

            return tests.Select(test =>
            {
                var testSet = test.OrderBy(i => i).ToArray();
                var held = new HashSet<int>(testSet);

                return new SplitFold(all.Where(i => !held.Contains(i)).ToArray(), testSet);
            }).ToArray();
        }
    }
}