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
using System.Threading.Tasks;
using Jube.Data.Cache.Dto;
using Jube.Data.Cache.Interfaces;
using log4net;

namespace Jube.Data.Cache.Postgres;

public class CacheSanctionRepository(string connectionString, ILog log) : ICacheSanctionRepository
{
    public Task<CacheSanctionDto> GetByMultiPartStringDistanceThresholdAsync(int tenantRegistryId,
        Guid entityAnalysisModelGuid,
        string multiPartString, int distanceThreshold)
    {
        throw new NotImplementedException();
    }

    public Task InsertAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string multiPartString,
        int distanceThreshold,
        double? value)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAsync(int tenantRegistryId, Guid entityAnalysisModelGuid, string multiPartString,
        int distanceThreshold,
        double? value)
    {
        throw new NotImplementedException();
    }
}