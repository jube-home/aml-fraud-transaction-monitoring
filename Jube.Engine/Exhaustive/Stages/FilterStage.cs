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
using System.Threading.Tasks;
using Jube.Data.Repository;
using Jube.Engine.Exhaustive.Preparation;
using Jube.Engine.Exhaustive.Stages.Models;
using Jube.Engine.Exhaustive.Statistics;
using Jube.Engine.Exhaustive.Utilities;
using log4net;

namespace Jube.Engine.Exhaustive.Stages
{
    internal class FilterStage(ILog log, DynamicEnvironment.DynamicEnvironment environment)
    {
        internal async Task<bool> ExecuteAsync(InstanceRun run)
        {
            var exhaustiveSearchInstance = run.Instance;
            var dbContext = run.DbContext;
            var repositoryExhaustiveSearchInstance = run.RepositoryInstance;
            var token = run.Token;
            var data = run.Data;
            var variables = run.Variables;
            var outputs = run.Outputs;
            var classCount = run.ClassCount;
            var mockData = run.MockData;

            if (exhaustiveSearchInstance.Filter)
            {
                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Switched to create a classification " +
                        $"from data for {exhaustiveSearchInstance.Id} updating status to 7 for filtering.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 7, token)
                    .ConfigureAwait(false);

                var (dataClassificationFilter, outputsClassificationFilter) = run.Dataset != null
                    ? await Extraction.GetDatasetClassDataAsync(dbContext, run.Dataset, log,
                        environment.AppSettings("ReportConnectionString"),
                        token).ConfigureAwait(false)
                    : await Extraction.GetClassDataAsync(dbContext,
                        exhaustiveSearchInstance.EntityAnalysisModelId,
                        exhaustiveSearchInstance.FilterSql,
                        exhaustiveSearchInstance.FilterTokens,
                        variables,
                        mockData, log,
                        environment.AppSettings("ReportConnectionString"), token).ConfigureAwait(false);

                var repositoryExhaustiveSearchInstanceVariablesClassification =
                    new ExhaustiveSearchInstanceVariableClassificationRepository(dbContext);

                var repositoryExhaustiveSearchInstanceVariableHistogramsClassification =
                    new ExhaustiveSearchInstanceVariableHistogramClassificationRepository(dbContext);

                if (log.IsInfoEnabled)
                {
                    log.Info(
                        $"Exhaustive Training: Switched to create a classification " +
                        $"from data for {exhaustiveSearchInstance.Id} updating status to 8 " +
                        $"for filtering statistics calculation.");
                }

                if (await InstanceStatus.IsStoppingOrStoppedOrCancelledAsync(repositoryExhaustiveSearchInstance,
                        exhaustiveSearchInstance.Id, token).ConfigureAwait(false))
                {
                    return false;
                }

                await repositoryExhaustiveSearchInstance.UpdateStatusAsync(exhaustiveSearchInstance.Id, 8, token)
                    .ConfigureAwait(false);

                await new StatisticsCalculator(log).CalculateStatisticsAsync(exhaustiveSearchInstance.Id, variables,
                    dataClassificationFilter,
                    repositoryExhaustiveSearchInstanceVariablesClassification,
                    repositoryExhaustiveSearchInstanceVariableHistogramsClassification, token).ConfigureAwait(false);

                if (!exhaustiveSearchInstance.Anomaly)
                {
                    run.BaseRate = run.Dataset != null
                        ? await Extraction.ResolveDatasetBaseRateAsync(dbContext, run.Dataset, log,
                            environment.AppSettings("ReportConnectionString"),
                            token).ConfigureAwait(false)
                        : await Extraction.ResolveArchiveBaseRateAsync(dbContext,
                            exhaustiveSearchInstance.TenantRegistryId,
                            exhaustiveSearchInstance.EntityAnalysisModelId, exhaustiveSearchInstance.FilterJson,
                            exhaustiveSearchInstance.FilterSql, exhaustiveSearchInstance.FilterTokens, mockData, log,
                            environment.AppSettings("ReportConnectionString"),
                            token).ConfigureAwait(false);
                }

                DatasetShaper.Append(data, dataClassificationFilter, out data, out outputs);

                classCount += (int)outputsClassificationFilter.Sum();
            }

            run.Data = data;
            run.Outputs = outputs;
            run.ClassCount = classCount;

            return true;
        }
    }
}