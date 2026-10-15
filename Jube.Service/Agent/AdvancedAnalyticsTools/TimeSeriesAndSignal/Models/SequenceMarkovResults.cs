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

namespace Jube.Service.Agent.AdvancedAnalyticsTools.TimeSeriesAndSignal.Models
{
    public sealed record MarkovChainEstimateResult(
        string[] States,
        int[] StateCounts,
        int[][] TransitionCounts,
        double[][] TransitionProbabilities,
        double[] StationaryDistribution,
        bool StationaryIsUnique,
        double[] ExpectedReturnSteps,
        string[] MostLikelyNextState,
        double EntropyRateBits,
        string[] AbsorbingStates,
        string[] StatesWithoutOutgoingTransitions,
        int Transitions);

    public sealed record MarkovStationaryResult(
        double[] StationaryDistribution,
        bool IsUnique,
        int RecurrentClasses,
        int[] TransientStates,
        int[] AbsorbingStates,
        double[] ExpectedReturnSteps,
        string Note);

    public sealed record MarkovMultiStepResult(
        double[][] StepMatrix,
        double[] Distribution,
        int Steps);

    public sealed record MarkovSurpriseResult(
        double[] SurprisalBits,
        double AverageBitsPerTransition,
        double TotalLogLikelihood,
        double Perplexity,
        int MostSurprisingTransition,
        int UnseenTransitions,
        int Transitions);

    public sealed record MarkovFirstPassageResult(
        double[][] MeanFirstPassageSteps,
        string Note);

    public sealed record MarkovAbsorptionResult(
        int[] TransientStates,
        int[] AbsorbingStates,
        double[] ExpectedStepsToAbsorption,
        double[][] AbsorptionProbabilities,
        double[][] ExpectedVisits);

    public sealed record MarkovDependenceResult(
        double ChiSquareStatistic,
        int DegreesOfFreedom,
        double PValue,
        double CramersV,
        int Transitions,
        bool Dependent,
        string Note);

    public sealed record NGramResult(
        string[][] NGrams,
        int[] Counts,
        double[] Shares,
        int DistinctNGrams,
        int TotalNGrams);

    public sealed record SequenceEntropyResult(
        double[] BlockEntropiesBits,
        double[] ConditionalEntropiesBits,
        double NormalisedFirstOrderEntropy,
        double Redundancy,
        int AlphabetSize,
        int Length);

    public sealed record LempelZivResult(
        int PhraseCount,
        double NormalisedComplexity,
        int AlphabetSize,
        int Length);

    public sealed record RunLengthResult(
        string[] Symbols,
        int[] Lengths,
        int[] StartIndices,
        int RunCount,
        int LongestRun,
        double MeanRunLength);

    public sealed record ThresholdRunsResult(
        int[] StartIndices,
        int[] Lengths,
        int RunCount,
        int LongestRun,
        double MeanRunLength,
        double ShareOfPointsInRuns);

    public sealed record PaaResult(
        double[] Values,
        int Segments);

    public sealed record SaxResult(
        string Word,
        string[] Symbols,
        double[] Paa,
        double[] Breakpoints,
        double Mean,
        double StandardDeviation);

    public sealed record DtwResult(
        double Distance,
        double NormalisedDistance,
        int[][] Path,
        int PathLength);

    public sealed record EditDistanceResult(
        double Distance,
        double NormalisedDistance,
        double Similarity);

    public sealed record HmmDecodeResult(
        int[] StatePath,
        double LogLikelihoodOfPath,
        int Observations);

    public sealed record HmmLikelihoodResult(
        double LogLikelihood,
        double AverageLogLikelihoodPerObservation,
        double[] StepLogProbabilities,
        int MostSurprisingIndex,
        int Observations);

    public sealed record HmmPosteriorResult(
        double[][] StateProbabilities,
        int[] MostLikelyStates,
        double LogLikelihood);

    public sealed record HmmNextDistributionResult(
        double[] FilteredStateProbabilities,
        double[] NextStateProbabilities,
        double[] NextSymbolProbabilities,
        int MostLikelyNextSymbol);

    public sealed record HmmNextValueResult(
        double[] FilteredStateProbabilities,
        double[] NextStateProbabilities,
        double ExpectedNextValue,
        double NextValueStandardDeviation);

    public sealed record HmmDiscreteTrainResult(
        int States,
        int Symbols,
        double[][] Transitions,
        double[][] Emissions,
        double[] Initial,
        double LogLikelihood,
        double Aic,
        double Bic,
        int Iterations,
        bool Converged,
        int BestRestart,
        int Restarts,
        int TrainingObservations);

    public sealed record HmmGaussianTrainResult(
        int States,
        double[][] Transitions,
        double[] Means,
        double[] StandardDeviations,
        double[] Initial,
        double LogLikelihood,
        double Aic,
        double Bic,
        int Iterations,
        bool Converged,
        int BestRestart,
        int Restarts,
        int TrainingObservations);

    public sealed record HmmStateCountCandidate(
        int States,
        double LogLikelihood,
        double Aic,
        double Bic,
        int Parameters);

    public sealed record HmmStateSelectionResult(
        HmmStateCountCandidate[] Candidates,
        int BestStatesByBic,
        int BestStatesByAic,
        HmmGaussianTrainResult BestModel);
}