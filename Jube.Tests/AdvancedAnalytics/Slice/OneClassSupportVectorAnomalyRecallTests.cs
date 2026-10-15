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
    public class OneClassSupportVectorAnomalyRecallTests
    {
        private static double[][] NormalCluster(int n, int seed, int dimensions = 2, double spread = 1d)
        {
            var random = new Random(seed);
            var rows = new double[n][];

            for (var i = 0; i < n; i++)
            {
                var row = new double[dimensions];

                for (var j = 0; j < dimensions; j++)
                {
                    row[j] = spread * SliceData.Gaussian(random);
                }

                rows[i] = row;
            }

            return rows;
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallOneClassSupportVectorAnomaly")]
        public void FarOutliersScoreHigherThanTheNormalClusterTheyWereFittedOn()
        {
            var normal = NormalCluster(600, 1);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Nu = 0.1, Calibrate = false });

            var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var normalScore = recall.Predict([0.1d, -0.1d])[0];
            var outlierScore = recall.Predict([25d, 25d])[0];

            outlierScore.Should().BeGreaterThan(normalScore);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var normal = NormalCluster(300, 2);
            var names = Names(2);
            var options = new OneClassSupportVectorAnomalyOptions { Seed = 3, Calibrate = false };

            var first = OneClassSupportVectorAnomalyTrainer.Train(normal, null, names, options);
            var second = OneClassSupportVectorAnomalyTrainer.Train(normal, null, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = OneClassSupportVectorAnomalyRecall.FromSnapshot(first.SnapshotJson);
            var recallB = OneClassSupportVectorAnomalyRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in normal.Take(50))
            {
                var a = recallA.Predict(row);
                var b = recallB.Predict(row);

                for (var i = 0; i < a.Length; i++)
                {
                    SliceData.Bits(a[i]).Should().Be(SliceData.Bits(b[i]));
                }
            }
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var normal = NormalCluster(200, 4);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Calibrate = false });

            var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);

            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var normal = NormalCluster(200, 5);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Calibrate = false });

            var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 0.2d };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var normal = NormalCluster(200, 6);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Calibrate = false });

            var envelope =
                AdvancedAnalyticsModelJson.Deserialise<AdvancedAnalyticsOneClassSupportVectorAnomalyPipelineSnapshot>(
                    result.SnapshotJson, OneClassSupportVectorAnomalyRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);

            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson)
                .Should().Be("OneClassSupportVectorAnomalyPipeline");

            envelope.Payload.SupportVectors.Should().HaveCount(result.Details.SupportVectorCount);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var normal = NormalCluster(200, 7);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Calibrate = false });

            Action wrongKind = () => OneClassSupportVectorAnomalyRecall.FromSnapshot(
                result.SnapshotJson.Replace("OneClassSupportVectorAnomalyPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["supportVectors"]!).RemoveAt(0);
            Action shortRows = () => OneClassSupportVectorAnomalyRecall.FromSnapshot(tampered.ToString());
            shortRows.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => OneClassSupportVectorAnomalyRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void UnlabelledTrainingFitsWithNoCalibratorAndReportsTheRawScore()
        {
            var normal = NormalCluster(400, 8);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1 });

            result.Details.Calibration.Applied.Should().BeFalse();
            result.Warnings.Should().Contain(w => w.Code == OneClassSupportVectorWarningCodes.CalibrationSkipped);

            var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var rawScore = recall.Predict([0.1d, 0.1d])[0];
            double.IsFinite(rawScore).Should().BeTrue();
        }

        [Fact]
        public void LabelledTrainingFitsACalibratorAndProducesARealRocAuc()
        {
            var random = new Random(9);
            var n = 3000;
            var rows = new double[n][];
            var labels = new int[n];

            for (var i = 0; i < n; i++)
            {
                var anomaly = i % 40 == 0;
                var centre = anomaly ? 20d : 0d;
                rows[i] = [centre + SliceData.Gaussian(random), centre + SliceData.Gaussian(random)];
                labels[i] = anomaly ? 1 : 0;
            }

            var result = OneClassSupportVectorAnomalyTrainer.Train(rows, labels, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 9, Nu = 0.05 });

            result.Details.Calibration.Applied.Should().BeTrue();
            result.Details.TestLabelledMetrics.Should().NotBeNull();
            result.Details.TestLabelledMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestLabelledMetrics.RocAuc!.Value.Should().BeGreaterThan(0.85);

            var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var probability = recall.Predict([0.1d, 0.1d])[0];
            probability.Should().BeInRange(0d, 1d);
        }

        [Fact]
        public void NuRoughlyControlsHowManyTrainingRowsBecomeSupportVectors()
        {
            var normal = NormalCluster(600, 12);

            var tight = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Nu = 0.05, Calibrate = false });
            var loose = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Nu = 0.5, Calibrate = false });

            loose.Details.SupportVectorCount.Should().BeGreaterThan(tight.Details.SupportVectorCount);
        }

        [Fact]
        public void TheEvidenceReportsPlausibleScorePercentilesAndSupportVectorCount()
        {
            var normal = NormalCluster(500, 13);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 2, Nu = 0.1, Calibrate = false });

            result.Details.SupportVectorCount.Should().BeGreaterThan(0);
            result.Details.Gamma.Should().BeApproximately(0.5d, 1e-9);
            result.Details.TrainScores.Should().NotBeNull();
            result.Details.TestScores.Should().NotBeNull();
            result.Details.TrainScores!.Max.Should().BeGreaterThanOrEqualTo(result.Details.TrainScores.P99);
            result.Details.TestScores!.P99.Should().BeGreaterThanOrEqualTo(result.Details.TestScores.P50);
        }

        [Fact]
        public void RecallIsThreadSafeAndDeterministicUnderContention()
        {
            var normal = NormalCluster(300, 14);

            var result = OneClassSupportVectorAnomalyTrainer.Train(normal, null, Names(2),
                new OneClassSupportVectorAnomalyOptions { Seed = 1, Calibrate = false });

            var recall = OneClassSupportVectorAnomalyRecall.FromSnapshot(result.SnapshotJson);
            var probeRows = normal.Take(100).ToArray();
            var expected = probeRows.Select(r => SliceData.Bits(recall.Predict(r)[0])).ToArray();
            var mismatches = 0;

            Parallel.For(0, 32, new ParallelOptions { MaxDegreeOfParallelism = 16 }, _ =>
            {
                for (var repeat = 0; repeat < 6; repeat++)
                {
                    var outputs = recall.PredictScores(probeRows);

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