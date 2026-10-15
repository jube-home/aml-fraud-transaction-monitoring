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

using Jube.Analytics;
using Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival.Models;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.CategoricalAndSurvival
{
    internal sealed class ScoreSweep
    {
        private ScoreSweep(double[] thresholds, int[] truePositives, int[] falsePositives, int positives, int negatives)
        {
            Thresholds = thresholds;
            TruePositives = truePositives;
            FalsePositives = falsePositives;
            Positives = positives;
            Negatives = negatives;
        }

        public double[] Thresholds { get; }
        public int[] TruePositives { get; }
        public int[] FalsePositives { get; }
        public int Positives { get; }
        public int Negatives { get; }
        public int Total => Positives + Negatives;
        public int Length => Thresholds.Length;

        public static ScoreSweep Build(double[] scores, int[] labels)
        {
            var order = Enumerable.Range(0, scores.Length).OrderByDescending(i => scores[i]).ThenBy(i => i).ToArray();
            var thresholds = new List<double>();
            var tps = new List<int>();
            var fps = new List<int>();
            var tp = 0;
            var fp = 0;
            var index = 0;

            while (index < order.Length)
            {
                var threshold = scores[order[index]];

                while (index < order.Length && Math.Abs(scores[order[index]] - threshold) < 0.0001)
                {
                    if (labels[order[index]] == 1)
                    {
                        tp++;
                    }
                    else
                    {
                        fp++;
                    }

                    index++;
                }

                thresholds.Add(threshold);
                tps.Add(tp);
                fps.Add(fp);
            }

            return new ScoreSweep(thresholds.ToArray(), tps.ToArray(), fps.ToArray(), tp, fp);
        }

        public int FalseNegatives(int i)
        {
            return Positives - TruePositives[i];
        }

        public int TrueNegatives(int i)
        {
            return Negatives - FalsePositives[i];
        }

        public ConfusionMetricsResult Metrics(int i, double beta, double level, double? costFalseNegative,
            double? costFalsePositive)
        {
            return ClassifierMath.Metrics(TruePositives[i], FalsePositives[i], TrueNegatives(i), FalseNegatives(i),
                beta,
                level, Thresholds[i], costFalseNegative, costFalsePositive);
        }

        public int IndexAtOrAbove(double threshold)
        {
            var lo = 0;
            var hi = Thresholds.Length - 1;
            var result = -1;

            while (lo <= hi)
            {
                var mid = (lo + hi) / 2;

                if (Thresholds[mid] >= threshold)
                {
                    result = mid;
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            return result;
        }

        public (int Tp, int Fp) CountsAt(double threshold)
        {
            var i = IndexAtOrAbove(threshold);

            return i < 0 ? (0, 0) : (TruePositives[i], FalsePositives[i]);
        }

        public double F(int i, double beta)
        {
            return new ConfusionMatrix(TruePositives[i], FalsePositives[i], TrueNegatives(i), FalseNegatives(i))
                .FBeta(beta);
        }

        public double Youden(int i)
        {
            return TruePositives[i] / (double)Positives - FalsePositives[i] / (double)Negatives;
        }

        public double Mcc(int i)
        {
            return new ConfusionMatrix(TruePositives[i], FalsePositives[i], TrueNegatives(i), FalseNegatives(i)).Mcc;
        }
    }
}