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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Repository.Exceptions;
using LinqToDB;
using LinqToDB.Data;
using Jube.Analytics.Datasets;

namespace Jube.Data.Repository
{
    public class AdvancedAnalyticsCacheRepository(
        DbContext dbContext,
        int tenantRegistryId,
        string userName,
        AdvancedAnalyticsCacheLimits limits)
    {
        private const string InsertWithinTenantLimitSql = """
                                                          INSERT INTO "AdvancedAnalyticsCache"
                                                              ("Guid", "TenantRegistryId", "Data", "Rows", "Columns",
                                                               "PackingVersion", "SizeBytes", "CreatedDate",
                                                               "CreatedUser", "ExpiryDate")
                                                          SELECT @Guid, @TenantRegistryId, @Data, @Rows, @Columns,
                                                                 @PackingVersion, @SizeBytes, @Now, @CreatedUser,
                                                                 @ExpiryDate
                                                          WHERE (SELECT COALESCE(SUM("SizeBytes"), 0)
                                                                 FROM "AdvancedAnalyticsCache"
                                                                 WHERE "TenantRegistryId" = @TenantRegistryId
                                                                   AND "ExpiryDate" > @Now) + @SizeBytes
                                                                <= @MaxTenantBytes
                                                          """;

        private const string DeleteExpiredForTenantSql = """
                                                         DELETE FROM "AdvancedAnalyticsCache"
                                                         WHERE "TenantRegistryId" = @TenantRegistryId
                                                           AND "ExpiryDate" <= @Now
                                                         """;

        private const string DeleteExpiredAllTenantsSql = """
                                                          DELETE FROM "AdvancedAnalyticsCache"
                                                          WHERE "ExpiryDate" <= @Now
                                                          """;

        private const string TenantBytesSql = """
                                              SELECT COALESCE(SUM("SizeBytes"), 0)
                                              FROM "AdvancedAnalyticsCache"
                                              WHERE "TenantRegistryId" = @TenantRegistryId
                                                AND "ExpiryDate" > @Now
                                              """;

        private readonly AdvancedAnalyticsCacheLimits cacheLimits = limits ?? new AdvancedAnalyticsCacheLimits();

        public AdvancedAnalyticsCacheRepository(DbContext dbContext, int tenantRegistryId, string userName)
            : this(dbContext, tenantRegistryId, userName, new AdvancedAnalyticsCacheLimits())
        {
        }

        public async Task<AdvancedAnalyticsCache> PutAsync(byte[] data, int rows, int columns,
            int packingVersion = AdvancedAnalyticsPacking.CurrentVersion, TimeSpan? ttl = null,
            CancellationToken token = default)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (rows < 0 || columns < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rows), "Shape cannot be negative.");
            }

            if (packingVersion == AdvancedAnalyticsPacking.CurrentVersion
                && (long)rows * columns * sizeof(double) != data.Length)
            {
                throw new ArgumentException("Shape does not match the length of the packed data.", nameof(data));
            }

            if (data.Length > cacheLimits.MaxEntryBytes)
            {
                throw new AdvancedAnalyticsCacheLimitExceededException(
                    $"Entry of {data.Length} bytes exceeds the maximum entry size of {cacheLimits.MaxEntryBytes} bytes.");
            }

            var lifetime = ttl ?? cacheLimits.Ttl;

            if (lifetime <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(ttl), "Time to live must be positive.");
            }

            var now = DateTime.UtcNow;
            var guid = Guid.NewGuid();
            var expiry = now.Add(lifetime);

            await DeleteExpiredAsync(token).ConfigureAwait(false);

            var inserted = await new CommandInfo(dbContext, InsertWithinTenantLimitSql,
                    new DataParameter("Guid", guid, DataType.Guid),
                    new DataParameter("TenantRegistryId", tenantRegistryId, DataType.Int32),
                    new DataParameter("Data", data, DataType.VarBinary),
                    new DataParameter("Rows", rows, DataType.Int32),
                    new DataParameter("Columns", columns, DataType.Int32),
                    new DataParameter("PackingVersion", packingVersion, DataType.Int32),
                    new DataParameter("SizeBytes", (long)data.Length, DataType.Int64),
                    new DataParameter("Now", now, DataType.DateTime),
                    new DataParameter("CreatedUser", userName, DataType.Text),
                    new DataParameter("ExpiryDate", expiry, DataType.DateTime),
                    new DataParameter("MaxTenantBytes", cacheLimits.MaxTenantBytes, DataType.Int64))
                .ExecuteAsync(token).ConfigureAwait(false);

            if (inserted == 0)
            {
                throw new AdvancedAnalyticsCacheLimitExceededException(
                    $"Tenant cache would exceed the maximum of {cacheLimits.MaxTenantBytes} bytes.");
            }

            return await GetAsync(guid, token).ConfigureAwait(false);
        }

        public Task<AdvancedAnalyticsCache> GetAsync(Guid guid, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.AdvancedAnalyticsCache.FirstOrDefaultAsync(w =>
                w.Guid == guid && w.TenantRegistryId == tenantRegistryId && w.ExpiryDate > now, token);
        }

        public Task<AdvancedAnalyticsCache> GetMetadataAsync(Guid guid, CancellationToken token = default)
        {
            var now = DateTime.UtcNow;

            return dbContext.AdvancedAnalyticsCache
                .Where(w => w.Guid == guid && w.TenantRegistryId == tenantRegistryId && w.ExpiryDate > now)
                .Select(s => new AdvancedAnalyticsCache
                {
                    Id = s.Id,
                    Guid = s.Guid,
                    TenantRegistryId = s.TenantRegistryId,
                    Rows = s.Rows,
                    Columns = s.Columns,
                    PackingVersion = s.PackingVersion,
                    SizeBytes = s.SizeBytes,
                    CreatedDate = s.CreatedDate,
                    CreatedUser = s.CreatedUser,
                    ExpiryDate = s.ExpiryDate
                })
                .FirstOrDefaultAsync(token);
        }

        public async Task<bool> DeleteAsync(Guid guid, CancellationToken token = default)
        {
            var records = await dbContext.AdvancedAnalyticsCache
                .Where(w => w.Guid == guid && w.TenantRegistryId == tenantRegistryId)
                .DeleteAsync(token).ConfigureAwait(false);

            return records > 0;
        }

        public Task<int> DeleteExpiredAsync(CancellationToken token = default)
        {
            return new CommandInfo(dbContext, DeleteExpiredForTenantSql,
                    new DataParameter("TenantRegistryId", tenantRegistryId, DataType.Int32),
                    new DataParameter("Now", DateTime.UtcNow, DataType.DateTime))
                .ExecuteAsync(token);
        }

        public Task<int> DeleteExpiredAllTenantsAsync(CancellationToken token = default)
        {
            return new CommandInfo(dbContext, DeleteExpiredAllTenantsSql,
                    new DataParameter("Now", DateTime.UtcNow, DataType.DateTime))
                .ExecuteAsync(token);
        }

        public Task<long> GetTenantBytesAsync(CancellationToken token = default)
        {
            return new CommandInfo(dbContext, TenantBytesSql,
                    new DataParameter("TenantRegistryId", tenantRegistryId, DataType.Int32),
                    new DataParameter("Now", DateTime.UtcNow, DataType.DateTime))
                .ExecuteAsync<long>(token);
        }
    }
}