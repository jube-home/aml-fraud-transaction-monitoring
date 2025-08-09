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

public class EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueDto
{
    public string AbstractionRuleName { get; set; }
    public string SearchKey { get; set; }
    public string SearchValue { get; set; }
}

public class CacheAbstractionRepository(string connectionString, ILog log) : ICacheAbstractionRepository
{
    public Task DeleteAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey, string searchValue,
        string name)
    {
        throw new NotImplementedException();
    }

    public Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey, string searchValue,
        string name,
        double value)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string searchKey, string searchValue,
        string name,
        double value)
    {
        throw new NotImplementedException();
    }

    public Task<double?> GetByNameSearchNameSearchValueAsync(int tenantRegistryId, Guid entityAnalysisModelGuid,
        string name,
        string searchKey, string searchValue)
    {
        throw new NotImplementedException();
    }

    public Task<Dictionary<string, double>>
        GetByNameSearchNameSearchValueReturnValueOnlyTreatingMissingAsNullByReturnZeroRecordAsync(int tenantRegistryId,
            Guid entityAnalysisModelGuid,
            List<EntityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueDto>
                entityAnalysisModelIdAbstractionRuleNameSearchKeySearchValueRequests)
    {
        throw new NotImplementedException();
    }
}