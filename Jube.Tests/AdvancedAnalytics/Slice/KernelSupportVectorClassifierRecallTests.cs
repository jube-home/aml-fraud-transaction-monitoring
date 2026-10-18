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
using FluentAssertions;
using Jube.Engine.AdvancedAnalytics.Classification;
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Classification.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class KernelSupportVectorClassifierRecallTests
    {
        private static (double[][] Inputs, int[] Labels) XorClusters(int perCluster, int seed, double centre = 6d,
            double noise = 0.6)
        {
            var random = new Random(seed);

            var centres = new (double X, double Y, int Label)[]
            {
                (centre, centre, 0),
                (-centre, -centre, 0),
                (centre, -centre, 1),
                (-centre, centre, 1)
            };

            var inputs = new double[perCluster * 4][];
            var labels = new int[perCluster * 4];
            var index = 0;

            foreach (var (x, y, label) in centres)
            {
                for (var i = 0; i < perCluster; i++)
                {
                    inputs[index] = [x + noise * SliceData.Gaussian(random), y + noise * SliceData.Gaussian(random)];
                    labels[index] = label;
                    index++;
                }
            }

            return (inputs, labels);
        }

        private static (double[][] Inputs, int[] Labels) ConcentricCircles(int perClass, int seed)
        {
            var random = new Random(seed);
            var inputs = new double[perClass * 2][];
            var labels = new int[perClass * 2];
            var index = 0;

            for (var i = 0; i < perClass; i++)
            {
                var angle = 2 * Math.PI * random.NextDouble();
                var radius = 1.5 + 0.3 * SliceData.Gaussian(random);
                inputs[index] = [radius * Math.Cos(angle), radius * Math.Sin(angle)];
                labels[index] = 1;
                index++;
            }

            for (var i = 0; i < perClass; i++)
            {
                var angle = 2 * Math.PI * random.NextDouble();
                var radius = 8d + 0.5 * SliceData.Gaussian(random);
                inputs[index] = [radius * Math.Cos(angle), radius * Math.Sin(angle)];
                labels[index] = 0;
                index++;
            }

            return (inputs, labels);
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallKernelSupportVectorClassifier")]
        public void AGaussianKernelSeparatesAnXorPatternThatIsNotLinearlySeparable()
        {
            var (inputs, labels) = XorClusters(120, 11);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions
                    { Kernel = KernelSupportVectorKernel.Gaussian, Seed = 5, Complexity = 2.0 });

            var recall = KernelSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var positive = recall.Predict([6d, -6d])[0];
            var negative = recall.Predict([6d, 6d])[0];

            positive.Should().BeGreaterThan(0.8);
            negative.Should().BeLessThan(0.2);
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.9);
        }

        [Fact]
        [Trait("Covers", "RecallKernelSupportVectorClassifier")]
        public void APolynomialKernelSeparatesAnXorPattern()
        {
            var (inputs, labels) = XorClusters(120, 12);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions
                    { Kernel = KernelSupportVectorKernel.Polynomial, Degree = 2, Seed = 6, Complexity = 2.0 });

            var recall = KernelSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var positive = recall.Predict([6d, -6d])[0];
            var negative = recall.Predict([6d, 6d])[0];

            positive.Should().BeGreaterThan(0.7);
            negative.Should().BeLessThan(0.3);
        }

        [Fact]
        [Trait("Covers", "RecallKernelSupportVectorClassifier")]
        public void ALaplacianKernelSeparatesConcentricCircles()
        {
            var (inputs, labels) = ConcentricCircles(150, 13);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions
                    { Kernel = KernelSupportVectorKernel.Laplacian, Seed = 7, Complexity = 2.0 });

            var recall = KernelSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var inner = recall.Predict([1.5d, 0d])[0];
            var outer = recall.Predict([8d, 0d])[0];

            inner.Should().BeGreaterThan(0.6);
            outer.Should().BeLessThan(0.4);
        }

        [Fact]
        [Trait("Covers", "RecallKernelSupportVectorClassifier")]
        public void ASigmoidKernelFitsAndRecallsWithoutError()
        {
            var (inputs, labels) = XorClusters(120, 14);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions
                {
                    Kernel = KernelSupportVectorKernel.Sigmoid, Gamma = 0.02, Seed = 8, Complexity = 1.0
                });

            var recall = KernelSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var prediction = recall.Predict([6d, -6d]);

            prediction.Should().OnlyContain(v => double.IsFinite(v) && v >= 0d && v <= 1d);
            result.Details.Kernel.Should().Be("Sigmoid");
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var (inputs, labels) = XorClusters(80, 21);
            var names = Names(2);
            var options = new KernelSupportVectorClassifierOptions { Seed = 9 };

            var first = KernelSupportVectorClassifierTrainer.Train(inputs, labels, names, options);
            var second = KernelSupportVectorClassifierTrainer.Train(inputs, labels, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = KernelSupportVectorClassifierRecall.FromSnapshot(first.SnapshotJson);
            var recallB = KernelSupportVectorClassifierRecall.FromSnapshot(second.SnapshotJson);

            foreach (var row in inputs.Take(50))
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
        public void ADifferentSeedCanProduceADifferentSnapshot()
        {
            var (inputs, labels) = XorClusters(80, 33);
            var names = Names(2);

            var a = KernelSupportVectorClassifierTrainer.Train(inputs, labels, names,
                new KernelSupportVectorClassifierOptions { Seed = 1 });
            var b = KernelSupportVectorClassifierTrainer.Train(inputs, labels, names,
                new KernelSupportVectorClassifierOptions { Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var (inputs, labels) = XorClusters(60, 5);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions { Seed = 1 });

            var recall = KernelSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 2*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var (inputs, labels) = XorClusters(80, 6);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions { Seed = 1 });

            var recall = KernelSupportVectorClassifierRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 6d };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v) && v >= 0d && v <= 1d);
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var (inputs, labels) = XorClusters(70, 13);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions { Seed = 4 });

            var envelope =
                AdvancedAnalyticsModelJson
                    .Deserialise<AdvancedAnalyticsKernelSupportVectorClassifierPipelineSnapshot>(
                        result.SnapshotJson, KernelSupportVectorClassifierRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);

            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson)
                .Should().Be("KernelSupportVectorClassifierPipeline");

            envelope.Payload.SupportVectors.Should().HaveCount(result.Details.SupportVectorCount);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var (inputs, labels) = XorClusters(60, 8);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions { Seed = 1 });

            Action wrongKind = () => KernelSupportVectorClassifierRecall.FromSnapshot(
                result.SnapshotJson.Replace("KernelSupportVectorClassifierPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["supportVectors"]!).RemoveAt(0);
            Action shortRows = () => KernelSupportVectorClassifierRecall.FromSnapshot(tampered.ToString());
            shortRows.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => KernelSupportVectorClassifierRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsCalibratedTestMetricsOnWellSeparatedClasses()
        {
            var (inputs, labels) = XorClusters(160, 19);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions
                {
                    Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2, Complexity = 2.0
                });

            result.Details.Kernel.Should().Be("Gaussian");
            result.Details.SupportVectorCount.Should().BeGreaterThan(0);
            result.Details.Calibration.Applied.Should().BeTrue();
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RocAuc.Should().NotBeNull();
            result.Details.TestMetrics.RocAuc!.Value.Should().BeGreaterThan(0.9);
        }

        [Fact]
        public void GammaResolvesAutomaticallyFromTheNumberOfPredictorsWhenLeftAtZero()
        {
            var (inputs, labels) = XorClusters(60, 25);

            var result = KernelSupportVectorClassifierTrainer.Train(inputs, labels, Names(2),
                new KernelSupportVectorClassifierOptions { Seed = 1, Gamma = 0d });

            result.Details.Gamma.Should().BeApproximately(0.5d, 1e-9);
        }
    }
}