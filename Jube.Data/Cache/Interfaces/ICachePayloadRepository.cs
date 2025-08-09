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

namespace Jube.Data.Cache.Interfaces;

public interface ICachePayloadRepository
{
    Task CreateIndexAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
        string name, string date, string expression);

    Task<List<string>> GetIndexesAsync(int tenantRegistryId, Guid entityAnalysisModelGuid);

    Task InsertAsync(int tenantRegistryId,
        Guid entityAnalysisModelGuid, Dictionary<string, object> payload,
        DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid);

    Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string key,
        string value,
        Dictionary<string, object> payload,
        DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid);

    Task UpsertAsync(int tenantRegistryId,
        Guid entityAnalysisModelGuid, Dictionary<string, object> payload,
        DateTime referenceDate, Guid entityAnalysisModelInstanceEntryGuid);

    public Task<List<Dictionary<string, object>>> GetExcludeCurrent(int tenantRegistryId,
        Guid entityAnalysisModelGuid,
        string key, string value, int limit,
        Guid entityInconsistentAnalysisModelInstanceEntryGuid);

    Task<List<Dictionary<string, object>>> GetInitialCountsAsync(int tenantRegistryId,
        Guid entityAnalysisModelGuid);

    Task DeleteByReferenceDate(int tenantRegistryId, Guid entityAnalysisModelGuid,
        DateTime referenceDate, int limit);
}