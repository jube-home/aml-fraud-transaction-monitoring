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
using Jube.Service.Exceptions.Query.GridModel;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.GridModel
{
    public sealed class GridModelAuthorizationService
    {
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IStringLocalizer strings;

        private GridModelAuthorizationService(string userName, PermissionValidation permissionValidation, ILog log,
            IStringLocalizer strings)
        {
            UserName = userName;
            this.permissionValidation = permissionValidation;
            this.log = log;
            this.strings = strings;
        }

        public string UserName { get; }

        public static async Task<GridModelAuthorizationService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(GridModelResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("GridModel.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[GridModelResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"GridModel.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[GridModelResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new GridModelAuthorizationService(userName, permissionValidation, log, strings);
        }

        public bool Permits(string area)
        {
            var (landlordOnly, permissions) = GridModelAccess.Resolve(area);

            return landlordOnly ? permissionValidation.Landlord : permissionValidation.Validate(permissions);
        }

        public void EnsurePermitted(string area)
        {
            if (Permits(area))
            {
                return;
            }

            var (_, permissions) = GridModelAccess.Resolve(area);

            if (log.IsWarnEnabled)
            {
                log.Warn($"GridModel.{area}: permission denied user={UserName}");
            }

            throw new ForbiddenException(strings[GridModelResources.PermissionDenied], permissions);
        }
    }
}
