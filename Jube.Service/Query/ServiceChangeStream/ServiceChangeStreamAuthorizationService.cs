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
using Jube.Service.Exceptions.Query.ServiceChangeStream;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.ServiceChangeStream
{
    public sealed class ServiceChangeStreamAuthorizationService
    {
        private ServiceChangeStreamAuthorizationService(string userName, int tenantRegistryId)
        {
            UserName = userName;
            TenantRegistryId = tenantRegistryId;
        }

        public string UserName { get; }
        public int TenantRegistryId { get; }

        public static async Task<ServiceChangeStreamAuthorizationService> CreateAsync(DbContext dbContext,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(ServiceChangeStreamResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("ServiceChangeStream.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[ServiceChangeStreamResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"ServiceChangeStream.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[ServiceChangeStreamResources.NotAuthenticated]);
            }

            return new ServiceChangeStreamAuthorizationService(userName, resolvedTenantRegistryId.Value);
        }
    }
}