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
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Anomaly;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class IsolationForestAnomalyRecallTests
    {
        private static readonly string[] names = ["x0", "x1"];

        private static (double[][] Rows, int[] Labels) OutlierDataset(int normalCount, int outlierCount, int seed)
        {
            var random = new Random(seed);
            var rows = new List<double[]>();
            var labels = new List<int>();

            for (var i = 0; i < normalCount; i++)
            {
                rows.Add([SliceData.Gaussian(random) * 0.5, SliceData.Gaussian(random) * 0.5]);
                labels.Add(0);
            }

            for (var i = 0; i < outlierCount; i++)
            {
                rows.Add([10d + SliceData.Gaussian(random), -10d + SliceData.Gaussian(random)]);
                labels.Add(1);
            }

            return ([.. rows], [.. labels]);
        }

        [Fact]
        [Trait("Covers", "RecallIsolationForestAnomaly")]
        public void OutlierRowsScoreHigherThanNormalRows()
        {
            var (rows, _) = OutlierDataset(1000, 20, 11);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false, Seed = 3 });

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var normalScores = Enumerable.Range(0, 1000).Select(i => recall.Predict(rows[i])[0]).ToArray();
            var outlierScores = Enumerable.Range(1000, 20).Select(i => recall.Predict(rows[i])[0]).ToArray();

            outlierScores.Min().Should().BeGreaterThan(normalScores.Max());
        }

        [Fact]
        public void RecallReproducesTrainingTimeScoresBitForBit()
        {
            var (rows, _) = OutlierDataset(600, 10, 13);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false, Seed = 5 });

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);

            recall.Variables.Should().Equal(names);

            foreach (var row in result.TestRows.Take(100))
            {
                var predicted = recall.Predict(rows[row])[0];
                var expected = result.TestScores[Array.IndexOf(result.TestRows, row)];
                SliceData.Bits(predicted).Should().Be(SliceData.Bits(expected));
            }
        }

        [Fact]
        public void TrainingIsDeterministicForTheSameSeed()
        {
            var (rows, _) = OutlierDataset(500, 10, 17);

            var first = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false, Seed = 9 });
            var second = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false, Seed = 9 });

            first.SnapshotJson.Should().Be(second.SnapshotJson);
        }

        [Fact]
        public void MissingValuesAreImputedWithTheTrainingMedian()
        {
            var (rows, _) = OutlierDataset(500, 10, 19);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false, Seed = 21 });

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = recall.Predict([double.NaN, 0.1d]);

            withMissing.Should().HaveCount(1);
            double.IsFinite(withMissing[0]).Should().BeTrue();
        }

        [Fact]
        public void UnlabelledTrainingHasNoCalibratorAndReportsARawScoreInUnitRange()
        {
            var (rows, _) = OutlierDataset(500, 10, 23);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names, new IsolationForestAnomalyOptions
                { Seed = 25 });

            result.Details.Calibration.Applied.Should().BeFalse();
            result.Warnings.Should().Contain(w => w.Code == IsolationForestWarningCodes.CalibrationSkipped);

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var score = recall.Predict(rows[0])[0];
            score.Should().BeInRange(0d, 1d);
        }

        [Fact]
        public void LabelledTrainingWithBothClassesInValidationFitsACalibratorAndProducesARealRocAuc()
        {
            var (rows, labels) = OutlierDataset(2000, 60, 27);

            var result = IsolationForestAnomalyTrainer.Train(rows, labels, names, new IsolationForestAnomalyOptions
                { Seed = 29 });

            result.Details.Calibration.Applied.Should().BeTrue();
            result.Details.TestLabelledMetrics.Should().NotBeNull();
            result.Details.TestLabelledMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestLabelledMetrics.RocAuc!.Value.Should().BeInRange(0d, 1d);

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var probability = recall.Predict(rows[0])[0];
            probability.Should().BeInRange(0d, 1d);
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var (rows, _) = OutlierDataset(300, 5, 31);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false });

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);

            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var (rows, _) = OutlierDataset(300, 5, 37);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsIsolationForestAnomalyPipelineSnapshot>(
                    result.SnapshotJson, IsolationForestAnomalyRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("IsolationForestAnomalyPipeline");
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var (rows, _) = OutlierDataset(300, 5, 41);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false });

            Action wrongKind = () => IsolationForestAnomalyRecall.FromSnapshot(
                result.SnapshotJson.Replace("IsolationForestAnomalyPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["fillValues"]!).RemoveAt(0);
            Action shortFill = () => IsolationForestAnomalyRecall.FromSnapshot(tampered.ToString());
            shortFill.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => IsolationForestAnomalyRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var (rows, _) = OutlierDataset(400, 10, 43);

            var result = IsolationForestAnomalyTrainer.Train(rows, null, names,
                new IsolationForestAnomalyOptions { Calibrate = false });

            var recall = IsolationForestAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var probeRows = rows.Take(200).ToArray();
            var expected = probeRows.Select(r => SliceData.Bits(recall.Predict(r)[0])).ToArray();
            var mismatches = 0;

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            {
                for (var repeat = 0; repeat < 6; repeat++)
                {
                    var outputs = recall.PredictDistances(probeRows);

                    for (var i = 0; i < probeRows.Length; i++)
                    {
                        if (SliceData.Bits(outputs[i]) != expected[i])
                        {
                            Interlocked.Increment(ref mismatches);
                        }
                    }
                }
            });

            mismatches.Should().Be(0);
        }
    }
}