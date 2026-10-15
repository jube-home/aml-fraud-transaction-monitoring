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
using Jube.Analytics;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Engine.AdvancedAnalytics.Classification
{
    internal sealed class TreeAugmentedNaiveBayesClassifierPipeline(
        double[] fill,
        int[] parents,
        double[] intercept1,
        double[] slope1,
        double[] variance1,
        double[] intercept0,
        double[] slope0,
        double[] variance0,
        double priorLogOdds,
        Calibrator? calibrator)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double[] intercept0 = (double[])intercept0.Clone();
        private readonly double[] intercept1 = (double[])intercept1.Clone();
        private readonly int[] parents = (int[])parents.Clone();
        private readonly double[] slope0 = (double[])slope0.Clone();
        private readonly double[] slope1 = (double[])slope1.Clone();
        private readonly double[] variance0 = (double[])variance0.Clone();
        private readonly double[] variance1 = (double[])variance1.Clone();

        public int InputCount => fill.Length;
        public double[] Fill => (double[])fill.Clone();
        public int[] Parents => (int[])parents.Clone();
        public double[] Intercept1 => (double[])intercept1.Clone();
        public double[] Slope1 => (double[])slope1.Clone();
        public double[] Variance1 => (double[])variance1.Clone();
        public double[] Intercept0 => (double[])intercept0.Clone();
        public double[] Slope0 => (double[])slope0.Clone();
        public double[] Variance0 => (double[])variance0.Clone();
        public double PriorLogOdds { get; } = priorLogOdds;
        public Calibrator? Calibrator { get; } = calibrator;

        public TreeAugmentedNaiveBayesRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var filled = new double[count];

            for (var j = 0; j < count; j++)
            {
                filled[j] = double.IsFinite(row[j]) ? row[j] : fill[j];
            }

            var contributions = new double[count];
            var sum = PriorLogOdds;

            for (var j = 0; j < count; j++)
            {
                var xj = filled[j];
                var parent = parents[j];
                var parentValue = parent >= 0 ? filled[parent] : 0d;

                var mean1 = intercept1[j] + (parent >= 0 ? slope1[j] * parentValue : 0d);
                var mean0 = intercept0[j] + (parent >= 0 ? slope0[j] * parentValue : 0d);

                var contribution = LogGaussianPdf(xj, mean1, variance1[j]) -
                                   LogGaussianPdf(xj, mean0, variance0[j]);

                contributions[j] = contribution;
                sum += contribution;
            }

            return new TreeAugmentedNaiveBayesRawExplain(sum, PriorLogOdds, contributions);
        }

        public double RawScore(double[] row)
        {
            return Explain(row).RawLogOdds;
        }

        public double Probability(double[] row)
        {
            var raw = RawScore(row);

            return Calibrator is { } calibrator ? calibrator.Apply(raw) : Sigmoid(raw);
        }

        public double[] Probabilities(double[][] rows)
        {
            var result = new double[rows.Length];

            for (var i = 0; i < rows.Length; i++)
            {
                result[i] = Probability(rows[i]);
            }

            return result;
        }

        public static double Sigmoid(double z)
        {
            if (z >= 0)
            {
                return 1d / (1d + Math.Exp(-z));
            }

            var e = Math.Exp(z);

            return e / (1d + e);
        }

        private static double LogGaussianPdf(double x, double mean, double variance)
        {
            var d = x - mean;

            return -0.5 * (Math.Log(2d * Math.PI * variance) + d * d / variance);
        }
    }
}