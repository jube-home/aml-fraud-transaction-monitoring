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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Reporting;
using Jube.Data.Repository;
using Jube.Data.Repository.Exceptions;
using LinqToDB;
using log4net;

namespace Jube.Data.Query.ExecuteVisualisationRegistryDatasourceCardQuery
{
    public class ExecuteVisualisationRegistryDatasourceCardQuery(DbContext dbContext, int tenantRegistryId)
    {
        public async Task<ExecuteVisualisationRegistryDatasourceCardQueryDto> ExecuteAsync(
            int visualisationRegistryId, string sql, IReadOnlyDictionary<int, string> suppliedByParameterId,
            bool assumeLocalDates, string reportConnectionString, ILog log,
            IReadOnlyDictionary<string, string> allowedTables, int maximumRows, int? contextId = null,
            CancellationToken token = default)
        {
            var registries = await dbContext.VisualisationRegistry
                .Where(w => w.Id == visualisationRegistryId
                            && w.TenantRegistryId == tenantRegistryId
                            && (w.Deleted == 0 || w.Deleted == null))
                .Select(w => w.ContextKindId).ToListAsync(token).ConfigureAwait(false);

            if (registries.Count == 0)
            {
                throw new KeyNotFoundException();
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw SqlValidationFailedException.Coded(DatasourceMessages.SqlEmpty);
            }

            var parameters = (await new VisualisationRegistryParameterRepository(dbContext, tenantRegistryId)
                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistryId, token)
                .ConfigureAwait(false)).ToList();

            var unknown = suppliedByParameterId.Keys.FirstOrDefault(id => parameters.All(p => p.Id != id));

            if (unknown != 0)
            {
                throw ParameterValueInvalidException.Coded(DatasourceMessages.ParameterForeign, unknown);
            }

            var values = new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                suppliedByParameterId.TryGetValue(parameter.Id, out var supplied);

                var value = ValueOf(parameter, supplied, assumeLocalDates);

                if (value != null)
                {
                    values.TryAdd(parameter.Name.Replace(" ", "_"), value);
                }
            }

            if (registries[0] == ContextParameters.CaseKind)
            {
                ContextParameters.Apply(values,
                    contextId.HasValue && await CaseInTenantAsync(contextId.Value, token).ConfigureAwait(false)
                        ? contextId
                        : null);
            }

            var clock = Stopwatch.StartNew();

            try
            {
                using var postgres = new Postgres(reportConnectionString, log, allowedTables == null);

                var result = await postgres
                    .ExecuteLimitedByNamedParametersAsync(sql, values, allowedTables, maximumRows, token)
                    .ConfigureAwait(false);

                clock.Stop();

                return new ExecuteVisualisationRegistryDatasourceCardQueryDto
                {
                    Result = result,
                    Milliseconds = (int)clock.ElapsedMilliseconds
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Info("VisualisationRegistryDatasource: the card query failed: " + e.Message);
                throw PostgresErrorDescriber.ToException(e);
            }
        }

        private async Task<bool> CaseInTenantAsync(int caseId, CancellationToken token)
        {
            return await (from c in dbContext.Case
                join w in dbContext.CaseWorkflow on c.CaseWorkflowGuid equals w.Guid
                join m in dbContext.EntityAnalysisModel on w.EntityAnalysisModelId equals m.Id
                where c.Id == caseId && m.TenantRegistryId == tenantRegistryId
                select c.Id).AnyAsync(token).ConfigureAwait(false);
        }

        private static object ValueOf(VisualisationRegistryParameter parameter, string supplied,
            bool assumeLocalDates)
        {
            var text = supplied ?? parameter.DefaultValue;
            var name = parameter.Name;

            switch (parameter.DataTypeId)
            {
                case 2:
                    return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integer)
                        ? integer
                        : Invalid(supplied, name, DatasourceMessages.ExpectedWholeNumber);
                case 3:
                    return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                        ? number
                        : Invalid(supplied, name, DatasourceMessages.ExpectedNumber);
                case 4:
                    if (supplied != null)
                    {
                        return DateTimeOffset.TryParse(supplied, CultureInfo.InvariantCulture,
                            assumeLocalDates ? DateTimeStyles.AssumeLocal : DateTimeStyles.AssumeUniversal,
                            out var date)
                            ? date.UtcDateTime
                            : Invalid(supplied, name, DatasourceMessages.ExpectedDateTime);
                    }

                    return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days)
                        ? DateTime.UtcNow.AddDays(days * -1)
                        : null;
                case 5:
                    if (string.Equals(text, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return (byte)1;
                    }

                    if (string.Equals(text, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return (byte)0;
                    }

                    return byte.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var flag)
                        ? flag
                        : Invalid(supplied, name, DatasourceMessages.ExpectedFlag);
                default:
                    return text ?? string.Empty;
            }
        }

        private static object Invalid(string supplied, string name, MessageTerm expected)
        {
            return supplied == null
                ? null
                : throw ParameterValueInvalidException.Coded(DatasourceMessages.ParameterValue, name, expected);
        }
    }
}
