/* Copyright (C) 2022-present Jube Holdings Limited.
 *
 * This file is part of Jube™ software.

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
using Jube.Dto.Payload;
using Jube.Dto.RespKeyActivity;
using Jube.Resources;
using Jube.Service.Agent.Models;
using Jube.Service.Exceptions.RespKeyActivity;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.RespKeyActivity
{
    public sealed class RespKeyActivityService
    {
        private static readonly int[] permissions = [];
        private readonly ILog auditLog;
        private readonly string connectionString;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private RespKeyActivityService(string connectionString, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.connectionString = connectionString;
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
        }

        public static IReadOnlyList<int> RequiredPermissions => permissions;
        public static bool RequiresLandlord => true;

        public static Task<RespKeyActivityService> CreateAsync(DbContext dbContext, string connectionString,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, CancellationToken token = default)
        {
            return CreateAsync(dbContext, connectionString, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<RespKeyActivityService> CreateAsync(DbContext dbContext, string connectionString,
            string? userName, ILog log, IStringLocalizerFactory stringLocalizerFactory,
            IServiceChangeBus serviceChangeBus, ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(RespKeyActivityResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("RespKeyActivity.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[RespKeyActivityResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"RespKeyActivity.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[RespKeyActivityResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new RespKeyActivityService(connectionString, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Samples up to 100 keys from the live RESP keyspace (SCAN, never the blocking KEYS) and " +
                     "reports each one's type, idle time (OBJECT IDLETIME) and, when maxmemory-policy is an " +
                     "LFU policy, its approximate access frequency (OBJECT FREQ) -- otherwise Frequency is " +
                     "null, which is RESP itself refusing a meaningless question, not a missing value. The " +
                     "100-key cap is fixed and cannot be raised: this is a live, SCAN-based sample for " +
                     "spotting a hot/cold key design problem, never a way to enumerate or dump the keyspace. " +
                     "Optional glob pattern (RESP SCAN MATCH syntax, e.g. 'session:*') narrows the sample to " +
                     "keys worth looking at rather than whatever SCAN happens to return first. Read-only.")]
        [ServiceOperation("RespKeyActivityList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<RespKeyActivityDto>> ListAsync(
            [Description("RESP SCAN MATCH glob pattern, e.g. 'session:*'; null or empty samples the whole keyspace.")]
            string? pattern = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("RespKeyActivity", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);

            if (log.IsDebugEnabled)
            {
                log.Debug($"RespKeyActivity.List: entry user={userName}");
            }

            try
            {
                EnsurePermitted("RespKeyActivity.List");

                using var repository = new RespKeyActivityRepository(connectionString);
                var rows = await repository.SampleAsync(pattern, token).ConfigureAwait(false);

                var dtos = rows.Select(r => new RespKeyActivityDto(
                    r.Key, r.Type, r.IdleTimeSeconds, r.Frequency, r.TimeToLiveSeconds)).ToList();

                op.Rows(dtos.Count);

                if (log.IsDebugEnabled)
                {
                    log.Debug($"RespKeyActivity.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<RespKeyActivityDto>(dtos, dtos.Count, new PayloadStatistics(new()));
            }
            catch (ForbiddenException)
            {
                op.Outcome("forbidden");
                throw;
            }
            catch (OperationCanceledException)
            {
                op.Outcome("cancelled");

                if (log.IsDebugEnabled)
                {
                    log.Debug($"RespKeyActivity.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"RespKeyActivity.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private void EnsurePermitted(string op)
        {
            if (permissionValidation.Landlord)
            {
                return;
            }

            if (log.IsWarnEnabled)
            {
                log.Warn($"{op}: permission denied (landlord only) user={userName}");
            }

            throw new ForbiddenException(strings[RespKeyActivityResources.PermissionDenied], permissions);
        }
    }
}