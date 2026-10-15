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
using Jube.Engine.AdvancedAnalytics.Models;
using Jube.Engine.AdvancedAnalytics.Regression;
using Jube.Engine.AdvancedAnalytics.Serialisation;
using Jube.Engine.AdvancedAnalytics.Serialisation.Exceptions;
using Newtonsoft.Json.Linq;
using Xunit;
using Jube.Engine.AdvancedAnalytics.Regression.Models;

namespace Jube.Test.AdvancedAnalytics.Slice
{
    public class KernelRidgeRegressorRecallTests
    {
        private static (double[][] Inputs, double[] Outputs) Sinusoid(int n, int seed, double noise = 0.05)
        {
            var random = new Random(seed);
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var x = -2d * Math.PI + 4d * Math.PI * random.NextDouble();
                inputs[i] = [x];
                outputs[i] = Math.Sin(x) + noise * SliceData.Gaussian(random);
            }

            return (inputs, outputs);
        }

        private static (double[][] Inputs, double[] Outputs) QuadraticSurface(int n, int seed, double noise = 0.1)
        {
            var random = new Random(seed);
            var inputs = new double[n][];
            var outputs = new double[n];

            for (var i = 0; i < n; i++)
            {
                var x0 = -2d + 4d * random.NextDouble();
                var x1 = -2d + 4d * random.NextDouble();
                inputs[i] = [x0, x1];
                outputs[i] = x0 * x0 + x1 * x1 + noise * SliceData.Gaussian(random);
            }

            return (inputs, outputs);
        }

        private static string[] Names(int count)
        {
            return [.. Enumerable.Range(0, count).Select(j => "x" + j)];
        }

        [Fact]
        [Trait("Covers", "RecallKernelRidgeRegressor")]
        public void AGaussianKernelFitsASinusoidThatALinearKernelCannot()
        {
            var (inputs, outputs) = Sinusoid(500, 41);
            var names = Names(1);

            var gaussian = KernelRidgeRegressorTrainer.Train(inputs, outputs, names,
                new KernelRidgeRegressorOptions { Kernel = KernelRidgeRegressorKernel.Gaussian, Seed = 5 });
            var linear = KernelRidgeRegressorTrainer.Train(inputs, outputs, names,
                new KernelRidgeRegressorOptions { Kernel = KernelRidgeRegressorKernel.Linear, Seed = 5 });

            gaussian.Details.TestMetrics.Should().NotBeNull();
            gaussian.Details.TestMetrics!.RSquared.Should().NotBeNull();
            gaussian.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.9);

