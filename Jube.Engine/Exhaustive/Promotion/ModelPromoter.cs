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
using Accord.Neuro;
using Jube.Data.Poco;
using Jube.Data.Repository.Models;
using Jube.Engine.Exhaustive.Algorithms;
using Jube.Engine.Exhaustive.Interfaces;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Calibration.Models;
using Jube.Engine.Exhaustive.Selection;
using Jube.Engine.Helpers;
using log4net;
using Newtonsoft.Json;

namespace Jube.Engine.Exhaustive.Promotion
{
    internal class ModelPromoter(
        ITrialRecorder recorder,
        Performance performance,
        JsonSerializationHelper jsonSerializationHelper,
        SensitivityAnalyser sensitivityAnalyser,
        ILog log)
    {
        internal async Task PerformMonteCarloSimulationToUnderstandTopologyModelAsync(int simulationsCount,
            Dictionary<int, TrialVariable> trialVariables, Func<double[], double> score,
            CancellationToken token = default)
        {
            foreach (var (prescription, bins) in PrescriptionBuilder.BuildPrescriptionsWithHistograms(simulationsCount,
                         trialVariables,
                         score))
            {
                var inserted = await recorder.InsertPromotedVariableAsync(prescription, token).ConfigureAwait(false);

                foreach (var bin in bins)
                {
                    bin.ExhaustiveSearchInstancePromotedTrialInstanceVariableId = inserted.Id;
                    await recorder.InsertPromotedVariableHistogramAsync(bin, token).ConfigureAwait(false);
                }
            }
        }

        internal async Task PerformSensitivityAnalysisAndStoreForPromotedAsync(
            int exhaustiveSearchInstanceTrialInstanceId,
            ActivationNetwork topologyNetwork,
            Dictionary<int, TrialVariable> trialVariables, double[][] dataTesting,
            CancellationToken token = default)
        {
            var sensitivityAnalysis = sensitivityAnalyser.PerformSensitivityAnalysis(
                exhaustiveSearchInstanceTrialInstanceId,
                topologyNetwork, trialVariables, dataTesting,
                performance.CalculateScores(topologyNetwork, dataTesting));

            var rank = sensitivityAnalysis.Count;

            foreach (var (key, value) in sensitivityAnalysis)
            {
                var exhaustiveSearchInstancePromotedTrialInstanceSensitivity =
                    new ExhaustiveSearchInstancePromotedTrialInstanceSensitivity
                    {
                        Sensitivity = value,
                        SensitivityRank = rank--,
                        ExhaustiveSearchInstanceTrialInstanceVariableId =
                            trialVariables[key].ExhaustiveSearchInstanceTrialInstanceVariableId
                    };

                await recorder.InsertPromotedSensitivityAsync(
                    exhaustiveSearchInstancePromotedTrialInstanceSensitivity, token).ConfigureAwait(false);
            }
        }

        internal async Task StoreRocAsync(double[][] outputsTesting,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            CancellationToken token = default)
        {
            foreach (var point in Performance.RocPoints(performance.Scores, outputsTesting))
            {
                var exhaustiveSearchInstancePromotedTrialInstanceRoc =
                    new ExhaustiveSearchInstancePromotedTrialInstanceRoc
                    {
                        ExhaustiveSearchInstanceTrialInstanceId =
                            exhaustiveSearchInstanceTrialInstance.Id,
                        Score = point.Score,
                        FalsePositive = point.Fp,
                        FalseNegative = point.Fn,
                        TruePositive = point.Tp,
                        TrueNegative = point.Tn,
                        Threshold = point.Threshold
                    };

                await recorder.InsertRocAsync(exhaustiveSearchInstancePromotedTrialInstanceRoc, token)
                    .ConfigureAwait(false);
            }
        }

        internal async Task StorePredictedVsActualAsync(
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] outputsTesting,
            Dictionary<int, TrialVariable> trialVariables,
            double[][] dataTesting,
            CancellationToken token = default)
        {
            var orderedVariables = trialVariables.Values.ToArray();

            for (var j = 0; j < performance.Scores.Length; j++)
            {
                var exhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    = new ExhaustiveSearchInstancePromotedTrialInstancePredictedActual
                    {
                        ExhaustiveSearchInstanceTrialInstanceId =
                            exhaustiveSearchInstanceTrialInstance.Id,
                        Predicted = performance.Scores[j],
                        Actual = outputsTesting[j][0]
                    };

                await recorder.InsertPredictedActualAsync(
                    exhaustiveSearchInstancePromotedTrialInstancePredictedActual, token).ConfigureAwait(false);

                for (var k = 0; k < orderedVariables.Length; k++)
                {
                    var variable = orderedVariables[k];

                    if (variable.NormalisationTypeId != 2)
                    {
                        continue;
                    }

                    var predictedActualVariableValue =
                        new ExhaustiveSearchInstancePromotedTrialInstancePredictedActualVariableValue
                        {
                            ExhaustiveSearchInstancePromotedTrialInstancePredictedActualId =
                                exhaustiveSearchInstancePromotedTrialInstancePredictedActual.Id,
                            ExhaustiveSearchInstanceVariableId = variable.ExhaustiveSearchInstanceVariableId,
                            Value = variable.ReverseZScore(dataTesting[j][k])
                        };

                    await recorder.InsertPredictedActualVariableValueAsync(predictedActualVariableValue, token)
                        .ConfigureAwait(false);
                }
            }
        }

