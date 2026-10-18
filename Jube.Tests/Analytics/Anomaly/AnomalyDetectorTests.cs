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
using Jube.Analytics;
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Jube.Analytics.Anomaly.Interfaces;
using Xunit;

namespace Jube.Test.Analytics.Anomaly
{
    public class AnomalyDetectorTests
    {
        public static IEnumerable<object[]> Detectors()
        {
            yield return ["IsolationForest", 0.95, (Func<IAnomalyDetector>)(() => new IsolationForestDetector())];
            yield return ["Hbos", 0.9, (Func<IAnomalyDetector>)(() => new HbosDetector())];
            yield return ["Marginal", 0.9, (Func<IAnomalyDetector>)(() => new MarginalLikelihoodDetector())];
            yield return ["Mahalanobis", 0.9, (Func<IAnomalyDetector>)(() => new MahalanobisDetector())];

            yield return
            [
                "RobustMahalanobis", 0.9,
                (Func<IAnomalyDetector>)(() => new MahalanobisDetector(robust: true))
            ];

            yield return
                ["Pca", 0.8, (Func<IAnomalyDetector>)(() => new PcaDetector(retention: PcaRetention.ParallelAnalysis))];

            yield return ["GaussianMixture", 0.75, (Func<IAnomalyDetector>)(() => new GaussianMixtureDetector(2))];
            yield return ["Knn", 0.9, (Func<IAnomalyDetector>)(() => new KnnDistanceDetector())];
            yield return ["Lof", 0.8, (Func<IAnomalyDetector>)(() => new LocalOutlierFactorDetector())];

            yield return
            [
                "FeatureBagging", 0.9,
                (Func<IAnomalyDetector>)(() => new FeatureBaggingDetector(() => new KnnDistanceDetector(), 8, 4))
            ];

            yield return
            [
                "Consensus", 0.9, (Func<IAnomalyDetector>)(() => new ConsensusDetector(
                    new List<Func<IAnomalyDetector>>
                    {
                        () => new IsolationForestDetector(50),
                        () => new HbosDetector(),
                        () => new MahalanobisDetector()
                    }))
            ];
        }

        [Theory]
        [MemberData(nameof(Detectors))]
        public void PlantedAnomaliesAreRankedAboveNormalRowsAcrossSeeds(string name, double minimumAuc,
            Func<IAnomalyDetector> factory)
        {
            foreach (var seed in new[] { 1, 2, 3 })
            {
                var (data, labels) = AnomalyFixtures.ClusterWithOutliers(seed);
                var detector = factory();
                detector.Fit(data, seed);
                var scores = AnomalyInput.ScoreAll(detector, data);
                var auc = BinaryMetrics.RocAuc(scores, labels);
                auc.Should().BeGreaterThan(minimumAuc, $"{name} with seed {seed}");
            }
        }

        [Theory]
        [MemberData(nameof(Detectors))]
        public void ScoresAreDeterministicBySeed(string name, double minimumAuc, Func<IAnomalyDetector> factory)
        {
            _ = name;
            _ = minimumAuc;
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(4, 300);
            var first = factory();
            var second = factory();
            first.Fit(data, 11);
            second.Fit(data, 11);
            AnomalyInput.ScoreAll(first, data).Should().Equal(AnomalyInput.ScoreAll(second, data));
        }

        [Fact]
        public void DifferentSeedsChangeARandomisedDetector()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(4, 300);
            var first = new IsolationForestDetector();
            var second = new IsolationForestDetector();
            first.Fit(data, 1);
            second.Fit(data, 2);
            AnomalyInput.ScoreAll(first, data).Should().NotEqual(AnomalyInput.ScoreAll(second, data));
        }

        [Theory]
        [MemberData(nameof(Detectors))]
        public void ScoringIsThreadSafe(string name, double minimumAuc, Func<IAnomalyDetector> factory)
        {
            _ = name;
            _ = minimumAuc;
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(5, 300);
            var detector = factory();
            detector.Fit(data, 3);
            var sequential = data.Select(detector.Score).ToArray();
            var parallel = new double[data.Length];

            Parallel.For(0, data.Length, new ParallelOptions { MaxDegreeOfParallelism = 8 },
                i => parallel[i] = detector.Score(data[i]));

            parallel.Should().Equal(sequential);
        }

