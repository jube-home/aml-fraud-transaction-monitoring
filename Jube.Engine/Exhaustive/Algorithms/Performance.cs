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
using Accord.Neuro;
using Accord.Statistics;

namespace Jube.Engine.Exhaustive.Algorithms
{
    public class Performance
    {
        private double PercentageCorrect { get; set; }
        private double Correlation { get; set; }
        public double Score { get; private set; }
        public double[] Scores { get; private set; }
        public int Tp { get; private set; }
        public int Fp { get; private set; }
        public int Tn { get; private set; }
        public int Fn { get; private set; }

        public double[] CalculateScores(ActivationNetwork topologyNetwork, double[][] data)
        {
            return CalculateScores(topologyNetwork.Compute, data);
        }

        public double[] CalculateScores(Func<double[], double[]> recall, double[][] data)
        {
            int i;
            Scores = new double[data.Length - 1 + 1];

            for (i = 0; i <= data.Length - 1; i++)
            {
                var output = recall(data[i]);
                Scores[i] = output[0];
            }

            return Scores;
        }

        public double CalculatePerformance(double[] data, double[][] dependent, double threshold)
        {
            ResetPerformance();

            var correct = new int[data.Length - 1 + 1];
            var errorsPredictedActual = new double[data.Length - 1 + 1];
            var predicted = new double[data.Length - 1 + 1];
            var actual = new double[data.Length - 1 + 1];

            for (var i = 0; i <= data.Length - 1; i++)
            {
                errorsPredictedActual[i] = Math.Abs(Math.Pow(data[i] - dependent[i][0], 2));

                var predictedValue = data[i];
                var actualValue = dependent[i][0];
                predictedValue = predictedValue > threshold ? 1 : 0;
                predicted[i] = predictedValue;
                actual[i] = actualValue;

                if (Math.Abs(predictedValue - dependent[i][0]) < 0.0001)
                {
                    correct[i] = 1;
                }
                else
                {
                    correct[i] = 0;
                }

                if ((Math.Abs(actualValue - 1) < 0.0001) & (Math.Abs(predictedValue - 1) < 0.0001))
                {
                    Tp += 1;
                }

                if ((actualValue == 0) & (predictedValue == 0))
                {
                    Tn += 1;
                }

                if ((Math.Abs(predictedValue - 1) < 0.0001) & (actualValue == 0))
                {
                    Fp += 1;
                }

                if ((predictedValue == 0) & (Math.Abs(actualValue - 1) < 0.0001))
                {
                    Fn += 1;
                }
            }

            errorsPredictedActual.Mean();
            PercentageCorrect = correct.Sum() / (double)correct.Length;
            Correlation = SpearmansCoeff(predicted, actual);

            if (double.IsNaN(Correlation))
            {
                Correlation = 0;
            }

            Score = (PercentageCorrect + Correlation) / 2d;

            return Score;
        }

        public static List<RocPoint> RocPoints(double[] scores, double[][] dependent)
        {
            const double minPrediction = 0d;
            const double maxPrediction = 1d;
            const double rocStep = (maxPrediction - minPrediction) / 20d;
            var rocStepThreshold = 0.05d;
            var calculator = new Performance();
            var points = new List<RocPoint>();

            for (var j = 0; j < 20; j++)
            {
                calculator.CalculatePerformance(scores, dependent, rocStepThreshold);

                points.Add(new RocPoint(rocStepThreshold, calculator.Score, calculator.Tp, calculator.Fp,
                    calculator.Tn, calculator.Fn));

                rocStepThreshold += rocStep;
            }

            return points;
        }

        private void ResetPerformance()
        {
            PercentageCorrect = 0;
            Correlation = 0;
            Score = 0;
            Tp = 0;
            Fp = 0;
            Tn = 0;
            Fn = 0;
        }

        public static double SpearmansCoeff(IEnumerable<double> current, IEnumerable<double> other)
        {
            var enumerable = current as double[] ?? [.. current];
            var doubles = other as double[] ?? [.. other];

            if (enumerable.Length != doubles.Length)
            {
                throw new ArgumentException("Both collections of data must contain an equal number of elements");
            }

            var n = enumerable.Length;

            if (n < 2)
            {
                return double.NaN;
            }

            var ranksX = GetRanking(enumerable);
            var ranksY = GetRanking(doubles);

            var meanX = ranksX.Average();
            var meanY = ranksY.Average();

            double covariance = 0;
            double varianceX = 0;
            double varianceY = 0;

            for (var i = 0; i < n; i++)
            {
                var dx = ranksX[i] - meanX;
                var dy = ranksY[i] - meanY;
                covariance += dx * dy;
                varianceX += dx * dx;
                varianceY += dy * dy;
            }

            if (varianceX == 0 || varianceY == 0)
            {
                return 0;
            }

            return covariance / Math.Sqrt(varianceX * varianceY);
        }

        private static double[] GetRanking(IEnumerable<double> values)
        {
            var enumerable = values as double[] ?? [.. values];

            var groupedValues = enumerable.OrderByDescending(n => n)
                .Select((val, i) => new
                {
                    Value = val,
                    IndexedRank = i + 1
                })
                .GroupBy(i => i.Value);

            var rankings = (from n in enumerable
                join grp in groupedValues on n equals grp.Key
                select grp.Average(g => g.IndexedRank)).ToArray();

            return rankings;
        }

        public static double[] Sensitivity(Random seeded, ActivationNetwork model, double[][] datasetInputs, int test,
            double[] compareValues)
        {
            var deepCopyDatasetInputs = (double[][])datasetInputs.DeepMemberwiseClone();

            for (int i = 0, loopTo = deepCopyDatasetInputs.Length - 1; i <= loopTo; i++)
            {
                var output = deepCopyDatasetInputs[i][test] * GetRandomNumber(seeded, 0.8d, 1.2d);
                deepCopyDatasetInputs[i][test] = output;
            }

            var outputValues = new double[deepCopyDatasetInputs.Length];

            for (int i = 0, loopTo1 = deepCopyDatasetInputs.Length - 1; i <= loopTo1; i++)
            {
                var output = model.Compute(deepCopyDatasetInputs[i]);
                outputValues[i] = Math.Abs(compareValues[i] - output[0]);
            }

            return outputValues;
        }

        private static double GetRandomNumber(Random seeded, double minimum, double maximum)
        {
            return seeded.NextDouble() * (maximum - minimum) + minimum;
        }
    }
}