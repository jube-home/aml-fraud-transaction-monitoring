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
    public enum ClusterSeeding
    {
        KMeansPlusPlus,
        Uniform
    }

    public enum MedoidSeeding
    {
        PamBuild,
        KMeansPlusPlus,
        Uniform
    }

    public enum MeanShiftKernelKind
    {
        Gaussian,
        Uniform,
        Epanechnikov
    }

    public enum MixtureCovarianceKind
    {
        Full,
        Diagonal
    }

    public sealed record ClusterProfile(
        int Cluster,
        int Size,
        double Proportion,
        double[] Centroid,
        double[] StandardDeviations,
        double WithinSumOfSquares,
        double MeanDistanceToCentroid,
        double MaximumDistanceToCentroid);

    public sealed record ClusteringResult(
        string Algorithm,
        int Rows,
        int Clusters,
        int[] Labels,
        double[][] Centroids,
        ClusterProfile[] Profiles,
        double Inertia,
        double SilhouetteScore,
        int Iterations,
        bool Converged);

    public sealed record MedoidClusteringResult(
        int Rows,
        int Clusters,
        int[] Labels,
        int[] MedoidRows,
        double[][] Medoids,
        ClusterProfile[] Profiles,
        double TotalDistanceToMedoids,
        double SilhouetteScore,
        int Iterations,
        bool Converged);

    public sealed record ModeClusteringResult(
        int Rows,
        int Clusters,
        int[] Labels,
        int[][] Modes,
        int[] ClusterSizes,
        double TotalMismatches,
        int Iterations,
        bool Converged);

    public sealed record MeanShiftClusteringResult(
        int Rows,
        int Clusters,
        int[] Labels,
        double[][] Modes,
        ClusterProfile[] Profiles,
        int[] ClusterSizes,
        double Bandwidth);

    public sealed record MixtureClusteringResult(
        int Rows,
        int Components,
        int[] Labels,
        double[] Weights,
        double[][] Means,
        double[][][] Covariances,
        double[][] Responsibilities,
        double LogLikelihood,
        int ParameterCount,
        double Aic,
        double Bic,
        int Iterations,
        bool Converged);

    public sealed record GaussianMixtureSelectionRow(
        int Components,
        double LogLikelihood,
        int ParameterCount,
        double Aic,
        double Bic,
        int Iterations,
        bool Converged);

    public sealed record GaussianMixtureSelectionResult(
        int Rows,
        GaussianMixtureSelectionRow[] Table,
        int BestComponentsByBic,
        int BestComponentsByAic);

    public sealed record MixtureAnomalyResult(
        int Rows,
        int Components,
        double[] LogDensity,
        int[] Labels,
        double Threshold,
        bool[] IsAnomaly,
        int[] AnomalyRows);

    public sealed record KMeansElbowRow(
        int Clusters,
        double Inertia,
        double SilhouetteScore,
        double DaviesBouldinIndex,
        double CalinskiHarabaszIndex,
        int Iterations);

    public sealed record KMeansElbowResult(
        int Rows,
        KMeansElbowRow[] Table,
        int ElbowClusters,
        int BestClustersBySilhouette,
        int BestClustersByDaviesBouldin,
        int BestClustersByCalinskiHarabasz);

    public sealed record GapStatisticRow(
        int Clusters,
        double LogWithinDispersion,
        double ExpectedLogWithinDispersion,
        double Gap,
        double StandardError);

    public sealed record GapStatisticResult(
        int Rows,
        int ReferenceSets,
        GapStatisticRow[] Table,
        int BestClusters);

    public sealed record SilhouetteResult(
        int Rows,
        int Clusters,
        double[] SampleScores,
        double[] ClusterMeanScores,
        int[] ClusterLabels,
        double OverallScore);

    public sealed record ClusterQualityResult(
        int Rows,
        int Clusters,
        double Inertia,
        double SilhouetteScore,
        double DaviesBouldinIndex,
        double CalinskiHarabaszIndex,
        double BetweenClusterSumOfSquares);

    public sealed record ClusterVariableEffect(string Variable, double StandardisedDifference);

    public sealed record ClusterProfileSummary(
        int Cluster,
        int Size,
        double Proportion,
        double[] Centroid,
        double[] StandardDeviations,
        double WithinSumOfSquares,
        double MeanDistanceToCentroid,
        double MaximumDistanceToCentroid,
        ClusterVariableEffect[] DistinguishingVariables);

    public sealed record ClusterProfilingResult(
        int Rows,
        int Clusters,
        string[] Variables,
        double[] OverallMeans,
        double[] OverallStandardDeviations,
        int[] ClusterLabels,
        ClusterProfileSummary[] Profiles);

    public sealed record CentroidAssignmentResult(
        int Rows,
        int[] Labels,
        double[] DistancesToNearest,
        double[][] DistancesToAll);

    public sealed record ClusterAgreementResult(
        int Rows,
        double RandIndex,
        double AdjustedRandIndex,
        double PairJaccard,
        int ClustersA,
        int ClustersB);

    public sealed record NeighbourMatch(int Row, double Distance);

    public sealed record NeighbourQueryResult(int Query, NeighbourMatch[] Neighbours);

    public sealed record NeighbourClassificationResult(
        int Rows,
        int[] ClassLabels,
        int[] PredictedLabels,
        double[][] ClassVoteShares,
        double[][] ClassDistanceWeightedShares);

    public sealed record NeighbourOutlierResult(
        int Rows,
        int Neighbours,
        double[] KthNeighbourDistance,
        double[] MeanNeighbourDistance,
        double Threshold,
        bool[] IsOutlier,
        int[] OutlierRows);

    public sealed record FrequentItemset(string[] Items, int Count, double Support);

    public sealed record AssociationRuleResult(
        string[] Antecedent,
        string[] Consequent,
        double Support,
        double Confidence,
        double Lift,
        double Leverage,
        int AntecedentCount,
        int RuleCount);

    public sealed record AssociationMiningResult(
        int Transactions,
        int DistinctItems,
        FrequentItemset[] FrequentItemsets,
        AssociationRuleResult[] Rules);

    public sealed record ClusterSeedResult(int Rows, int[] SeedRows, double[][] Seeds);
}