        [Theory]
        [MemberData(nameof(Detectors))]
        public void NonFiniteValuesAreImputedInFitAndScore(string name, double minimumAuc,
            Func<IAnomalyDetector> factory)
        {
            _ = name;
            _ = minimumAuc;
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(6, 300);
            data[3][1] = double.NaN;
            data[9][2] = double.PositiveInfinity;
            var detector = factory();
            detector.Fit(data, 3);
            var probe = (double[])data[0].Clone();
            probe[0] = double.NaN;
            probe[4] = double.NegativeInfinity;
            var score = detector.Score(probe);
            double.IsFinite(score).Should().BeTrue();
            AnomalyInput.ScoreAll(detector, data).Should().OnlyContain(v => double.IsFinite(v));
        }

        [Theory]
        [MemberData(nameof(Detectors))]
        public void ACancelledTokenStopsTheFit(string name, double minimumAuc, Func<IAnomalyDetector> factory)
        {
            _ = name;
            _ = minimumAuc;
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(7, 300);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;
            var detector = factory();
            var act = () => detector.Fit(data, 1, token);
            act.Should().Throw<OperationCanceledException>();
            detector.IsFitted.Should().BeFalse();
        }

        [Fact]
        public void ACancelledTokenStopsScoring()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(7, 300);
            var detector = new HbosDetector();
            detector.Fit(data, 1);
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;
            var act = () => AnomalyInput.ScoreAll(detector, data, token);
            act.Should().Throw<OperationCanceledException>();
        }

        [Theory]
        [MemberData(nameof(Detectors))]
        public void DegenerateInputsThrowClearExceptions(string name, double minimumAuc, Func<IAnomalyDetector> factory)
        {
            _ = minimumAuc;
            var constant = Enumerable.Range(0, 50).Select(_ => new[] { 1d, 2d, 3d }).ToArray();
            var act = () => factory().Fit(constant, 1);
            act.Should().Throw<ArgumentException>(name).WithMessage("*constant*");

            var one = factory();
            var single = () => one.Fit([[1d, 2d, 3d]], 1);
            single.Should().Throw<ArgumentException>().WithMessage("*2 rows*");

            var empty = () => factory().Fit([], 1);
            empty.Should().Throw<ArgumentException>();

            var ragged = () => factory().Fit([[1d, 2d], [1d]], 1);
            ragged.Should().Throw<ArgumentException>().WithMessage("*Row 1*");

            var unfitted = () => factory().Score([1d, 2d, 3d]);
            unfitted.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ScoringARowOfTheWrongWidthIsRejected()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(8, 100);
            var detector = new HbosDetector();
            detector.Fit(data, 1);
            var act = () => detector.Score([1d, 2d]);
            act.Should().Throw<ArgumentException>().WithMessage("*fitted on 6*");
        }

        [Fact]
        public void UnregularisedMahalanobisRejectsMoreVariablesThanRows()
        {
            var rng = new Random(1);

            var data = Enumerable.Range(0, 10).Select(_ => Enumerable.Range(0, 20)
                .Select(_ => AnomalyFixtures.Gaussian(rng)).ToArray()).ToArray();

            var act = () => new MahalanobisDetector(CovarianceRegularisation.None).Fit(data, 1);
            act.Should().Throw<ArgumentException>().WithMessage("*without regularisation*");
        }

        [Fact]
        public void ShrinkageMakesMahalanobisUsableWhenVariablesExceedRows()
        {
            var rng = new Random(1);

            var data = Enumerable.Range(0, 30).Select(_ => Enumerable.Range(0, 60)
                .Select(_ => AnomalyFixtures.Gaussian(rng)).ToArray()).ToArray();

            var detector = new MahalanobisDetector();
            detector.Fit(data, 1);
            var far = Enumerable.Repeat(8d, 60).ToArray();
            detector.Score(far).Should().BeGreaterThan(detector.Score(data[0]));
        }

        [Fact]
        public void GaussianMixtureRejectsTooFewRowsForItsComponents()
        {
            var rng = new Random(1);

            var data = Enumerable.Range(0, 20).Select(_ => Enumerable.Range(0, 10)
                .Select(_ => AnomalyFixtures.Gaussian(rng)).ToArray()).ToArray();

            var act = () => new GaussianMixtureDetector(3, true).Fit(data, 1);
            act.Should().Throw<ArgumentException>().WithMessage("*needs at least*");
        }

        [Fact]
        public void MahalanobisBeatsMarginalDetectorsWhenTheCorrelationIsBroken()
        {
            var (data, labels) = AnomalyFixtures.CorrelationBreak(3);
            var mahalanobis = new MahalanobisDetector();
            var hbos = new HbosDetector();
            mahalanobis.Fit(data, 1);
            hbos.Fit(data, 1);
            var mAuc = BinaryMetrics.RocAuc(AnomalyInput.ScoreAll(mahalanobis, data), labels);
            var hAuc = BinaryMetrics.RocAuc(AnomalyInput.ScoreAll(hbos, data), labels);
            mAuc.Should().BeGreaterThan(0.95);
            hAuc.Should().BeLessThan(0.8);
        }

        [Fact]
        public void IsolationForestPathLengthMatchesTheReferenceDefinition()
        {
            IsolationForestDetector.AveragePathLength(2).Should().BeApproximately(1.0, 1e-12);
            IsolationForestDetector.AveragePathLength(3).Should().BeApproximately(1.207392357589623, 1e-12);
            IsolationForestDetector.AveragePathLength(10).Should().BeApproximately(3.748880484475505, 1e-12);
            IsolationForestDetector.AveragePathLength(256).Should().BeApproximately(10.244770920119917, 1e-12);
            IsolationForestDetector.AveragePathLength(1000).Should().BeApproximately(12.969940887100174, 1e-12);
        }

        [Fact]
        public void HbosMatchesTheReferenceDefinition()
        {
            var detector = new HbosDetector(4);
            detector.Fit(AnomalyFixtures.ReferenceRows(), 1);
            var probes = AnomalyFixtures.ReferenceProbes().Append([0d, 0d, 0d]).ToArray();
            var expected = new[] { 0.13353139262452274, 7.154615356913663, 2.772588722239781, 0.8266785731844679 };

            for (var i = 0; i < expected.Length; i++)
            {
                detector.Score(probes[i]).Should().BeApproximately(expected[i], 1e-9);
            }
        }

        [Theory]
        [InlineData(CovarianceRegularisation.None, 0.2510404007172531, 59.18421269287603, 15.680554623828524)]
        [InlineData(CovarianceRegularisation.LedoitWolf, 0.1001873787333624, 24.189263230869678, 2.883269037206729)]
        [InlineData(CovarianceRegularisation.Ridge, 0.19412484729028004, 44.35642722179105, 7.476859743772722)]
        public void MahalanobisMatchesTheReferenceDefinition(CovarianceRegularisation mode, double a, double b,
            double c)
        {
            var detector = new MahalanobisDetector(mode);
            detector.Fit(AnomalyFixtures.ReferenceRows(), 1);
            var probes = AnomalyFixtures.ReferenceProbes();
            detector.Score(probes[0]).Should().BeApproximately(a, 1e-8);
            detector.Score(probes[1]).Should().BeApproximately(b, 1e-8);
            detector.Score(probes[2]).Should().BeApproximately(c, 1e-8);
        }

        [Fact]
        public void LedoitWolfIntensityMatchesTheReferenceDefinition()
        {
            var rows = AnomalyFixtures.ReferenceRows();
            var mean = Enumerable.Range(0, 3).Select(j => rows.Average(r => r[j])).ToArray();
            var s = new double[3, 3];

            for (var a = 0; a < 3; a++)
            for (var b = 0; b < 3; b++)
            {
                s[a, b] = rows.Sum(r => (r[a] - mean[a]) * (r[b] - mean[b])) / rows.Length;
            }

            var mu = (s[0, 0] + s[1, 1] + s[2, 2]) / 3d;

            CovarianceShrinkage.LedoitWolfIntensity(rows, mean, s, mu).Should()
                .BeApproximately(0.8911265328289509, 1e-9);
        }

        [Fact]
        public void LocalOutlierFactorMatchesTheReferenceDefinition()
        {
            var rows = AnomalyFixtures.ReferenceRows();
            var detector = new LocalOutlierFactorDetector(4);
            detector.Fit(rows, 1);
            var probes = AnomalyFixtures.ReferenceProbes();
            detector.Score(probes[0]).Should().BeApproximately(0.9911617961108151, 1e-9);
            detector.Score(probes[1]).Should().BeApproximately(4.69155276117694, 1e-9);
            detector.Score(probes[2]).Should().BeApproximately(1.0461727854322382, 1e-9);
            detector.Score(rows[0]).Should().BeApproximately(1.0261164210208245, 1e-9);
            detector.Score(rows[5]).Should().BeApproximately(1.0506019201859225, 1e-9);
            detector.Score(rows[15]).Should().BeApproximately(6.168904697894312, 1e-9);
        }

        [Fact]
        public void KnnDistanceMatchesTheReferenceDefinition()
        {
            var detector = new KnnDistanceDetector(4);
            detector.Fit(AnomalyFixtures.ReferenceRows(), 1);
            var probes = AnomalyFixtures.ReferenceProbes();
            detector.Score(probes[0]).Should().BeApproximately(0.5415656327497969, 1e-9);
            detector.Score(probes[1]).Should().BeApproximately(4.278683562357978, 1e-9);
            detector.Score(probes[2]).Should().BeApproximately(1.0524706271529585, 1e-9);
        }

        [Fact]
        public void KnnAndLofUseARowCapDeterministically()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(9);
            var first = new KnnDistanceDetector(5, 100);
            var second = new KnnDistanceDetector(5, 100);
            first.Fit(data, 5);
            second.Fit(data, 5);
            data.Select(first.Score).Should().Equal(data.Select(second.Score));
        }

        [Fact]
        public void PcaRetainsFewerComponentsThanVariablesAndScoresOffSubspacePoints()
        {
            var rng = new Random(2);

            var data = Enumerable.Range(0, 400).Select(_ =>
            {
                var a = AnomalyFixtures.Gaussian(rng);
                var b = AnomalyFixtures.Gaussian(rng);

                return new[]
                {
                    a, b, a + b + 0.05 * AnomalyFixtures.Gaussian(rng), a - b + 0.05 * AnomalyFixtures.Gaussian(rng)
                };
            }).ToArray();

            var detector = new PcaDetector(PcaScoreMode.Reconstruction, explainedVariance: 0.95);
            detector.Fit(data, 1);
            detector.RetainedComponents.Should().Be(2);
            detector.Score([1d, 1d, 2d, 0d]).Should().BeLessThan(detector.Score([1d, 1d, -2d, 3d]));
        }

        [Fact]
        public void FeatureBaggingDrawsDifferentSubspacesPerMemberAndIsReproducible()
        {
            var (data, _) = AnomalyFixtures.ClusterWithOutliers(9, 200, 12);
            var detector = new FeatureBaggingDetector(() => new HbosDetector(), 6, 4);
            detector.Fit(data, 5);
            detector.Subspaces.Should().HaveCount(6);
            detector.Subspaces.Should().OnlyContain(s => s.Length == 4);
            detector.Subspaces.Select(s => string.Join(",", s)).Distinct().Count().Should().BeGreaterThan(1);
            var again = new FeatureBaggingDetector(() => new HbosDetector(), 6, 4);
            again.Fit(data, 5);

            again.Subspaces.Select(s => string.Join(",", s)).Should()
                .Equal(detector.Subspaces.Select(s => string.Join(",", s)));
        }

        [Theory]
        [InlineData(ScoreCombination.Average)]
        [InlineData(ScoreCombination.Maximum)]
        [InlineData(ScoreCombination.RankAverage)]
        public void EnsembleCombinationsAllRankPlantedAnomaliesHigh(ScoreCombination combination)
        {
            var (data, labels) = AnomalyFixtures.ClusterWithOutliers(2, 500, 10);
            var detector = new FeatureBaggingDetector(() => new HbosDetector(), 10, 5, combination);
            detector.Fit(data, 1);
            BinaryMetrics.RocAuc(AnomalyInput.ScoreAll(detector, data), labels).Should().BeGreaterThan(0.9);
        }
    }
}