        internal string SerializeTopologyNetwork(Network topologyNetwork)
        {
            return JsonConvert.SerializeObject(topologyNetwork,
                jsonSerializationHelper.TopologyNetworkJsonSerializerSettings);
        }

        internal Task<ExhaustiveSearchInstancePromotedTrialInstance> PromoteTopologyNetworkAsync(
            Network topologyNetwork, int topologyComplexity,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            CancellationToken token = default,
            Outcome calibration = null)
        {
            var json = SerializeTopologyNetwork(topologyNetwork);

            var exhaustiveSearchInstancePromotedTrial = new ExhaustiveSearchInstancePromotedTrialInstance
            {
                Active = 1,
                Score = performance.Score,
                TopologyComplexity = topologyComplexity,
                TrueNegative = performance.Tn,
                TruePositive = performance.Tp,
                FalseNegative = performance.Fn,
                FalsePositive = performance.Fp,
                Json = json,
                ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance
                    .Id
            };

            calibration?.WriteTo(exhaustiveSearchInstancePromotedTrial);

            return recorder.InsertPromotedAsync(exhaustiveSearchInstancePromotedTrial, token);
        }

        internal static string SerializeChallengerVariables(ChallengerTrainingResult challenger,
            Dictionary<int, TrialVariable> trialVariables)
        {
            return JsonConvert.SerializeObject(challenger.Variables.Select(name =>
            {
                var match = trialVariables.Values.FirstOrDefault(v => v.Name == name);

                return new { Name = name, ProcessingTypeId = match?.ProcessingTypeId ?? 0 };
            }));
        }

        internal Task<ExhaustiveSearchInstancePromotedTrialInstance> PromoteChallengerAsync(
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            ChallengerTrainingResult challenger, Dictionary<int, TrialVariable> trialVariables,
            CancellationToken token = default)
        {
            var variablesJson = SerializeChallengerVariables(challenger, trialVariables);

            var exhaustiveSearchInstancePromotedTrial = new ExhaustiveSearchInstancePromotedTrialInstance
            {
                Active = 1,
                Score = performance.Score,
                TrueNegative = performance.Tn,
                TruePositive = performance.Tp,
                FalseNegative = performance.Fn,
                FalsePositive = performance.Fp,
                KindId = Enum.Parse<AdvancedAnalyticsModelKind>(challenger.Kind),
                Json = challenger.ModelJson,
                MetricsJson = challenger.MetricsJson,
                VariablesJson = variablesJson,
                ExhaustiveSearchInstanceTrialInstanceId = exhaustiveSearchInstanceTrialInstance.Id
            };

            return recorder.InsertPromotedAsync(exhaustiveSearchInstancePromotedTrial, token);
        }

