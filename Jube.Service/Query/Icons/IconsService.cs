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

using System.ComponentModel;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Dto.Query.Icons;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.Query.Icons;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Query.Icons
{
    public sealed class IconsService
    {
        private static readonly int[] permissions = [24];

        private readonly ILog auditLog;
        private readonly IIconFileSource iconFileSource;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private IconsService(string userName, int tenantRegistryId, PermissionValidation permissionValidation,
            IIconFileSource iconFileSource, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            this.iconFileSource = iconFileSource;
        }

        public static Task<IconsService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IIconFileSource iconFileSource, CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus, iconFileSource,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<IconsService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            IIconFileSource iconFileSource, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(IconsResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("Icons.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[IconsResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"Icons.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[IconsResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new IconsService(userName, resolvedTenantRegistryId.Value, permissionValidation, iconFileSource,
                log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the icon files available to the user interface, as the file names found in the " +
                     "icons directory of the web root, in directory enumeration order. Read-only.")]
        [ServiceOperation("IconsGet", OperationKind.Read, Idempotent = true)]
        public Task<List<IconDto>> GetAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            using var op = OperationScope.Start("Icons", "Get", userName, tenantRegistryId, auditLog, log,
                serviceChangeBus);
            const string name = "Icons.Get";
            if (log.IsDebugEnabled)
            {
                log.Debug($"{name}: entry user={userName}");
            }

            try
            {
                EnsurePermitted(name);
                var icons = iconFileSource.GetFileNames().Select(IconsMapper.ToDto).ToList();
                op.Rows(icons.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"{name}: {icons.Count} rows user={userName}");
                }

                return Task.FromResult(icons);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");
                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"{name}: unexpected failure user={userName}", ex);
                throw;
            }
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

            throw new ForbiddenException(strings[IconsResources.PermissionDenied], permissions);
        }
    }
}