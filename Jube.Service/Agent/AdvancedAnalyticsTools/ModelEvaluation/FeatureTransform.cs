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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.ModelEvaluation
{
    internal sealed class FeatureTransform
    {
        private readonly bool standardise;

        private FeatureTransform(double[] fill, double[] means, double[] deviations, bool standardise,
            int[] missingCounts, int[] constantColumns)
        {
            FillValues = fill;
            Means = means;
            Deviations = deviations;
            this.standardise = standardise;
            MissingCounts = missingCounts;
            ConstantColumns = constantColumns;
        }

        public int[] MissingCounts { get; }
        public int[] ConstantColumns { get; }
        public double[] Means { get; }

        public double[] Deviations { get; }

        public double[] FillValues { get; }

        public static FeatureTransform Fit(double[][] train, string[] names, bool standardise)
        {
            var p = names.Length;
            var fill = new double[p];
            var means = new double[p];
            var deviations = new double[p];
            var missing = new int[p];
            var constants = new List<int>();

            for (var j = 0; j < p; j++)
            {
                var observed = new List<double>(train.Length);

                foreach (var row in train)
                {
                    if (double.IsFinite(row[j]))
                    {
                        observed.Add(row[j]);
                    }
                }

                missing[j] = train.Length - observed.Count;

                if (observed.Count == 0)
                {
                    throw new ArgumentException($"Variable '{names[j]}' has no observed value in the training split.");
                }

                observed.Sort();
                var middle = observed.Count / 2;
                fill[j] = observed.Count % 2 == 1 ? observed[middle] : (observed[middle - 1] + observed[middle]) / 2d;
                means[j] = observed.Average();

                var variance = observed.Count > 1
                    ? observed.Sum(v => (v - means[j]) * (v - means[j])) / (observed.Count - 1)
                    : 0d;

                deviations[j] = Math.Sqrt(variance);

                if (observed[0] == observed[^1])
                {
                    constants.Add(j);
                }
            }

            return new FeatureTransform(fill, means, deviations, standardise, missing, constants.ToArray());
        }

        public double[] ApplyRow(double[] row)
        {
            var result = new double[row.Length];

            for (var j = 0; j < row.Length; j++)
            {
                var value = double.IsFinite(row[j]) ? row[j] : FillValues[j];

                if (standardise)
                {
                    value = Deviations[j] > 0 ? (value - Means[j]) / Deviations[j] : 0d;
                }

                result[j] = value;
            }

            return result;
        }

        public double[][] Apply(double[][] rows)
        {
            var result = new double[rows.Length][];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = ApplyRow(rows[i]);
            }

            return result;
        }
    }
}