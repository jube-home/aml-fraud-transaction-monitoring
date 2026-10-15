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
using Accord.Statistics.Distributions.Univariate;
using FluentAssertions;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Promotion;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class TrainMetricsTests
    {
        private static double[][] Labels(params double[] labels)
        {
            return [.. labels.Select(l => new[] { l })];
        }

        private static ActivationNetwork ConstantNetwork(double output, int inputs = 2)
        {
            var network = new ActivationNetwork(new IdentityFunction(), inputs, [],
                1);

            foreach (var neuron in network.Layers[0].Neurons)
            {
                for (var i = 0; i < neuron.Weights.Length; i++)
                {
                    neuron.Weights[i] = 0;
                }

                ((ActivationNeuron)neuron).Threshold = output;
            }

            return network;
        }

        private static ActivationNetwork AlternatingNetwork()
        {
            var network = new ActivationNetwork(new IdentityFunction(), 1, [],
                1);

            var neuron = (ActivationNeuron)network.Layers[0].Neurons[0];
            neuron.Weights[0] = 1;
            neuron.Threshold = 0.5;

            return network;
        }

        [Fact]
        public void CalculatePerformance_CountsTheConfusionMatrixAtTheThreshold()
        {
            var performance = new Performance();

            performance.CalculatePerformance([0.9, 0.6, 0.4, 0.1, 0.5, 0.7], Labels(1, 0, 1, 0, 1, 0), 0.5);

            performance.Tp.Should().Be(1);
            performance.Fp.Should().Be(2);
            performance.Fn.Should().Be(2);
            performance.Tn.Should().Be(1);
        }

        [Fact]
        public void CalculatePerformance_ThresholdIsStrictlyGreaterThan()
        {
            var performance = new Performance();

            performance.CalculatePerformance([0.5, 0.5], Labels(1, 0), 0.5);

            performance.Tp.Should().Be(0);
            performance.Fp.Should().Be(0);
            performance.Fn.Should().Be(1);
            performance.Tn.Should().Be(1);
        }

        [Fact]
        public void CalculatePerformance_ScoreIsTheMeanOfAccuracyAndSpearmanOfPredictedVersusActual()
        {
            var performance = new Performance();
            double[] scores = [0.9, 0.8, 0.2, 0.1, 0.7, 0.3];
            var labels = Labels(1, 1, 0, 0, 0, 1);

            var score = performance.CalculatePerformance(scores, labels, 0.5);

            var predicted = scores.Select(s => s > 0.5 ? 1d : 0d).ToArray();
            var accuracy = predicted.Zip(labels, (p, l) => p == l[0] ? 1d : 0d).Average();
            var rho = Performance.SpearmansCoeff(predicted, labels.Select(l => l[0]));
            score.Should().BeApproximately((accuracy + rho) / 2, 1e-12);
            performance.Score.Should().Be(score);
        }

        [Fact]
        public void CalculatePerformance_InvertedClassifier_ScoresMinusOneHalf_WithTiedBinaryRanks()
        {
            var performance = new Performance();

            var score = performance.CalculatePerformance([0, 0, 1, 1], Labels(1, 1, 0, 0), 0.5);

            score.Should().BeApproximately(-0.5, 1e-12);
        }

        [Fact]
        public void CalculatePerformance_PerfectClassifier_ScoresOne()
        {
            new Performance().CalculatePerformance([0.1, 0.2, 0.8, 0.9], Labels(0, 0, 1, 1), 0.5)
                .Should().BeApproximately(1, 1e-12);
        }

        [Fact]
        public void CalculatePerformance_ASingleRow_HasNaNCorrelationTreatedAsZero()
        {
            var performance = new Performance();

            var score = performance.CalculatePerformance([0.9], Labels(1), 0.5);

            score.Should().BeApproximately(0.5, 1e-12);
        }

        [Fact]
        public void CalculatePerformance_NoRows_ScoresNaN_Characterisation()
        {
            var performance = new Performance();

            var score = performance.CalculatePerformance([], [], 0.5);

            double.IsNaN(score).Should().BeTrue();
            performance.Tp.Should().Be(0);
        }

        [Fact]
        public void CalculatePerformance_NaNScoreIsPredictedNegative_AndInfinityHonoursTheThreshold()
        {
            var performance = new Performance();

            performance.CalculatePerformance([double.NaN, double.PositiveInfinity, double.NegativeInfinity],
                Labels(1, 1, 0), 0.5);

            performance.Fn.Should().Be(1);
            performance.Tp.Should().Be(1);
            performance.Tn.Should().Be(1);
            performance.Fp.Should().Be(0);
        }

        [Fact]
        public void CalculatePerformance_ResetsCountersOnEveryCall()
        {
            var performance = new Performance();
            performance.CalculatePerformance([0.9, 0.9], Labels(1, 1), 0.5);

            performance.CalculatePerformance([0.1], Labels(0), 0.5);

            performance.Tp.Should().Be(0);
            performance.Tn.Should().Be(1);
        }

        [Fact]
        public void CalculateScores_ComputesTheFirstOutputNeuronForEveryRow()
        {
            var network = ConstantNetwork(0.25);
            var performance = new Performance();

            var scores = performance.CalculateScores(network, [[1, 2], [3, 4], [5, 6]]);

            scores.Should().Equal(0.25, 0.25, 0.25);
            performance.Scores.Should().BeSameAs(scores);
            performance.CalculateScores(network, []).Should().BeEmpty();
        }

        [Fact]
        public void RocPoints_IsTwentyThresholdsFromFiveHundredthsToOne()
        {
            var points = Performance.RocPoints([0.02, 0.3, 0.6, 0.97, 0.5], Labels(0, 0, 1, 1, 1));

            points.Should().HaveCount(20);

            points.Select(p => p.Threshold).Should().BeEquivalentTo(Enumerable.Range(1, 20).Select(i => i * 0.05),
                o => o.WithStrictOrdering().Using<double>(c => c.Subject.Should().BeApproximately(c.Expectation, 1e-9))
                    .WhenTypeIs<double>());
        }

        [Fact]
        public void RocPoints_MatchesAnIndependentSweepAtEveryThreshold()
        {
            var random = new Random(12);
            var scores = Enumerable.Range(0, 300).Select(_ => random.NextDouble()).ToArray();
            var labels = scores.Select(s => random.NextDouble() < s ? 1d : 0d).ToArray();

            var points = Performance.RocPoints(scores, Labels(labels));

            foreach (var point in points)
            {
                var predicted = scores.Select(s => s > point.Threshold).ToArray();
                point.Tp.Should().Be(predicted.Zip(labels, (p, l) => p && l == 1).Count(x => x));
                point.Fp.Should().Be(predicted.Zip(labels, (p, l) => p && l == 0).Count(x => x));
                point.Tn.Should().Be(predicted.Zip(labels, (p, l) => !p && l == 0).Count(x => x));
                point.Fn.Should().Be(predicted.Zip(labels, (p, l) => !p && l == 1).Count(x => x));
            }

            points.Select(p => p.Tp).Should().BeInDescendingOrder();
            points.Select(p => p.Fp).Should().BeInDescendingOrder();
            points.Select(p => p.Tn).Should().BeInAscendingOrder();
            points.Select(p => p.Fn).Should().BeInAscendingOrder();
        }

        [Fact]
        public void RocPoints_AtTheHalfThreshold_AgreesWithCalculatePerformance()
        {
            var random = new Random(2);
            var scores = Enumerable.Range(0, 100).Select(_ => random.NextDouble()).ToArray();
            var labels = Labels([.. scores.Select(s => random.NextDouble() < s ? 1d : 0d)]);
            var performance = new Performance();
            performance.CalculatePerformance(scores, labels, 0.5);

            var half = Performance.RocPoints(scores, labels).Single(p => Math.Abs(p.Threshold - 0.5) < 1e-6);

            (half.Tp, half.Fp, half.Tn, half.Fn).Should().Be((performance.Tp, performance.Fp, performance.Tn,
                performance.Fn));

            half.Score.Should().BeApproximately(performance.Score, 1e-12);
        }

        [Fact]
        public void RocPoints_ScoresAllAboveOne_PredictEveryRowPositiveEvenAtTheLastThreshold()
        {
            var points = Performance.RocPoints([1.5, 2, 3], Labels(1, 0, 1));

            points.Should().OnlyContain(p => p.Tp == 2 && p.Fp == 1 && p.Tn == 0 && p.Fn == 0);
        }

        [Fact]
        public void RocPoints_DoesNotDisturbAnExistingPerformanceInstance()
        {
            var performance = new Performance();
            performance.CalculatePerformance([0.9, 0.1], Labels(1, 0), 0.5);

            Performance.RocPoints([0.9, 0.1], Labels(1, 0));

            performance.Tp.Should().Be(1);
            performance.Tn.Should().Be(1);
            performance.Score.Should().BeApproximately(1, 1e-12);
        }

        [Fact]
        public void Spearman_PerfectAgreementAndDisagreement()
        {
            Performance.SpearmansCoeff([1d, 2, 3, 4, 5], [10d, 20, 30, 40, 50]).Should().BeApproximately(1, 1e-12);
            Performance.SpearmansCoeff([1d, 2, 3, 4, 5], [50d, 40, 30, 20, 10]).Should().BeApproximately(-1, 1e-12);
        }

        [Fact]
        public void Spearman_IsRankBased_SoAMonotoneNonLinearRelationIsOne()
        {
            var x = Enumerable.Range(1, 30).Select(i => (double)i).ToArray();

            Performance.SpearmansCoeff(x, [.. x.Select(v => Math.Exp(v))]).Should().BeApproximately(1, 1e-12);
        }

        [Fact]
        public void Spearman_KnownValueWithoutTies()
        {
            Performance.SpearmansCoeff([86d, 97, 99, 100, 101, 103, 106, 110, 112, 113],
                [2d, 20, 28, 27, 50, 29, 7, 17, 6, 12]).Should().BeApproximately(-29d / 165d, 1e-12);
        }

        [Theory]
        [InlineData(new[] { 1d, 2, 2, 3 }, new[] { 1d, 2, 3, 4 }, 0.9486832980505138)]
        [InlineData(new[] { 0d, 0, 1, 1 }, new[] { 1d, 1, 0, 0 }, -1.0)]
        [InlineData(new[] { 1d, 0, 1, 0, 1, 1, 0, 0 }, new[] { 1d, 0, 0, 0, 1, 1, 1, 0 }, 0.5)]
        [InlineData(new[] { 1d, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new[] { 1d, 3, 2, 4, 4, 6, 7, 7, 9, 10 },
            0.9817255676437577)]
        [InlineData(new[] { 0d, 0, 0, 1, 1, 1, 1, 1, 1, 1 }, new[] { 0d, 1, 0, 1, 1, 0, 1, 1, 1, 1 },
            0.5238095238095238)]
        public void Spearman_WithTies_IsThePearsonCorrelationOfAverageRanks(double[] x, double[] y, double expected)
        {
            Performance.SpearmansCoeff(x, y).Should().BeApproximately(expected, 1e-12);
        }

        [Fact]
        public void Spearman_IsSymmetric()
        {
            var random = new Random(9);
            var a = Enumerable.Range(0, 50).Select(_ => Math.Round(random.NextDouble() * 10)).ToArray();
            var b = Enumerable.Range(0, 50).Select(_ => Math.Round(random.NextDouble() * 10)).ToArray();

            Performance.SpearmansCoeff(a, b).Should().BeApproximately(Performance.SpearmansCoeff(b, a), 1e-12);
        }

        [Fact]
        public void Spearman_LengthMismatchThrows()
        {
            var act = () => Performance.SpearmansCoeff([1d, 2], [1d]);

            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Spearman_AConstantSeriesHasNoCorrelation_AndSingleElementAndEmptyAreNaN()
        {
            Performance.SpearmansCoeff([3d, 3, 3], [8d, 8, 8]).Should().Be(0);
            Performance.SpearmansCoeff([3d, 3, 3], [1d, 2, 3]).Should().Be(0);
            double.IsNaN(Performance.SpearmansCoeff([1d], [2d])).Should().BeTrue();
            double.IsNaN(Performance.SpearmansCoeff([], [])).Should().BeTrue();
        }

        [Fact]
        public void Spearman_ConstantAgainstVarying_IsFiniteNotNaN()
        {
            var rho = Performance.SpearmansCoeff([1d, 1, 1, 1], [1d, 2, 3, 4]);

            double.IsNaN(rho).Should().BeFalse();
            rho.Should().BeInRange(-1, 1);
        }

        [Fact]
        public void Sensitivity_DoesNotMutateTheInputRows_AndIsRepeatableForTheSameSeed()
        {
            var network = ConstantNetwork(0.5, 3);
            network.Layers[0].Neurons[0].Weights[1] = 1;
            double[][] inputs = [[1, 2, 3], [4, 5, 6]];
            double[] baseline = [2, 5];

            var first = Performance.Sensitivity(new Random(1), network, inputs, 1, baseline);
            var second = Performance.Sensitivity(new Random(1), network, inputs, 1, baseline);

            inputs[0].Should().Equal(1, 2, 3);
            inputs[1].Should().Equal(4, 5, 6);
            first.Should().Equal(second);
        }

        [Fact]
        public void Sensitivity_AVariableTheModelIgnoresHasZeroSensitivity_AndAZeroValueCannotBePerturbed()
        {
            var network = ConstantNetwork(0);
            network.Layers[0].Neurons[0].Weights[0] = 1;
            double[][] inputs = [[3, 100], [0, 100]];
            double[] baseline = [3, 0];

            Performance.Sensitivity(new Random(1), network, inputs, 1, baseline).Should().OnlyContain(v => v == 0);
            var perturbed = Performance.Sensitivity(new Random(1), network, inputs, 0, baseline);
            perturbed[0].Should().BeGreaterThan(0);
            perturbed[1].Should().Be(0);
        }

        [Fact]
        public void BuildPrescriptions_KeepsOnlyActivationsAboveHalfAndEmitsOneRowPerVariable()
        {
            var trialVariables = new Dictionary<int, TrialVariable>
            {
                [3] = new()
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = 31, NormalisationTypeId = 2, Mean = 10, Sd = 2,
                    TriangularDistribution = new TriangularDistribution(-1, 3, 0)
                },
                [7] = new()
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = 71, NormalisationTypeId = 0,
                    TriangularDistribution = new TriangularDistribution(0, 5, 1)
                }
            };

            var network = ConstantNetwork(0.9);

            var prescriptions = PrescriptionBuilder.BuildPrescriptions(200, trialVariables, network);

            prescriptions.Select(p => p.ExhaustiveSearchInstanceTrialInstanceVariableId).Should().Equal(31, 71);
            prescriptions[0].Minimum.Should().BeGreaterThanOrEqualTo(10 + -1 * 2);
            prescriptions[0].Maximum.Should().BeLessThanOrEqualTo(10 + 3 * 2);
            prescriptions[0].Mean.Should().BeInRange(8, 16);
            prescriptions[0].Iqr.Should().BeGreaterThan(0);
            prescriptions[0].StandardDeviation.Should().BeGreaterThan(0);
            prescriptions[1].Minimum.Should().BeGreaterThanOrEqualTo(0);
            prescriptions[1].Maximum.Should().BeLessThanOrEqualTo(5);
        }

        [Fact]
        public void BuildPrescriptionsWithHistograms_HistogramCoversTheSampleAndConstantVariablesGetNone()
        {
            var trialVariables = new Dictionary<int, TrialVariable>
            {
                [0] = new()
                {
                    NormalisationTypeId = 2, Mean = 10, Sd = 2,
                    TriangularDistribution = new TriangularDistribution(-1, 3, 0)
                },
                [1] = new() { NormalisationTypeId = 2, Mean = 4, Sd = 3, TriangularDistribution = null }
            };

            var result =
                PrescriptionBuilder.BuildPrescriptionsWithHistograms(200, trialVariables, ConstantNetwork(0.9));

            result[0].Bins.Should().NotBeEmpty();
            result[0].Bins.Sum(b => b.Frequency!.Value).Should().Be(200);
            result[0].Bins.Select(b => b.BinIndex!.Value).Should().Equal(Enumerable.Range(0, result[0].Bins.Count));

            result[0].Bins.Min(b => b.BinRangeStart).Should()
                .BeApproximately(result[0].Prescription.Minimum!.Value, 1e-9);
            result[0].Bins.Max(b => b.BinRangeEnd).Should()
                .BeApproximately(result[0].Prescription.Maximum!.Value, 1e-9);

            result[1].Bins.Should().BeEmpty();
        }

        [Fact]
        public void BuildPrescriptions_NetworkThatNeverActivatesPositively_ReturnsOnlyTheNegativeClassPrescription()
        {
            var trialVariables = new Dictionary<int, TrialVariable>
            {
                [0] = new() { TriangularDistribution = new TriangularDistribution(0, 1, 0.5) }
            };

            var prescriptions = PrescriptionBuilder.BuildPrescriptions(100, trialVariables, ConstantNetwork(0.4, 1));

            prescriptions.Should().ContainSingle().Which.Positive.Should().BeFalse();
            PrescriptionBuilder.BuildPrescriptions(0, trialVariables, ConstantNetwork(0.9, 1)).Should().BeEmpty();
        }

        [Fact]
        public void BuildPrescriptions_SplitsSimulationsIntoAPositiveAndANegativeClassPrescriptionPerVariable()
        {
            var trialVariables = new Dictionary<int, TrialVariable>
            {
                [3] = new()
                {
                    ExhaustiveSearchInstanceTrialInstanceVariableId = 31, NormalisationTypeId = 2, Mean = 10, Sd = 2,
                    TriangularDistribution = new TriangularDistribution(-1, 3, 0)
                }
            };

            var prescriptions = PrescriptionBuilder.BuildPrescriptions(200, trialVariables,
                AlternatingNetwork());

            prescriptions.Should().HaveCount(2);
            prescriptions.Count(p => p.Positive == true).Should().Be(1);
            prescriptions.Count(p => p.Positive == false).Should().Be(1);

            prescriptions.Should()
                .OnlyContain(p => p.ExhaustiveSearchInstanceTrialInstanceVariableId == 31);
        }

        [Fact]
        public void BuildPrescriptions_BinaryVariablesAreSnappedToZeroOrOne_AndMissingDistributionsBecomeZero()
        {
            var trialVariables = new Dictionary<int, TrialVariable>
            {
                [0] = new() { NormalisationTypeId = 1, TriangularDistribution = new TriangularDistribution(0, 1, 1) },
                [1] = new() { NormalisationTypeId = 2, Mean = 4, Sd = 3, TriangularDistribution = null }
            };

            var prescriptions = PrescriptionBuilder.BuildPrescriptions(200, trialVariables, ConstantNetwork(0.9));

            prescriptions[0].Minimum.Should().BeOneOf(0d, 1d);
            prescriptions[0].Maximum.Should().Be(1);
            prescriptions[0].Mean.Should().BeGreaterThan(0.5);
            prescriptions[1].Minimum.Should().Be(4);
            prescriptions[1].Maximum.Should().Be(4);
            prescriptions[1].StandardDeviation.Should().Be(0);
            prescriptions[1].Iqr.Should().Be(0);
        }

        [Theory]
        [InlineData(27, 5, 30)]
        [InlineData(27, 5, 10)]
        [InlineData(27, 20, 22)]
        [InlineData(10, 3, 30)]
        public void GetRandomVariableCount_StaysWithinMinimumAndMaximumAndBelowTheAvailableCount(int available, int min,
            int max)
        {
            var supervised = TrainSupport.NewSupervised(TrainSupport.Settings(), new TrainRecordingRecorder(),
                new TrainPrepared(new Dictionary<int, Variable>(), [], []), 5);

            for (var i = 0; i < 500; i++)
            {
                var count = supervised.Selector.GetRandomVariableCount(available, min, max);
                count.Should().BeInRange(min, Math.Min(max, available - 1));
            }
        }

        [Fact]
        public void GetRandomVariableCount_FewerAvailableThanTheMinimum_ReturnsEveryAvailableVariable()
        {
            var supervised = TrainSupport.NewSupervised(TrainSupport.Settings(), new TrainRecordingRecorder(),
                new TrainPrepared(new Dictionary<int, Variable>(), [], []), 5);

            supervised.Selector.GetRandomVariableCount(4, 5, 30).Should().Be(4);
        }

        [Fact]
        public void Unsupervised_OneClassModel_ScoresAnOutlierLowerThanTheClusterAndTheClassifierFlagsIt()
        {
            var random = new Random(21);

            var data = Enumerable.Range(0, 80)
                .Select(_ => new[] { random.NextDouble() * 0.5, random.NextDouble() * 0.5, random.NextDouble() * 0.5 })
                .ToArray();

            var model = Unsupervised.Learn(data, TrainSupport.Log);

            var inlier = model.Probability([0.25, 0.25, 0.25]);
            var outlier = model.Probability([9d, 9, 9]);
            outlier.Should().BeLessThan(inlier);

            double[][] probe = [[0.25, 0.25, 0.25], [9, 9, 9]];
            var flags = TrainSupport.NewTraining(1).GetClassVariableByAnomaly(model, probe, (inlier + outlier) / 2);
            flags.Should().Equal(0, 1);
        }

        [Fact]
        public void GetClassVariableByAnomaly_IsStrictlyBelowTheProbability()
        {
            var random = new Random(4);

            var data = Enumerable.Range(0, 40).Select(_ => new[] { random.NextDouble(), random.NextDouble() })
                .ToArray();

            var model = Unsupervised.Learn(data, TrainSupport.Log);
            var p = model.Probability(data[0]);

            var flags = TrainSupport.NewTraining(1).GetClassVariableByAnomaly(model, [data[0]], p);

            flags.Should().Equal(0);
            TrainSupport.NewTraining(1).GetClassVariableByAnomaly(model, [data[0]], p + 1e-9).Should().Equal(1);
        }
    }
}