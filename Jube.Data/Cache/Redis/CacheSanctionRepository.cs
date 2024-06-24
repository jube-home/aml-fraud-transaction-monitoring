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
using System.IO;
using System.Threading.Tasks;
using Jube.Data.Cache.Interfaces;
using log4net;
using StackExchange.Redis;
using Jube.Data.Cache.Dto;
using Jube.Data.Cache.Redis.MessagePack;
using MessagePack;

namespace Jube.Data.Cache.Redis;

public class CacheSanctionRepository(IDatabaseAsync redisDatabase, ILog log) : ICacheSanctionRepository
{
    public async Task<CacheSanctionDto> GetByMultiPartStringDistanceThresholdAsync(int tenantRegistryId,
        int entityAnalysisModelId, string multiPartString,
        int distanceThreshold)
    {
        try
        {
            var redisKey = $"Sanction:{tenantRegistryId}:{entityAnalysisModelId}";
            var redisHSetKey = $"{multiPartString}:{distanceThreshold}";

            var hashValue = await redisDatabase.HashGetAsync(redisKey, redisHSetKey);

            if (!hashValue.HasValue) return null;

            var sanction = MessagePackSerializer
                .Deserialize<Sanction>(hashValue,
                    MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(false));

            return new CacheSanctionDto
            {
                CreatedDate = sanction.CreatedDate,
                Value = sanction.Value
            };
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }

        return null;
    }

    public async Task InsertAsync(int tenantRegistryId, int entityAnalysisModelId, string multiPartString,
        int distanceThreshold,
        double? value)
    {
        try
        {
            var redisKey = $"Sanction:{tenantRegistryId}:{entityAnalysisModelId}";
            var redisHSetKey = $"{multiPartString}:{distanceThreshold}";

            var sanction = new Sanction
            {
                Value = value,
                CreatedDate = DateTime.Now
            };

            var ms = new MemoryStream();
            await MessagePackSerializer.SerializeAsync(ms, sanction,
                MessagePackSerializerOptionsHelper.StandardMessagePackSerializerWithCompressionOptions(false));
            await redisDatabase.HashSetAsync(redisKey, redisHSetKey, ms.ToArray());
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }

    public async Task UpdateAsync(int tenantRegistryId, int entityAnalysisModelId, string multiPartString,
        int distanceThreshold,
        double? value)
    {
        try
        {
            await InsertAsync(tenantRegistryId, entityAnalysisModelId, multiPartString, distanceThreshold, value);
        }
        catch (Exception ex)
        {
            log.Error($"Cache Redis: Has created an exception as {ex}.");
        }
    }
}