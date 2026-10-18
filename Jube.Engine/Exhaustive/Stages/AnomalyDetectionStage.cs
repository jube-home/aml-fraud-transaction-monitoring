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

using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Accord.Math;
using Jube.Analytics.Anomaly;
using Jube.Analytics.Anomaly.Models;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Anomaly;
using Jube.Engine.Exhaustive.Calibration;
using Jube.Engine.Exhaustive.Preparation;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Exhaustive.Statistics;
using Jube.Engine.Exhaustive.Utilities;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class AnomalyDetectionStage(ILog log, DynamicEnvironment.DynamicEnvironment environment = null)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var dbContext = run.DbContext;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;
            var data = run.Data;
            var variables = run.Variables;

            double[] outputs = null;
            double[] cloneOfOutputsFromAnomaly = null;
            var classCount = 0;

            if (exhaustiveSearchInstance.Anomaly)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 3 for unsupervised training data normalisation.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 3, token)
                    .ConfigureAwait(false);

                var copyOfDataForUnsupervised = new Normalisation(log).NormaliseData(variables, data.Copy());

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 4 for unsupervised training.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 4, token)
                    .ConfigureAwait(false);

                var algorithm = exhaustiveSearchInstance.AnomalyAlgorithmId.HasValue
                    ? (AnomalyAlgorithm)exhaustiveSearchInstance.AnomalyAlgorithmId.Value
                    : AnomalyAlgorithm.OneClassSvm;

                var parameters = AnomalyParameters.Parse(exhaustiveSearchInstance.AnomalyParametersJson);

                double[][] labelledPositives = null;

                if (algorithm == AnomalyAlgorithm.Automatic && exhaustiveSearchInstance.Filter)
                {
                    var classification = await Extraction.GetClassDataAsync(dbContext,
                        exhaustiveSearchInstance.EntityAnalysisModelId,
                        exhaustiveSearchInstance.FilterSql,
                        exhaustiveSearchInstance.FilterTokens,
                        variables,
                        run.MockData, log,
                        environment?.AppSettings("ReportConnectionString"), token).ConfigureAwait(false);

                    labelledPositives = new Normalisation(log).NormaliseData(variables, classification.Item1);
                }

                var request = new AnomalyRunRequest(copyOfDataForUnsupervised,
                    [.. variables.OrderBy(v => v.Key).Select(v => v.Value.Name)],
                    algorithm,
                    parameters,
                    exhaustiveSearchInstance.AnomalyProbability,
                    unchecked(exhaustiveSearchInstance.Id * 7919 + 17),
                    labelledPositives,
                    log);

                var budget = 120d;

                if (environment != null && double.TryParse(
                        environment.AppSettings("ExhaustiveAnomalyAutoBudgetSeconds"),
                        NumberStyles.Float, CultureInfo.InvariantCulture,
                        out var configured) && configured > 0)
                {
                    budget = configured;
                }

                if (parameters.Auto?.TimeBudgetSeconds == null)
                {
                    request = request with
                    {
                        Parameters = parameters with
                        {
                            Auto = (parameters.Auto ?? new AutoOptions()) with { TimeBudgetSeconds = budget }
                        }
                    };
                }

                var result = await Task.Run(() => AnomalyRunner.Run(request, token), token).ConfigureAwait(false);

                await repositoryExhaustiveSearchInstance.UpdateAnomalyReportAsync(exhaustiveSearchInstance.Id,
                    result.Report.ToJson(), token).ConfigureAwait(false);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Anomaly detection for {exhaustiveSearchInstance.Id} finished: {result.Report.Summary()}. " +
                        $"Updating status to 5 for unsupervised training recall.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 5, token)
                    .ConfigureAwait(false);

                outputs = result.Labels;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Recalled One Class Support Vector Machine.  Summing class count for {exhaustiveSearchInstance.Id}.");
                }

                classCount = (int)outputs.Sum();

                run.BaseRate = BaseRateResolution.FromAnomalyLabels(outputs);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: found {classCount} class for {exhaustiveSearchInstance.Id}.");
                }

                var onlyOutputWithClassificationForData =
                    DatasetShaper.OnlyOutputWithClassificationForData(classCount, outputs, data);

                var repositoryExhaustiveSearchInstanceVariablesAnomaly =
                    new ExhaustiveSearchInstanceVariableAnomalyRepository(dbContext);

                var repositoryExhaustiveSearchInstanceVariableHistogramsAnomaly =
                    new ExhaustiveSearchInstanceVariableHistogramAnomalyRepository(dbContext);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Trained One Class Support Vector Machine " +
                        $"for {exhaustiveSearchInstance.Id} updating status to 6 for unsupervised " +
                        $"training recall statistics calculation.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 6, token)
                    .ConfigureAwait(false);

                if (classCount > 0)
                {
                    await new StatisticsCalculator(log).CalculateStatisticsAsync(exhaustiveSearchInstance.Id, variables,
                        onlyOutputWithClassificationForData,
                        repositoryExhaustiveSearchInstanceVariablesAnomaly,
                        repositoryExhaustiveSearchInstanceVariableHistogramsAnomaly, token).ConfigureAwait(false);
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Is about to clone the array of anomaly detection values.  This is useful for storing training data so it can be observed which values were anomaly and not overriden by filter.");
                }

                cloneOfOutputsFromAnomaly = (double[])outputs.Clone();

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        "Has cloned the array of anomaly detection values.");
                }
            }

            run.Outputs = outputs;
            run.CloneOfOutputsFromAnomaly = cloneOfOutputsFromAnomaly;
            run.ClassCount = classCount;

            return true;
        }
    }
}