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
using Jube.Data.Poco;
using Jube.Data.Repository.Interfaces;
using Jube.Data.Repository.Models;
using LinqToDB;
using LinqToDB.Data;

namespace Jube.Data.Repository
{
    public class ExhaustiveInstanceDatasetRepository
    {
        public const byte StatusPending = 0;
        public const byte StatusConfirmed = 1;
        public const int ExportMaxRowsPerClass = 10000;
        private readonly DbContext dbContext;
        private readonly int? tenantRegistryId;
        private readonly string userName;

        public ExhaustiveInstanceDatasetRepository(DbContext dbContext, string userName)
        {
            this.dbContext = dbContext;
            this.userName = userName;

            tenantRegistryId = this.dbContext.UserInTenant.Where(w => w.User == this.userName)
                .Select(s => s.TenantRegistryId).FirstOrDefault();
        }

        public ExhaustiveInstanceDatasetRepository(DbContext dbContext, int tenantRegistryId)
        {
            this.dbContext = dbContext;
            this.tenantRegistryId = tenantRegistryId;
        }

        public ExhaustiveInstanceDatasetRepository(DbContext dbContext, int tenantRegistryId, string userName)
            : this(dbContext, tenantRegistryId)
        {
            this.userName = userName;
        }

        public async Task<List<IDatasetStore>> GetAsync(DatasetKind? kind = null, CancellationToken token = default)
        {
            var result = new List<IDatasetStore>();

            if (kind is null or DatasetKind.Csv)
            {
                result.AddRange(await Visible<ExhaustiveInstanceCsv>().OrderBy(o => o.Id).ToListAsync(token)
                    .ConfigureAwait(false));
            }

            if (kind is null or DatasetKind.Json)
            {
                result.AddRange(await Visible<ExhaustiveInstanceJson>().OrderBy(o => o.Id).ToListAsync(token)
                    .ConfigureAwait(false));
            }

            return result;
        }

        public async Task<IDatasetStore> GetByIdAsync(DatasetKind kind, int id, CancellationToken token = default)
        {
            return kind == DatasetKind.Csv
                ? await Visible<ExhaustiveInstanceCsv>().FirstOrDefaultAsync(w => w.Id == id, token)
                    .ConfigureAwait(false)
                : await Visible<ExhaustiveInstanceJson>().FirstOrDefaultAsync(w => w.Id == id, token)
                    .ConfigureAwait(false);
        }

        public Task<IDatasetStore> GetByIdAsync(DatasetReference reference, CancellationToken token = default)
        {
            return GetByIdAsync(reference.Kind, reference.Id, token);
        }

        public async Task<IDatasetStore> GetConfirmedAsync(DatasetReference reference,
            CancellationToken token = default)
        {
            var store = await GetByIdAsync(reference, token).ConfigureAwait(false);

            return store is { StatusId: StatusConfirmed } ? store : null;
        }

        public Task<bool> IsConfirmedAsync(DatasetKind kind, int id, CancellationToken token = default)
        {
            return kind == DatasetKind.Csv
                ? Visible<ExhaustiveInstanceCsv>().AnyAsync(w => w.Id == id && w.StatusId == StatusConfirmed, token)
                : Visible<ExhaustiveInstanceJson>().AnyAsync(w => w.Id == id && w.StatusId == StatusConfirmed, token);
        }

        public Task<ExhaustiveInstanceCsv> InsertAsync(ExhaustiveInstanceCsv model, CancellationToken token = default)
        {
            return InsertStoreAsync(model, token);
        }

        public Task<ExhaustiveInstanceJson> InsertAsync(ExhaustiveInstanceJson model,
            CancellationToken token = default)
        {
            return InsertStoreAsync(model, token);
        }

        public static IDatasetRow NewRow(DatasetKind kind, int datasetId, Guid orderingGuid, string json)
        {
            return kind == DatasetKind.Csv
                ? new ExhaustiveInstanceCsvRow
                    { DatasetId = datasetId, EntityAnalysisModelInstanceEntryGuid = orderingGuid, Json = json }
                : new ExhaustiveInstanceJsonRow
                    { DatasetId = datasetId, EntityAnalysisModelInstanceEntryGuid = orderingGuid, Json = json };
        }

