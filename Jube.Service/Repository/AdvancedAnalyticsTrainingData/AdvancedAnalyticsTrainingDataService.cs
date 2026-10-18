/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

 * Jube™ is free software: you can redistribute it and/or modify it under the terms of the GNU Affero General Public License
 * as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Jube™ is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
 * of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more details.

 * You should have received a copy of the GNU Affero General Public License along with Jube™. If not,
 * see <https://www.gnu.org/licenses/>.
 */

using System.ComponentModel;
using FluentValidation.Results;
using Jube.AskJooby.DataCache.Interfaces;
using Jube.AskJooby.DataCache.Models;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Agent.AdvancedAnalyticsTools;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Repository.AdvancedAnalyticsOperation;
using Jube.Service.Repository.AdvancedAnalyticsTrainingData.Models;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsTrainingData
{
    public sealed class AdvancedAnalyticsTrainingDataService
    {
        private const int MinimumRows = 2;
        private readonly AdvancedAnalyticsCacheRepository repository;
        private readonly AdvancedAnalyticsOperationRunner runner;
        private readonly IToolResultCache? toolCache;

        private AdvancedAnalyticsTrainingDataService(AdvancedAnalyticsOperationRunner runner,
            IToolResultCache? toolCache)
        {
            this.runner = runner;
            this.toolCache = toolCache;

            repository = new AdvancedAnalyticsCacheRepository(runner.DbContext, runner.TenantRegistryId,
                runner.UserName);
        }

        public static Task<AdvancedAnalyticsTrainingDataService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IToolResultCache? resultCache, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, resultCache,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<AdvancedAnalyticsTrainingDataService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, IToolResultCache? resultCache, ILog auditLog,
            CancellationToken token = default)
        {
            var runner = await AdvancedAnalyticsOperationRunner.CreateAsync(
                AdvancedAnalyticsChangeAreas.TrainedModel, typeof(AdvancedAnalyticsTrainingResources),
                AdvancedAnalyticsTrainingResources.NotAuthenticated, dbContext, userName, log,
                stringLocalizerFactory, serviceChangeBus, auditLog, token).ConfigureAwait(false);

            return new AdvancedAnalyticsTrainingDataService(runner, resultCache);
        }

        [Description(
            "Turns a cached table into training data a run can be submitted against, and returns the cache " +
            "pointer to give to any Submit tool. Takes the data_pointer of a table already in the tool cache -- " +
            "whatever RunSqlQuery, GetDemoDataset or an attached file put there -- and the names of the columns " +
            "to train on. Name the predictors in the order you want them; the label column is named separately " +
            "and is always packed last, so the labelColumn index this returns is the one to pass on and the " +
            "VariableNames it returns are the variableNames to pass on, in that order. Every value must be a " +
            "number: a blank becomes a missing value for the missing value policy to handle, and a column of " +
            "text that is not numeric is refused rather than silently encoded. The label must be 0 or 1 in " +
            "every row. The entry expires, so submit the run in the same session rather than storing the " +
            "pointer for later.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Training)]
        [ServiceOperation("AdvancedAnalyticsTrainingDataPut", OperationKind.Write)]
        public Task<AdvancedAnalyticsTrainingDataDto> PutAsync(
            [Description("The data_pointer of the cached table, as returned by RunSqlQuery or GetDemoDataset.")]
            string dataPointer,
            [Description("The predictor column names, in the order the model should receive them.")]
            string[] variableNames,
            [Description("The name of the column holding the outcome, which must be 0 or 1 in every row.")]
            string labelColumnName,
            CancellationToken token = default)
        {
            return runner.RunAsync("PutTrainingData", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.trainingOnly, "PutTrainingData");

                var table = await ReadTableAsync(dataPointer, token).ConfigureAwait(false);
                var matrix = AdvancedAnalyticsTrainingMatrix.Build(table, variableNames, labelColumnName);

                var entry = await repository.PutAsync(matrix.Packed, matrix.Rows, matrix.Columns, token: token)
                    .ConfigureAwait(false);

                op.Rows(table.Rows.Count);

                return Describe(entry, variableNames, labelColumnName, matrix.MissingValues, matrix.PositiveLabels);
            });
        }

        [Description(
            "Reports whether training data put in the cache earlier is still there, and what shape it is: how " +
            "many rows and columns it holds, how large it is and when it expires. Returns null when the pointer " +
            "has expired or belongs to another tenant, which is the answer to give a caller whose run has just " +
            "failed for want of its input. The columns themselves are not returned: the entry holds numbers, " +
            "and the names belong to the submission that was made against it.")]
        [AdvancedAnalyticsRequiresPermission(AdvancedAnalyticsPermissions.Analytics)]
        [ServiceOperation("AdvancedAnalyticsTrainingDataGet", OperationKind.Read, Idempotent = true)]
        public Task<AdvancedAnalyticsTrainingDataDto?> GetAsync(
            [Description("The cache pointer returned when the training data was put in the cache.")]
            Guid cachePointer,
            CancellationToken token = default)
        {
            return runner.RunAsync("GetTrainingData", async op =>
            {
                Ensure(AdvancedAnalyticsPermissionSpecifications.analyticsOnly, "GetTrainingData");

                var entry = await repository.GetMetadataAsync(cachePointer, token).ConfigureAwait(false);

                if (entry == null)
                {
                    return null;
                }

                op.Rows(entry.Rows);

                return Describe(entry, [], string.Empty, 0, 0);
            });
        }

        private async Task<CachedTable> ReadTableAsync(string dataPointer, CancellationToken token)
        {
            if (toolCache == null)
            {
                throw Invalid("dataPointer", "NoToolCache",
                    "This deployment holds no tool result cache, so there is no cached table to read.");
            }

            if (string.IsNullOrWhiteSpace(dataPointer))
            {
                throw Invalid("dataPointer", "DataPointerRequired", "Name the data_pointer of a cached table.");
            }

            var table = await toolCache.GetAsync(dataPointer, token).ConfigureAwait(false);

            if (table == null)
            {
                throw Invalid("dataPointer", "DataPointerNotFound",
                    "No cached table matches that data_pointer. Run the query again to cache a fresh one.");
            }

            if (table.Rows.Count < MinimumRows)
            {
                throw Invalid("dataPointer", "TooFewRows",
                    $"A table of {table.Rows.Count} row(s) cannot be split into training and test rows.");
            }

            return table;
        }

        private static AdvancedAnalyticsTrainingDataDto Describe(AdvancedAnalyticsCache entry,
            IReadOnlyList<string> variableNames, string labelColumnName, int missing, int positives)
        {
            return new AdvancedAnalyticsTrainingDataDto
            {
                CachePointer = entry.Guid,
                Rows = entry.Rows,
                Columns = entry.Columns,
                LabelColumn = entry.Columns - 1,
                VariableNames = variableNames,
                LabelColumnName = labelColumnName,
                SizeBytes = entry.SizeBytes,
                ExpiryDate = entry.ExpiryDate,
                MissingValues = missing,
                PositiveLabels = positives
            };
        }

        private void Ensure(int[] specifications, string operation)
        {
            runner.EnsurePermitted(specifications, operation, AdvancedAnalyticsTrainingResources.PermissionDenied);
        }

        private static DtoValidationException Invalid(string property, string code, string message)
        {
            return new DtoValidationException(new ValidationResult(
            [
                new ValidationFailure(property, message) { ErrorCode = code }
            ]));
        }
    }
}