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
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Anomaly;
using Jube.Test.Analytics.Anomaly;
using Xunit;

namespace Jube.Test.Exhaustive
{
    public class AnomalyRunnerTests
    {
        private static double[][] SvmData(int seed)
        {
            var random = new Random(seed);

            return
            [
                .. Enumerable.Range(0, 120)
                    .Select(i => new[]
                    {
                        random.NextDouble() * 0.5 + (i < 6 ? 3 : 0), random.NextDouble() * 0.5,
                        random.NextDouble() * 0.5
                    })
            ];
        }

        private static AnomalyRunRequest Request(double[][] data, AnomalyAlgorithm algorithm, double probability,
            AnomalyParameters? parameters = null, double[][]? positives = null)
        {
            return new AnomalyRunRequest(data, [.. data[0].Select((_, j) => $"v{j}")], algorithm,
                parameters ?? new AnomalyParameters(), probability, 17, positives, TrainSupport.Log);
        }

        [Fact]
        public void TheOneClassSvmPathProducesTheSameLabelsAsTheOriginalCodePath()
        {
            foreach (var seed in new[] { 3, 8, 21 })
            {
                var data = SvmData(seed);
                var model = Unsupervised.Learn(data, TrainSupport.Log);
                var probabilities = data.Select(model.Probability).OrderBy(p => p).ToArray();

                foreach (var probability in new[] { probabilities[10], probabilities[60], 0.5, 0d, 1d })
                {
                    var original = TrainSupport.NewTraining(1).GetClassVariableByAnomaly(model, data, probability);

                    var result = AnomalyRunner.Run(Request(data, AnomalyAlgorithm.OneClassSvm, probability),
                        CancellationToken.None);

                    result.Labels.Should().Equal(original, $"seed {seed} probability {probability}");
                    result.Report.Threshold!.Method.Should().Be("SvmProbability");
                    result.Report.Threshold.Value.Should().Be(probability);
                    result.Report.Reduction.Should().Be("None");
                    result.Report.KeptVariables.Should().Be(3);
                }
            }
        }

        [Fact]
        public void TheOneClassSvmAdapterScoresTheNegatedProbabilityAndIsThreadSafe()
        {
            var data = SvmData(5);
            var detector = new OneClassSvmDetector(TrainSupport.Log);
            detector.Fit(data, 1);
            var sequential = data.Select(detector.Score).ToArray();
            sequential.Should().Equal(data.Select(r => -detector.Probability(r)));
            var parallel = new double[data.Length];

            Parallel.For(0, data.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                i => parallel[i] = detector.Score(data[i]));

            parallel.Should().Equal(sequential);
        }

        [Fact]
        public void TheAdapterHonoursCancellationAndRefusesUnfittedUse()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;
            var detector = new OneClassSvmDetector(TrainSupport.Log);
            var act = () => detector.Fit(SvmData(1), 1, token);
            act.Should().Throw<OperationCanceledException>();
            var unfitted = () => detector.Score([1d, 2d, 3d]);
            unfitted.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ANativeDetectorFlagsTheContaminationFractionAndReportsTheReduction()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 400);
            var wide = data.Select(r => r.Concat([5d, r[0] * 2]).ToArray()).ToArray();

            var result = AnomalyRunner.Run(Request(wide, AnomalyAlgorithm.IsolationForest, 0.05),
                CancellationToken.None);