        public async Task InsertRowsAsync(DatasetKind kind, IEnumerable<IDatasetRow> rows,
            CancellationToken token = default)
        {
            var options = new BulkCopyOptions { BulkCopyType = BulkCopyType.MultipleRows, MaxBatchSize = 500 };

            if (kind == DatasetKind.Csv)
            {
                await dbContext.BulkCopyAsync(options, rows.Cast<ExhaustiveInstanceCsvRow>(), token)
                    .ConfigureAwait(false);
            }
            else
            {
                await dbContext.BulkCopyAsync(options, rows.Cast<ExhaustiveInstanceJsonRow>(), token)
                    .ConfigureAwait(false);
            }
        }

        public async Task<bool> UpdateMappingAsync(DatasetKind kind, int id, string name, string columnsJson,
            CancellationToken token = default)
        {
            var records = kind == DatasetKind.Csv
                ? await UpdateMappingAsync<ExhaustiveInstanceCsv>(id, name, columnsJson, token)
                    .ConfigureAwait(false)
                : await UpdateMappingAsync<ExhaustiveInstanceJson>(id, name, columnsJson, token)
                    .ConfigureAwait(false);

            return records > 0;
        }

        public async Task UpdateInspectionAsync(DatasetKind kind, int id, string columnsJson, string reportJson,
            long rowCount, long sizeBytes, string sourceDetail, CancellationToken token = default)
        {
            if (kind == DatasetKind.Csv)
            {
                var delimiterId = sourceDetail switch
                {
                    "," => ExhaustiveCsvDelimiter.Comma,
                    ";" => ExhaustiveCsvDelimiter.Semicolon,
                    _ => (ExhaustiveCsvDelimiter?)null
                };

                await dbContext.ExhaustiveInstanceCsv.Where(w => w.Id == id)
                    .Set(s => s.ColumnsJson, columnsJson)
                    .Set(s => s.ReportJson, reportJson)
                    .Set(s => s.DelimiterId, delimiterId)
                    .Set(s => s.RowCount, rowCount)
                    .Set(s => s.SizeBytes, sizeBytes)
                    .UpdateAsync(token).ConfigureAwait(false);
            }
            else
            {
                await dbContext.ExhaustiveInstanceJson.Where(w => w.Id == id)
                    .Set(s => s.ColumnsJson, columnsJson)
                    .Set(s => s.ReportJson, reportJson)
                    .Set(s => s.ContentHash, sourceDetail)
                    .Set(s => s.RowCount, rowCount)
                    .Set(s => s.SizeBytes, sizeBytes)
                    .UpdateAsync(token).ConfigureAwait(false);
            }
        }

        public async Task<bool> NameExistsAsync(string name, DatasetKind? exceptKind = null, int? exceptId = null,
            CancellationToken token = default)
        {
            var lower = name.ToLower();
            var csvExcept = exceptKind == DatasetKind.Csv ? exceptId : null;
            var jsonExcept = exceptKind == DatasetKind.Json ? exceptId : null;

            return await Visible<ExhaustiveInstanceCsv>()
                       .AnyAsync(w => w.Name.ToLower() == lower && (csvExcept == null || w.Id != csvExcept), token)
                       .ConfigureAwait(false)
                   || await Visible<ExhaustiveInstanceJson>()
                       .AnyAsync(w => w.Name.ToLower() == lower && (jsonExcept == null || w.Id != jsonExcept), token)
                       .ConfigureAwait(false);
        }

        public Task<bool> IsReferencedAsync(DatasetKind kind, int id, CancellationToken token = default)
        {
            return kind == DatasetKind.Csv
                ? dbContext.ExhaustiveSearchInstance.AnyAsync(w =>
                    w.ExhaustiveInstanceCsvId == id && (w.Deleted == 0 || w.Deleted == null), token)
                : dbContext.ExhaustiveSearchInstance.AnyAsync(w =>
                    w.ExhaustiveInstanceJsonId == id && (w.Deleted == 0 || w.Deleted == null), token);
        }

