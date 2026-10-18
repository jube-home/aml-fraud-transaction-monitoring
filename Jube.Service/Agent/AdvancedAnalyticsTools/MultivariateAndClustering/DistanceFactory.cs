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

using Accord.Math;
using Accord.Math.Distances;

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering
{
    public enum VectorDistanceMetric
    {
        Euclidean,
        SquaredEuclidean,
        Manhattan,
        Chebyshev,
        Minkowski,
        Cosine,
        Angular,
        Canberra,
        BrayCurtis,
        Hellinger,
        PearsonCorrelation,
        Hamming,
        Jaccard,
        Dice,
        Matching,
        Kulczynski,
        RogersTanimoto,
        RusselRao,
        SokalMichener,
        SokalSneath,
        Yule,
        WeightedEuclidean,
        WeightedSquaredEuclidean
    }

    internal static class DistanceFactory
    {
        public static Func<double[], double[], double> Create(VectorDistanceMetric metric, double minkowskiOrder = 3,
            double[]? weights = null)
        {
            switch (metric)
            {
                case VectorDistanceMetric.Euclidean:
                    return Distance.Euclidean;
                case VectorDistanceMetric.SquaredEuclidean:
                    return Distance.SquareEuclidean;
                case VectorDistanceMetric.Manhattan:
                    return Distance.Manhattan;
                case VectorDistanceMetric.Chebyshev:
                    return Distance.Chebyshev;
                case VectorDistanceMetric.Minkowski:
                    if (double.IsNaN(minkowskiOrder) || double.IsInfinity(minkowskiOrder) || minkowskiOrder < 1)
                    {
                        throw new ArgumentException(
                            $"minkowskiOrder must be a finite number of at least 1 (1 is Manhattan, 2 is Euclidean) but was {minkowskiOrder}.",
                            "minkowskiOrder");
                    }

                    return (x, y) => Distance.Minkowski(x, y, minkowskiOrder);
                case VectorDistanceMetric.Cosine:
                    return Distance.Cosine;
                case VectorDistanceMetric.Angular:
                    return Distance.Angular;
                case VectorDistanceMetric.Canberra:
                    return Distance.Canberra;
                case VectorDistanceMetric.BrayCurtis:
                    return Distance.BrayCurtis;
                case VectorDistanceMetric.Hellinger:
                    return Distance.Hellinger;
                case VectorDistanceMetric.PearsonCorrelation:
                    return (x, y) => 1.0 - new PearsonCorrelation().Similarity(x, y);
                case VectorDistanceMetric.Hamming:
                    return (x, y) => new Hamming<double>().Distance(x, y);
                case VectorDistanceMetric.Jaccard:
                    return Distance.Jaccard;
                case VectorDistanceMetric.Dice:
                    return Distance.Dice;
                case VectorDistanceMetric.Matching:
                    return Distance.Matching;
                case VectorDistanceMetric.Kulczynski:
                    return Distance.Kulczynski;
                case VectorDistanceMetric.RogersTanimoto:
                    return Distance.RogersTanimoto;
                case VectorDistanceMetric.RusselRao:
                    return Distance.RusselRao;
                case VectorDistanceMetric.SokalMichener:
                    return Distance.SokalMichener;
                case VectorDistanceMetric.SokalSneath:
                    return Distance.SokalSneath;
                case VectorDistanceMetric.Yule:
                    return Distance.Yule;
                case VectorDistanceMetric.WeightedEuclidean:
                    RequireWeights(weights, metric);

                    return (x, y) => Distance.WeightedEuclidean(x, y, weights!);
                case VectorDistanceMetric.WeightedSquaredEuclidean:
                    RequireWeights(weights, metric);

                    return (x, y) => Distance.WeightedSquareEuclidean(x, y, weights!);
                default:
                    throw new ArgumentException($"Distance metric {metric} is not supported.", "metric");
            }
        }

        private static void RequireWeights(double[]? weights, VectorDistanceMetric metric)
        {
            if (weights == null || weights.Length == 0)
            {
                throw new ArgumentException(
                    $"weights is required for the {metric} metric: give one non negative weight per dimension.",
                    "weights");
            }

            if (weights.Any(w => double.IsNaN(w) || double.IsInfinity(w) || w < 0))
            {
                throw new ArgumentException("weights must be finite and non negative.", "weights");
            }
        }
    }
}