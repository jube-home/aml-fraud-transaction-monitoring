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

using Jube.Data.Context;
using Jube.Data.Poco;
using Jube.Data.Query;
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Exceptions.Query.EntityVersionHistory;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.TenantRegistryVersionHistory
{
    public sealed class TenantRegistryVersionHistoryService
    {
        private readonly GetEntityVersionHistoryQuery<TenantRegistryVersion> query;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly IStringLocalizer strings;
        private readonly string userName;

        private TenantRegistryVersionHistoryService(DbContext dbContext, string userName,
            PermissionValidation permissionValidation, ILog log, IStringLocalizer strings)
        {
            this.userName = userName;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.strings = strings;
            query = new GetEntityVersionHistoryQuery<TenantRegistryVersion>(dbContext,
                nameof(TenantRegistryVersion.TenantRegistryId));
        }

        public static Task<TenantRegistryVersionHistoryService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, CancellationToken token = default)
        {
            return CreateInternalAsync(dbContext, userName, log, stringLocalizerFactory, token);
        }

        private static async Task<TenantRegistryVersionHistoryService> CreateInternalAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory, CancellationToken token)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityVersionHistoryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("TenantRegistryVersionHistory.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityVersionHistoryResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"TenantRegistryVersionHistory.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityVersionHistoryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new TenantRegistryVersionHistoryService(dbContext, userName, permissionValidation, log, strings);
        }

        public Task<TenantRegistryVersion> GetByIdAsync(int id, CancellationToken token = default)
        {
            EnsureLandlord("TenantRegistryVersionHistory.GetById");
            return query.ByIdAsync(id, token);
        }

        public Task<List<TenantRegistryVersion>> GetByParentIdAsync(int parentId, CancellationToken token = default)
        {
            EnsureLandlord("TenantRegistryVersionHistory.GetByParentId");
            return query.ByParentIdAsync(parentId, token);
        }

        public Task<List<TenantRegistryVersion>> GetByDateRangeAsync(int parentId, DateTime from, DateTime to,
            CancellationToken token = default)
        {
            EnsureLandlord("TenantRegistryVersionHistory.GetByDateRange");
            return query.ByDateRangeAsync(parentId, from, to, token);
        }

        public Task<TenantRegistryVersion> GetByIdAndDateRangeAsync(int id, DateTime from, DateTime to,
            CancellationToken token = default)
        {
            EnsureLandlord("TenantRegistryVersionHistory.GetByIdAndDateRange");
            return query.ByIdAndDateRangeAsync(id, from, to, token);
        }

        public Task<TenantRegistryVersion> GetLatestAsync(int parentId, CancellationToken token = default)
        {
            EnsureLandlord("TenantRegistryVersionHistory.GetLatest");
            return query.LatestAsync(parentId, token);
        }

        public Task<IReadOnlyList<VersionFieldChange>> CompareAsync(int fromId, int toId,
            CancellationToken token = default)
        {
            EnsureLandlord("TenantRegistryVersionHistory.Compare");
            return query.CompareAsync(fromId, toId, token);
        }

        private void EnsureLandlord(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (caller's tenant is not Landlord) user={userName}");
            }

            throw new ForbiddenException(strings[EntityVersionHistoryResources.PermissionDenied], []);
        }
    }
}