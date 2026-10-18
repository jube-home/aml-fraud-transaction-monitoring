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
    internal sealed class NaiveBayesClassifierPipeline(
        double[] fill,
        double[] mean1,
        double[] variance1,
        double[] mean0,
        double[] variance0,
        double priorLogOdds,
        Calibrator? calibrator)
    {
        private readonly double[] fill = (double[])fill.Clone();
        private readonly double[] mean0 = (double[])mean0.Clone();
        private readonly double[] mean1 = (double[])mean1.Clone();
        private readonly double[] variance0 = (double[])variance0.Clone();
        private readonly double[] variance1 = (double[])variance1.Clone();

        public int InputCount => fill.Length;
        public double[] Fill => (double[])fill.Clone();
        public double[] Mean1 => (double[])mean1.Clone();
        public double[] Variance1 => (double[])variance1.Clone();
        public double[] Mean0 => (double[])mean0.Clone();
        public double[] Variance0 => (double[])variance0.Clone();
        public double PriorLogOdds { get; } = priorLogOdds;
        public Calibrator? Calibrator { get; } = calibrator;

        public NaiveBayesRawExplain Explain(double[] row)
        {
            var count = fill.Length;

            if (row.Length != count)
            {
                throw new ArgumentException($"A row has {row.Length} values but the model expects {count}.");
            }

            var contributions = new double[count];
            var sum = PriorLogOdds;

            for (var j = 0; j < count; j++)
            {
                var value = double.IsFinite(row[j]) ? row[j] : fill[j];

                var contribution = LogGaussianPdf(value, mean1[j], variance1[j]) -
                                   LogGaussianPdf(value, mean0[j], variance0[j]);

                contributions[j] = contribution;
                sum += contribution;
            }

            return new NaiveBayesRawExplain(sum, PriorLogOdds, contributions);
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