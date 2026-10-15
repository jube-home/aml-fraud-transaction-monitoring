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

namespace Jube.Data.Query.CreateOrUpdateVisualisationRegistryDatasourceWithValidationQuery
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Context;
    using LinqToDB;
    using log4net;
    using Microsoft.Extensions.Logging.Abstractions;
    using Npgsql;
    using Poco;
    using Reporting;
    using Repository;
    using Repository.Exceptions;

    public class CreateOrUpdateVisualisationRegistryDatasourceWithValidationQuery(
        DbContext dbContext,
        int tenantRegistryId,
        string userName)
    {
        public Task<VisualisationRegistryDatasource> ExecuteAsync(VisualisationRegistryDatasource model,
            bool isUpdate, ILog log, string interpolationConnectionString,
            IReadOnlyDictionary<string, string> allowedTables = null, CancellationToken token = default)
        {
            return isUpdate
                ? UpdateWithValidationAsync(model, log, interpolationConnectionString, allowedTables, token)
                : InsertWithValidationAsync(model, log, interpolationConnectionString, allowedTables, token);
        }

        private async Task<VisualisationRegistryDatasource> InsertWithValidationAsync(
            VisualisationRegistryDatasource model, ILog log, string interpolationConnectionString,
            IReadOnlyDictionary<string, string> allowedTables, CancellationToken token)
        {
            if (model.VisualisationRegistryId == null)
            {
                return model;
            }

            Dictionary<string, string> columns;

            try
            {
                columns = await ValidateSeriesAsync(model.VisualisationRegistryId.Value, model.Command,
                    interpolationConnectionString, log, allowedTables).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Info("VisualisationRegistryDatasource: the SQL command was rejected: " + e.Message);
                throw new SqlValidationFailedException(SanitiseSqlError(e));
            }

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            await FillSeriesAsync(model.Id, columns, token);

            return model;
        }

        private async Task<VisualisationRegistryDatasource> UpdateWithValidationAsync(
            VisualisationRegistryDatasource model, ILog log, string interpolationConnectionString,
            IReadOnlyDictionary<string, string> allowedTables, CancellationToken token)
        {
            if (model.VisualisationRegistryId == null)
            {
                return model;
            }

            Dictionary<string, string> columns;

            try
            {
                columns = await ValidateSeriesAsync(model.VisualisationRegistryId.Value, model.Command,
                    interpolationConnectionString, log, allowedTables).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.Info("VisualisationRegistryDatasource: the SQL command was rejected: " + e.Message);
                throw new SqlValidationFailedException(SanitiseSqlError(e));
            }

            var existing = await dbContext.VisualisationRegistryDatasource
                .FirstOrDefaultAsync(w => w.VisualisationRegistry.TenantRegistryId == tenantRegistryId
                                          && w.Id == model.Id
                                          && (w.Deleted == 0 || w.Deleted == null)
                                          && (w.Locked == 0 || w.Locked == null), token);

            if (existing == null)
            {
                throw new KeyNotFoundException();
            }

            model.Version = existing.Version + 1;
            model.Guid = existing.Guid;
            model.CreatedUser = existing.CreatedUser;
            model.CreatedDate = existing.CreatedDate;
            model.UpdatedUser = userName;
            model.UpdatedDate = DateTime.UtcNow;
            model.ImportId = existing.ImportId;

            await dbContext.UpdateAsync(model, token: token).ConfigureAwait(false);

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<VisualisationRegistryDatasource, VisualisationRegistryDatasourceVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<VisualisationRegistryDatasourceVersion>(existing);
            audit.VisualisationRegistryDatasourceId = existing.Id;

            await dbContext.InsertAsync(audit, token: token).ConfigureAwait(false);

            await FillSeriesAsync(model.Id, columns, token);

            return model;
        }

        private async Task FillSeriesAsync(int id, Dictionary<string, string> columns,
            CancellationToken token = default)
        {
            await dbContext.BeginTransactionAsync(token);

            var visualisationRegistryDatasourceSeriesRepository =
                new VisualisationRegistryDatasourceSeriesRepository(dbContext);

            await visualisationRegistryDatasourceSeriesRepository.DeleteByVisualisationRegistryDatasourceIdAsync(id);

            foreach (var (key, value) in columns)
            {
                var visualisationRegistryDatasourceSeries = new VisualisationRegistryDatasourceSeries
                {
                    VisualisationRegistryDatasourceId = id,
                    Name = key
                };

                switch (value)
                {
                    case "integer":
                    case "bigint":
                        visualisationRegistryDatasourceSeries.DataTypeId = 2;
                        break;
                    case "double precision":
                        visualisationRegistryDatasourceSeries.DataTypeId = 3;
                        break;
                    default:
                    {
                        if (value.Contains("timestamp"))
                        {
                            visualisationRegistryDatasourceSeries.DataTypeId = 4;
                        }
                        else
                        {
                            visualisationRegistryDatasourceSeries.DataTypeId = value switch
                            {
                                "smallint" => 5,
                                "double precision[]" => 6,
                                _ => value.EndsWith("[]") ? 7 : 1
                            };
                        }

                        break;
                    }
                }

                await dbContext.InsertAsync(visualisationRegistryDatasourceSeries, token: token);
                await dbContext.CommitTransactionAsync(token);
            }
        }

        private static string SanitiseSqlError(Exception e)
        {
            if (e is InvalidOperationException)
            {
                return e.Message.Contains("is not permitted in this query")
                    ? e.Message
                    : "The SQL command must be a single SELECT statement that uses no blocked statement, function or table.";
            }

            for (var inner = e; inner != null; inner = inner.InnerException)
            {
                if (inner is not PostgresException postgresException)
                {
                    continue;
                }

                var message =
                    $"The reporting database rejected the SQL command: {postgresException.MessageText} ({postgresException.SqlState}).";

                return string.IsNullOrWhiteSpace(postgresException.Hint)
                    ? message
                    : $"{message} {postgresException.Hint}";
            }

            return "The SQL command was rejected by the reporting database.";
        }

        private async Task<Dictionary<string, string>> ValidateSeriesAsync(int visualisationRegistryId, string sql,
            string interpolationConnectionString, ILog log, IReadOnlyDictionary<string, string> allowedTables)
        {
            var visualisationRegistryParameterRepository = new VisualisationRegistryParameterRepository(dbContext);

            var parameters =
                await visualisationRegistryParameterRepository.GetByVisualisationRegistryIdOrderByIdAsync(
                    visualisationRegistryId);

            var parametersDefaultValues = new Dictionary<string, object>();

            foreach (var parameter in parameters)
            {
                object defaultValue = parameter.DataTypeId switch
                {
                    1 => "",
                    2 => 0,
                    3 => 0d,
                    4 => new DateTime(),
                    5 => true,
                    _ => ""
                };

                parametersDefaultValues.Add(parameter.Name.Replace(" ", "_"), defaultValue);
            }

            using var postgres = new Postgres(interpolationConnectionString, log, allowedTables == null);

            if (allowedTables == null)
            {
                return await postgres.IntrospectAsync(sql, parametersDefaultValues).ConfigureAwait(false);
            }

            return await postgres.IntrospectWithTableTokenSwitchAsync(sql, parametersDefaultValues, allowedTables)
                .ConfigureAwait(false);
        }
    }
}