        public async Task<bool> DeleteAsync(DatasetKind kind, int id, CancellationToken token = default)
        {
            if (await GetByIdAsync(kind, id, token).ConfigureAwait(false) == null)
            {
                return false;
            }

            await DeleteRowsAsync(kind, [id], token).ConfigureAwait(false);

            if (kind == DatasetKind.Csv)
            {
                await SoftDeleteAsync<ExhaustiveInstanceCsv>([id], null, token).ConfigureAwait(false);
            }
            else
            {
                await SoftDeleteAsync<ExhaustiveInstanceJson>([id], null, token).ConfigureAwait(false);
            }

            return true;
        }

        public async Task PurgeAsync(DatasetKind kind, int id, CancellationToken token = default)
        {
            await DeleteRowsAsync(kind, [id], token).ConfigureAwait(false);

            if (kind == DatasetKind.Csv)
            {
                await dbContext.ExhaustiveInstanceCsv.Where(w => w.Id == id).DeleteAsync(token).ConfigureAwait(false);
            }
            else
            {
                await dbContext.ExhaustiveInstanceJson.Where(w => w.Id == id).DeleteAsync(token)
                    .ConfigureAwait(false);
            }
        }

        public async Task<List<(IDatasetStore Dataset, List<IDatasetRow> Rows)>> GetForExportAsync(DatasetKind kind,
            int maxRowsPerClass = ExportMaxRowsPerClass, CancellationToken token = default)
        {
            var result = new List<(IDatasetStore, List<IDatasetRow>)>();

            var datasets = (await GetAsync(kind, token).ConfigureAwait(false))
                .Where(w => w.StatusId == StatusConfirmed).ToList();

            foreach (var dataset in datasets)
            {
                var reference = DatasetStores.ReferenceOf(dataset);

                var sql =
                    $"select * from \"{reference.RowTable}\" where \"{reference.KeyColumn}\" = @id order by \"EntityAnalysisModelInstanceEntryGuid\" limit {maxRowsPerClass * 2}";

                var rows = kind == DatasetKind.Csv
                    ? (await dbContext
                        .QueryToListAsync<ExhaustiveInstanceCsvRow>(sql, token, new DataParameter("id", dataset.Id))
                        .ConfigureAwait(false)).Cast<IDatasetRow>().ToList()
                    : (await dbContext
                        .QueryToListAsync<ExhaustiveInstanceJsonRow>(sql, token, new DataParameter("id", dataset.Id))
                        .ConfigureAwait(false)).Cast<IDatasetRow>().ToList();
                result.Add((dataset, rows));
            }

            return result;
        }

        public async Task<int> InsertImportedAsync(IDatasetStore dataset, IEnumerable<IDatasetRow> rows, int importId,
            bool rekey, CancellationToken token = default)
        {
            var kind = DatasetStores.KindOf(dataset);
            dataset.ImportId = importId;
            dataset.Guid = rekey ? Guid.NewGuid() : dataset.Guid;

            var newId = dataset is ExhaustiveInstanceCsv csv
                ? (await InsertAsync(csv, token).ConfigureAwait(false)).Id
                : (await InsertAsync((ExhaustiveInstanceJson)dataset, token).ConfigureAwait(false)).Id;

            var list = rows.ToList();

            foreach (var row in list)
            {
                row.Id = 0;
                row.DatasetId = newId;
            }

            await InsertRowsAsync(kind, list, token).ConfigureAwait(false);

            return newId;
        }

