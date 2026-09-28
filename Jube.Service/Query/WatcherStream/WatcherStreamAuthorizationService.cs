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
using Jube.Data.Repository;
using Jube.Resources;
using Jube.Service.Exceptions.Query.WatcherStream;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.WatcherStream
{
    public sealed class WatcherStreamAuthorizationService
    {
        private static readonly int[] permissions = [30];

        private WatcherStreamAuthorizationService(string userName, int tenantRegistryId)
        {
            UserName = userName;
            TenantRegistryId = tenantRegistryId;
        }

        public string UserName { get; }
        public int TenantRegistryId { get; }

        public static async Task<WatcherStreamAuthorizationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(WatcherStreamResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("WatcherStream.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[WatcherStreamResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"WatcherStream.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[WatcherStreamResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            if (!permissionValidation.Validate(permissions))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn(
                        $"WatcherStream.Create: permission denied user={userName} specs=[{string.Join(",", permissions)}]");
                }

                throw new ForbiddenException(strings[WatcherStreamResources.PermissionDenied], permissions);
            }

            return new WatcherStreamAuthorizationService(userName, resolvedTenantRegistryId.Value);
        }
    }
}