        internal Outcome FitCalibration(ActivationNetwork topologyNetwork,
            CalibrationInputs inputs,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            double[][] dataTesting, double[][] outputsTesting,
            string requestedCalibrationMethod = ExhaustiveCalibrationRequest.Automatic)
        {
            if (inputs == null)
            {
                return null;
            }

            try
            {
                var outcome = ExhaustiveCalibrationFitter.Fit(topologyNetwork, inputs.DataCrossValidation,
                    inputs.OutputsCrossValidation, dataTesting, outputsTesting, inputs.OutputsTraining,
                    inputs.BaseRate, requestedCalibrationMethod);

                if (outcome.Method == ExhaustiveCalibration.MethodNone)
                {
                    log.Warn(
                        $"Exhaustive Training: Trial instance ID {exhaustiveSearchInstanceTrialInstance.Id} is promoted without calibration as {outcome.Reason}");
                }
                else if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trial instance ID {exhaustiveSearchInstanceTrialInstance.Id} is calibrated with status {outcome.Status}, slope {outcome.Slope}, intercept {outcome.Intercept}, prior {outcome.CalibrationPrior} and base rate {outcome.BaseRate} from {outcome.BaseRateSource}.");
                }

                return outcome;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.Error(
                    $"Exhaustive Training: Trial instance ID {exhaustiveSearchInstanceTrialInstance.Id} could not be calibrated as {ex}. The model is promoted with raw scores.");

                return null;
            }
        }

        internal Task RecordBestScoreAsync(int i,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance, double bestScore,
            int modelsSinceBest, int topologyComplexity, CancellationToken token)
        {
            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    $"Has promoted best score as {bestScore}.  There have been {modelsSinceBest},  which will be reset. " +
                    "Will promote model in the database.");
            }

            return recorder.UpdateBestScoreAsync(bestScore,
                topologyComplexity, token);
        }

        internal async Task PromoteAndExplainAsync(int i,
            ExhaustiveSearchInstanceTrialInstance exhaustiveSearchInstanceTrialInstance,
            ActivationNetwork topologyNetwork, int topologyComplexity, Dictionary<int, TrialVariable> trialVariables,
            double[][] dataTesting, double[][] outputsTesting, int simulationsCount, CancellationToken token,
            CalibrationInputs calibrationInputs = null,
            string requestedCalibrationMethod = ExhaustiveCalibrationRequest.Automatic)
        {
            var calibration = FitCalibration(topologyNetwork, calibrationInputs, exhaustiveSearchInstanceTrialInstance,
                dataTesting, outputsTesting, requestedCalibrationMethod);

            await PromoteTopologyNetworkAsync(topologyNetwork, topologyComplexity,
                exhaustiveSearchInstanceTrialInstance, token, calibration).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has promoted the model in the database,  will now store the ROC data.");
            }

            await StoreRocAsync(outputsTesting,
                exhaustiveSearchInstanceTrialInstance, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has stored ROC data and will now store the predicted vs actual data (this will be sampled when" +
                    "presenting it in the user interface).");
            }

            await StorePredictedVsActualAsync(exhaustiveSearchInstanceTrialInstance, outputsTesting, trialVariables,
                dataTesting, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has stored the predicted vs. the actual in the database,  will now proceed to perform " +
                    "sensitivity analysis on the model and variables.");
            }

            await PerformSensitivityAnalysisAndStoreForPromotedAsync(
                exhaustiveSearchInstanceTrialInstance.Id,
                topologyNetwork, trialVariables,
                dataTesting, token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has performed sensitivity analysis on the model and variables and will now use Monte Carlo " +
                    "simulation to explain the model topology in practice.");
            }

            await PerformMonteCarloSimulationToUnderstandTopologyModelAsync(simulationsCount,
                trialVariables,
                x => topologyNetwork.Compute(x)[0],
                token).ConfigureAwait(false);

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Trial instance count {i}. Has created an Exhaustive Search Instance " +
                    $"Trial Instance ID of {exhaustiveSearchInstanceTrialInstance.Id}. " +
                    "Has performed Monte Carlo simulation on the model and variables and the promotion is concluded.");
            }
        }
    }
}