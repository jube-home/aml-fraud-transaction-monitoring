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
using System.Threading.Tasks;
using Jube.Analytics.Categorical;
using Jube.Dto.ExhaustiveSearchInstance;
using Jube.Engine.Exhaustive.Algorithms.Models;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Exhaustive.Storage;
using Jube.Engine.Exhaustive.Utilities;
using Jube.Engine.Helpers;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    using ExhaustiveDataSource = ExhaustiveDataSource;

    internal class SampleStage(
        ILog log,
        DynamicEnvironment.DynamicEnvironment environment,
        JsonSerializationHelper jsonSerializationHelper)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var dbContext = run.DbContext;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Found Exhaustive Search Instance ID {exhaustiveSearchInstance.Id}.  " +
                    $"Updating Status to 1 for pickup.");
            }

            var mockData = false;
            DatasetTrainingPlan datasetPlan = null;
            var dataSource = (ExhaustiveDataSource)exhaustiveSearchInstance.DataSourceId;

            if (dataSource == ExhaustiveDataSource.DemoDataset)
            {
                await new MockDataLoader(log, jsonSerializationHelper).LoadMockDataAsync(dbContext,
                        exhaustiveSearchInstance.EntityAnalysisModelId, token)
                    .ConfigureAwait(false);

                mockData = true;

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Found Exhaustive Search Instance ID {exhaustiveSearchInstance.Id}.  Will use the demonstration dataset for this training.");
                }
            }
            else if (dataSource == ExhaustiveDataSource.UploadedDataset)
            {
                datasetPlan = await DatasetTrainingPlan.LoadAsync(dbContext,
                    exhaustiveSearchInstance.TenantRegistryId, exhaustiveSearchInstance.ExhaustiveInstanceCsvId,
                    exhaustiveSearchInstance.ExhaustiveInstanceJsonId, exhaustiveSearchInstance.FilterSql,
                    exhaustiveSearchInstance.FilterTokens, token).ConfigureAwait(false);

                if (datasetPlan == null)
                {
                    log.Error(
                        $"Exhaustive Training: Exhaustive Search Instance ID {exhaustiveSearchInstance.Id} selects an uploaded dataset that is missing, not confirmed or not in the tenant; setting status 17.");

                    await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 17, token)
                        .ConfigureAwait(false);

                    return false;
                }

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Found Exhaustive Search Instance ID {exhaustiveSearchInstance.Id}.  Will use uploaded dataset (CSV {exhaustiveSearchInstance.ExhaustiveInstanceCsvId}, JSON {exhaustiveSearchInstance.ExhaustiveInstanceJsonId}) for this training.");
                }
            }

            if (log.IsInfoEnabled)
            {
                log.Info(
                    $"Exhaustive Training: Set status to 1 for {exhaustiveSearchInstance.Id}. Starting");
            }

            if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                    exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
            {
                return false;
            }

            await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 1, token)
                .ConfigureAwait(false);

            double[][] data;
            Dictionary<int, Variable> variables;
            var categoricalOptions = CategoricalOptions.FromJson(exhaustiveSearchInstance.CategoricalOptionsJson);

            if (datasetPlan != null)
            {
                (variables, data) = await Extraction.GetDatasetSampleDataAsync(dbContext, datasetPlan,
                    exhaustiveSearchInstance.Anomaly, log,
                    environment.AppSettings("ReportConnectionString"), token).ConfigureAwait(false);
            }
            else if (exhaustiveSearchInstance.Anomaly)
            {
                (variables, data) = await Extraction.GetSampleDataAsync(dbContext,
                    exhaustiveSearchInstance.TenantRegistryId,
                    exhaustiveSearchInstance.EntityAnalysisModelId,
                    mockData, token, categoricalOptions, plan => run.CategoricalPlan = plan).ConfigureAwait(false);
            }
            else
            {
                IReadOnlyList<string> categoricalFitJson = null;

                if (categoricalOptions.Enabled && exhaustiveSearchInstance.Filter && !mockData)
                {
                    categoricalFitJson = await Extraction.GetClassJsonAsync(dbContext,
                        exhaustiveSearchInstance.EntityAnalysisModelId,
                        exhaustiveSearchInstance.FilterSql,
                        exhaustiveSearchInstance.FilterTokens,
                        false, log,
                        environment.AppSettings("ReportConnectionString"), token).ConfigureAwait(false);
                }

                var getSampleDataResponse = await Extraction.GetSampleDataAsync(dbContext,
                    exhaustiveSearchInstance.TenantRegistryId,
                    exhaustiveSearchInstance.EntityAnalysisModelId,
                    exhaustiveSearchInstance.FilterJson,
                    exhaustiveSearchInstance.FilterTokens,
                    mockData, log,
                    environment.AppSettings("ReportConnectionString"), token, categoricalOptions,
                    plan => run.CategoricalPlan = plan, categoricalFitJson).ConfigureAwait(false);

                variables = getSampleDataResponse.Item1;
                data = getSampleDataResponse.Item2;
            }

            run.MockData = mockData;
            run.Dataset = datasetPlan;
            run.Variables = variables;
            run.Data = data;

            return true;
        }
    }
}