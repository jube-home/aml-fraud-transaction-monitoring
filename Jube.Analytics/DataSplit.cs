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
    public static class DataSplit
    {
        public static DataSplitIndices Split(double[] labels, double trainFraction, double validationFraction,
            double testFraction, int seed, bool stratify = true)
        {
            ArgumentNullException.ThrowIfNull(labels);
            Validate(trainFraction, validationFraction, testFraction);

            var rows = Enumerable.Range(0, labels.Length);

            var groups = stratify
                ? rows.GroupBy(i => labels[i]).OrderBy(g => g.Key).Select(g => g.ToArray()).ToList()
                : [rows.ToArray()];

            var random = new Random(seed);
            var train = new List<int>();
            var validation = new List<int>();
            var test = new List<int>();

            foreach (var group in groups)
            {
                var shuffled = group.ToArray();

                for (var i = shuffled.Length - 1; i > 0; i--)
                {
                    var j = random.Next(i + 1);
                    (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
                }

                var trainCount = (int)Math.Floor(shuffled.Length * trainFraction);
                var validationCount = (int)Math.Floor(shuffled.Length * validationFraction);
                var testCount = (int)Math.Floor(shuffled.Length * testFraction);

                while (trainCount + validationCount + testCount > shuffled.Length)
                {
                    if (testCount > 0)
                    {
                        testCount--;
                    }
                    else if (validationCount > 0)
                    {
                        validationCount--;
                    }
                    else
                    {
                        trainCount--;
                    }
                }

                train.AddRange(shuffled.Take(trainCount));
                validation.AddRange(shuffled.Skip(trainCount).Take(validationCount));
                test.AddRange(shuffled.Skip(trainCount + validationCount).Take(testCount));
            }

            return new DataSplitIndices(Shuffle(train, random), Shuffle(validation, random), Shuffle(test, random));
        }

        public static T[] Take<T>(T[] source, int[] indices)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(indices);
            var result = new T[indices.Length];

            for (var i = 0; i < indices.Length; i++)
            {
                result[i] = source[indices[i]];
            }

            return result;
        }

        private static int[] Shuffle(List<int> values, Random random)
        {
            var array = values.ToArray();

            for (var i = array.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }

            return array;
        }

        private static void Validate(double train, double validation, double test)
        {
            foreach (var fraction in new[] { train, validation, test })
            {
                if (double.IsNaN(fraction) || fraction < 0 || fraction > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(train), "Fractions must be between 0 and 1.");
                }
            }

            if (train + validation + test > 1 + 1e-9)
            {
                throw new ArgumentException("Split fractions must not sum to more than 1.");
            }
        }
    }
}