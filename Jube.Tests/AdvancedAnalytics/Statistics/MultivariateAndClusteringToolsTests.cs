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
using Accord.Math.Random;
using FluentAssertions;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering;
using Xunit;
using Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models;

namespace Jube.Test.AdvancedAnalytics.Statistics
{
    public class MultivariateAndClusteringToolsTests
    {
        private const double Tight = 1e-9;
        private const double Loose = 1e-6;
        private static readonly DistanceAndKernelTools distances = new();
        private static readonly ProjectionTools projections = new();
        private static readonly DiscriminantAndEmbeddingTools discriminants = new();
        private static readonly ClusteringTools clustering = new();
        private static readonly ClusterEvaluationTools evaluation = new();
        private static readonly NeighbourAndRuleTools neighbours = new();

        private static readonly double[][] twoSquares =
        [
            [0.0, 0.0], [0.0, 1.0], [1.0, 0.0], [1.0, 1.0],
            [10.0, 10.0], [10.0, 11.0], [11.0, 10.0], [11.0, 11.0]
        ];

        private static readonly int[] twoSquaresLabels = [0, 0, 0, 0, 1, 1, 1, 1];

        private static readonly double[][] smithPcaTutorial =
        [
            [2.5, 2.4], [0.5, 0.7], [2.2, 2.9], [1.9, 2.2], [3.1, 3.0],
            [2.3, 2.7], [2.0, 1.6], [1.0, 1.1], [1.5, 1.6], [1.1, 0.9]
        ];

        private static readonly double[][] perfectLine =
        [
            [1.0, 2.0], [2.0, 4.0], [3.0, 6.0], [4.0, 8.0], [5.0, 10.0]
        ];

        private static readonly double[][] unitCross =
        [
            [1.0, 0.0], [-1.0, 0.0], [0.0, 1.0], [0.0, -1.0]
        ];

        private static double[][] Blobs(int perBlob, double separation, int seed, int dimensions = 2)
        {
            var random = new Random(seed);
            var rows = new double[2 * perBlob][];

            for (var i = 0; i < rows.Length; i++)
            {
                rows[i] = new double[dimensions];

                for (var j = 0; j < dimensions; j++)
                {
                    var u1 = 1.0 - random.NextDouble();
                    var u2 = random.NextDouble();
                    var gaussian = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
                    rows[i][j] = gaussian + (i >= perBlob ? separation : 0.0);
                }
            }

            return rows;
        }

        private static int[] BlobLabels(int perBlob)
        {
            return [.. Enumerable.Range(0, 2 * perBlob).Select(i => i < perBlob ? 0 : 1)];
        }

