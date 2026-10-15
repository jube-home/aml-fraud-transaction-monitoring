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

using System.Runtime.CompilerServices;
using Jube.Data.Context;
using Jube.Data.Repository;
using Jube.Service.Exceptions.AdvancedAnalytics;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.Repository.AdvancedAnalyticsOperation
{
    internal sealed class AdvancedAnalyticsOperationRunner
    {
        private readonly PermissionValidation permissionValidation;

        private AdvancedAnalyticsOperationRunner(string area, DbContext dbContext, string userName,
            int tenantRegistryId, PermissionValidation permissionValidation, ILog log, ILog auditLog,
            IServiceChangeBus serviceChangeBus, IStringLocalizer strings)
        {
            Area = area;
            DbContext = dbContext;
            UserName = userName;
            TenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            Log = log;
            AuditLog = auditLog;
            ServiceChangeBus = serviceChangeBus;
            Strings = strings;
        }

        public string Area { get; }
        public DbContext DbContext { get; }
        public string UserName { get; }
        public int TenantRegistryId { get; }
        public ILog Log { get; }
        public ILog AuditLog { get; }
        public IServiceChangeBus ServiceChangeBus { get; }
        public IStringLocalizer Strings { get; }

        public static async Task<AdvancedAnalyticsOperationRunner> CreateAsync(string area, Type resourceType,
            string notAuthenticatedKey, DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus, ILog auditLog,
            CancellationToken token)
        {
            var strings = stringLocalizerFactory.Create(resourceType);

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{area}.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[notAuthenticatedKey]);
            }

            var tenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (tenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"{area}.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[notAuthenticatedKey]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new AdvancedAnalyticsOperationRunner(area, dbContext, userName, tenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        public bool Has(int[] specifications)
        {
            return permissionValidation.Validate(specifications);
        }

        public void EnsurePermitted(int[] specifications, string operation, string permissionDeniedKey)
        {
            if (Has(specifications))
            {
                return;
            }

            if (Log.IsWarnEnabled)
            {
                Log.Warn($"{Area}.{operation}: permission denied user={UserName} " +
                         $"specs=[{string.Join(",", specifications)}]");
            }

            throw new ForbiddenException(Strings[permissionDeniedKey], specifications);
        }

        public async Task<T> RunAsync<T>(string operation, Func<OperationScope, Task<T>> body,
            [CallerMemberName] string caller = "")
        {
            using var op = OperationScope.Start(Area, operation, UserName, TenantRegistryId, AuditLog, Log,
                ServiceChangeBus, caller);

            if (Log.IsDebugEnabled)
            {
                Log.Debug($"{Area}.{operation}: entry user={UserName}");
            }

            try
            {
                return await body(op).ConfigureAwait(false);
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (DtoValidationException ex)
            {
                op.Outcome("invalid");

                if (Log.IsWarnEnabled)
                {
                    Log.Warn($"{Area}.{operation}: validation failed user={UserName} " +
                             $"props=[{string.Join(",", ex.Result.Errors.Select(e => e.PropertyName).Distinct())}]");
                }

                throw;
            }
            catch (NotFoundException)
            {
                op.Outcome("notfound");
                throw;
            }
            catch (ConflictException)
            {
                op.Outcome("conflict");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (Log.IsDebugEnabled)
                {
                    Log.Debug($"{Area}.{operation}: cancelled user={UserName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                Log.Error($"{Area}.{operation}: unexpected failure user={UserName}", ex);
                throw;
            }
        }

        public Task RunAsync(string operation, Func<OperationScope, Task> body,
            [CallerMemberName] string caller = "")
        {
            return RunAsync(operation, async op =>
            {
                await body(op).ConfigureAwait(false);

                return true;
            }, caller);
        }

        public void Info(string message)
        {
            if (Log.IsInfoEnabled)
            {
                Log.Info(message);
            }
        }
    }
}