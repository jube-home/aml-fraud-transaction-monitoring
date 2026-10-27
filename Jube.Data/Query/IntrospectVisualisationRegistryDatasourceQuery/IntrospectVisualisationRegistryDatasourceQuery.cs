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
using Jube.Data.Context;
using Jube.Data.Reporting;
using Jube.Data.Reporting.Models;
using Jube.Data.Repository;
using Jube.Data.Repository.Exceptions;
using LinqToDB;
using log4net;

namespace Jube.Data.Query.IntrospectVisualisationRegistryDatasourceQuery
{
    public class IntrospectVisualisationRegistryDatasourceQuery(DbContext dbContext, int tenantRegistryId)
    {
        private const string UnnamedColumn = "?column?";

        public async Task<List<IntrospectVisualisationRegistryDatasourceQueryDto>> ExecuteAsync(
            int visualisationRegistryId, string sql, string interpolationConnectionString, ILog log,
            IReadOnlyDictionary<string, string> allowedTables = null, CancellationToken token = default)
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

            List<PostgresColumn> columns;

            try
            {
                var parameters = await DefaultParametersAsync(visualisationRegistryId, registries[0] == ContextParameters.CaseKind, token)
                    .ConfigureAwait(false);

                using var postgres = new Postgres(interpolationConnectionString, log, allowedTables == null);

                columns = allowedTables == null
                    ? await postgres.IntrospectAsync(sql, parameters, token).ConfigureAwait(false)
                    : await postgres.IntrospectWithTableTokenSwitchAsync(sql, parameters, allowedTables, token)
                        .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                log.Info("VisualisationRegistryDatasource: the SQL command was rejected: " + e.Message);
                throw PostgresErrorDescriber.ToException(e);
            }

            return Describe(columns);
        }

        private async Task<Dictionary<string, object>> DefaultParametersAsync(int visualisationRegistryId,
            bool inCase, CancellationToken token)
        {
            var parameters = await new VisualisationRegistryParameterRepository(dbContext, tenantRegistryId)
                .GetByVisualisationRegistryIdOrderByIdAsync(visualisationRegistryId, token).ConfigureAwait(false);

            var defaults = new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                object defaultValue = parameter.DataTypeId switch
                {
                    1 => "",
                    2 => 0,
                    3 => 0d,
                    4 => new DateTime(),
                    5 => (byte)0,
                    _ => ""
                };

                defaults.TryAdd(parameter.Name.Replace(" ", "_"), defaultValue);
            }

            if (inCase)
            {
                ContextParameters.Defaults(defaults);
            }

            return defaults;
        }

        private static List<IntrospectVisualisationRegistryDatasourceQueryDto> Describe(
            List<PostgresColumn> columns)
        {
            var described = new List<IntrospectVisualisationRegistryDatasourceQueryDto>(columns.Count);
            var names = new HashSet<string>(StringComparer.Ordinal);

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                var ordinal = i + 1;

                if (!names.Add(column.Name))
                {
                    throw column.Name == UnnamedColumn
                        ? SqlValidationFailedException.Coded(DatasourceMessages.ColumnsUnnamed)
                        : SqlValidationFailedException.Coded(DatasourceMessages.ColumnDuplicate, column.Name);
                }

                described.Add(new IntrospectVisualisationRegistryDatasourceQueryDto
                {
                    Name = column.Name,
                    Ordinal = ordinal,
                    DataTypeName = column.DataTypeName,
                    DataType = PostgresDataTypeClassifier.Classify(column.DataTypeName)
                });
            }

            return described;
        }
    }
}
