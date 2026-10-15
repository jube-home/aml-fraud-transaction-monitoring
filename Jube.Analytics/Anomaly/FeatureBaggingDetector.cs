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

using Jube.Analytics.Anomaly.Interfaces;
using Jube.Analytics.Anomaly.Models;

namespace Jube.Analytics.Anomaly
{
    public sealed class FeatureBaggingDetector(
        Func<IAnomalyDetector> factory,
        int members = 20,
        int subspaceSize = 0,
        ScoreCombination combination = ScoreCombination.RankAverage) : AnomalyDetectorBase
    {
        private MemberCalibration[] calibration = [];
        private IAnomalyDetector[] detectors = [];
        private int[][] subspaces = [];
        public override string Name => "FeatureBagging";
        public int Members { get; } = members > 0 ? members : throw new ArgumentOutOfRangeException(nameof(members));
        public int SubspaceSize { get; } = subspaceSize;
        public ScoreCombination Combination { get; } = combination;
        public IReadOnlyList<int[]> Subspaces => subspaces;

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            var d = data[0].Length;
            var rng = new Random(AnomalyInput.DeriveSeed(seed, 31));
            subspaces = new int[Members][];

            for (var m = 0; m < Members; m++)
            {
                var size = SubspaceSize > 0
                    ? Math.Min(SubspaceSize, d)
                    : d <= 2
                        ? d
                        : rng.Next(Math.Max(1, d / 2), d);

                var order = Enumerable.Range(0, d).ToArray();

                for (var i = 0; i < size; i++)
                {
                    var j = i + rng.Next(d - i);
                    (order[i], order[j]) = (order[j], order[i]);
                }

                subspaces[m] = order.Take(size).OrderBy(v => v).ToArray();
            }

            detectors = new IAnomalyDetector[Members];
            calibration = new MemberCalibration[Members];

            Parallel.For(0, Members, AnomalyInput.Options(token), m =>
            {
                var projected = Project(data, subspaces[m]);
                var detector = factory();
                detector.Fit(projected, AnomalyInput.DeriveSeed(seed, 100 + m), token);
                var scores = new double[projected.Length];

                for (var i = 0; i < projected.Length; i++)
                {
                    scores[i] = detector.Score(projected[i]);
                }

                detectors[m] = detector;
                calibration[m] = new MemberCalibration(scores);
            });
        }

        private static double[][] Project(double[][] data, int[] columns)
        {
            var result = new double[data.Length][];

            for (var i = 0; i < data.Length; i++)
            {
                result[i] = new double[columns.Length];

                for (var c = 0; c < columns.Length; c++)
                {
                    result[i][c] = data[i][columns[c]];
                }
            }

            return result;
        }

        protected override double ScoreCore(double[] row)
        {
            var raw = new double[detectors.Length];

            for (var m = 0; m < detectors.Length; m++)
            {
                var projected = new double[subspaces[m].Length];

                for (var c = 0; c < projected.Length; c++)
                {
                    projected[c] = row[subspaces[m][c]];
                }

                raw[m] = detectors[m].Score(projected);
            }

            return Combiner.Combine(Combination, calibration, raw);
        }
    }
}