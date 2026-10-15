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

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable NotAccessedPositionalProperty.Global
// ReSharper disable RedundantAttributeUsageProperty

namespace Jube.Service.Agent.AdvancedAnalyticsTools.MultivariateAndClustering.Models
{
    public enum StringDistanceKind
    {
        Levenshtein,
        NormalisedLevenshtein,
        Hamming
    }

    public sealed record DistanceMatrixResult(
        int Rows,
        int Columns,
        string Metric,
        double[][] Matrix,
        double Minimum,
        double Maximum,
        double Mean);

    public sealed record KernelMatrixResult(
        int Rows,
        int Columns,
        string Kernel,
        double[][] Matrix,
        double[] Diagonal);

    public sealed record DiscreteDistributionOverlapResult(
        double BhattacharyyaCoefficient,
        double BhattacharyyaDistance,
        double HellingerDistance);

    public sealed record StringMatch(
        int Index,
        string Candidate,
        double Distance,
        double Similarity);

    public sealed record SetSimilarityResult(
        int SizeA,
        int SizeB,
        int Intersection,
        int Union,
        double Jaccard,
        double Dice,
        double Overlap);

    public sealed record StringDistanceMatrixResult(
        int Count,
        string Kind,
        double[][] Matrix);
}