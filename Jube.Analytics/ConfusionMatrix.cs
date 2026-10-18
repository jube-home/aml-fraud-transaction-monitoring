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

namespace Jube.Analytics
{
    public readonly record struct ConfusionMatrix(
        int TruePositives,
        int FalsePositives,
        int TrueNegatives,
        int FalseNegatives)
    {
        public int Total => TruePositives + FalsePositives + TrueNegatives + FalseNegatives;
        public double Accuracy => Total == 0 ? double.NaN : (TruePositives + TrueNegatives) / (double)Total;
        public double Precision => Ratio(TruePositives, TruePositives + FalsePositives);
        public double Recall => Ratio(TruePositives, TruePositives + FalseNegatives);
        public double Specificity => Ratio(TrueNegatives, TrueNegatives + FalsePositives);
        public double F1 => FBeta(1d);

        public double Mcc
        {
            get
            {
                double tp = TruePositives, fp = FalsePositives, tn = TrueNegatives, fn = FalseNegatives;
                var denominator = Math.Sqrt((tp + fp) * (tp + fn) * (tn + fp) * (tn + fn));

                return denominator == 0 ? 0d : (tp * tn - fp * fn) / denominator;
            }
        }

        public double CohenKappa
        {
            get
            {
                if (Total == 0)
                {
                    return double.NaN;
                }

                double n = Total;
                var observed = (TruePositives + TrueNegatives) / n;

                var expected = ((TruePositives + FalsePositives) * (double)(TruePositives + FalseNegatives) +
                                (TrueNegatives + FalseNegatives) * (double)(TrueNegatives + FalsePositives)) / (n * n);

                return Math.Abs(expected - 1d) < 0.0001 ? 0d : (observed - expected) / (1d - expected);
            }
        }

        public double FBeta(double beta)
        {
            if (double.IsNaN(beta) || beta <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(beta), "Beta must be positive.");
            }

            var b2 = beta * beta;
            var denominator = (1d + b2) * TruePositives + b2 * FalseNegatives + FalsePositives;

            return denominator == 0 ? 0d : (1d + b2) * TruePositives / denominator;
        }

        public double ExpectedCost(CostMatrix costs)
        {
            return Total == 0
                ? double.NaN
                : (TruePositives * costs.TruePositive + FalsePositives * costs.FalsePositive +
                   TrueNegatives * costs.TrueNegative + FalseNegatives * costs.FalseNegative) / Total;
        }

        private static double Ratio(int numerator, int denominator)
        {
            return denominator == 0 ? 0d : numerator / (double)denominator;
        }
    }
}