            linear.Details.TestMetrics.Should().NotBeNull();
            linear.Details.TestMetrics!.RSquared.Should().NotBeNull();
            linear.Details.TestMetrics.RSquared!.Value.Should().BeLessThan(0.5);
        }

        [Fact]
        [Trait("Covers", "RecallKernelRidgeRegressor")]
        public void APolynomialKernelFitsAQuadraticSurface()
        {
            var (inputs, outputs) = QuadraticSurface(400, 42);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(2),
                new KernelRidgeRegressorOptions
                    { Kernel = KernelRidgeRegressorKernel.Polynomial, Degree = 2, Seed = 6, Complexity = 5.0 });

            var recall = KernelRidgeRegressorRecall.FromSnapshot(result.SnapshotJson);

            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.9);

            var predicted = recall.Predict([1d, 1d])[0];
            predicted.Should().BeApproximately(2d, 0.6);
        }

        [Fact]
        public void SameSeedProducesTheSameSnapshotAndTheSameRecallBitForBit()
        {
            var (inputs, outputs) = Sinusoid(200, 21);
            var names = Names(1);
            var options = new KernelRidgeRegressorOptions { Seed = 9 };

            var first = KernelRidgeRegressorTrainer.Train(inputs, outputs, names, options);
            var second = KernelRidgeRegressorTrainer.Train(inputs, outputs, names, options);

            first.SnapshotJson.Should().Be(second.SnapshotJson);

            var recallA = KernelRidgeRegressorRecall.FromSnapshot(first.SnapshotJson);
            var recallB = KernelRidgeRegressorRecall.FromSnapshot(second.SnapshotJson);

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
            var (inputs, outputs) = Sinusoid(200, 33);
            var names = Names(1);

            var a = KernelRidgeRegressorTrainer.Train(inputs, outputs, names,
                new KernelRidgeRegressorOptions { Seed = 1 });
            var b = KernelRidgeRegressorTrainer.Train(inputs, outputs, names,
                new KernelRidgeRegressorOptions { Seed = 2 });

            (a.SnapshotJson == b.SnapshotJson).Should().BeFalse();
        }

        [Fact]
        public void RecallRejectsARowOfTheWrongLength()
        {
            var (inputs, outputs) = Sinusoid(150, 5);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(1),
                new KernelRidgeRegressorOptions { Seed = 1 });

            var recall = KernelRidgeRegressorRecall.FromSnapshot(result.SnapshotJson);

            Action act = () => recall.Predict([1d, 2d]);
            act.Should().Throw<ArgumentException>().WithMessage("*expects 1*");
        }

        [Fact]
        public void AMissingInputIsImputedWithTheTrainingMedian()
        {
            var (inputs, outputs) = QuadraticSurface(200, 6);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(2),
                new KernelRidgeRegressorOptions { Seed = 1 });

            var recall = KernelRidgeRegressorRecall.FromSnapshot(result.SnapshotJson);

            var withMissing = new[] { double.NaN, 1d };
            var prediction = recall.Predict(withMissing);

            prediction.Should().OnlyContain(v => double.IsFinite(v));
        }

        [Fact]
        public void TheSnapshotRoundTripsThroughTheEnvelopeUnchanged()
        {
            var (inputs, outputs) = Sinusoid(150, 13);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(1),
                new KernelRidgeRegressorOptions { Seed = 4 });

            var envelope =
                AdvancedAnalyticsModelJson
                    .Deserialise<AdvancedAnalyticsKernelRidgeRegressorPipelineSnapshot>(
                        result.SnapshotJson, KernelRidgeRegressorRecall.ModelKind);

            AdvancedAnalyticsModelJson.Serialise(envelope).Should().Be(result.SnapshotJson);
            AdvancedAnalyticsModelJson.ReadKind(result.SnapshotJson).Should().Be("KernelRidgeRegressorPipeline");
            envelope.Payload.SupportRows.Should().HaveCount(result.Details.TrainingRowsInExpansion);
            envelope.VariablesInOrder.Should().Equal(result.Variables);
        }

        [Fact]
        public void ATamperedOrForeignSnapshotIsRefused()
        {
            var (inputs, outputs) = Sinusoid(150, 8);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(1),
                new KernelRidgeRegressorOptions { Seed = 1 });

            Action wrongKind = () => KernelRidgeRegressorRecall.FromSnapshot(
                result.SnapshotJson.Replace("KernelRidgeRegressorPipeline", "SomethingElse"));
            wrongKind.Should().Throw<AdvancedAnalyticsSnapshotException>();

            var tampered = JObject.Parse(result.SnapshotJson);
            ((JArray)tampered["payload"]!["supportRows"]!).RemoveAt(0);
            Action shortRows = () => KernelRidgeRegressorRecall.FromSnapshot(tampered.ToString());
            shortRows.Should().Throw<AdvancedAnalyticsSnapshotException>();

            Action garbage = () => KernelRidgeRegressorRecall.FromSnapshot("not json");
            garbage.Should().Throw<AdvancedAnalyticsSnapshotException>();
        }

        [Fact]
        public void TheEvidenceReportsPlausibleMetricsAndExpansionSize()
        {
            var (inputs, outputs) = Sinusoid(400, 19);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(1),
                new KernelRidgeRegressorOptions { Seed = 2, TestFraction = 0.2, ValidationFraction = 0.2 });

            result.Details.Kernel.Should().Be("Gaussian");
            result.Details.TrainingRowsInExpansion.Should().Be(result.SplitSizes.Train);
            result.Details.RidgePenalty.Should().BeApproximately(1d / result.Options.Complexity, 1e-12);
            result.Details.TestMetrics.Should().NotBeNull();
            result.Details.TestMetrics!.RSquared.Should().NotBeNull();
            result.Details.TestMetrics.RSquared!.Value.Should().BeGreaterThan(0.9);
            result.Details.TestMetrics.RootMeanSquaredError.Should().NotBeNull();
            result.Details.TestMetrics.MeanAbsoluteError.Should().NotBeNull();
            result.Details.TestMetrics.MeanError.Should().NotBeNull();
        }

        [Fact]
        public void GammaResolvesAutomaticallyFromTheNumberOfPredictorsWhenLeftAtZero()
        {
            var (inputs, outputs) = QuadraticSurface(120, 25);

            var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, Names(2),
                new KernelRidgeRegressorOptions { Seed = 1, Gamma = 0d });

            result.Details.Gamma.Should().BeApproximately(0.5d, 1e-9);
        }

        [Fact]
        public void ANearSingularSystemIsHandledEitherWithAWarningOrARefusalRatherThanSilentGarbage()
        {
            var outputs = Enumerable.Range(0, 40).Select(i => (double)(i % 2)).ToArray();
            var inputs = Enumerable.Range(0, 40).Select(_ => new[] { 2d }).ToArray();
            var names = Names(1);

            var options = new KernelRidgeRegressorOptions
            {
                Kernel = KernelRidgeRegressorKernel.Gaussian, Complexity = 1e17, Seed = 1, ValidationFraction = 0d,
                TestFraction = 0d
            };

            try
            {
                var result = KernelRidgeRegressorTrainer.Train(inputs, outputs, names, options);

                result.Warnings.Select(w => w.Code)
                    .Should().Contain(KernelRidgeRegressorWarningCodes.NearSingularSystem);

                var recall = KernelRidgeRegressorRecall.FromSnapshot(result.SnapshotJson);
                recall.Predict([2d]).Should().OnlyContain(v => double.IsFinite(v));
            }
            catch (InvalidOperationException exception)
            {
                exception.Message.Should().NotBeNullOrWhiteSpace();
            }
        }
    }
}