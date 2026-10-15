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
using FluentAssertions;
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Jube.Analytics.Anomaly.Interfaces;
using Xunit;

namespace Jube.Test.Analytics.Anomaly
{
    public class AnomalyAutoSelectorTests
    {
        private static AutoCandidate Candidate(AnomalyAlgorithm algorithm, Func<IAnomalyDetector> factory)
        {
            return new AutoCandidate(algorithm, algorithm.ToString(), factory);
        }

        private static IReadOnlyList<AutoCandidate> Real()
        {
            var parameters = new AnomalyParameters();

            return
            [
                Candidate(AnomalyAlgorithm.Hbos, AnomalyDetectorFactory.Factory(AnomalyAlgorithm.Hbos, parameters)),
                Candidate(AnomalyAlgorithm.Mahalanobis,
                    AnomalyDetectorFactory.Factory(AnomalyAlgorithm.Mahalanobis, parameters)),
                Candidate(AnomalyAlgorithm.IsolationForest,
                    AnomalyDetectorFactory.Factory(AnomalyAlgorithm.IsolationForest, parameters))
            ];
        }

        [Fact]
        public void LabelledSelectionPicksTheDetectorThatFindsTheKnownPositives()
        {
            var (data, labels) = AnomalyFixtures.CorrelationBreak(2, 1500);
            var normal = data.Where((_, i) => labels[i] == 0).ToArray();
            var positives = data.Where((_, i) => labels[i] == 1).ToArray();

            var result = AnomalyAutoSelector.Select(new AutoRequest
            {
                Candidates = Real(),
                Data = normal,
                LabelledPositives = positives,
                Contamination = 0.03,
                Seed = 1
            });

            result.Selected.Should().Be(AnomalyAlgorithm.Mahalanobis);
            result.Evaluation.UsedLabels.Should().BeTrue();
            result.Evaluation.LabelledPositives.Should().Be(positives.Length);
            result.Evaluation.Candidates.Single(c => c.Selected).Auc.Should().BeGreaterThan(0.9);
            result.Evaluation.Candidates.Single(c => c.Algorithm == "Hbos").Auc.Should().BeLessThan(0.8);
        }

        [Fact]
        public void SelectionIsDeterministicAndRecordsEveryCandidate()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 500);
            var request = new AutoRequest { Candidates = Real(), Data = data, Contamination = 0.03, Seed = 4 };
            var first = AnomalyAutoSelector.Select(request);
            var second = AnomalyAutoSelector.Select(request);
            first.Selected.Should().Be(second.Selected);
            first.Selected.Should().NotBeNull();
            first.Evaluation.UsedLabels.Should().BeFalse();
            first.Evaluation.Candidates.Should().HaveCount(3);
            first.Evaluation.Candidates.Should().OnlyContain(c => c.Status == "Evaluated");

            first.Evaluation.Candidates.Should().OnlyContain(c => c.BootstrapStability.HasValue &&
                                                                  c.SubspaceStability.HasValue &&
                                                                  c.ExcessMass.HasValue &&
                                                                  c.MassVolume.HasValue && c.MeanRank.HasValue);

            first.Evaluation.Candidates.Count(c => c.Selected).Should().Be(1);
            first.Evaluation.Rule.Should().Contain("Goix");

