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
using Jube.Service.Query.VersionHistory;

namespace Jube.Service.Query.EntityAnalysisModelInlineFunctionVersionHistory
{
    public sealed class EntityAnalysisModelInlineFunctionVersionHistoryService : IVersionHistoryService<
        EntityAnalysisModelInlineFunctionVersionHistoryService, EntityAnalysisModelInlineFunctionVersion>
    {
        private static readonly int[] permissions = [8];

        private readonly GetEntityVersionHistoryQuery<EntityAnalysisModelInlineFunctionVersion> query;
        private readonly EntityAnalysisModelInlineFunctionRepository parentRepository;
        private readonly PermissionValidation permissionValidation;
        private readonly ILog log;
        private readonly IStringLocalizer strings;
        private readonly string userName;

        private EntityAnalysisModelInlineFunctionVersionHistoryService(DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, IStringLocalizer strings)
        {
            this.userName = userName;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.strings = strings;
            query = new GetEntityVersionHistoryQuery<EntityAnalysisModelInlineFunctionVersion>(dbContext,
                nameof(EntityAnalysisModelInlineFunctionVersion.EntityAnalysisModelInlineFunctionId));
            parentRepository = new EntityAnalysisModelInlineFunctionRepository(dbContext, tenantRegistryId);
        }

        public static Task<EntityAnalysisModelInlineFunctionVersionHistoryService> CreateAsync(DbContext dbContext,
            string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, CancellationToken token = default)
        {
            return CreateInternalAsync(dbContext, userName, log, stringLocalizerFactory, token);
        }

        private static async Task<EntityAnalysisModelInlineFunctionVersionHistoryService> CreateInternalAsync(
            DbContext dbContext, string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            CancellationToken token)
        {
            var strings = stringLocalizerFactory.Create(typeof(EntityVersionHistoryResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        "EntityAnalysisModelInlineFunctionVersionHistory.Create: no authenticated user; refusing.");
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
                        $"EntityAnalysisModelInlineFunctionVersionHistory.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[EntityVersionHistoryResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new EntityAnalysisModelInlineFunctionVersionHistoryService(dbContext, userName,
                resolvedTenantRegistryId.Value, permissionValidation, log, strings);
        }

        public async Task<EntityAnalysisModelInlineFunctionVersion?> GetByIdAsync(int id,
            CancellationToken token = default)
        {
            EnsurePermitted("EntityAnalysisModelInlineFunctionVersionHistory.GetById");
            var version = await query.ByIdAsync(id, token).ConfigureAwait(false);
            return await IsOwnedAsync(version, token).ConfigureAwait(false) ? version : null;
        }

        public async Task<List<EntityAnalysisModelInlineFunctionVersion>> GetByParentIdAsync(int parentId,
            CancellationToken token = default)
        {
            EnsurePermitted("EntityAnalysisModelInlineFunctionVersionHistory.GetByParentId");
            if (!await IsParentOwnedAsync(parentId, token).ConfigureAwait(false))
            {
                return [];
            }

            return await query.ByParentIdAsync(parentId, token).ConfigureAwait(false);
        }

        public async Task<List<EntityAnalysisModelInlineFunctionVersion>> GetByDateRangeAsync(int parentId,
            DateTime from, DateTime to,
            CancellationToken token = default)
        {
            EnsurePermitted("EntityAnalysisModelInlineFunctionVersionHistory.GetByDateRange");
            if (!await IsParentOwnedAsync(parentId, token).ConfigureAwait(false))
            {
                return [];
            }

            return await query.ByDateRangeAsync(parentId, from, to, token).ConfigureAwait(false);
        }

        public async Task<EntityAnalysisModelInlineFunctionVersion?> GetByIdAndDateRangeAsync(int id, DateTime from,
            DateTime to, CancellationToken token = default)
        {
            EnsurePermitted("EntityAnalysisModelInlineFunctionVersionHistory.GetByIdAndDateRange");
            var version = await query.ByIdAndDateRangeAsync(id, from, to, token).ConfigureAwait(false);
            return await IsOwnedAsync(version, token).ConfigureAwait(false) ? version : null;
        }

        public async Task<EntityAnalysisModelInlineFunctionVersion?> GetLatestAsync(int parentId,
            CancellationToken token = default)
        {
            EnsurePermitted("EntityAnalysisModelInlineFunctionVersionHistory.GetLatest");
            if (!await IsParentOwnedAsync(parentId, token).ConfigureAwait(false))
            {
                return null;
            }

            return await query.LatestAsync(parentId, token).ConfigureAwait(false);
        }

        public async Task<IReadOnlyList<VersionFieldChange>> CompareAsync(int fromId, int toId,
            CancellationToken token = default)
        {
            EnsurePermitted("EntityAnalysisModelInlineFunctionVersionHistory.Compare");
            var from = await query.ByIdAsync(fromId, token).ConfigureAwait(false);
            var to = await query.ByIdAsync(toId, token).ConfigureAwait(false);

            if (!await IsOwnedAsync(from, token).ConfigureAwait(false))
            {
                from = null;
            }

            if (!await IsOwnedAsync(to, token).ConfigureAwait(false))
            {
                to = null;
            }

            return VersionDiff.Compare(from, to);
        }

        private async Task<bool> IsOwnedAsync(EntityAnalysisModelInlineFunctionVersion? version,
            CancellationToken token)
        {
            if (version is null)
            {
                return false;
            }

            var parentId = query.ParentIdOf(version);
            return parentId.HasValue && await IsParentOwnedAsync(parentId.Value, token).ConfigureAwait(false);
        }

        private async Task<bool> IsParentOwnedAsync(int parentId, CancellationToken token)
        {
            return await parentRepository.GetByIdAsync(parentId, token).ConfigureAwait(false) is not null;
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Validate(permissions))
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
            }

            throw new ForbiddenException(strings[EntityVersionHistoryResources.PermissionDenied], permissions);
        }
    }
}