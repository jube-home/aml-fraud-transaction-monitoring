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
using Jube.Data.Cache.Interfaces;
using log4net;

namespace Jube.Data.Cache.Postgres;

public class CachePayloadRepository() : ICachePayloadRepository
{
    public CachePayloadRepository(string connectionString, ILog log) : this()
    {
        throw new NotImplementedException();
    }

    public CachePayloadRepository(string connectionString, string sqlSelect,
        string sqlFrom, string sqlOrderBy, ILog log) : this()
    {
        throw new NotImplementedException();
    }

    public Task CreateIndexAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string name, string date,
        string expression)
    {
        throw new NotImplementedException();
    }

    public Task<List<string>> GetIndexesAsync(int tenantRegistryId, Guid entityAnalysisModelGuid)
    {
        throw new NotImplementedException();
    }

    public Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, Dictionary<string, object> payload,
        DateTime referenceDate,
        Guid entityAnalysisModelInstanceEntryGuid)
    {
        throw new NotImplementedException();
    }

    public Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string key, string value,
        Dictionary<string, object> payload,
        DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid)
    {
        throw new NotImplementedException();
    }

    public Task UpsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, Dictionary<string, object> payload,
        DateTime referenceDate,
        Guid entityAnalysisModelInstanceEntryGuid)
    {
        throw new NotImplementedException();
    }

    public Task<List<Dictionary<string, object>>> GetExcludeCurrent(int tenantRegistryId, Guid entityAnalysisModelGuid,
        string key, string value, int limit,
        Guid entityInconsistentAnalysisModelInstanceEntryGuid)
    {
        throw new NotImplementedException();
    }

    public Task<List<Dictionary<string, object>>> GetInitialCountsAsync(int tenantRegistryId,
        Guid entityAnalysisModelGuid)
    {
        throw new NotImplementedException();
    }

    public Task DeleteByReferenceDate(int tenantRegistryId, Guid entityAnalysisModelGuid, DateTime referenceDate,
        int limit)
    {
        throw new NotImplementedException();
    }


    public async Task<List<Dictionary<string, object>>> GetSqlByKeyValueLimitAsync(
        int tenantRegistryId,
        Guid entityAnalysisModelGuid, string sql,
        string key, string value, string order, int limit)
    {
        throw new NotImplementedException();
    }
}