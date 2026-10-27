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
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoMapper;
    using Context;
    using LinqToDB;
    using log4net;
    using Microsoft.Extensions.Logging.Abstractions;
    using Poco;
    using Repository;
    using IntrospectQuery =
        Jube.Data.Query.IntrospectVisualisationRegistryDatasourceQuery.IntrospectVisualisationRegistryDatasourceQuery;
    using IntrospectColumn =
        Jube.Data.Query.IntrospectVisualisationRegistryDatasourceQuery.IntrospectVisualisationRegistryDatasourceQueryDto;

    public class CreateOrUpdateVisualisationRegistryDatasourceWithValidationQuery(
        DbContext dbContext,
        int tenantRegistryId,
        string userName)
    {
        public Task<VisualisationRegistryDatasource> ExecuteAsync(VisualisationRegistryDatasource model,
            bool isUpdate, ILog log, string interpolationConnectionString,
            IReadOnlyDictionary<string, string> allowedTables = null,
            IReadOnlyDictionary<int, IReadOnlyList<string>> roleColumns = null, CancellationToken token = default)
        {
            return isUpdate
                ? UpdateWithValidationAsync(model, log, interpolationConnectionString, allowedTables, roleColumns,
                    token)
                : InsertWithValidationAsync(model, log, interpolationConnectionString, allowedTables, roleColumns,
                    token);
        }

        private async Task<VisualisationRegistryDatasource> InsertWithValidationAsync(
            VisualisationRegistryDatasource model, ILog log, string interpolationConnectionString,
            IReadOnlyDictionary<string, string> allowedTables,
            IReadOnlyDictionary<int, IReadOnlyList<string>> roleColumns, CancellationToken token)
        {
            if (model.VisualisationRegistryId == null)
            {
                return model;
            }

            var columns = await new IntrospectQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(model.VisualisationRegistryId.Value, model.Command, interpolationConnectionString,
                    log, allowedTables, token).ConfigureAwait(false);

            model.CreatedUser = userName;
            model.CreatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;

            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            try
            {
                model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

                await FillSeriesAsync(model.Id, columns, roleColumns, token).ConfigureAwait(false);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            return model;
        }

        private async Task<VisualisationRegistryDatasource> UpdateWithValidationAsync(
            VisualisationRegistryDatasource model, ILog log, string interpolationConnectionString,
            IReadOnlyDictionary<string, string> allowedTables,
            IReadOnlyDictionary<int, IReadOnlyList<string>> roleColumns, CancellationToken token)
        {
            if (model.VisualisationRegistryId == null)
            {
                return model;
            }

            var columns = await new IntrospectQuery(dbContext, tenantRegistryId)
                .ExecuteAsync(model.VisualisationRegistryId.Value, model.Command, interpolationConnectionString,
                    log, allowedTables, token).ConfigureAwait(false);

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

            var mapper = new Mapper(new MapperConfiguration(
                cfg => { cfg.CreateMap<VisualisationRegistryDatasource, VisualisationRegistryDatasourceVersion>(); },
                NullLoggerFactory.Instance));

            var audit = mapper.Map<VisualisationRegistryDatasourceVersion>(existing);
            audit.VisualisationRegistryDatasourceId = existing.Id;

            await dbContext.BeginTransactionAsync(token).ConfigureAwait(false);

            try
            {
                await dbContext.UpdateAsync(model, token: token).ConfigureAwait(false);

                await dbContext.InsertAsync(audit, token: token).ConfigureAwait(false);

                await FillSeriesAsync(model.Id, columns, roleColumns, token).ConfigureAwait(false);

                await dbContext.CommitTransactionAsync(token).ConfigureAwait(false);
            }
            catch
            {
                await dbContext.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false);
                throw;
            }

            return model;
        }

        private async Task FillSeriesAsync(int id, List<IntrospectColumn> columns,
            IReadOnlyDictionary<int, IReadOnlyList<string>> roleColumns, CancellationToken token)
        {
            await new VisualisationRegistryDatasourceSeriesRepository(dbContext)
                .DeleteByVisualisationRegistryDatasourceIdAsync(id).ConfigureAwait(false);

            foreach (var column in columns)
            {
                int? roleId = null;
                int? roleOrder = null;

                foreach (var (role, names) in roleColumns ?? new Dictionary<int, IReadOnlyList<string>>())
                {
                    var position = names.ToList().IndexOf(column.Name);

                    if (position < 0)
                    {
                        continue;
                    }

                    roleId = role;
                    roleOrder = position + 1;
                    break;
                }

                await dbContext.InsertAsync(new VisualisationRegistryDatasourceSeries
                {
                    VisualisationRegistryDatasourceId = id,
                    Name = column.Name,
                    DataTypeId = (int)column.DataType,
                    Ordinal = column.Ordinal,
                    RoleId = roleId,
                    RoleOrder = roleOrder
                }, token: token).ConfigureAwait(false);
            }
        }
    }
}
