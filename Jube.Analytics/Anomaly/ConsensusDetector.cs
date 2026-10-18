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
    public sealed class ConsensusDetector(
        IReadOnlyList<Func<IAnomalyDetector>> factories,
        ScoreCombination combination = ScoreCombination.RankAverage) : AnomalyDetectorBase
    {
        private MemberCalibration[] calibration = [];
        private IAnomalyDetector[] detectors = [];
        public override string Name => "Consensus";
        public IReadOnlyList<IAnomalyDetector> Members => detectors;

        protected override void FitCore(double[][] data, int seed, CancellationToken token)
        {
            if (factories.Count == 0)
            {
                throw new ArgumentException("A consensus needs at least one detector.");
            }

            detectors = new IAnomalyDetector[factories.Count];
            calibration = new MemberCalibration[factories.Count];

            for (var m = 0; m < factories.Count; m++)
            {
                token.ThrowIfCancellationRequested();
                var detector = factories[m]();
                detector.Fit(data, AnomalyInput.DeriveSeed(seed, 200 + m), token);
                var scores = AnomalyInput.ScoreAll(detector, data, token);
                detectors[m] = detector;
                calibration[m] = new MemberCalibration(scores);
            }
        }

        protected override double ScoreCore(double[] row)
        {
            var raw = new double[detectors.Length];

            for (var m = 0; m < detectors.Length; m++)
            {
                raw[m] = detectors[m].Score(row);
            }

            return Combiner.Combine(combination, calibration, raw);
        }
    }
}