            foreach (var c in first.Evaluation.Candidates)
            {
                var other = second.Evaluation.Candidates.Single(o => o.Algorithm == c.Algorithm);
                other.BootstrapStability.Should().Be(c.BootstrapStability);
                other.ExcessMass.Should().Be(c.ExcessMass);
            }
        }

        [Fact]
        public void TheGuardRejectsDegenerateAndFailingDetectors()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);

            var result = AnomalyAutoSelector.Select(new AutoRequest
            {
                Candidates =
                [
                    Candidate(AnomalyAlgorithm.MarginalLikelihood, () => new ConstantDetector()),
                    Candidate(AnomalyAlgorithm.KnnDistance, () => new ThrowingDetector()),
                    Candidate(AnomalyAlgorithm.Hbos, AnomalyDetectorFactory.Factory(AnomalyAlgorithm.Hbos,
                        new AnomalyParameters()))
                ],
                Data = data,
                Contamination = 0.03,
                Seed = 1
            });

            result.Selected.Should().Be(AnomalyAlgorithm.Hbos);
            var rejected = result.Evaluation.Candidates.Where(c => c.Status == "Rejected").ToList();
            rejected.Should().HaveCount(2);
            rejected.Single(c => c.Algorithm == "MarginalLikelihood").Reason.Should().Contain("distinct scores");
            rejected.Single(c => c.Algorithm == "KnnDistance").Reason.Should().Contain("cannot fit");
        }

        [Fact]
        public void NothingSelectedWhenEveryCandidateIsRejected()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);

            var result = AnomalyAutoSelector.Select(new AutoRequest
            {
                Candidates = [Candidate(AnomalyAlgorithm.Hbos, () => new ConstantDetector())],
                Data = data,
                Seed = 1
            });
            result.Selected.Should().BeNull();
        }

        [Fact]
        public void TheTimeBudgetSkipsCandidatesNotReached()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);

            var result = AnomalyAutoSelector.Select(new AutoRequest
            {
                Candidates =
                [
                    Candidate(AnomalyAlgorithm.KnnDistance, () => new SlowDetector()),
                    Candidate(AnomalyAlgorithm.Hbos, AnomalyDetectorFactory.Factory(AnomalyAlgorithm.Hbos,
                        new AnomalyParameters()))
                ],
                Data = data,
                TimeBudgetSeconds = 1,
                Seed = 1
            });

            result.Evaluation.Candidates[0].Status.Should().Be("TimedOut");
            result.Evaluation.Candidates[1].Status.Should().Be("SkippedTimeBudget");
            result.Selected.Should().BeNull();
        }

        [Fact]
        public void CancellationIsPropagatedNotRecordedAsATimeout()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(3, 300);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;
            var act = () => AnomalyAutoSelector.Select(new AutoRequest { Candidates = Real(), Data = data }, token);
            act.Should().Throw<OperationCanceledException>();
        }

        [Fact]
        public void TheMassCurvesPreferATightLevelSet()
        {
            var rng = new Random(1);
            var test = Enumerable.Range(0, 500).Select(_ => AnomalyFixtures.Gaussian(rng)).ToArray();
            var uniform = Enumerable.Range(0, 2000).Select(_ => rng.NextDouble() * 12 - 6).ToArray();

            var good = AnomalyAutoSelector.Curves([.. test.Select(v => -Math.Abs(v))],
                [.. uniform.Select(v => -Math.Abs(v))]);

            var noise = AnomalyAutoSelector.Curves([.. test.Select(_ => rng.NextDouble())],
                [.. uniform.Select(_ => rng.NextDouble())]);

            good.MassVolume.Should().BeLessThan(noise.MassVolume);
            good.ExcessMass.Should().BeGreaterThan(noise.ExcessMass);
        }

        private sealed class ConstantDetector : IAnomalyDetector
        {
            public string Name => "Constant";
            public int Dimensions => 0;
            public bool IsFitted => true;

            public void Fit(double[][] data, int seed, CancellationToken token = default)
            {
            }

            public double Score(double[] row)
            {
                return 1d;
            }
        }

        private sealed class SlowDetector : IAnomalyDetector
        {
            public string Name => "Slow";
            public int Dimensions => 0;
            public bool IsFitted => true;

            public void Fit(double[][] data, int seed, CancellationToken token = default)
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(10);
                }
            }

            public double Score(double[] row)
            {
                return 0d;
            }
        }

        private sealed class ThrowingDetector : IAnomalyDetector
        {
            public string Name => "Throwing";
            public int Dimensions => 0;
            public bool IsFitted => false;

            public void Fit(double[][] data, int seed, CancellationToken token = default)
            {
                throw new ArgumentException("cannot fit");
            }

            public double Score(double[] row)
            {
                return 0d;
            }
        }
    }
}