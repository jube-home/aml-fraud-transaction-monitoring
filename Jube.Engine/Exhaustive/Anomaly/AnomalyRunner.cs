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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using log4net;

namespace Jube.Engine.Exhaustive.Anomaly
{
    internal sealed record AnomalyRunRequest(
        double[][] Data,
        IReadOnlyList<string> VariableNames,
        AnomalyAlgorithm Algorithm,
        AnomalyParameters Parameters,
        double Probability,
        int Seed,
        double[][] LabelledPositives,
        ILog Log);

    internal sealed record AnomalyRunResult(double[] Labels, AnomalyReport Report);

    internal static class AnomalyRunner
    {
        internal static AnomalyRunResult Run(AnomalyRunRequest request, CancellationToken token)
        {
            var clock = Stopwatch.StartNew();
            var parameters = request.Parameters ?? new AnomalyParameters();
            var requested = request.Algorithm;
            var warnings = new List<string>();

            var reduction = parameters.Reduction ?? (requested == AnomalyAlgorithm.OneClassSvm
                ? ReductionOptions.Off
                : ReductionOptions.Recommended);

            var input = request.Data;
            IReadOnlyList<VariableDecision> decisions;
            var components = 0;
            var kept = input[0].Length;
            double[][] matrix;
            ReducerModel reducer = null;

            if (reduction.Mode == ReductionMode.None && reduction.Scaling == ReductionScaling.None &&
                reduction.Projection == ReductionProjection.None)
            {
                matrix = input;

                decisions =
                [
                    .. Enumerable.Range(0, kept).Select(j =>
                        new VariableDecision(j, Name(request, j), true, "Kept", string.Empty))
                ];
            }
            else
            {
                reducer = AnomalyVariableReducer.Fit(input, request.VariableNames, reduction, request.Seed, token);
                matrix = reducer.Transform(input);
                decisions = reducer.Decisions;
                components = reducer.Components;
                kept = reducer.KeptIndices.Count;
            }

            token.ThrowIfCancellationRequested();

            AutoEvaluation auto = null;
            var algorithm = requested;

            if (requested == AnomalyAlgorithm.Automatic)
            {
                var positives = request.LabelledPositives is { Length: > 0 } && reducer != null
                    ? reducer.Transform(request.LabelledPositives)
                    : request.LabelledPositives;

                var candidates = AnomalyCandidates.For(matrix[0].Length, matrix.Length, parameters, request.Log);

                var selection = AnomalyAutoSelector.Select(new AutoRequest
                {
                    Candidates = candidates,
                    Data = matrix,
                    LabelledPositives = positives,
                    Contamination = Contamination(request, parameters),
                    TimeBudgetSeconds = parameters.Auto?.TimeBudgetSeconds ?? AnomalyCandidates.DefaultBudgetSeconds,
                    EvaluationRowCap = parameters.Auto?.EvaluationRowCap ?? 2000,
                    Resamples = parameters.Auto?.Resamples ?? 4,
                    Subspaces = parameters.Auto?.Subspaces ?? 4,
                    Seed = request.Seed
                }, token);

                auto = selection.Evaluation;

                if (selection.Selected.HasValue)
                {
                    algorithm = selection.Selected.Value;
                }
                else
                {
                    algorithm = AnomalyAlgorithm.IsolationForest;

                    warnings.Add(
                        "No candidate survived automatic evaluation; IsolationForest was used as the safe default.");
                }
            }

            var detector = AnomalyCandidates.Create(algorithm, parameters, request.Log);
            detector.Fit(matrix, request.Seed, token);
            var scores = AnomalyInput.ScoreAll(detector, matrix, token, detector is OneClassSvmDetector ? 1 : 0);

            double[] labels;
            ThresholdApplied threshold;

            if (algorithm == AnomalyAlgorithm.OneClassSvm)
            {
                var cut = -request.Probability;
                labels = AnomalyLabeller.Label(scores, cut);

                threshold = new ThresholdApplied("SvmProbability", request.Probability, null,
                    (int)labels.Sum(), labels.Length);
            }
            else
            {
                var contamination = Contamination(request, parameters);
                threshold = AnomalyLabeller.Contamination(scores, contamination);
                labels = AnomalyLabeller.Label(scores, threshold.Value);
            }

            if (threshold.Flagged == 0)
            {
                warnings.Add("No rows were flagged as anomalous; supervised training will report no class available.");
            }

            var report = new AnomalyReport
            {
                Algorithm = algorithm.ToString(),
                Requested = requested.ToString(),
                Parameters = parameters.ToJson(),
                Seed = request.Seed,
                Rows = matrix.Length,
                InputVariables = input[0].Length,
                KeptVariables = kept,
                DetectorDimensions = matrix[0].Length,
                Components = components,
                Reduction = reduction.Mode.ToString(),
                Scaling = reduction.Scaling.ToString(),
                Variables = decisions,
                Scores = ScoreSummary.From(scores),
                Threshold = threshold,
                Auto = auto,
                Warnings = warnings,
                ElapsedMilliseconds = clock.ElapsedMilliseconds
            };

            return new AnomalyRunResult(labels, report);
        }

        private static double Contamination(AnomalyRunRequest request, AnomalyParameters parameters)
        {
            var contamination = parameters.Contamination ?? request.Probability;

            if (!(contamination is > 0 and <= 0.5))
            {
                throw new ArgumentException(
                    $"The contamination fraction {contamination} must be in (0, 0.5] for {request.Algorithm}.");
            }

            return contamination;
        }

        private static string Name(AnomalyRunRequest request, int index)
        {
            return request.VariableNames != null && index < request.VariableNames.Count
                ? request.VariableNames[index]
                : $"V{index}";
        }
    }
}