        [Fact]
        [Trait("Covers", "ComputeVectorDistance")]
        public void VectorDistanceMatchesClosedFormsForContinuousMetrics()
        {
            var origin = new[] { 0.0, 0.0 };
            var point = new[] { 3.0, 4.0 };

            distances.ComputeVectorDistance(origin, point).Should().BeApproximately(5.0, Tight);

            distances.ComputeVectorDistance(origin, point, VectorDistanceMetric.SquaredEuclidean).Should()
                .BeApproximately(25.0, Tight);
            distances.ComputeVectorDistance(origin, point, VectorDistanceMetric.Manhattan).Should()
                .BeApproximately(7.0, Tight);
            distances.ComputeVectorDistance(origin, point, VectorDistanceMetric.Chebyshev).Should()
                .BeApproximately(4.0, Tight);

            distances.ComputeVectorDistance(origin, point, VectorDistanceMetric.Minkowski).Should()
                .BeApproximately(Math.Pow(91.0, 1.0 / 3.0), Tight);

            distances.ComputeVectorDistance(origin, point, VectorDistanceMetric.Minkowski, 1).Should()
                .BeApproximately(7.0, Tight);

            distances.ComputeVectorDistance([1.0, 2.0, 3.0], [2.0, 4.0, 6.0],
                VectorDistanceMetric.Cosine).Should().BeApproximately(0.0, Tight);

            distances.ComputeVectorDistance([1.0, 0.0], [0.0, 1.0], VectorDistanceMetric.Cosine)
                .Should().BeApproximately(1.0, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeVectorDistance")]
        public void VectorDistanceAngularIsTheArcCosineOfTheCosineSimilarity()
        {
            distances.ComputeVectorDistance([1.0, 0.0], [1.0, 0.0], VectorDistanceMetric.Angular)
                .Should().BeApproximately(0.0, Tight);

            distances.ComputeVectorDistance([1.0, 0.0], [1.0, 1.0], VectorDistanceMetric.Angular)
                .Should().BeApproximately(Math.PI / 4.0, Tight);
            distances.ComputeVectorDistance([1.0, 0.0], [0.0, 1.0], VectorDistanceMetric.Angular)
                .Should().BeApproximately(Math.PI / 2.0, Tight);

            distances.ComputeVectorDistance([1.0, 0.0], [-1.0, 0.0], VectorDistanceMetric.Angular)
                .Should().BeApproximately(Math.PI, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeVectorDistance")]
        public void VectorDistanceHellingerFollowsTheStandardDefinition()
        {
            distances.ComputeVectorDistance([0.5, 0.5], [1.0, 0.0], VectorDistanceMetric.Hellinger)
                .Should().BeApproximately(Math.Sqrt(1.0 - Math.Sqrt(0.5)), Tight);

            distances.ComputeVectorDistance([0.3, 0.7], [0.3, 0.7], VectorDistanceMetric.Hellinger)
                .Should().BeApproximately(0.0, Tight);
            distances.ComputeVectorDistance([1.0, 0.0], [0.0, 1.0], VectorDistanceMetric.Hellinger)
                .Should().BeApproximately(1.0, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeVectorDistance")]
        public void VectorDistanceBinaryMetricsMatchHandCounts()
        {
            var x = new[] { 1.0, 1.0, 0.0 };
            var y = new[] { 1.0, 0.0, 0.0 };

            distances.ComputeVectorDistance(x, y, VectorDistanceMetric.Jaccard).Should().BeApproximately(0.5, Tight);

            distances.ComputeVectorDistance(x, y, VectorDistanceMetric.Dice).Should()
                .BeApproximately(1.0 / 3.0, Tight);

            distances.ComputeVectorDistance([1.0, 0.0, 1.0], [1.0, 1.0, 0.0],
                VectorDistanceMetric.Hamming).Should().BeApproximately(2.0, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeVectorDistance")]
        public void VectorDistanceWeightedEuclideanUsesTheWeights()
        {
            distances.ComputeVectorDistance([0.0, 0.0], [3.0, 4.0],
                    VectorDistanceMetric.WeightedEuclidean, 3, [1.0, 0.0])
                .Should().BeApproximately(3.0, Tight);
            distances.ComputeVectorDistance([0.0, 0.0], [3.0, 4.0],
                    VectorDistanceMetric.WeightedSquaredEuclidean, 3, [1.0, 4.0])
                .Should().BeApproximately(73.0, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeVectorDistance")]
        public void VectorDistanceRejectsBadInputWithAnExplanation()
        {
            var lengths = () => distances.ComputeVectorDistance([1.0], [1.0, 2.0]);
            lengths.Should().Throw<ArgumentException>().And.ParamName.Should().Be("y");

            var nan = () => distances.ComputeVectorDistance([double.NaN], [1.0]);
            nan.Should().Throw<ArgumentException>().And.ParamName.Should().Be("x");

            var empty = () => distances.ComputeVectorDistance([], []);
            empty.Should().Throw<ArgumentException>();

            var order = () => distances.ComputeVectorDistance([1.0], [2.0],
                VectorDistanceMetric.Minkowski, 0.5);

            order.Should().Throw<ArgumentException>().And.ParamName.Should().Be("minkowskiOrder");

            var weights = () => distances.ComputeVectorDistance([1.0], [2.0],
                VectorDistanceMetric.WeightedEuclidean);

            weights.Should().Throw<ArgumentException>().And.ParamName.Should().Be("weights");

            distances.ComputeVectorDistance([1e150], [-1e150], VectorDistanceMetric.Manhattan)
                .Should().BeApproximately(2e150, 1e140);

            distances.ComputeVectorDistance([2.0, 2.0], [2.0, 2.0]).Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "ComputeDistanceMatrixWithinSet")]
        public void DistanceMatrixWithinSetIsSymmetricWithZeroDiagonal()
        {
            var result = distances.ComputeDistanceMatrixWithinSet([
                [0.0, 0.0], [3.0, 4.0], [6.0, 8.0]
            ]);

            result.Matrix[0].Should().Equal(0.0, 5.0, 10.0);
            result.Matrix[1].Should().Equal(5.0, 0.0, 5.0);
            result.Matrix[2].Should().Equal(10.0, 5.0, 0.0);
            result.Minimum.Should().Be(5.0);
            result.Maximum.Should().Be(10.0);
            result.Mean.Should().BeApproximately(20.0 / 3.0, Tight);
            result.Metric.Should().Be("Euclidean");
        }

        [Fact]
        [Trait("Covers", "ComputeDistanceMatrixWithinSet")]
        public void DistanceMatrixWithinSetHandlesSingleRowAndRejectsRaggedAndHugeInput()
        {
            var single = distances.ComputeDistanceMatrixWithinSet([[1.0, 2.0]]);
            single.Matrix.Should().HaveCount(1);
            single.Matrix[0].Should().Equal(0.0);
            double.IsNaN(single.Mean).Should().BeTrue();

            var ragged = () => distances.ComputeDistanceMatrixWithinSet([[1.0], [1.0, 2.0]]);
            ragged.Should().Throw<ArgumentException>().And.ParamName.Should().Be("data");

            var empty = () => distances.ComputeDistanceMatrixWithinSet([]);
            empty.Should().Throw<ArgumentException>();

            var tooMany = () => distances.ComputeDistanceMatrixWithinSet(
                [.. Enumerable.Range(0, 2001).Select(i => new[] { (double)i })]);

            tooMany.Should().Throw<ArgumentException>().WithMessage("*at most 2000*");

            var withNan = () => distances.ComputeDistanceMatrixWithinSet([[double.NaN]]);
            withNan.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeDistanceMatrixBetweenSets")]
        public void DistanceMatrixBetweenSetsIsRectangular()
        {
            var result = distances.ComputeDistanceMatrixBetweenSets(
                [[0.0, 0.0], [1.0, 0.0]],
                [[0.0, 3.0], [4.0, 0.0], [0.0, 0.0]],
                VectorDistanceMetric.Manhattan);

            result.Rows.Should().Be(2);
            result.Columns.Should().Be(3);
            result.Matrix[0].Should().Equal(3.0, 4.0, 0.0);
            result.Matrix[1].Should().Equal(4.0, 3.0, 1.0);
            result.Minimum.Should().Be(0.0);
            result.Maximum.Should().Be(4.0);
            result.Mean.Should().BeApproximately(15.0 / 6.0, Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeDistanceMatrixBetweenSets")]
        public void DistanceMatrixBetweenSetsRejectsColumnMismatchAndEmpty()
        {
            var mismatch = () => distances.ComputeDistanceMatrixBetweenSets([[1.0, 2.0]],
                [[1.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("setB");

            var empty = () => distances.ComputeDistanceMatrixBetweenSets([],
                [[1.0]]);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeKernelValue")]
        public void KernelValueMatchesClosedForms()
        {
            var x = new[] { 1.0, 2.0 };
            var y = new[] { 3.0, 4.0 };

            distances.ComputeKernelValue(x, y, SimilarityKernelKind.Linear).Should().BeApproximately(11.0, Tight);

            distances.ComputeKernelValue(x, y, SimilarityKernelKind.Linear, kernelConstant: 2).Should()
                .BeApproximately(13.0, Tight);
            distances.ComputeKernelValue(x, y, SimilarityKernelKind.Polynomial, kernelDegree: 2).Should()
                .BeApproximately(144.0, Tight);

            distances.ComputeKernelValue([0.0, 0.0], [3.0, 4.0],
                kernelSigma: 1).Should().BeApproximately(Math.Exp(-12.5), 1e-12);

            distances.ComputeKernelValue([0.0, 0.0], [3.0, 4.0], SimilarityKernelKind.Laplacian).Should()
                .BeApproximately(Math.Exp(-5.0), 1e-12);

            distances.ComputeKernelValue(x, y, SimilarityKernelKind.Sigmoid, kernelAlpha: 0.1, kernelConstant: 0)
                .Should()
                .BeApproximately(Math.Tanh(1.1), Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeKernelValue")]
        public void KernelValueSelfSimilarityOfRadialKernelsIsOneAndBadParametersAreRejected()
        {
            var x = new[] { 0.3, 0.7 };

            foreach (var kind in new[]
                     {
                         SimilarityKernelKind.Gaussian, SimilarityKernelKind.Laplacian, SimilarityKernelKind.Cauchy,
                         SimilarityKernelKind.Spherical,
                         SimilarityKernelKind.Circular, SimilarityKernelKind.Wave
                     })
            {
                var value = distances.ComputeKernelValue(x, x, kind, 2);
                double.IsFinite(value).Should().BeTrue(kind.ToString());
            }

            distances.ComputeKernelValue(x, x, kernelSigma: 2).Should()
                .BeApproximately(1.0, Tight);

            distances.ComputeKernelValue(x, x, SimilarityKernelKind.Laplacian, 2).Should()
                .BeApproximately(1.0, Tight);

            var sigma = () => distances.ComputeKernelValue(x, x, kernelSigma: 0);
            sigma.Should().Throw<ArgumentException>().And.ParamName.Should().Be("kernelSigma");

            var degree = () => distances.ComputeKernelValue(x, x, SimilarityKernelKind.Polynomial, kernelDegree: 0);
            degree.Should().Throw<ArgumentException>().And.ParamName.Should().Be("kernelDegree");

            var lengths = () => distances.ComputeKernelValue(x, [1.0]);
            lengths.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeKernelValue")]
        public void KernelValueEveryKernelKindEvaluatesFinitelyOnPositiveData()
        {
            var x = new[] { 0.2, 0.5, 0.3 };
            var y = new[] { 0.1, 0.6, 0.3 };

            foreach (var kind in Enum.GetValues<SimilarityKernelKind>())
            {
                var value = distances.ComputeKernelValue(x, y, kind, 1.5, 1.5,
                    kernelDegree: 2, kernelAlpha: 1, kernelBeta: 1);

                double.IsNaN(value).Should().BeFalse(kind.ToString());
            }
        }

        [Fact]
        [Trait("Covers", "ComputeKernelValue")]
        public void KernelValueDirichletHandlesEqualCoordinatesByTheirLimit()
        {
            distances.ComputeKernelValue([0.3, 0.5, 0.7], [0.3, 0.5, 0.7],
                    SimilarityKernelKind.Dirichlet)
                .Should().BeApproximately(Math.Pow(3.5, 3), 1e-9);

            distances.ComputeKernelValue([0.0], [1.0], SimilarityKernelKind.Dirichlet).Should()
                .BeApproximately(Math.Sin(1.5) / (2.0 * Math.Sin(0.5)), 1e-9);
        }

        [Fact]
        [Trait("Covers", "ComputeKernelGramMatrix")]
        public void KernelGramMatrixIsSymmetricAndNormalisationGivesUnitDiagonal()
        {
            var data = new[] { new[] { 1.0, 0.0 }, new[] { 0.0, 2.0 }, new[] { 1.0, 1.0 } };
            var raw = distances.ComputeKernelGramMatrix(data, SimilarityKernelKind.Linear);

            raw.Matrix[0].Should().Equal(1.0, 0.0, 1.0);
            raw.Matrix[1].Should().Equal(0.0, 4.0, 2.0);
            raw.Matrix[2].Should().Equal(1.0, 2.0, 2.0);
            raw.Diagonal.Should().Equal(1.0, 4.0, 2.0);

            var normalised = distances.ComputeKernelGramMatrix(data, SimilarityKernelKind.Linear, normalise: true);
            normalised.Diagonal.Should().Equal(1.0, 1.0, 1.0);
            normalised.Matrix[0][2].Should().BeApproximately(1.0 / Math.Sqrt(2.0), Tight);
            normalised.Matrix[1][2].Should().BeApproximately(2.0 / Math.Sqrt(8.0), Tight);
            normalised.Matrix[2][1].Should().BeApproximately(normalised.Matrix[1][2], Tight);
        }

        [Fact]
        [Trait("Covers", "ComputeKernelGramMatrix")]
        public void KernelGramMatrixGaussianDiagonalIsOneAndInputIsValidated()
        {
            var gram = distances.ComputeKernelGramMatrix(twoSquares, kernelSigma: 2);
            gram.Rows.Should().Be(8);
            gram.Diagonal.Should().OnlyContain(v => Math.Abs(v - 1.0) < Tight);
            gram.Matrix[0][7].Should().BeLessThan(gram.Matrix[0][1]);

            var empty = () => distances.ComputeKernelGramMatrix([]);
            empty.Should().Throw<ArgumentException>();

            var tooMany = () => distances.ComputeKernelGramMatrix(
                [.. Enumerable.Range(0, 1001).Select(i => new[] { (double)i })]);

            tooMany.Should().Throw<ArgumentException>().WithMessage("*at most 1000*");
        }

        [Fact]
        [Trait("Covers", "ComputeKernelCrossMatrix")]
        public void KernelCrossMatrixIsRectangularAndMatchesTheGramWhenSetsAreEqual()
        {
            var setA = new[] { new[] { 1.0, 0.0 }, new[] { 0.0, 2.0 } };
            var setB = new[] { new[] { 1.0, 1.0 }, new[] { 2.0, 0.0 }, new[] { 0.0, 0.0 } };
            var cross = distances.ComputeKernelCrossMatrix(setA, setB, SimilarityKernelKind.Linear);

            cross.Rows.Should().Be(2);
            cross.Columns.Should().Be(3);
            cross.Matrix[0].Should().Equal(1.0, 2.0, 0.0);
            cross.Matrix[1].Should().Equal(2.0, 0.0, 0.0);
            cross.Diagonal.Should().BeEmpty();

            var same = distances.ComputeKernelCrossMatrix(twoSquares, twoSquares);
            var gram = distances.ComputeKernelGramMatrix(twoSquares);
            same.Matrix[2][5].Should().BeApproximately(gram.Matrix[2][5], Tight);

            var mismatch = () => distances.ComputeKernelCrossMatrix(setA, [[1.0]]);
            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("setB");
        }

        [Fact]
        [Trait("Covers", "CompareHistogramsBhattacharyya")]
        public void HistogramOverlapMatchesClosedForms()
        {
            var identical = distances.CompareHistogramsBhattacharyya([2.0, 6.0, 2.0], [1.0, 3.0, 1.0]);
            identical.BhattacharyyaCoefficient.Should().BeApproximately(1.0, Tight);
            identical.BhattacharyyaDistance.Should().BeApproximately(0.0, 1e-7);
            identical.HellingerDistance.Should().BeApproximately(0.0, 1e-7);

            var disjoint = distances.CompareHistogramsBhattacharyya([1.0, 0.0], [0.0, 1.0]);
            disjoint.BhattacharyyaCoefficient.Should().BeApproximately(0.0, Tight);
            double.IsPositiveInfinity(disjoint.BhattacharyyaDistance).Should().BeTrue();
            disjoint.HellingerDistance.Should().BeApproximately(1.0, Tight);

            var partial = distances.CompareHistogramsBhattacharyya([5.0, 5.0], [1.0, 0.0]);
            partial.BhattacharyyaCoefficient.Should().BeApproximately(Math.Sqrt(0.5), Tight);
            partial.BhattacharyyaDistance.Should().BeApproximately(-Math.Log(Math.Sqrt(0.5)), Tight);
            partial.HellingerDistance.Should().BeApproximately(Math.Sqrt(1.0 - Math.Sqrt(0.5)), Tight);
        }

        [Fact]
        [Trait("Covers", "CompareHistogramsBhattacharyya")]
        public void HistogramOverlapRejectsNegativeEmptyAndMismatchedInput()
        {
            var negative = () => distances.CompareHistogramsBhattacharyya([-1.0, 2.0], [1.0, 1.0]);
            negative.Should().Throw<ArgumentException>().And.ParamName.Should().Be("histogramA");

            var zeros = () => distances.CompareHistogramsBhattacharyya([0.0, 0.0], [1.0, 1.0]);
            zeros.Should().Throw<ArgumentException>();

            var lengths = () => distances.CompareHistogramsBhattacharyya([1.0], [1.0, 1.0]);
            lengths.Should().Throw<ArgumentException>().And.ParamName.Should().Be("histogramB");

            var empty = () => distances.CompareHistogramsBhattacharyya([], []);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CompareSamplesBhattacharyyaGaussian")]
        public void GaussianBhattacharyyaEqualsOneEighthOfSquaredMeanShiftForEqualCovariance()
        {
            var shifted = unitCross.Select(r => new[] { r[0] + 2.0, r[1] }).ToArray();
            var extra = new[] { new[] { 0.0, 0.0 } };
            var a = unitCross.Concat(extra).ToArray();
            var b = shifted.Concat([[2.0, 0.0]]).ToArray();

            var distance = distances.CompareSamplesBhattacharyyaGaussian(a, b);
            var meanA = new[] { 0.0, 0.0 };
            var covarianceVariance = unitCross.Sum(r => r[0] * r[0]) / 4.0;

            distance.Should().BeApproximately(4.0 / covarianceVariance / 8.0, Loose);
            distances.CompareSamplesBhattacharyyaGaussian(a, a).Should().BeApproximately(0.0, Loose);
            meanA.Should().HaveCount(2);
        }

        [Fact]
        [Trait("Covers", "CompareSamplesBhattacharyyaGaussian")]
        public void GaussianBhattacharyyaRejectsSmallOrMismatchedSamples()
        {
            var small = () => distances.CompareSamplesBhattacharyyaGaussian(
                [[1.0, 2.0], [2.0, 3.0]], twoSquares);

            small.Should().Throw<ArgumentException>().And.ParamName.Should().Be("sampleA");

            var mismatch = () => distances.CompareSamplesBhattacharyyaGaussian(twoSquares,
                [[1.0], [2.0], [3.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("sampleB");
        }

        [Fact]
        [Trait("Covers", "ComputeStringDistance")]
        public void StringDistanceMatchesWikipediaExamples()
        {
            var levenshtein = distances.ComputeStringDistance("kitten", "sitting");
            levenshtein.Distance.Should().Be(3.0);
            levenshtein.Similarity.Should().BeApproximately(1.0 - 3.0 / 7.0, Tight);

            distances.ComputeStringDistance("kitten", "sitting", StringDistanceKind.NormalisedLevenshtein).Distance
                .Should().BeApproximately(3.0 / 7.0, Tight);

            distances.ComputeStringDistance("karolin", "kathrin", StringDistanceKind.Hamming).Distance.Should()
                .Be(3.0);

            distances.ComputeStringDistance("Kitten", "KITTEN").Distance.Should().Be(0.0);
            distances.ComputeStringDistance("Kitten", "KITTEN", ignoreCase: false).Distance.Should().Be(5.0);
        }

        [Fact]
        [Trait("Covers", "ComputeStringDistance")]
        public void StringDistanceHandlesEmptyStringsAndRejectsBadInput()
        {
            distances.ComputeStringDistance("", "").Similarity.Should().Be(1.0);
            distances.ComputeStringDistance("", "abc").Distance.Should().Be(3.0);
            distances.ComputeStringDistance("", "abc").Similarity.Should().Be(0.0);

            var hamming = () => distances.ComputeStringDistance("ab", "abc", StringDistanceKind.Hamming);
            hamming.Should().Throw<ArgumentException>();

            var nulls = () => distances.ComputeStringDistance(null!, "abc");
            nulls.Should().Throw<ArgumentException>().And.ParamName.Should().Be("first");

            var huge = () => distances.ComputeStringDistance(new string('a', 2001), "a");
            huge.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "FindClosestStringMatches")]
        public void ClosestStringMatchesAreRankedNearestFirstWithStableTies()
        {
            var matches = distances.FindClosestStringMatches("smith",
                ["smyth", "smith", "jones", "smithe"], 3);

            matches.Select(m => m.Candidate).Should().Equal("smith", "smyth", "smithe");
            matches[0].Distance.Should().Be(0.0);
            matches[0].Similarity.Should().Be(1.0);
            matches[1].Distance.Should().Be(1.0);
            matches[1].Similarity.Should().BeApproximately(0.8, Tight);
            matches[2].Index.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "FindClosestStringMatches")]
        public void ClosestStringMatchesRejectsEmptyCandidatesAndBadTop()
        {
            var empty = () => distances.FindClosestStringMatches("a", []);
            empty.Should().Throw<ArgumentException>().And.ParamName.Should().Be("candidates");

            var top = () => distances.FindClosestStringMatches("a", ["a"], 0);
            top.Should().Throw<ArgumentException>().And.ParamName.Should().Be("top");

            distances.FindClosestStringMatches("abc", ["abd", "abcd", "xyz"], 5, StringDistanceKind.Hamming)
                .Select(m => m.Candidate).Should().Equal("abd", "xyz");
        }

        [Fact]
        [Trait("Covers", "ComputeStringDistanceMatrix")]
        public void StringDistanceMatrixIsSymmetric()
        {
            var result = distances.ComputeStringDistanceMatrix(["kitten", "sitting", "kitten"]);

            result.Count.Should().Be(3);
            result.Matrix[0].Should().Equal(0.0, 3.0, 0.0);
            result.Matrix[1].Should().Equal(3.0, 0.0, 3.0);
            result.Matrix[2].Should().Equal(0.0, 3.0, 0.0);
            result.Kind.Should().Be("Levenshtein");
        }

        [Fact]
        [Trait("Covers", "ComputeStringDistanceMatrix")]
        public void StringDistanceMatrixRejectsEmptyAndNullEntries()
        {
            var empty = () => distances.ComputeStringDistanceMatrix([]);
            empty.Should().Throw<ArgumentException>();

            var nulls = () => distances.ComputeStringDistanceMatrix(["a", null!]);
            nulls.Should().Throw<ArgumentException>();

            distances.ComputeStringDistanceMatrix(["only"]).Matrix[0].Should().Equal(0.0);
        }

        [Fact]
        [Trait("Covers", "ComputeTokenSequenceEditDistance")]
        public void TokenSequenceEditDistanceCountsInsertionsDeletionsAndSubstitutions()
        {
            var missing = distances.ComputeTokenSequenceEditDistance(["login", "transfer", "logout"],
                ["login", "logout"]);
            missing.Distance.Should().Be(1.0);
            missing.Similarity.Should().BeApproximately(1.0 - 1.0 / 3.0, Tight);

            var reordered = distances.ComputeTokenSequenceEditDistance(["a", "b"], ["b", "a"]);
            reordered.Distance.Should().Be(2.0);

            distances.ComputeTokenSequenceEditDistance(["A"], ["a"]).Distance.Should().Be(0.0);
            distances.ComputeTokenSequenceEditDistance(["A"], ["a"], false).Distance.Should().Be(1.0);
        }

        [Fact]
        [Trait("Covers", "ComputeTokenSequenceEditDistance")]
        public void TokenSequenceEditDistanceHandlesEmptySequencesAndRejectsNull()
        {
            var both = distances.ComputeTokenSequenceEditDistance([], []);
            both.Distance.Should().Be(0.0);
            both.Similarity.Should().Be(1.0);

            var one = distances.ComputeTokenSequenceEditDistance([], ["x", "y"]);
            one.Distance.Should().Be(2.0);
            one.Similarity.Should().Be(0.0);

            var nulls = () => distances.ComputeTokenSequenceEditDistance(null!, ["x"]);
            nulls.Should().Throw<ArgumentException>().And.ParamName.Should().Be("sequenceA");

            var huge = () => distances.ComputeTokenSequenceEditDistance(new string[2001], ["x"]);
            huge.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeTokenSetSimilarity")]
        public void TokenSetSimilarityMatchesHandCounts()
        {
            var result = distances.ComputeTokenSetSimilarity(["a", "b", "c", "a"], ["B", "c", "d"]);

            result.SizeA.Should().Be(3);
            result.SizeB.Should().Be(3);
            result.Intersection.Should().Be(2);
            result.Union.Should().Be(4);
            result.Jaccard.Should().BeApproximately(0.5, Tight);
            result.Dice.Should().BeApproximately(4.0 / 6.0, Tight);
            result.Overlap.Should().BeApproximately(2.0 / 3.0, Tight);

            distances.ComputeTokenSetSimilarity(["a"], ["B"], false).Jaccard.Should()
                .Be(0.0);
        }

        [Fact]
        [Trait("Covers", "ComputeTokenSetSimilarity")]
        public void TokenSetSimilarityHandlesEmptySetsAndRejectsNull()
        {
            var both = distances.ComputeTokenSetSimilarity([], []);
            both.Jaccard.Should().Be(1.0);
            both.Union.Should().Be(0);

            var one = distances.ComputeTokenSetSimilarity([], ["a"]);
            one.Jaccard.Should().Be(0.0);
            one.Overlap.Should().Be(0.0);

            var nulls = () => distances.ComputeTokenSetSimilarity(null!, ["a"]);
            nulls.Should().Throw<ArgumentException>().And.ParamName.Should().Be("tokensA");
        }

        [Fact]
        [Trait("Covers", "RunKMeansClustering")]
        public void SeededRandomScopeRestoresThePreviousThreadSeedAfterAClusteringCall()
        {
            var before = Generator.ThreadSeed;

            clustering.RunKMeansClustering(twoSquares, 2, seed: 5);

            Generator.ThreadSeed.Should().Be(before);
        }

        private const double CornerSilhouette = 0.9234016980924623;
        private const double MeanTwoSquaresSilhouette = 0.9195260905673607;

        private static void ShouldSplitTheTwoSquares(int[] labels)
        {
            labels.Take(4).Distinct().Should().HaveCount(1);
            labels.Skip(4).Distinct().Should().HaveCount(1);
            labels[0].Should().NotBe(labels[4]);
        }

        [Fact]
        [Trait("Covers", "RunKMeansClustering")]
        public void KMeansSplitsTwoSquaresAndReportsHandComputedInertiaProfilesAndSilhouette()
        {
            var result = clustering.RunKMeansClustering(twoSquares, 2, standardise: false);

            ShouldSplitTheTwoSquares(result.Labels);
            result.Clusters.Should().Be(2);
            result.Algorithm.Should().Be("KMeans");
            result.Inertia.Should().BeApproximately(4.0, Tight);
            result.Converged.Should().BeTrue();
            result.Centroids.OrderBy(c => c[0]).First().Should().Equal(0.5, 0.5);
            result.Centroids.OrderBy(c => c[0]).Last().Should().Equal(10.5, 10.5);
            result.Profiles.Should().OnlyContain(p => p.Size == 4 && Math.Abs(p.Proportion - 0.5) < Tight);
            result.Profiles.Should().OnlyContain(p => Math.Abs(p.WithinSumOfSquares - 2.0) < Tight);
            result.Profiles.Should().OnlyContain(p => Math.Abs(p.MeanDistanceToCentroid - Math.Sqrt(0.5)) < Tight);
            result.Profiles.Should().OnlyContain(p => Math.Abs(p.MaximumDistanceToCentroid - Math.Sqrt(0.5)) < Tight);
            result.Profiles[0].StandardDeviations[0].Should().BeApproximately(Math.Sqrt(1.0 / 3.0), Tight);
            result.SilhouetteScore.Should().BeApproximately(MeanTwoSquaresSilhouette, 1e-9);
        }

        [Fact]
        [Trait("Covers", "RunKMeansClustering")]
        public void KMeansIsRepeatableForASeedAndReportsCentroidsInOriginalUnitsWhenStandardised()
        {
            var data = Blobs(40, 6.0, 12);
            var first = clustering.RunKMeansClustering(data, 2, seed: 4);
            var second = clustering.RunKMeansClustering(data, 2, seed: 4);

            second.Labels.Should().Equal(first.Labels);
            second.Inertia.Should().Be(first.Inertia);
            first.Centroids.Select(c => c[0]).Max().Should().BeGreaterThan(4.0);
            first.Centroids.Select(c => c[0]).Min().Should().BeLessThan(2.0);
            first.Profiles.Sum(p => p.Size).Should().Be(80);
        }

        [Fact]
        [Trait("Covers", "RunKMeansClustering")]
        public void KMeansHandlesSingleClusterAndRejectsBadArguments()
        {
            var one = clustering.RunKMeansClustering(twoSquares, 1, standardise: false);
            one.Clusters.Should().Be(1);
            double.IsNaN(one.SilhouetteScore).Should().BeTrue();
            one.Centroids[0].Should().Equal(5.5, 5.5);

            var many = () => clustering.RunKMeansClustering(twoSquares, 9);
            many.Should().Throw<ArgumentException>().And.ParamName.Should().Be("clusters");

            var iterations = () => clustering.RunKMeansClustering(twoSquares, 2, maximumIterations: 0);
            iterations.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maximumIterations");

            var empty = () => clustering.RunKMeansClustering([], 1);
            empty.Should().Throw<ArgumentException>();

            var nan = () => clustering.RunKMeansClustering([[double.NaN], [1.0]], 1);
            nan.Should().Throw<ArgumentException>();

            var constant = clustering.RunKMeansClustering(
                [[3.0, 3.0], [3.0, 3.0], [3.0, 3.0]], 1);
            constant.Inertia.Should().Be(0.0);

            var huge = clustering.RunKMeansClustering(
                [.. twoSquares.Select(r => new[] { r[0] * 1e12, r[1] * 1e12 })], 2);
            ShouldSplitTheTwoSquares(huge.Labels);

            var manhattan = clustering.RunKMeansClustering(twoSquares, 2, VectorDistanceMetric.Manhattan,
                ClusterSeeding.Uniform);
            ShouldSplitTheTwoSquares(manhattan.Labels);
        }

        [Fact]
        [Trait("Covers", "RunMiniBatchKMeansClustering")]
        public void MiniBatchKMeansSeparatesWellSeparatedBlobs()
        {
            var data = Blobs(60, 12.0, 6);
            var result = clustering.RunMiniBatchKMeansClustering(data, 2, 40, seed: 3);

            result.Algorithm.Should().Be("MiniBatchKMeans");
            result.Labels.Take(60).Distinct().Should().HaveCount(1);
            result.Labels.Skip(60).Distinct().Should().HaveCount(1);
            result.Labels[0].Should().NotBe(result.Labels[60]);
            clustering.RunMiniBatchKMeansClustering(data, 2, 40, seed: 3).Labels.Should().Equal(result.Labels);
        }

        [Fact]
        [Trait("Covers", "RunMiniBatchKMeansClustering")]
        public void MiniBatchKMeansRejectsBadBatchSize()
        {
            var batch = () => clustering.RunMiniBatchKMeansClustering(twoSquares, 2, 0);
            batch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("batchSize");

            var clusters = () => clustering.RunMiniBatchKMeansClustering(twoSquares, 0);
            clusters.Should().Throw<ArgumentException>().And.ParamName.Should().Be("clusters");
        }

        [Fact]
        [Trait("Covers", "RunBinarySplitClustering")]
        public void BinarySplitSeparatesTwoSquares()
        {
            var result = clustering.RunBinarySplitClustering(twoSquares, 2, standardise: false);

            ShouldSplitTheTwoSquares(result.Labels);
            result.Algorithm.Should().Be("BinarySplit");
            result.Inertia.Should().BeApproximately(4.0, Tight);

            clustering.RunBinarySplitClustering(twoSquares, 2, standardise: false).Labels.Should()
                .Equal(result.Labels);
        }

        [Fact]
        [Trait("Covers", "RunBinarySplitClustering")]
        public void BinarySplitRejectsBadArguments()
        {
            var tolerance = () => clustering.RunBinarySplitClustering(twoSquares, 2, tolerance: -1);
            tolerance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("tolerance");

            var ragged = () => clustering.RunBinarySplitClustering([[1.0], [1.0, 2.0]], 1);
            ragged.Should().Throw<ArgumentException>().And.ParamName.Should().Be("data");
        }

        [Fact]
        [Trait("Covers", "RunBalancedKMeansClustering")]
        public void BalancedKMeansGivesEqualSizedClusters()
        {
            var result = clustering.RunBalancedKMeansClustering(twoSquares, 2, standardise: false);

            result.Algorithm.Should().Be("BalancedKMeans");
            result.Profiles.Select(p => p.Size).Should().Equal(4, 4);
            ShouldSplitTheTwoSquares(result.Labels);

            var skewed = Blobs(30, 0.0, 2).Take(50).ToArray();
            var sizes = clustering.RunBalancedKMeansClustering(skewed, 5).Profiles.Select(p => p.Size).ToArray();
            sizes.Max().Should().BeLessOrEqualTo(11);
            sizes.Min().Should().BeGreaterOrEqualTo(9);
        }

        [Fact]
        [Trait("Covers", "RunBalancedKMeansClustering")]
        public void BalancedKMeansRejectsTooManyRowsAndClusters()
        {
            var rows = () => clustering.RunBalancedKMeansClustering(
                [.. Enumerable.Range(0, 5001).Select(i => new[] { (double)i })], 2);

            rows.Should().Throw<ArgumentException>().WithMessage("*at most 5000*");

            var clusters = () => clustering.RunBalancedKMeansClustering(twoSquares, 201);
            clusters.Should().Throw<ArgumentException>().And.ParamName.Should().Be("clusters");
        }

        [Fact]
        [Trait("Covers", "RunKMedoidsClustering")]
        public void KMedoidsUsesActualRowsAsCentres()
        {
            var result = clustering.RunKMedoidsClustering(twoSquares, 2, standardise: false);

            ShouldSplitTheTwoSquares(result.Labels);
            result.MedoidRows.Should().OnlyContain(r => r >= 0 && r < 8);
            result.MedoidRows.Count(r => r < 4).Should().Be(1);
            result.MedoidRows.Count(r => r >= 4).Should().Be(1);
            result.Medoids.Should().OnlyContain(m => m.Length == 2);
            result.TotalDistanceToMedoids.Should().BeApproximately(8.0, Tight);
            result.SilhouetteScore.Should().BeGreaterThan(0.9);
            result.Profiles.Sum(p => p.Size).Should().Be(8);
        }

        [Fact]
        [Trait("Covers", "RunKMedoidsClustering")]
        public void KMedoidsSupportsOtherSeedingAndRejectsBadInput()
        {
            var data = Blobs(20, 9.0, 14);

            var result = clustering.RunKMedoidsClustering(data, 2, VectorDistanceMetric.Euclidean,
                MedoidSeeding.KMeansPlusPlus, seed: 2);

            result.Labels.Take(20).Distinct().Should().HaveCount(1);
            result.Labels.Skip(20).Distinct().Should().HaveCount(1);

            var many = () => clustering.RunKMedoidsClustering(twoSquares, 10);
            many.Should().Throw<ArgumentException>().And.ParamName.Should().Be("clusters");

            var empty = () => clustering.RunKMedoidsClustering([], 1);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "RunKModesClustering")]
        public void KModesFindsCategoricalModesAndCountsMismatches()
        {
            var data = new[]
            {
                new[] { 0, 0, 0 }, new[] { 0, 0, 1 }, new[] { 0, 1, 0 }, new[] { 0, 0, 0 },
                new[] { 5, 5, 5 }, new[] { 5, 5, 6 }, new[] { 5, 6, 5 }, new[] { 5, 5, 5 }
            };

            var result = clustering.RunKModesClustering(data, 2);

            result.Labels.Take(4).Distinct().Should().HaveCount(1);
            result.Labels.Skip(4).Distinct().Should().HaveCount(1);
            result.Labels[0].Should().NotBe(result.Labels[4]);
            result.Modes[result.Labels[0]].Should().Equal(0, 0, 0);
            result.Modes[result.Labels[4]].Should().Equal(5, 5, 5);
            result.TotalMismatches.Should().Be(4.0);
            result.ClusterSizes.Should().Equal(4, 4);
        }

        [Fact]
        [Trait("Covers", "RunKModesClustering")]
        public void KModesRejectsRaggedEmptyAndTooManyClusters()
        {
            var ragged = () => clustering.RunKModesClustering([[1, 2], [1]], 1);
            ragged.Should().Throw<ArgumentException>().And.ParamName.Should().Be("data");

            var empty = () => clustering.RunKModesClustering([], 1);
            empty.Should().Throw<ArgumentException>();

            var many = () => clustering.RunKModesClustering([[1], [2]], 3);
            many.Should().Throw<ArgumentException>().And.ParamName.Should().Be("clusters");

            clustering.RunKModesClustering([[1], [1]], 1).TotalMismatches.Should().Be(0.0);
        }

        [Fact]
        [Trait("Covers", "RunMeanShiftClustering")]
        public void MeanShiftDiscoversTheNumberOfClusters()
        {
            var result = clustering.RunMeanShiftClustering(twoSquares, 3.0, MeanShiftKernelKind.Uniform,
                standardise: false);

            result.Clusters.Should().Be(2);
            ShouldSplitTheTwoSquares(result.Labels);
            result.ClusterSizes.Should().Equal(4, 4);
            result.Modes.Should().HaveCount(2);
            result.Bandwidth.Should().Be(3.0);
            result.Profiles.Should().HaveCount(2);

            clustering.RunMeanShiftClustering(twoSquares, 100.0, MeanShiftKernelKind.Uniform, standardise: false)
                .Clusters.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "RunMeanShiftClustering")]
        public void MeanShiftSupportsGaussianKernelAndRejectsBadBandwidth()
        {
            var data = Blobs(25, 14.0, 17);

            var gaussian = clustering.RunMeanShiftClustering(data,
                standardise: false);
            gaussian.Clusters.Should().BeGreaterOrEqualTo(2);

            var bandwidth = () => clustering.RunMeanShiftClustering(twoSquares, 0);
            bandwidth.Should().Throw<ArgumentException>().And.ParamName.Should().Be("bandwidth");

            var single = () => clustering.RunMeanShiftClustering([[1.0]]);
            single.Should().Throw<ArgumentException>();

            clustering.RunMeanShiftClustering(twoSquares, 1.0, MeanShiftKernelKind.Epanechnikov, standardise: false)
                .Clusters.Should().BeGreaterOrEqualTo(2);
        }

        [Fact]
        [Trait("Covers", "RunGaussianMixtureClustering")]
        public void GaussianMixtureRecoversTwoBlobsAndReportsConsistentInformationCriteria()
        {
            var data = Blobs(100, 10.0, 31);
            var result = clustering.RunGaussianMixtureClustering(data, 2);

            result.Labels.Take(100).Distinct().Should().HaveCount(1);
            result.Labels.Skip(100).Distinct().Should().HaveCount(1);
            result.Labels[0].Should().NotBe(result.Labels[100]);
            result.Weights.Should().OnlyContain(w => Math.Abs(w - 0.5) < 0.01);
            result.Means.Select(m => m[0]).OrderBy(v => v).First().Should().BeApproximately(0.0, 0.5);
            result.Means.Select(m => m[0]).OrderBy(v => v).Last().Should().BeApproximately(10.0, 0.5);
            result.ParameterCount.Should().Be(2 * 2 + 2 * 3 + 1);

            result.Bic.Should()
                .BeApproximately(result.ParameterCount * Math.Log(200) - 2.0 * result.LogLikelihood, Tight);

            result.Aic.Should().BeApproximately(2.0 * result.ParameterCount - 2.0 * result.LogLikelihood, Tight);
            result.Responsibilities.Should().HaveCount(200);
            result.Responsibilities.Should().OnlyContain(r => Math.Abs(r.Sum() - 1.0) < 1e-6);
            result.Covariances.Should().HaveCount(2);
            result.Covariances[0].Should().HaveCount(2);
            clustering.RunGaussianMixtureClustering(data, 2).Labels.Should().Equal(result.Labels);
        }

        [Fact]
        [Trait("Covers", "RunGaussianMixtureClustering")]
        public void GaussianMixtureDiagonalCountsFewerParametersAndValidatesInput()
        {
            var data = Blobs(50, 8.0, 3);

            var diagonal = clustering.RunGaussianMixtureClustering(data, 2, MixtureCovarianceKind.Diagonal,
                returnResponsibilities: false);

            diagonal.ParameterCount.Should().Be(2 * 2 + 2 * 2 + 1);
            diagonal.Responsibilities.Should().BeEmpty();

            var components = () => clustering.RunGaussianMixtureClustering(twoSquares, 5);
            components.Should().Throw<ArgumentException>().And.ParamName.Should().Be("components");

            var tolerance = () => clustering.RunGaussianMixtureClustering(data, 2, tolerance: 0);
            tolerance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("tolerance");

            var empty = () => clustering.RunGaussianMixtureClustering([], 1);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "SelectGaussianMixtureComponentsByBic")]
        public void MixtureSelectionPrefersTwoComponentsForTwoBlobs()
        {
            var data = Blobs(100, 10.0, 31);
            var result = clustering.SelectGaussianMixtureComponentsByBic(data, 1, 4);

            result.Table.Select(r => r.Components).Should().Equal(1, 2, 3, 4);
            result.BestComponentsByBic.Should().Be(2);
            result.Table.Should().OnlyContain(r => double.IsFinite(r.Bic) && double.IsFinite(r.Aic));
            result.Table[0].LogLikelihood.Should().BeLessThan(result.Table[1].LogLikelihood);

            result.Table[1].Bic.Should().BeApproximately(
                result.Table[1].ParameterCount * Math.Log(200) - 2.0 * result.Table[1].LogLikelihood, Tight);
        }

        [Fact]
        [Trait("Covers", "SelectGaussianMixtureComponentsByBic")]
        public void MixtureSelectionRejectsBadRange()
        {
            var range = () => clustering.SelectGaussianMixtureComponentsByBic(Blobs(20, 5.0, 1), 3, 2);
            range.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maxComponents");

            var wide = () => clustering.SelectGaussianMixtureComponentsByBic(Blobs(50, 5.0, 1), 1, 20);
            wide.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maxComponents");

            var empty = () => clustering.SelectGaussianMixtureComponentsByBic([]);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "DetectAnomaliesWithGaussianMixture")]
        public void MixtureAnomalyDetectionFlagsAPointFarFromEveryComponent()
        {
            var reference = Blobs(100, 10.0, 41);

            var result = clustering.DetectAnomaliesWithGaussianMixture(reference,
                [[5.0, 30.0], [0.0, 0.0], [10.0, 10.0]], 2);

            result.Rows.Should().Be(3);
            result.IsAnomaly[0].Should().BeTrue();
            result.IsAnomaly[1].Should().BeFalse();
            result.IsAnomaly[2].Should().BeFalse();
            result.AnomalyRows.Should().Equal(0);
            result.LogDensity[0].Should().BeLessThan(result.Threshold);
            result.LogDensity[1].Should().BeGreaterThan(result.Threshold);
        }

        [Fact]
        [Trait("Covers", "DetectAnomaliesWithGaussianMixture")]
        public void MixtureAnomalyDetectionScoresReferenceRowsByDefaultAndValidatesContamination()
        {
            var reference = Blobs(50, 8.0, 2);
            var result = clustering.DetectAnomaliesWithGaussianMixture(reference, components: 2, contamination: 0.05);

            result.Rows.Should().Be(100);
            result.AnomalyRows.Length.Should().BeInRange(1, 10);

            var contamination = () => clustering.DetectAnomaliesWithGaussianMixture(reference, contamination: 0.9);
            contamination.Should().Throw<ArgumentException>().And.ParamName.Should().Be("contamination");

            var mismatch = () => clustering.DetectAnomaliesWithGaussianMixture(reference, [[1.0]]);
            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("newData");
        }

        [Fact]
        [Trait("Covers", "SelectKMeansPlusPlusSeeds")]
        public void KMeansPlusPlusSeedsAreDistinctRepeatableRealRows()
        {
            var result = clustering.SelectKMeansPlusPlusSeeds(twoSquares, 2, false, 5);

            result.SeedRows.Should().HaveCount(2);
            result.SeedRows.Distinct().Should().HaveCount(2);
            result.Seeds.Select((s, i) => s.SequenceEqual(twoSquares[result.SeedRows[i]])).Should().OnlyContain(b => b);
            clustering.SelectKMeansPlusPlusSeeds(twoSquares, 2, false, 5).SeedRows.Should().Equal(result.SeedRows);

            var all = clustering.SelectKMeansPlusPlusSeeds(twoSquares, 8, false);
            all.SeedRows.OrderBy(v => v).Should().Equal(Enumerable.Range(0, 8));
        }

        [Fact]
        [Trait("Covers", "SelectKMeansPlusPlusSeeds")]
        public void KMeansPlusPlusSeedsRejectBadCount()
        {
            var seeds = () => clustering.SelectKMeansPlusPlusSeeds(twoSquares, 9);
            seeds.Should().Throw<ArgumentException>().And.ParamName.Should().Be("seeds");

            var empty = () => clustering.SelectKMeansPlusPlusSeeds([], 1);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeSilhouetteScores")]
        public void SilhouetteMatchesHandComputationForTwoSquares()
        {
            var result = evaluation.ComputeSilhouetteScores(twoSquares, twoSquaresLabels);

            result.Clusters.Should().Be(2);
            result.SampleScores[0].Should().BeApproximately(CornerSilhouette, 1e-9);
            result.SampleScores[7].Should().BeApproximately(CornerSilhouette, 1e-9);
            result.OverallScore.Should().BeApproximately(MeanTwoSquaresSilhouette, 1e-9);
            result.ClusterMeanScores.Should().OnlyContain(s => Math.Abs(s - MeanTwoSquaresSilhouette) < 1e-9);
            result.ClusterLabels.Should().Equal(0, 1);
        }

        [Fact]
        [Trait("Covers", "ComputeSilhouetteScores")]
        public void SilhouetteScoresSingletonsZeroAndFlagsMisassignedRowsNegative()
        {
            var data = new[] { new[] { 0.0 }, new[] { 1.0 }, new[] { 10.0 }, new[] { 11.0 }, new[] { 5.0 } };
            var result = evaluation.ComputeSilhouetteScores(data, [0, 0, 1, 1, 2]);

            result.SampleScores[4].Should().Be(0.0);

            var wrong = evaluation.ComputeSilhouetteScores([.. data.Take(4)], [0, 1, 0, 1]);
            wrong.OverallScore.Should().BeLessThan(0.0);

            var one = () => evaluation.ComputeSilhouetteScores(data, [1, 1, 1, 1, 1]);
            one.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labels");

            var length = () => evaluation.ComputeSilhouetteScores(data, [1, 2]);
            length.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labels");

            evaluation.ComputeSilhouetteScores(twoSquares, twoSquaresLabels, VectorDistanceMetric.Manhattan)
                .OverallScore.Should().BeGreaterThan(0.9);
        }

        [Fact]
        [Trait("Covers", "ComputeDaviesBouldinIndex")]
        public void DaviesBouldinIsOneTenthForTwoSquares()
        {
            evaluation.ComputeDaviesBouldinIndex(twoSquares, twoSquaresLabels).Should().BeApproximately(0.1, 1e-9);

            evaluation.ComputeDaviesBouldinIndex(twoSquares, [5, 5, 5, 5, 9, 9, 9, 9]).Should()
                .BeApproximately(0.1, 1e-9);
        }

        [Fact]
        [Trait("Covers", "ComputeDaviesBouldinIndex")]
        public void DaviesBouldinIsWorseForAScrambledLabelling()
        {
            var scrambled = evaluation.ComputeDaviesBouldinIndex(twoSquares, [0, 1, 0, 1, 0, 1, 0, 1]);
            scrambled.Should().BeGreaterThan(1.0);

            var one = () => evaluation.ComputeDaviesBouldinIndex(twoSquares, new int[8]);
            one.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeCalinskiHarabaszIndex")]
        public void CalinskiHarabaszIsSixHundredForTwoSquares()
        {
            evaluation.ComputeCalinskiHarabaszIndex(twoSquares, twoSquaresLabels).Should().BeApproximately(600.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "ComputeCalinskiHarabaszIndex")]
        public void CalinskiHarabaszIsInfiniteWhenClustersAreDegenerateAndNanWhenEveryRowIsAlone()
        {
            var points = new[] { new[] { 0.0 }, new[] { 0.0 }, new[] { 5.0 }, new[] { 5.0 } };

            double.IsPositiveInfinity(evaluation.ComputeCalinskiHarabaszIndex(points, [0, 0, 1, 1])).Should()
                .BeTrue();
            double.IsNaN(evaluation.ComputeCalinskiHarabaszIndex(points, [0, 1, 2, 3])).Should().BeTrue();

            var empty = () => evaluation.ComputeCalinskiHarabaszIndex([], []);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "EvaluateClusteringQuality")]
        public void QualitySummaryCombinesTheHandComputedMeasures()
        {
            var result = evaluation.EvaluateClusteringQuality(twoSquares, twoSquaresLabels);

            result.Inertia.Should().BeApproximately(4.0, Tight);
            result.BetweenClusterSumOfSquares.Should().BeApproximately(400.0, Tight);
            result.SilhouetteScore.Should().BeApproximately(MeanTwoSquaresSilhouette, 1e-9);
            result.DaviesBouldinIndex.Should().BeApproximately(0.1, 1e-9);
            result.CalinskiHarabaszIndex.Should().BeApproximately(600.0, 1e-9);
            result.Clusters.Should().Be(2);
            result.Rows.Should().Be(8);
        }

        [Fact]
        [Trait("Covers", "EvaluateClusteringQuality")]
        public void QualitySummaryRejectsSingleClusterAndMismatchedLabels()
        {
            var one = () => evaluation.EvaluateClusteringQuality(twoSquares, new int[8]);
            one.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labels");

            var length = () => evaluation.EvaluateClusteringQuality(twoSquares, [0, 1]);
            length.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labels");
        }

        [Fact]
        [Trait("Covers", "ComputeKMeansElbowTable")]
        public void ElbowTableIsMonotoneInInertiaAndAllMeasuresPreferTwoForTwoSquares()
        {
            var result = evaluation.ComputeKMeansElbowTable(twoSquares, 1, 4, standardise: false);

            result.Table.Select(r => r.Clusters).Should().Equal(1, 2, 3, 4);
            result.Table[0].Inertia.Should().BeApproximately(8.0 * 50.0 + 4.0, Tight);
            double.IsNaN(result.Table[0].SilhouetteScore).Should().BeTrue();
            result.Table[1].Inertia.Should().BeApproximately(4.0, Tight);
            result.Table[2].Inertia.Should().BeLessThan(result.Table[1].Inertia);
            result.Table[3].Inertia.Should().BeLessThan(result.Table[2].Inertia);
            result.Table[1].SilhouetteScore.Should().BeApproximately(MeanTwoSquaresSilhouette, 1e-9);
            result.Table[1].DaviesBouldinIndex.Should().BeApproximately(0.1, 1e-9);
            result.Table[1].CalinskiHarabaszIndex.Should().BeApproximately(600.0, 1e-9);
            result.BestClustersBySilhouette.Should().Be(2);
            result.BestClustersByDaviesBouldin.Should().Be(2);
            result.BestClustersByCalinskiHarabasz.Should().Be(2);
            result.ElbowClusters.Should().Be(2);
        }

        [Fact]
        [Trait("Covers", "ComputeKMeansElbowTable")]
        public void ElbowTableRejectsBadRanges()
        {
            var min = () => evaluation.ComputeKMeansElbowTable(twoSquares, 0, 3);
            min.Should().Throw<ArgumentException>().And.ParamName.Should().Be("minClusters");

            var max = () => evaluation.ComputeKMeansElbowTable(twoSquares, 3, 2);
            max.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maxClusters");

            var tooMany = () => evaluation.ComputeKMeansElbowTable(twoSquares, 2, 8);
            tooMany.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maxClusters");

            var tiny = () => evaluation.ComputeKMeansElbowTable([[1.0], [2.0]]);
            tiny.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputeKMeansGapStatistic")]
        public void GapStatisticFindsTwoClustersInTwoBlobsAndIsRepeatable()
        {
            var data = Blobs(30, 12.0, 23);
            var result = evaluation.ComputeKMeansGapStatistic(data, 1, 4, seed: 3);

            result.Table.Select(r => r.Clusters).Should().Equal(1, 2, 3, 4);
            result.BestClusters.Should().Be(2);
            result.Table[1].Gap.Should().BeGreaterThan(result.Table[0].Gap);
            result.Table.Should().OnlyContain(r => double.IsFinite(r.Gap) && r.StandardError >= 0.0);
            result.ReferenceSets.Should().Be(10);

            evaluation.ComputeKMeansGapStatistic(data, 1, 4, seed: 3).Table[1].Gap.Should()
                .Be(result.Table[1].Gap);
        }

        [Fact]
        [Trait("Covers", "ComputeKMeansGapStatistic")]
        public void GapStatisticRejectsBadArguments()
        {
            var references = () => evaluation.ComputeKMeansGapStatistic(twoSquares, referenceSets: 0);
            references.Should().Throw<ArgumentException>().And.ParamName.Should().Be("referenceSets");

            var range = () => evaluation.ComputeKMeansGapStatistic(twoSquares, 3, 2);
            range.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maxClusters");

            var empty = () => evaluation.ComputeKMeansGapStatistic([]);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ProfileClusters")]
        public void ProfilingReportsCentroidsSpreadAndStandardisedDifferences()
        {
            var result = evaluation.ProfileClusters(twoSquares, twoSquaresLabels, ["x", "y"], 1);

            result.Clusters.Should().Be(2);
            result.OverallMeans.Should().Equal(5.5, 5.5);
            result.Profiles[0].Centroid.Should().Equal(0.5, 0.5);
            result.Profiles[1].Centroid.Should().Equal(10.5, 10.5);
            result.Profiles[0].Size.Should().Be(4);
            result.Profiles[0].DistinguishingVariables.Should().HaveCount(1);
            var overallDeviation = result.OverallStandardDeviations[0];

            result.Profiles[0].DistinguishingVariables[0].StandardisedDifference.Should()
                .BeApproximately(-5.0 / overallDeviation, 1e-9);

            result.Profiles[1].DistinguishingVariables[0].StandardisedDifference.Should()
                .BeApproximately(5.0 / overallDeviation, 1e-9);

            result.Profiles[0].MeanDistanceToCentroid.Should().BeApproximately(Math.Sqrt(0.5), Tight);
        }

        [Fact]
        [Trait("Covers", "ProfileClusters")]
        public void ProfilingHandlesConstantColumnsSingletonsAndBadNames()
        {
            var data = new[] { new[] { 1.0, 7.0 }, new[] { 2.0, 7.0 }, new[] { 30.0, 7.0 } };
            var result = evaluation.ProfileClusters(data, [4, 4, 9], topVariables: 2);

            result.ClusterLabels.Should().Equal(4, 9);
            result.Variables.Should().Equal("V0", "V1");
            result.Profiles[1].StandardDeviations.Should().OnlyContain(v => double.IsNaN(v));

            result.Profiles[0].DistinguishingVariables.Single(e => e.Variable == "V1").StandardisedDifference.Should()
                .Be(0.0);

            var names = () => evaluation.ProfileClusters(data, [4, 4, 9], ["a"]);
            names.Should().Throw<ArgumentException>().And.ParamName.Should().Be("variableNames");

            var top = () => evaluation.ProfileClusters(data, [4, 4, 9], topVariables: 3);
            top.Should().Throw<ArgumentException>().And.ParamName.Should().Be("topVariables");
        }

        [Fact]
        [Trait("Covers", "AssignRowsToNearestCentroid")]
        public void AssignmentPicksTheNearestCentroidAndReportsAllDistances()
        {
            var result = evaluation.AssignRowsToNearestCentroid(
                [[1.0, 1.0], [9.0, 9.0], [4.0, 4.0]],
                [[0.0, 0.0], [10.0, 10.0]]);

            result.Labels.Should().Equal(0, 1, 0);
            result.DistancesToNearest[0].Should().BeApproximately(Math.Sqrt(2.0), Tight);
            result.DistancesToAll[1].Should().HaveCount(2);
            result.DistancesToAll[1][1].Should().BeApproximately(Math.Sqrt(2.0), Tight);

            evaluation.AssignRowsToNearestCentroid([[1.0, 1.0]],
                    [[0.0, 4.0], [3.0, 0.0]], VectorDistanceMetric.Manhattan).Labels.Should()
                .Equal(1);
        }

        [Fact]
        [Trait("Covers", "AssignRowsToNearestCentroid")]
        public void AssignmentRejectsMismatchedColumnsAndEmptyCentroids()
        {
            var mismatch = () => evaluation.AssignRowsToNearestCentroid([[1.0]],
                [[1.0, 2.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("newData");

            var empty = () => evaluation.AssignRowsToNearestCentroid([[1.0]], []);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "CompareClusterAssignments")]
        public void ClusterAgreementMatchesHandCountedPairs()
        {
            var identical = evaluation.CompareClusterAssignments([0, 0, 1, 1], [7, 7, 3, 3]);
            identical.RandIndex.Should().Be(1.0);
            identical.AdjustedRandIndex.Should().BeApproximately(1.0, Tight);
            identical.PairJaccard.Should().Be(1.0);

            var partial = evaluation.CompareClusterAssignments([0, 0, 1, 1], [0, 0, 1, 2]);
            partial.RandIndex.Should().BeApproximately(5.0 / 6.0, Tight);
            partial.AdjustedRandIndex.Should().BeApproximately((1.0 - 1.0 / 3.0) / (1.5 - 1.0 / 3.0), Tight);
            partial.PairJaccard.Should().BeApproximately(0.5, Tight);
            partial.ClustersA.Should().Be(2);
            partial.ClustersB.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "CompareClusterAssignments")]
        public void ClusterAgreementHandlesTrivialPartitionsAndRejectsBadInput()
        {
            evaluation.CompareClusterAssignments([1, 1, 1], [2, 2, 2]).AdjustedRandIndex.Should()
                .Be(1.0);

            evaluation.CompareClusterAssignments([0, 0, 1, 1], [0, 1, 0, 1]).AdjustedRandIndex
                .Should().BeLessThan(0.0);

            var length = () => evaluation.CompareClusterAssignments([0, 1], [0]);
            length.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labelsB");

            var single = () => evaluation.CompareClusterAssignments([0], [0]);
            single.Should().Throw<ArgumentException>();

            var nulls = () => evaluation.CompareClusterAssignments(null!, [0]);
            nulls.Should().Throw<ArgumentException>();
        }

        private static readonly double[][] lineOfPoints =
        [
            [0.0, 0.0], [1.0, 0.0], [2.0, 0.0], [5.0, 0.0], [10.0, 0.0]
        ];

        private static readonly string[][] textbookBaskets =
        [
            ["bread", "milk"],
            ["bread", "diaper", "beer", "eggs"],
            ["milk", "diaper", "beer", "cola"],
            ["bread", "milk", "diaper", "beer"],
            ["bread", "milk", "diaper", "cola"]
        ];

        [Theory]
        [InlineData(NeighbourSearchIndex.KdTree)]
        [InlineData(NeighbourSearchIndex.VpTree)]
        [InlineData(NeighbourSearchIndex.BruteForce)]
        [Trait("Covers", "FindKNearestNeighbours")]
        public void KNearestNeighboursAgreeAcrossEveryIndexForAQuery(NeighbourSearchIndex index)
        {
            var result = neighbours.FindKNearestNeighbours(lineOfPoints, [[0.9, 0.0]], 2, index: index);

            result.Should().HaveCount(1);
            result[0].Neighbours.Select(n => n.Row).Should().Equal(1, 0);
            result[0].Neighbours[0].Distance.Should().BeApproximately(0.1, Tight);
            result[0].Neighbours[1].Distance.Should().BeApproximately(0.9, Tight);
        }

        [Theory]
        [InlineData(NeighbourSearchIndex.KdTree)]
        [InlineData(NeighbourSearchIndex.VpTree)]
        [InlineData(NeighbourSearchIndex.BruteForce)]
        [Trait("Covers", "FindKNearestNeighbours")]
        public void KNearestNeighboursExcludeTheRowItselfWhenQueriesAreTheReference(NeighbourSearchIndex index)
        {
            var untied = new[]
            {
                new[] { 0.0, 0.0 }, new[] { 1.0, 0.0 }, new[] { 2.5, 0.0 }, new[] { 5.0, 0.0 }, new[] { 10.0, 0.0 }
            };

            var result = neighbours.FindKNearestNeighbours(untied, neighbours: 1, index: index);

            result.Select(r => r.Neighbours[0].Row).Should().Equal(1, 0, 1, 2, 3);
            result.Select(r => r.Neighbours[0].Distance).Should().Equal(1.0, 1.0, 1.5, 2.5, 5.0);
        }

        [Fact]
        [Trait("Covers", "FindKNearestNeighbours")]
        public void KNearestNeighboursHonourTheMetricAndMatchBruteForceOnRandomData()
        {
            var data = Blobs(60, 3.0, 19, 3);
            var queries = data.Take(10).Select(r => r.Select(v => v + 0.3).ToArray()).ToArray();

            foreach (var metric in new[]
                     {
                         NeighbourMetric.Euclidean, NeighbourMetric.Manhattan, NeighbourMetric.Chebyshev,
                         NeighbourMetric.Minkowski
                     })
            {
                var brute = neighbours.FindKNearestNeighbours(data, queries, 4, metric, 3,
                    NeighbourSearchIndex.BruteForce);

                var tree = neighbours.FindKNearestNeighbours(data, queries, 4, metric);

                var vantage =
                    neighbours.FindKNearestNeighbours(data, queries, 4, metric, 3, NeighbourSearchIndex.VpTree);

                for (var q = 0; q < queries.Length; q++)
                {
                    tree[q].Neighbours.Select(n => n.Row).Should()
                        .Equal(brute[q].Neighbours.Select(n => n.Row), metric.ToString());
                    vantage[q].Neighbours.Select(n => n.Row).Should()
                        .Equal(brute[q].Neighbours.Select(n => n.Row), metric.ToString());
                }
            }

            var manhattan = neighbours.FindKNearestNeighbours(lineOfPoints, [[0.0, 1.0]], 1,
                NeighbourMetric.Manhattan);

            manhattan[0].Neighbours[0].Distance.Should().BeApproximately(1.0, Tight);
        }

        [Fact]
        [Trait("Covers", "FindKNearestNeighbours")]
        public void KNearestNeighboursRejectBadArguments()
        {
            var tooMany = () => neighbours.FindKNearestNeighbours(lineOfPoints, neighbours: 5);
            tooMany.Should().Throw<ArgumentException>().And.ParamName.Should().Be("neighbours");

            var single = () => neighbours.FindKNearestNeighbours([[1.0]]);
            single.Should().Throw<ArgumentException>();

            var mismatch = () => neighbours.FindKNearestNeighbours(lineOfPoints, [[1.0]]);
            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("queryData");

            var empty = () => neighbours.FindKNearestNeighbours([]);
            empty.Should().Throw<ArgumentException>();

            neighbours.FindKNearestNeighbours(lineOfPoints, [[0.0, 0.0]])[0].Neighbours
                .Should().HaveCount(5);
        }

        [Fact]
        [Trait("Covers", "FindNeighboursWithinRadius")]
        public void RadiusSearchReturnsEveryRowWithinTheRadiusNearestFirst()
        {
            var result = neighbours.FindNeighboursWithinRadius(lineOfPoints, 2.0);

            result[0].Neighbours.Select(n => n.Row).Should().Equal(1, 2);
            result[3].Neighbours.Should().BeEmpty();
            result[4].Neighbours.Should().BeEmpty();
            result[1].Neighbours.Select(n => n.Row).Should().Equal(0, 2);

            var query = neighbours.FindNeighboursWithinRadius(lineOfPoints, 1.5, [[0.5, 0.0]]);
            query[0].Neighbours.Select(n => n.Row).Should().Equal(0, 1, 2);

            var capped = neighbours.FindNeighboursWithinRadius(lineOfPoints, 100.0, [[0.0, 0.0]], 2);
            capped[0].Neighbours.Select(n => n.Row).Should().Equal(0, 1);
        }

        [Fact]
        [Trait("Covers", "FindNeighboursWithinRadius")]
        public void RadiusSearchRejectsBadRadiusAndCap()
        {
            var radius = () => neighbours.FindNeighboursWithinRadius(lineOfPoints, 0.0);
            radius.Should().Throw<ArgumentException>().And.ParamName.Should().Be("radius");

            var cap = () => neighbours.FindNeighboursWithinRadius(lineOfPoints, 1.0, maxPerQuery: 0);
            cap.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maxPerQuery");

            var nan = () => neighbours.FindNeighboursWithinRadius(lineOfPoints, double.NaN);
            nan.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ClassifyByNearestNeighbours")]
        public void NeighbourClassificationVotesAmongTheNearestLabelledRows()
        {
            var labels = new[] { 3, 3, 3, 3, 8, 8, 8, 8 };

            var result = neighbours.ClassifyByNearestNeighbours(twoSquares, labels,
                [[0.4, 0.4], [10.4, 10.4], [4.0, 4.0]], 3);

            result.ClassLabels.Should().Equal(3, 8);
            result.PredictedLabels.Should().Equal(3, 8, 3);
            result.ClassVoteShares[0].Should().Equal(1.0, 0.0);
            result.ClassVoteShares[1].Should().Equal(0.0, 1.0);
            result.ClassVoteShares[2].Sum().Should().BeApproximately(1.0, Tight);

            var mixed = neighbours.ClassifyByNearestNeighbours(lineOfPoints, [0, 0, 1, 1, 1],
                [[0.9, 0.0]], 3);

            mixed.ClassVoteShares[0][0].Should().BeApproximately(2.0 / 3.0, Tight);
            mixed.ClassVoteShares[0][1].Should().BeApproximately(1.0 / 3.0, Tight);
            var near = 1.0 / 1.1 + 1.0 / 1.9;
            var far = 1.0 / 2.1;
            mixed.ClassDistanceWeightedShares[0][0].Should().BeApproximately(near / (near + far), 1e-9);
            mixed.PredictedLabels[0].Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "ClassifyByNearestNeighbours")]
        public void NeighbourClassificationRejectsBadArguments()
        {
            var k = () =>
                neighbours.ClassifyByNearestNeighbours(twoSquares, twoSquaresLabels, [[1.0, 1.0]], 9);

            k.Should().Throw<ArgumentException>().And.ParamName.Should().Be("neighbours");

            var labels = () =>
                neighbours.ClassifyByNearestNeighbours(twoSquares, [1], [[1.0, 1.0]]);

            labels.Should().Throw<ArgumentException>().And.ParamName.Should().Be("trainingLabels");

            var mismatch = () =>
                neighbours.ClassifyByNearestNeighbours(twoSquares, twoSquaresLabels, [[1.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("newData");
        }

        [Fact]
        [Trait("Covers", "DetectOutliersByNearestNeighbourDistance")]
        public void NearestNeighbourOutliersFlagOnlyTheIsolatedRow()
        {
            var grid = Enumerable.Range(0, 20).Select(i => new[] { (double)(i % 5), i / 5 })
                .Concat([[100.0, 100.0]]).ToArray();

            var result = neighbours.DetectOutliersByNearestNeighbourDistance(grid, 3);

            result.OutlierRows.Should().Equal(20);
            result.IsOutlier[20].Should().BeTrue();
            result.KthNeighbourDistance[20].Should().BeGreaterThan(100.0);
            result.KthNeighbourDistance[0].Should().BeApproximately(Math.Sqrt(2.0), Tight);
            result.MeanNeighbourDistance[0].Should().BeApproximately((1.0 + 1.0 + Math.Sqrt(2.0)) / 3.0, Tight);
            result.Threshold.Should().BeLessThan(20.0);
        }

        [Fact]
        [Trait("Covers", "DetectOutliersByNearestNeighbourDistance")]
        public void NearestNeighbourOutliersRejectBadArguments()
        {
            var tooManyNeighbours = () =>
                neighbours.DetectOutliersByNearestNeighbourDistance(lineOfPoints);

            tooManyNeighbours.Should().Throw<ArgumentException>().And.ParamName.Should().Be("neighbours");

            var multiplier = () =>
                neighbours.DetectOutliersByNearestNeighbourDistance(lineOfPoints, 2,
                    -1);

            multiplier.Should().Throw<ArgumentException>().And.ParamName.Should().Be("iqrMultiplier");

            var few = () =>
                neighbours.DetectOutliersByNearestNeighbourDistance([[1.0], [2.0]]);
            few.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "MineAssociationRules")]
        public void AssociationRulesReproduceTheTextbookMarketBasketExample()
        {
            var result = neighbours.MineAssociationRules(textbookBaskets, 0.4);

            result.Transactions.Should().Be(5);
            result.DistinctItems.Should().Be(6);

            var beer = result.FrequentItemsets.Single(f => f.Items.SequenceEqual(new[] { "beer" }));
            beer.Count.Should().Be(3);
            beer.Support.Should().BeApproximately(0.6, Tight);

            result.FrequentItemsets.Single(f => f.Items.SequenceEqual(new[] { "beer", "diaper", "milk" })).Count
                .Should().Be(2);

            result.FrequentItemsets.Should().NotContain(f => f.Items.Contains("eggs"));

            var rule = result.Rules.Single(r => r.Antecedent.OrderBy(a => a).SequenceEqual(["diaper", "milk"])
                                                && r.Consequent.SequenceEqual(new[] { "beer" }));

            rule.Support.Should().BeApproximately(0.4, Tight);
            rule.Confidence.Should().BeApproximately(2.0 / 3.0, Tight);
            rule.Lift.Should().BeApproximately(2.0 / 3.0 / 0.6, Tight);
            rule.Leverage.Should().BeApproximately(0.4 - 0.6 * 0.6, Tight);
            rule.AntecedentCount.Should().Be(3);
            rule.RuleCount.Should().Be(2);

            var beerToDiaper = result.Rules.Single(r => r.Antecedent.SequenceEqual(new[] { "beer" })
                                                        && r.Consequent.SequenceEqual(new[] { "diaper" }));

            beerToDiaper.Confidence.Should().BeApproximately(1.0, Tight);
            beerToDiaper.Lift.Should().BeApproximately(1.0 / 0.8, Tight);
        }

        [Fact]
        [Trait("Covers", "MineAssociationRules")]
        public void AssociationRulesRespectConfidenceAndCapsAndIgnoreRepeatedItems()
        {
            var strict = neighbours.MineAssociationRules(textbookBaskets, 0.4, 1.0);
            strict.Rules.Should().OnlyContain(r => Math.Abs(r.Confidence - 1.0) < Tight);

            var capped = neighbours.MineAssociationRules(textbookBaskets, 0.4, 0.0, 3, 4);
            capped.Rules.Should().HaveCount(3);
            capped.FrequentItemsets.Should().HaveCount(4);
            capped.Rules.Select(r => r.Lift).Should().BeInDescendingOrder();

            var repeated = neighbours.MineAssociationRules([
                ["a", "a", "b"], ["a", "b"], ["c", "", "c"]
            ], 0.5);

            repeated.FrequentItemsets.Single(f => f.Items.SequenceEqual(new[] { "a", "b" })).Count.Should().Be(2);
            repeated.DistinctItems.Should().Be(3);
        }

        [Fact]
        [Trait("Covers", "MineAssociationRules")]
        public void AssociationRulesRejectBadInputAndUnboundedSizes()
        {
            var empty = () => neighbours.MineAssociationRules([]);
            empty.Should().Throw<ArgumentException>().And.ParamName.Should().Be("transactions");

            var support = () => neighbours.MineAssociationRules(textbookBaskets, 0.0);
            support.Should().Throw<ArgumentException>().And.ParamName.Should().Be("minSupport");

            var confidence = () => neighbours.MineAssociationRules(textbookBaskets, 0.4, 2.0);
            confidence.Should().Throw<ArgumentException>().And.ParamName.Should().Be("minConfidence");

            var nulls = () => neighbours.MineAssociationRules(new string[][] { null! });
            nulls.Should().Throw<ArgumentException>();

            var wide = () => neighbours.MineAssociationRules([
                [.. Enumerable.Range(0, 13).Select(i => "item" + i)]
            ]);

            wide.Should().Throw<ArgumentException>().WithMessage("*at most 12*");

            var vocabulary = () => neighbours.MineAssociationRules(
                [.. Enumerable.Range(0, 201).Select(i => new[] { "item" + i })]);

            vocabulary.Should().Throw<ArgumentException>().WithMessage("*at most 200*");

            var none = neighbours.MineAssociationRules([[], []]);
            none.FrequentItemsets.Should().BeEmpty();
            none.Rules.Should().BeEmpty();
        }

        private const double SmithEigenvalueOne = 1.28402771;
        private const double SmithEigenvalueTwo = 0.0490833989;
        private const double SmithVectorLarge = 0.735178656;
        private const double SmithVectorSmall = 0.677873399;

        private static readonly double[][] twoClassData =
        [
            [1.0, 1.0], [2.0, 1.0], [1.0, 2.0], [2.0, 2.0],
            [5.0, 5.0], [6.0, 5.0], [5.0, 6.0], [6.0, 6.0]
        ];

        private static readonly int[] twoClassLabels = [10, 10, 10, 10, 20, 20, 20, 20];

        [Fact]
        [Trait("Covers", "RunPrincipalComponentAnalysis")]
        public void PrincipalComponentAnalysisReproducesTheSmithTutorialEigenAnalysis()
        {
            var result =
                projections.RunPrincipalComponentAnalysis(smithPcaTutorial, PrincipalComponentBasis.Covariance);

            result.Rows.Should().Be(10);
            result.Variables.Should().Be(2);
            result.Means[0].Should().BeApproximately(1.81, Tight);
            result.Means[1].Should().BeApproximately(1.91, Tight);
            result.Eigenvalues[0].Should().BeApproximately(SmithEigenvalueOne, 1e-7);
            result.Eigenvalues[1].Should().BeApproximately(SmithEigenvalueTwo, 1e-8);

            result.ExplainedVarianceRatio[0].Should()
                .BeApproximately(SmithEigenvalueOne / (SmithEigenvalueOne + SmithEigenvalueTwo), 1e-7);

            result.CumulativeExplainedVarianceRatio[1].Should().BeApproximately(1.0, Tight);
            result.Loadings[0][0].Should().BeApproximately(SmithVectorSmall, 1e-7);
            result.Loadings[0][1].Should().BeApproximately(SmithVectorLarge, 1e-7);
            result.Loadings[1][0].Should().BeApproximately(SmithVectorLarge, 1e-7);
            result.Loadings[1][1].Should().BeApproximately(-SmithVectorSmall, 1e-7);

            result.ScaledLoadings[0][0].Should()
                .BeApproximately(SmithVectorSmall * Math.Sqrt(SmithEigenvalueOne), 1e-7);

            result.ComponentsFor95Percent.Should().Be(1);
            result.ComponentsFor99Percent.Should().Be(2);
            result.KaiserComponentCount.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "RunPrincipalComponentAnalysis")]
        public void PrincipalComponentAnalysisScoresAreProjectionsWithNormalisedSign()
        {
            var result = projections.RunPrincipalComponentAnalysis(smithPcaTutorial, PrincipalComponentBasis.Covariance,
                1);

            var expected = smithPcaTutorial
                .Select(r => (r[0] - 1.81) * SmithVectorSmall + (r[1] - 1.91) * SmithVectorLarge).ToArray();

            result.ComponentsReturned.Should().Be(1);
            result.Loadings.Should().HaveCount(1);

            for (var i = 0; i < expected.Length; i++)
            {
                result.Scores[i].Should().HaveCount(1);
                result.Scores[i][0].Should().BeApproximately(expected[i], 1e-7);
            }

            result.Scores.Sum(s => s[0]).Should().BeApproximately(0.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "RunPrincipalComponentAnalysis")]
        public void PrincipalComponentAnalysisOfPerfectlyCorrelatedDataHasOneComponent()
        {
            var covariance = projections.RunPrincipalComponentAnalysis(perfectLine, PrincipalComponentBasis.Covariance);
            covariance.Eigenvalues[0].Should().BeApproximately(12.5, 1e-9);
            covariance.Eigenvalues[1].Should().BeApproximately(0.0, 1e-9);
            covariance.Loadings[0][0].Should().BeApproximately(1.0 / Math.Sqrt(5.0), 1e-9);
            covariance.Loadings[0][1].Should().BeApproximately(2.0 / Math.Sqrt(5.0), 1e-9);

            var correlation = projections.RunPrincipalComponentAnalysis(perfectLine);
            correlation.Basis.Should().Be("Correlation");
            correlation.Eigenvalues[0].Should().BeApproximately(2.0, 1e-9);
            correlation.Eigenvalues[1].Should().BeApproximately(0.0, 1e-9);
            correlation.Loadings[0][0].Should().BeApproximately(Math.Sqrt(0.5), 1e-9);
            correlation.Communalities.Should().OnlyContain(c => Math.Abs(c - 1.0) < 1e-9);
            correlation.KaiserComponentCount.Should().Be(1);
            correlation.ComponentsFor80Percent.Should().Be(1);
        }

        [Fact]
        [Trait("Covers", "RunPrincipalComponentAnalysis")]
        public void PrincipalComponentAnalysisOnTheCorrelationBasisIgnoresColumnScale()
        {
            var scaled = perfectLine.Select(r => new[] { r[0] * 1e12, r[1] }).ToArray();
            var baseline = projections.RunPrincipalComponentAnalysis(smithPcaTutorial);

            var stretched = projections.RunPrincipalComponentAnalysis(
                [.. smithPcaTutorial.Select(r => new[] { r[0] * 1e9, r[1] })]);

            stretched.Eigenvalues[0].Should().BeApproximately(baseline.Eigenvalues[0], 1e-9);
            stretched.Eigenvalues[1].Should().BeApproximately(baseline.Eigenvalues[1], 1e-9);
            projections.RunPrincipalComponentAnalysis(scaled).Eigenvalues[0].Should().BeApproximately(2.0, 1e-9);
        }

        [Fact]
        [Trait("Covers", "RunPrincipalComponentAnalysis")]
        public void PrincipalComponentAnalysisRejectsBadInputWithExplanations()
        {
            var single = () => projections.RunPrincipalComponentAnalysis([[1.0, 2.0]]);
            single.Should().Throw<ArgumentException>().And.ParamName.Should().Be("data");

            var nan = () =>
                projections.RunPrincipalComponentAnalysis([[1.0, double.NaN], [2.0, 3.0]]);

            nan.Should().Throw<ArgumentException>().WithMessage("*not a finite number*");

            var constant = () => projections.RunPrincipalComponentAnalysis(
                [[1.0, 5.0], [2.0, 5.0], [3.0, 5.0]]);

            constant.Should().Throw<ArgumentException>().WithMessage("*constant*");

            projections.RunPrincipalComponentAnalysis(
                [[1.0, 5.0], [2.0, 5.0], [3.0, 5.0]],
                PrincipalComponentBasis.Covariance).Eigenvalues[1].Should().BeApproximately(0.0, 1e-9);

            var components = () => projections.RunPrincipalComponentAnalysis(smithPcaTutorial, components: 3);
            components.Should().Throw<ArgumentException>().And.ParamName.Should().Be("components");

            var empty = () => projections.RunPrincipalComponentAnalysis([]);
            empty.Should().Throw<ArgumentException>();

            projections.RunPrincipalComponentAnalysis(smithPcaTutorial, returnScores: false).Scores.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "InterpretPrincipalComponents")]
        public void InterpretPrincipalComponentsListsLargestLoadingsAndCommunalities()
        {
            var result = projections.InterpretPrincipalComponents(smithPcaTutorial, ["x", "y"],
                PrincipalComponentBasis.Covariance, 2, 1);

            result.Components.Should().HaveCount(2);
            result.Components[0].TopVariables.Should().ContainSingle();
            result.Components[0].TopVariables[0].Variable.Should().Be("y");
            result.Components[0].TopVariables[0].Loading.Should().BeApproximately(SmithVectorLarge, 1e-7);

            result.Components[0].TopVariables[0].SquaredLoadingShare.Should()
                .BeApproximately(SmithVectorLarge * SmithVectorLarge, 1e-7);

            result.Components[1].TopVariables[0].Variable.Should().Be("x");
            result.Components[0].Eigenvalue.Should().BeApproximately(SmithEigenvalueOne, 1e-7);
            result.Communalities.Should().OnlyContain(c => Math.Abs(c - 1.0) < 1e-9);
            result.Variables.Should().Equal("x", "y");
        }

        [Fact]
        [Trait("Covers", "InterpretPrincipalComponents")]
        public void InterpretPrincipalComponentsDefaultsNamesAndRejectsBadNames()
        {
            var defaults = projections.InterpretPrincipalComponents(smithPcaTutorial);
            defaults.Variables.Should().Equal("V0", "V1");

            var duplicate = () => projections.InterpretPrincipalComponents(smithPcaTutorial, ["a", "a"]);
            duplicate.Should().Throw<ArgumentException>().And.ParamName.Should().Be("variableNames");

            var count = () => projections.InterpretPrincipalComponents(smithPcaTutorial, ["a"]);
            count.Should().Throw<ArgumentException>().And.ParamName.Should().Be("variableNames");

            var top = () => projections.InterpretPrincipalComponents(smithPcaTutorial, topVariables: 0);
            top.Should().Throw<ArgumentException>().And.ParamName.Should().Be("topVariables");
        }

        [Fact]
        [Trait("Covers", "ProjectOntoPrincipalComponents")]
        public void ProjectingNewRowsUsesTheReferenceMeansAndComponents()
        {
            var scores = projections.ProjectOntoPrincipalComponents(smithPcaTutorial,
                [[1.81, 1.91], [2.81, 1.91]], PrincipalComponentBasis.Covariance, 1);

            scores[0][0].Should().BeApproximately(0.0, 1e-9);
            scores[1][0].Should().BeApproximately(SmithVectorSmall, 1e-7);
        }

        [Fact]
        [Trait("Covers", "ProjectOntoPrincipalComponents")]
        public void ProjectingNewRowsRejectsColumnMismatchAndNan()
        {
            var mismatch = () => projections.ProjectOntoPrincipalComponents(smithPcaTutorial, [[1.0]]);
            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("newData");

            var nan = () =>
                projections.ProjectOntoPrincipalComponents(smithPcaTutorial, [[double.NaN, 1.0]]);
            nan.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "WhitenDataWithPca")]
        public void WhiteningGivesUncorrelatedUnitVarianceColumns()
        {
            var result = projections.WhitenDataWithPca(smithPcaTutorial, PrincipalComponentBasis.Covariance);

            result.Components.Should().Be(2);
            result.ColumnStandardDeviations.Should().OnlyContain(s => Math.Abs(s - 1.0) < 1e-9);
            var covariance = result.WhitenedData.Sum(r => r[0] * r[1]) / (result.WhitenedData.Length - 1);
            covariance.Should().BeApproximately(0.0, 1e-9);
            result.Eigenvalues[0].Should().BeApproximately(SmithEigenvalueOne, 1e-7);
        }

        [Fact]
        [Trait("Covers", "WhitenDataWithPca")]
        public void WhiteningDropsNegligibleComponentsOfCollinearData()
        {
            var result = projections.WhitenDataWithPca(perfectLine, PrincipalComponentBasis.Covariance);

            result.Components.Should().Be(1);
            result.WhitenedData[0].Should().HaveCount(1);
            result.ColumnStandardDeviations[0].Should().BeApproximately(1.0, 1e-9);

            var constant = () => projections.WhitenDataWithPca(
                [[1.0, 1.0], [1.0, 1.0], [1.0, 1.0]],
                PrincipalComponentBasis.Covariance);
            constant.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "ComputePrincipalComponentDiagnostics")]
        public void DiagnosticsMatchHandComputedHotellingAndPredictionError()
        {
            var result = projections.ComputePrincipalComponentDiagnostics(smithPcaTutorial,
                basis: PrincipalComponentBasis.Covariance, components: 1);

            var first = smithPcaTutorial[0];
            var score = (first[0] - 1.81) * SmithVectorSmall + (first[1] - 1.91) * SmithVectorLarge;
            var total = Math.Pow(first[0] - 1.81, 2) + Math.Pow(first[1] - 1.91, 2);

            result.HotellingT2[0].Should().BeApproximately(score * score / SmithEigenvalueOne, 1e-6);
            result.SquaredPredictionError[0].Should().BeApproximately(total - score * score, 1e-6);
            result.HotellingT2Cutoff.Should().BeApproximately(6.6348966, 1e-5);
            result.ComponentsUsed.Should().Be(1);
            result.IsAbnormalT2.Should().OnlyContain(f => !f);
        }

        [Fact]
        [Trait("Covers", "ComputePrincipalComponentDiagnostics")]
        public void DiagnosticsFlagAFarAwayRowAndValidateInput()
        {
            var result = projections.ComputePrincipalComponentDiagnostics(smithPcaTutorial,
                [[20.0, 20.0], [1.81, 1.91]], PrincipalComponentBasis.Covariance);

            result.IsAbnormalT2[0].Should().BeTrue();
            result.IsAbnormalT2[1].Should().BeFalse();
            result.HotellingT2[1].Should().BeApproximately(0.0, 1e-9);
            result.HotellingT2Cutoff.Should().BeApproximately(9.2103404, 1e-5);

            var significance = () =>
                projections.ComputePrincipalComponentDiagnostics(smithPcaTutorial, significance: 0.9);

            significance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("significance");

            var mismatch = () =>
                projections.ComputePrincipalComponentDiagnostics(smithPcaTutorial, [[1.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("testData");
        }

        [Fact]
        [Trait("Covers", "ReconstructFromPrincipalComponents")]
        public void ReconstructionFromOneComponentMatchesHandProjectionAndIsExactForCollinearData()
        {
            var result = projections.ReconstructFromPrincipalComponents(smithPcaTutorial,
                basis: PrincipalComponentBasis.Covariance, components: 1);

            var first = smithPcaTutorial[0];
            var score = (first[0] - 1.81) * SmithVectorSmall + (first[1] - 1.91) * SmithVectorLarge;

            result.Reconstructed[0][0].Should().BeApproximately(1.81 + score * SmithVectorSmall, 1e-7);
            result.Reconstructed[0][1].Should().BeApproximately(1.91 + score * SmithVectorLarge, 1e-7);
            result.SquaredError.Sum().Should().BeApproximately(9.0 * SmithEigenvalueTwo, 1e-6);

            result.RetainedVarianceRatio.Should()
                .BeApproximately(SmithEigenvalueOne / (SmithEigenvalueOne + SmithEigenvalueTwo), 1e-7);

            var exact = projections.ReconstructFromPrincipalComponents(perfectLine,
                basis: PrincipalComponentBasis.Covariance);

            for (var i = 0; i < perfectLine.Length; i++)
            {
                exact.Reconstructed[i][0].Should().BeApproximately(perfectLine[i][0], 1e-9);
                exact.Reconstructed[i][1].Should().BeApproximately(perfectLine[i][1], 1e-9);
                exact.SquaredError[i].Should().BeApproximately(0.0, 1e-9);
            }

            var standardised = projections.ReconstructFromPrincipalComponents(perfectLine);
            standardised.SquaredError.Should().OnlyContain(e => e < 1e-9);
        }

        [Fact]
        [Trait("Covers", "ReconstructFromPrincipalComponents")]
        public void ReconstructionScoresNewRowsAndRejectsBadInput()
        {
            var result = projections.ReconstructFromPrincipalComponents(perfectLine,
                [[3.0, 6.0], [3.0, 0.0]],
                PrincipalComponentBasis.Covariance);

            result.SquaredError[0].Should().BeApproximately(0.0, 1e-9);
            result.SquaredError[1].Should().BeGreaterThan(1.0);

            var components = () => projections.ReconstructFromPrincipalComponents(perfectLine, components: 3);
            components.Should().Throw<ArgumentException>().And.ParamName.Should().Be("components");

            var mismatch = () => projections.ReconstructFromPrincipalComponents(perfectLine, [[1.0]]);
            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("testData");
        }

        [Fact]
        [Trait("Covers", "RunKernelPrincipalComponentAnalysis")]
        public void KernelPrincipalComponentAnalysisWithLinearKernelMatchesOrdinaryScores()
        {
            var pca = projections.RunPrincipalComponentAnalysis(smithPcaTutorial, PrincipalComponentBasis.Covariance,
                1);

            var kernel = projections.RunKernelPrincipalComponentAnalysis(smithPcaTutorial, SimilarityKernelKind.Linear,
                components: 1, basis: PrincipalComponentBasis.Covariance);

            kernel.ComponentsReturned.Should().Be(1);
            kernel.Eigenvalues[0].Should().BeApproximately(9.0 * SmithEigenvalueOne, 1e-6);

            for (var i = 0; i < 10; i++)
            {
                Math.Abs(kernel.Scores[i][0]).Should().BeApproximately(Math.Abs(pca.Scores[i][0]), 1e-6);
            }

            kernel.ExplainedVarianceRatio[0].Should()
                .BeApproximately(SmithEigenvalueOne / (SmithEigenvalueOne + SmithEigenvalueTwo), 1e-6);
        }

        [Fact]
        [Trait("Covers", "RunKernelPrincipalComponentAnalysis")]
        public void KernelPrincipalComponentAnalysisWithGaussianKernelSeparatesTwoBlobsOnTheFirstComponent()
        {
            var data = Blobs(20, 8.0, 3);

            var result = projections.RunKernelPrincipalComponentAnalysis(data,
                kernelSigma: 3,
                components: 2);

            var first = result.Scores.Take(20).Select(s => s[0]).Average();
            var second = result.Scores.Skip(20).Select(s => s[0]).Average();

            result.Rows.Should().Be(40);
            result.Scores.Should().OnlyContain(s => s.All(double.IsFinite));
            Math.Abs(first - second).Should().BeGreaterThan(0.5);
            (first * second).Should().BeLessThan(0.0);
        }

        [Fact]
        [Trait("Covers", "RunKernelPrincipalComponentAnalysis")]
        public void KernelPrincipalComponentAnalysisRejectsBadKernelParametersAndSize()
        {
            var sigma = () => projections.RunKernelPrincipalComponentAnalysis(smithPcaTutorial, kernelSigma: -1);
            sigma.Should().Throw<ArgumentException>().And.ParamName.Should().Be("kernelSigma");

            var small = () => projections.RunKernelPrincipalComponentAnalysis([[1.0], [2.0]]);
            small.Should().Throw<ArgumentException>();

            var components = () => projections.RunKernelPrincipalComponentAnalysis(smithPcaTutorial, components: 0);
            components.Should().Throw<ArgumentException>().And.ParamName.Should().Be("components");

            var constant = () => projections.RunKernelPrincipalComponentAnalysis(
                [[1.0, 2.0], [1.0, 3.0], [1.0, 4.0]]);

            constant.Should().Throw<ArgumentException>().WithMessage("*constant*");
        }

        private static (double[][] Observed, double[][] Sources) MixedUniformSources(int rows, int seed)
        {
            var random = new Random(seed);
            var sources = new double[rows][];
            var observed = new double[rows][];

            for (var i = 0; i < rows; i++)
            {
                var s1 = random.NextDouble() * 2.0 - 1.0;
                var s2 = random.NextDouble() * 2.0 - 1.0;
                sources[i] = [s1, s2];
                observed[i] = [0.6 * s1 + 0.4 * s2, 0.3 * s1 + 0.7 * s2];
            }

            return (observed, sources);
        }

        private static double AbsoluteCorrelation(double[] a, double[] b)
        {
            var meanA = a.Average();
            var meanB = b.Average();
            var covariance = a.Zip(b, (x, y) => (x - meanA) * (y - meanB)).Sum();
            var varianceA = a.Sum(x => (x - meanA) * (x - meanA));
            var varianceB = b.Sum(y => (y - meanB) * (y - meanB));

            return Math.Abs(covariance / Math.Sqrt(varianceA * varianceB));
        }

        [Fact]
        [Trait("Covers", "RunIndependentComponentAnalysis")]
        public void IndependentComponentAnalysisRecoversMixedUniformSources()
        {
            var (observed, sources) = MixedUniformSources(600, 11);
            var result = projections.RunIndependentComponentAnalysis(observed, seed: 3);

            result.Components.Should().Be(2);
            result.Sources.Should().HaveCount(600);

            var estimated = new[]
                { result.Sources.Select(r => r[0]).ToArray(), result.Sources.Select(r => r[1]).ToArray() };

            var truth = new[] { sources.Select(r => r[0]).ToArray(), sources.Select(r => r[1]).ToArray() };

            var direct = AbsoluteCorrelation(estimated[0], truth[0]) + AbsoluteCorrelation(estimated[1], truth[1]);
            var swapped = AbsoluteCorrelation(estimated[0], truth[1]) + AbsoluteCorrelation(estimated[1], truth[0]);

            Math.Max(direct, swapped).Should().BeGreaterThan(1.9);
            result.MixingMatrix.Should().HaveCount(2);
            result.DemixingMatrix.Should().HaveCount(2);
            result.Means[0].Should().BeApproximately(observed.Average(r => r[0]), 1e-9);
        }

        [Fact]
        [Trait("Covers", "RunIndependentComponentAnalysis")]
        public void IndependentComponentAnalysisIsRepeatableForAFixedSeedAndValidatesInput()
        {
            var (observed, _) = MixedUniformSources(200, 5);
            var first = projections.RunIndependentComponentAnalysis(observed, seed: 7);
            var second = projections.RunIndependentComponentAnalysis(observed, seed: 7);

            second.Sources.Select(r => r[0]).Should().Equal(first.Sources.Select(r => r[0]));

            var oneColumn = () => projections.RunIndependentComponentAnalysis(
                [[1.0], [2.0], [3.0]]);
            oneColumn.Should().Throw<ArgumentException>();

            var iterations = () => projections.RunIndependentComponentAnalysis(observed, maximumIterations: 0);
            iterations.Should().Throw<ArgumentException>().And.ParamName.Should().Be("maximumIterations");

            var components = () => projections.RunIndependentComponentAnalysis(observed, 5);
            components.Should().Throw<ArgumentException>().And.ParamName.Should().Be("components");
        }

        [Fact]
        [Trait("Covers", "RunLinearDiscriminantAnalysis")]
        public void LinearDiscriminantAnalysisSeparatesTwoSymmetricClasses()
        {
            var result = discriminants.RunLinearDiscriminantAnalysis(twoClassData, twoClassLabels);

            result.Classes.Should().Be(2);
            result.ClassLabels.Should().Equal(10, 20);
            result.ClassCounts.Should().Equal(4, 4);
            result.ClassPrevalence.Should().Equal(0.5, 0.5);
            result.ClassMeans[0].Should().Equal(1.5, 1.5);
            result.ClassMeans[1].Should().Equal(5.5, 5.5);
            result.Eigenvalues.Should().HaveCount(1);
            result.Eigenvalues[0].Should().BeGreaterThan(0.0);
            result.ExplainedVarianceRatio[0].Should().BeApproximately(1.0, 1e-9);

            Math.Abs(result.DiscriminantVectors[0][0]).Should()
                .BeApproximately(Math.Abs(result.DiscriminantVectors[0][1]), 1e-9);

            result.DiscriminantVectors[0].Should().OnlyContain(v => v > 0);
            result.TrainingAccuracy.Should().Be(1.0);
            result.PredictedLabels.Should().Equal(twoClassLabels);
            result.ProjectedClassMeans[0][0].Should().BeLessThan(result.ProjectedClassMeans[1][0]);
            result.Scores.Should().HaveCount(8);
            var projectedGap = result.ProjectedClassMeans[1][0] - result.ProjectedClassMeans[0][0];

            projectedGap.Should()
                .BeApproximately(4.0 * (result.DiscriminantVectors[0][0] + result.DiscriminantVectors[0][1]), 1e-9);
        }

        [Fact]
        [Trait("Covers", "RunLinearDiscriminantAnalysis")]
        public void LinearDiscriminantAnalysisWithThreeClassesReturnsTwoDirections()
        {
            var data = new[]
            {
                new[] { 0.0, 0.0 }, new[] { 1.0, 0.5 }, new[] { 0.5, 1.0 }, new[] { 0.2, 0.3 },
                new[] { 8.0, 0.0 }, new[] { 9.0, 0.5 }, new[] { 8.5, 1.0 }, new[] { 8.2, 0.3 },
                new[] { 0.0, 8.0 }, new[] { 1.0, 8.5 }, new[] { 0.5, 9.0 }, new[] { 0.2, 8.3 }
            };

            var labels = new[] { 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2 };
            var result = discriminants.RunLinearDiscriminantAnalysis(data, labels, returnScores: false);

            result.Classes.Should().Be(3);
            result.Eigenvalues.Should().HaveCount(2);
            result.CumulativeExplainedVarianceRatio[1].Should().BeApproximately(1.0, 1e-9);
            result.TrainingAccuracy.Should().Be(1.0);
            result.Scores.Should().BeEmpty();
        }

        [Fact]
        [Trait("Covers", "RunLinearDiscriminantAnalysis")]
        public void LinearDiscriminantAnalysisRejectsBadLabels()
        {
            var one = () => discriminants.RunLinearDiscriminantAnalysis(twoClassData, [1, 1, 1, 1, 1, 1, 1, 1]);
            one.Should().Throw<ArgumentException>().WithMessage("*at least 2 classes*");

            var singleton = () => discriminants.RunLinearDiscriminantAnalysis(twoClassData,
                [1, 1, 1, 1, 1, 1, 1, 2]);

            singleton.Should().Throw<ArgumentException>().WithMessage("*only one row*");

            var length = () => discriminants.RunLinearDiscriminantAnalysis(twoClassData, [1, 2]);
            length.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labels");

            var components = () => discriminants.RunLinearDiscriminantAnalysis(twoClassData, twoClassLabels, 2);
            components.Should().Throw<ArgumentException>().And.ParamName.Should().Be("components");

            var empty = () => discriminants.RunLinearDiscriminantAnalysis([], []);
            empty.Should().Throw<ArgumentException>();
        }

        [Fact]
        [Trait("Covers", "RunKernelDiscriminantAnalysis")]
        public void KernelDiscriminantAnalysisSeparatesTwoBlobs()
        {
            var data = Blobs(15, 7.0, 21);
            var labels = BlobLabels(15);

            var result =
                discriminants.RunKernelDiscriminantAnalysis(data, labels,
                    kernelSigma: 4);

            result.Classes.Should().Be(2);
            result.TrainingAccuracy.Should().BeGreaterThan(0.95);
            result.Eigenvalues.Should().HaveCount(1);
            result.DiscriminantVectors.Should().BeEmpty();
            result.ClassMeans.Should().BeEmpty();
            result.Scores.Should().HaveCount(30);
            result.ProjectedClassMeans.Should().HaveCount(2);
            Math.Abs(result.ProjectedClassMeans[0][0] - result.ProjectedClassMeans[1][0]).Should().BeGreaterThan(0.0);
        }

        [Fact]
        [Trait("Covers", "RunKernelDiscriminantAnalysis")]
        public void KernelDiscriminantAnalysisValidatesRegularisationAndLabels()
        {
            var regularisation = () => discriminants.RunKernelDiscriminantAnalysis(twoClassData, twoClassLabels,
                regularization: -1);

            regularisation.Should().Throw<ArgumentException>().And.ParamName.Should().Be("regularization");

            var labels = () => discriminants.RunKernelDiscriminantAnalysis(twoClassData, [1, 2]);
            labels.Should().Throw<ArgumentException>().And.ParamName.Should().Be("labels");

            var sigma = () => discriminants.RunKernelDiscriminantAnalysis(twoClassData, twoClassLabels,
                kernelSigma: 0);

            sigma.Should().Throw<ArgumentException>().And.ParamName.Should().Be("kernelSigma");
        }

        [Fact]
        [Trait("Covers", "ClassifyWithLinearDiscriminant")]
        public void LinearDiscriminantClassificationAssignsToTheNearestProjectedClassMean()
        {
            var result = discriminants.ClassifyWithLinearDiscriminant(twoClassData, twoClassLabels,
                [[1.5, 1.5], [5.5, 5.5], [3.4, 3.4], [3.6, 3.6]]);

            result.ClassLabels.Should().Equal(10, 20);
            result.PredictedLabels.Should().Equal(10, 20, 10, 20);
            result.Scores.Should().HaveCount(4);
            result.Scores[0].Should().HaveCount(2);
            result.Scores[0][0].Should().BeGreaterThan(result.Scores[0][1]);
            result.Scores[1][1].Should().BeGreaterThan(result.Scores[1][0]);
        }

        [Fact]
        [Trait("Covers", "ClassifyWithLinearDiscriminant")]
        public void LinearDiscriminantClassificationRejectsMismatchedColumns()
        {
            var mismatch = () => discriminants.ClassifyWithLinearDiscriminant(twoClassData, twoClassLabels,
                [[1.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("newData");

            var labels = () => discriminants.ClassifyWithLinearDiscriminant(twoClassData, [1],
                [[1.0, 1.0]]);

            labels.Should().Throw<ArgumentException>().And.ParamName.Should().Be("trainingLabels");
        }

        [Fact]
        [Trait("Covers", "DetectMultivariateOutliersMahalanobis")]
        public void MahalanobisDistancesMatchHandComputationOnTheUnitCross()
        {
            var result = discriminants.DetectMultivariateOutliersMahalanobis(unitCross, 0.05);

            result.Rank.Should().Be(2);
            result.Centre.Should().Equal(0.0, 0.0);
            result.SquaredDistances.Should().OnlyContain(d => Math.Abs(d - 1.5) < 1e-9);
            result.Distances.Should().OnlyContain(d => Math.Abs(d - Math.Sqrt(1.5)) < 1e-9);
            result.PValues.Should().OnlyContain(p => Math.Abs(p - Math.Exp(-0.75)) < 1e-9);
            result.SquaredCutoff.Should().BeApproximately(5.9914645, 1e-6);
            result.Cutoff.Should().BeApproximately(Math.Sqrt(5.9914645), 1e-6);
            result.OutlierCount.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "DetectMultivariateOutliersMahalanobis")]
        public void MahalanobisOutlierDetectionFlagsAnInjectedOutlierAndUsesPseudoInverseForCollinearData()
        {
            var data = Blobs(30, 0.0, 9).Concat([[12.0, -12.0]]).ToArray();
            var flagged = discriminants.DetectMultivariateOutliersMahalanobis(data);

            flagged.OutlierRows.Should().Contain(60);
            flagged.IsOutlier[60].Should().BeTrue();

            var collinear = discriminants.DetectMultivariateOutliersMahalanobis(perfectLine);
            collinear.Rank.Should().Be(1);
            collinear.SquaredDistances.Should().OnlyContain(d => double.IsFinite(d));

            var few = () => discriminants.DetectMultivariateOutliersMahalanobis([[1.0], [2.0]]);
            few.Should().Throw<ArgumentException>();

            var constant = () => discriminants.DetectMultivariateOutliersMahalanobis(
                [[1.0], [1.0], [1.0]]);
            constant.Should().Throw<ArgumentException>();

            var significance = () => discriminants.DetectMultivariateOutliersMahalanobis(unitCross, 0.9);
            significance.Should().Throw<ArgumentException>().And.ParamName.Should().Be("significance");
        }

        [Fact]
        [Trait("Covers", "ScoreMahalanobisDistanceAgainstReference")]
        public void ScoringAgainstReferenceUsesOnlyTheReferenceCentreAndCovariance()
        {
            var result = discriminants.ScoreMahalanobisDistanceAgainstReference(unitCross,
                [[2.0, 0.0], [0.0, 0.0]], 0.05);

            result.Rows.Should().Be(2);
            result.SquaredDistances[0].Should().BeApproximately(6.0, 1e-9);
            result.PValues[0].Should().BeApproximately(Math.Exp(-3.0), 1e-9);
            result.IsOutlier[0].Should().BeTrue();
            result.SquaredDistances[1].Should().BeApproximately(0.0, 1e-9);
            result.IsOutlier[1].Should().BeFalse();
            result.OutlierRows.Should().Equal(0);

            discriminants.ScoreMahalanobisDistanceAgainstReference(unitCross, [[2.0, 0.0]], 0.01)
                .OutlierCount.Should().Be(0);
        }

        [Fact]
        [Trait("Covers", "ScoreMahalanobisDistanceAgainstReference")]
        public void ScoringAgainstReferenceRejectsMismatchAndTinyReference()
        {
            var mismatch = () =>
                discriminants.ScoreMahalanobisDistanceAgainstReference(unitCross, [[1.0]]);

            mismatch.Should().Throw<ArgumentException>().And.ParamName.Should().Be("newData");

            var tiny = () => discriminants.ScoreMahalanobisDistanceAgainstReference(
                [[1.0, 2.0], [2.0, 3.0]], [[1.0, 2.0]]);

            tiny.Should().Throw<ArgumentException>().And.ParamName.Should().Be("referenceData");
        }

        [Fact]
        [Trait("Covers", "EmbedWithTsne")]
        public void TsneKeepsTwoSeparatedClustersApartAndIsRepeatableForASeed()
        {
            var data = Blobs(20, 25.0, 4, 5);
            var first = discriminants.EmbedWithTsne(data, seed: 2);
            var again = discriminants.EmbedWithTsne(data, seed: 2);

            first.Rows.Should().Be(40);
            first.Dimensions.Should().Be(2);
            first.Coordinates.Should().OnlyContain(c => c.Length == 2 && c.All(double.IsFinite));
            first.Perplexity.Should().BeLessThan(13.0);
            again.Coordinates.Select(c => c[0]).Should().Equal(first.Coordinates.Select(c => c[0]));

            double Distance(double[] a, double[] b)
            {
                return Math.Sqrt(a.Zip(b, (x, y) => (x - y) * (x - y)).Sum());
            }

            var within = 0.0;
            var between = 0.0;
            var withinCount = 0;
            var betweenCount = 0;

            for (var i = 0; i < 40; i++)
            {
                for (var j = i + 1; j < 40; j++)
                {
                    var distance = Distance(first.Coordinates[i], first.Coordinates[j]);

                    if (i < 20 == j < 20)
                    {
                        within += distance;
                        withinCount++;
                    }
                    else
                    {
                        between += distance;
                        betweenCount++;
                    }
                }
            }

            (within / withinCount).Should().BeLessThan(between / betweenCount);
        }

        [Fact]
        [Trait("Covers", "EmbedWithTsne")]
        public void TsneSupportsThreeDimensionsAndRejectsBadInput()
        {
            var data = Blobs(15, 10.0, 8, 4);
            var three = discriminants.EmbedWithTsne(data, 3, 5, seed: 1);

            three.Coordinates[0].Should().HaveCount(3);
            three.Perplexity.Should().Be(5.0);

            var few = () => discriminants.EmbedWithTsne([.. twoSquares.Take(5)]);
            few.Should().Throw<ArgumentException>().And.ParamName.Should().Be("data");

            var dimensions = () => discriminants.EmbedWithTsne(data, 4);
            dimensions.Should().Throw<ArgumentException>().And.ParamName.Should().Be("dimensions");

            var theta = () => discriminants.EmbedWithTsne(data, theta: 2);
            theta.Should().Throw<ArgumentException>().And.ParamName.Should().Be("theta");

            var nan = () =>
                discriminants.EmbedWithTsne([.. data.Select((r, i) => i == 0 ? [double.NaN, 0, 0, 0] : r)]);
            nan.Should().Throw<ArgumentException>();
        }
    }
}