            result.Labels.Sum().Should().Be(20);
            var report = result.Report;
            report.Algorithm.Should().Be("IsolationForest");
            report.Reduction.Should().Be("Standard");
            report.Scaling.Should().Be("Robust");
            report.InputVariables.Should().Be(8);
            report.KeptVariables.Should().Be(6);
            report.Variables.Should().HaveCount(8);
            report.Variables.Single(v => v.Name == "v6").Reason.Should().Be("Constant");
            report.Variables.Single(v => v.Name == "v7").Reason.Should().Be("Correlated");
            report.Threshold!.Method.Should().Be("Contamination");
            report.Threshold.Flagged.Should().Be(20);
            report.Scores!.Maximum.Should().BeGreaterThan(report.Scores.P50);
            report.Seed.Should().Be(17);
            AnomalyReport.FromJson(report.ToJson()).Should().NotBeNull();
        }

        [Fact]
        public void RunsAreDeterministic()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);

            var first = AnomalyRunner.Run(Request(data, AnomalyAlgorithm.IsolationForest, 0.05),
                CancellationToken.None);
            var second = AnomalyRunner.Run(Request(data, AnomalyAlgorithm.IsolationForest, 0.05),
                CancellationToken.None);

            first.Labels.Should().Equal(second.Labels);
        }

        [Fact]
        public void ReductionCanBeSwitchedOffForANativeDetector()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);
            var wide = data.Select(r => r.Concat([5d]).ToArray()).ToArray();
            var parameters = new AnomalyParameters { Reduction = ReductionOptions.Off };

            var result = AnomalyRunner.Run(Request(wide, AnomalyAlgorithm.Hbos, 0.05, parameters),
                CancellationToken.None);

            result.Report.Reduction.Should().Be("None");
            result.Report.KeptVariables.Should().Be(7);
            result.Report.DetectorDimensions.Should().Be(7);
        }

        [Fact]
        public void TheContaminationParameterOverridesTheProbabilityAndIsValidated()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);

            var overridden = AnomalyRunner.Run(
                Request(data, AnomalyAlgorithm.Hbos, 0.05, new AnomalyParameters { Contamination = 0.1 }),
                CancellationToken.None);

            overridden.Report.Threshold!.Flagged.Should().Be(30);
            var bad = () => AnomalyRunner.Run(Request(data, AnomalyAlgorithm.Hbos, 0.9), CancellationToken.None);
            bad.Should().Throw<ArgumentException>().WithMessage("*contamination*");
        }

        [Fact]
        public void AutomaticRecordsEveryCandidateAndResolvesTheAlgorithm()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 500, 8);

            var parameters = new AnomalyParameters
            {
                Auto = new AutoOptions
                {
                    TimeBudgetSeconds = 120,
                    Candidates = [AnomalyAlgorithm.IsolationForest, AnomalyAlgorithm.Hbos, AnomalyAlgorithm.Mahalanobis]
                }
            };

            var result = AnomalyRunner.Run(Request(data, AnomalyAlgorithm.Automatic, 0.03, parameters),
                CancellationToken.None);

            var report = result.Report;
            report.Requested.Should().Be("Automatic");
            report.Algorithm.Should().NotBe("Automatic");
            report.Auto.Should().NotBeNull();
            report.Auto!.Candidates.Should().HaveCount(3);
            report.Auto.Candidates.Count(c => c.Selected).Should().Be(1);
            report.Auto.Candidates.Single(c => c.Selected).Algorithm.Should().Be(report.Algorithm);
            report.Summary().Should().Contain("selected by Automatic");
        }

        [Fact]
        public void AutomaticUsesTrustedPositivesWhenSupplied()
        {
            var (data, labels) = AnomalyFixtures.CorrelationBreak(2, 1500);
            var normal = data.Where((_, i) => labels[i] == 0).ToArray();
            var positives = data.Where((_, i) => labels[i] == 1).ToArray();

            var parameters = new AnomalyParameters
            {
                Reduction = ReductionOptions.Off,
                Auto = new AutoOptions
                {
                    Candidates = [AnomalyAlgorithm.Hbos, AnomalyAlgorithm.Mahalanobis, AnomalyAlgorithm.IsolationForest]
                }
            };

            var result = AnomalyRunner.Run(Request(normal, AnomalyAlgorithm.Automatic, 0.03, parameters, positives),
                CancellationToken.None);

            result.Report.Algorithm.Should().Be("Mahalanobis");
            result.Report.Auto!.UsedLabels.Should().BeTrue();
        }

        [Fact]
        public void ACancelledRunThrows()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;
            var act = () => AnomalyRunner.Run(Request(data, AnomalyAlgorithm.IsolationForest, 0.05), token);
            act.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void TheAutomaticCandidateSetIsRestrictedByDimensionalityAndRows()
        {
            var high = AnomalyCandidates.Default(200, 5000);
            high.Should().Contain([AnomalyAlgorithm.IsolationForest, AnomalyAlgorithm.Hbos, AnomalyAlgorithm.Pca]);

            high.Should().NotContain([
                AnomalyAlgorithm.Mahalanobis, AnomalyAlgorithm.RobustMahalanobis, AnomalyAlgorithm.KnnDistance,
                AnomalyAlgorithm.Consensus
            ]);

            var low = AnomalyCandidates.Default(20, 5000);

            low.Should().Contain([
                AnomalyAlgorithm.Mahalanobis, AnomalyAlgorithm.RobustMahalanobis, AnomalyAlgorithm.KnnDistance,
                AnomalyAlgorithm.Consensus, AnomalyAlgorithm.GaussianMixture
            ]);

            var @short = AnomalyCandidates.Default(20, 30);
            @short.Should().NotContain(AnomalyAlgorithm.Mahalanobis);
            @short.Should().NotContain(AnomalyAlgorithm.RobustMahalanobis);

            foreach (var set in new[] { high, low, @short })
            {
                set.Should().NotContain([
                    AnomalyAlgorithm.OneClassSvm, AnomalyAlgorithm.Automatic,
                    AnomalyAlgorithm.LocalOutlierFactor, AnomalyAlgorithm.FeatureBagging
                ]);
            }
        }

        [Fact]
        public void AnExplicitCandidateListOverridesTheDefaultsButNeverAdmitsTheSvm()
        {
            var parameters = new AnomalyParameters
            {
                Auto = new AutoOptions
                {
                    Candidates =
                        [AnomalyAlgorithm.OneClassSvm, AnomalyAlgorithm.LocalOutlierFactor, AnomalyAlgorithm.Hbos]
                }
            };

            var candidates = AnomalyCandidates.For(10, 500, parameters, TrainSupport.Log);

            candidates.Select(c => c.Algorithm).Should()
                .Equal(AnomalyAlgorithm.LocalOutlierFactor, AnomalyAlgorithm.Hbos);
        }
    }
}