        public async Task DeleteByTenantRegistryIdOutsideOfInstanceAsync(int tenantRegistryIdOutsideOfInstance,
            int importId, CancellationToken token = default)
        {
            var csvIds = await dbContext.ExhaustiveInstanceCsv
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Select(s => s.Id).ToListAsync(token).ConfigureAwait(false);

            await DeleteRowsAsync(DatasetKind.Csv, csvIds, token).ConfigureAwait(false);
            await SoftDeleteAsync<ExhaustiveInstanceCsv>(csvIds, importId, token).ConfigureAwait(false);

            var jsonIds = await dbContext.ExhaustiveInstanceJson
                .Where(d => d.TenantRegistryId == tenantRegistryIdOutsideOfInstance
                            && (d.Deleted == 0 || d.Deleted == null))
                .Select(s => s.Id).ToListAsync(token).ConfigureAwait(false);

            await DeleteRowsAsync(DatasetKind.Json, jsonIds, token).ConfigureAwait(false);
            await SoftDeleteAsync<ExhaustiveInstanceJson>(jsonIds, importId, token).ConfigureAwait(false);
        }

        public Task<long> CountRowsAsync(DatasetKind kind, int id, CancellationToken token = default)
        {
            return kind == DatasetKind.Csv
                ? dbContext.ExhaustiveInstanceCsvRow.LongCountAsync(w => w.DatasetId == id, token)
                : dbContext.ExhaustiveInstanceJsonRow.LongCountAsync(w => w.DatasetId == id, token);
        }

        private IQueryable<T> Visible<T>() where T : class, IDatasetStore
        {
            return dbContext.GetTable<T>().Where(w =>
                tenantRegistryId.HasValue && w.TenantRegistryId == tenantRegistryId.Value
                                          && (w.Deleted == 0 || w.Deleted == null));
        }

        private async Task<T> InsertStoreAsync<T>(T model, CancellationToken token) where T : class, IDatasetStore
        {
            model.TenantRegistryId = tenantRegistryId.GetValueOrDefault();
            model.Guid = model.Guid == Guid.Empty ? Guid.NewGuid() : model.Guid;
            model.CreatedUser = userName ?? model.CreatedUser;
            model.CreatedDate = DateTime.UtcNow;
            model.UpdatedDate = DateTime.UtcNow;
            model.Version = 1;
            model.Deleted = 0;
            model.Id = await dbContext.InsertWithInt32IdentityAsync(model, token: token).ConfigureAwait(false);

            return model;
        }

        private Task<int> UpdateMappingAsync<T>(int id, string name, string columnsJson,
            CancellationToken token) where T : class, IDatasetStore
        {
            return Visible<T>().Where(w => w.Id == id)
                .Set(s => s.Name, name)
                .Set(s => s.ColumnsJson, columnsJson)
                .Set(s => s.StatusId, StatusConfirmed)
                .Set(s => s.UpdatedDate, DateTime.UtcNow)
                .Set(s => s.Version, s => s.Version + 1)
                .UpdateAsync(token);
        }

        private Task<int> SoftDeleteAsync<T>(List<int> ids, int? importId, CancellationToken token)
            where T : class, IDatasetStore
        {
            var query = dbContext.GetTable<T>().Where(w => ids.Contains(w.Id))
                .Set(s => s.Deleted, (byte?)1)
                .Set(s => s.DeletedUser, userName)
                .Set(s => s.DeletedDate, DateTime.UtcNow)
                .Set(s => s.UpdatedDate, DateTime.UtcNow);

            return importId.HasValue
                ? query.Set(s => s.ImportId, importId).UpdateAsync(token)
                : query.UpdateAsync(token);
        }

        private async Task DeleteRowsAsync(DatasetKind kind, List<int> ids, CancellationToken token)
        {
            if (kind == DatasetKind.Csv)
            {
                await dbContext.ExhaustiveInstanceCsvRow.Where(w => ids.Contains(w.DatasetId)).DeleteAsync(token)
                    .ConfigureAwait(false);
            }
            else
            {
                await dbContext.ExhaustiveInstanceJsonRow.Where(w => ids.Contains(w.DatasetId)).DeleteAsync(token)
                    .ConfigureAwait(false);
            }
        }
    }
}