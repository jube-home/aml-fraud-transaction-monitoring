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
using Jube.Dto.Payload;
using Jube.Dto.UserLogout;
using Jube.Resources;
using Jube.Service.Agent;
using Jube.Service.Exceptions.UserLogout;
using Jube.Service.Observability;
using Jube.Service.Reactivity.Interfaces;
using Jube.Service.Security;
using log4net;
using Microsoft.Extensions.Localization;

namespace Jube.Service.UserLogout
{
    public sealed class UserLogoutService
    {
        private const int MaxListTake = 100000;
        private static readonly int[] permissions = [35];
        private readonly ILog auditLog;
        private readonly ILog log;
        private readonly PermissionValidation permissionValidation;
        private readonly UserLogoutRepository repository;
        private readonly IServiceChangeBus serviceChangeBus;
        private readonly IStringLocalizer strings;
        private readonly int tenantRegistryId;
        private readonly string userName;

        private UserLogoutService(DbContext dbContext, string userName, int tenantRegistryId,
            PermissionValidation permissionValidation, ILog log, ILog auditLog, IServiceChangeBus serviceChangeBus,
            IStringLocalizer strings)
        {
            this.log = log;
            this.auditLog = auditLog;
            this.serviceChangeBus = serviceChangeBus;
            this.strings = strings;
            this.userName = userName;
            this.tenantRegistryId = tenantRegistryId;
            this.permissionValidation = permissionValidation;
            repository = new UserLogoutRepository(dbContext, permissionValidation.Landlord ? null : tenantRegistryId);
        }

        public static Task<UserLogoutService> CreateAsync(DbContext dbContext, string? userName, ILog log,
            IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            CancellationToken token = default)
        {
            return CreateAsync(dbContext, userName, log, stringLocalizerFactory, serviceChangeBus,
                LogManager.GetLogger("Jube.Audit"), token);
        }

        internal static async Task<UserLogoutService> CreateAsync(DbContext dbContext, string? userName,
            ILog log, IStringLocalizerFactory stringLocalizerFactory, IServiceChangeBus serviceChangeBus,
            ILog auditLog, CancellationToken token = default)
        {
            var strings = stringLocalizerFactory.Create(typeof(UserLogoutResources));

            if (string.IsNullOrWhiteSpace(userName))
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn("UserLogout.Create: no authenticated user; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserLogoutResources.NotAuthenticated]);
            }

            var resolvedTenantRegistryId = await UserInTenantRepository
                .GetTenantRegistryIdAsync(dbContext, userName, token).ConfigureAwait(false);

            if (resolvedTenantRegistryId is null)
            {
                if (log.IsWarnEnabled)
                {
                    log.Warn($"UserLogout.Create: user '{userName}' resolves to no tenant; refusing.");
                }

                throw new NotAuthenticatedException(strings[UserLogoutResources.NotAuthenticated]);
            }

            var permissionValidation = await PermissionValidation.CreateAsync(dbContext, userName, log, token)
                .ConfigureAwait(false);

            return new UserLogoutService(dbContext, userName, resolvedTenantRegistryId.Value,
                permissionValidation, log, auditLog, serviceChangeBus, strings);
        }

        [Description("Lists the Logout Audit Trail -- every time a session was cut: the user logged out, an " +
                     "administrator revoked the user's tokens, or a password change revoked the earlier sessions " +
                     "-- ordered per sortField/sortDirection (most recent first by default), capped at 'take' rows " +
                     "(max 100000, default 100000). Each of from/to defaults independently to the last hour when " +
                     "omitted. Scoped to the caller's own tenant's users; a landlord sees every tenant. Optionally " +
                     "restrict to a date range (by CreatedDate) and/or a case-insensitive substring search against " +
                     "CreatedUser, CutByUser, RemoteIp, UserAgent or Message. Optionally apply samplePercentage on " +
                     "top of every other filter to draw a random subset instead of the most recent rows. " +
                     "Statistics is always empty for this DTO -- ReasonId and OutcomeId are categorical codes, not " +
                     "continuous/measured quantities.")]
        [ServiceOperation("UserLogoutList", OperationKind.Read, Idempotent = true)]
        public async Task<PayloadResult<UserLogoutDto>> ListAsync(
            [Description("Maximum number of rows to return; clamped to 100000.")]
            int take = 100000,
            [Description(
                "Only include rows with CreatedDate on or after this UTC timestamp; defaults to 1 hour before the current time when omitted.")]
            DateTime? from = null,
            [Description(
                "Only include rows with CreatedDate on or before this UTC timestamp; defaults to the current time when omitted.")]
            DateTime? to = null,
            [Description(
                "Case-insensitive substring match against CreatedUser, CutByUser, RemoteIp, UserAgent or Message; null or empty matches every row.")]
            string? search = null,
            [Description(
                "Percentage (0-100) chance of including each row that already matches every other filter, evaluated independently per row -- e.g. 10 returns roughly a random tenth of the matching rows.")]
            double? samplePercentage = null,
            [Description(
                "Column to sort by: createdDate, createdUser, reasonId, outcomeId, remoteIp, localIp, userAgent, sessionStartDate, cutByUser or message. Unrecognised or omitted sorts by Id descending.")]
            string? sortField = null,
            [Description(
                "Sort direction, case-insensitive: 'asc' for ascending, anything else (including omitted) for descending.")]
            string? sortDirection = null,
            CancellationToken token = default)
        {
            using var op = OperationScope.Start("UserLogout", "List", userName, tenantRegistryId, auditLog,
                log, serviceChangeBus);
            var clampedTake = Math.Clamp(take, 1, MaxListTake);
            var clampedSamplePercentage = samplePercentage.HasValue
                ? Math.Clamp(samplePercentage.Value, 0.0, 100.0)
                : (double?)null;
            from ??= DateTime.UtcNow.AddHours(-1);
            to ??= DateTime.UtcNow;
            if (log.IsDebugEnabled)
            {
                log.Debug($"UserLogout.List: entry take={clampedTake} user={userName}");
            }

            try
            {
                EnsurePermitted("UserLogout.List");

                var rows = await repository.GetLastAsync(clampedTake, from, to, search, clampedSamplePercentage,
                    sortField, sortDirection, token).ConfigureAwait(false);
                var total = await repository.CountAsync(from, to, search, clampedSamplePercentage, token)
                    .ConfigureAwait(false);
                var statistics = await repository.GetStatisticsAsync(from, to, search, clampedSamplePercentage,
                    MaxListTake, token).ConfigureAwait(false);

                var dtos = rows.Select(r =>
                {
                    var created = r.CreatedDate.GetValueOrDefault();
                    return new UserLogoutDto(
                        r.Id, created, r.CreatedUser, r.TenantRegistryId, r.ReasonId, DescribeReason(r.ReasonId),
                        r.OutcomeId, DescribeOutcome(r.OutcomeId), r.RemoteIp, r.LocalIp, r.UserAgent,
                        r.SessionStartDate,
                        r.SessionStartDate.HasValue
                            ? (long)Math.Max(0, (created - r.SessionStartDate.Value).TotalSeconds)
                            : null,
                        r.CutByUser, r.Message);
                }).ToList();

                op.Rows(dtos.Count);
                if (log.IsDebugEnabled)
                {
                    log.Debug($"UserLogout.List: {dtos.Count} rows user={userName}");
                }

                return new PayloadResult<UserLogoutDto>(dtos, total, statistics);
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
                    log.Debug($"UserLogout.List: cancelled user={userName}");
                }

                throw;
            }
            catch (Exception ex)
            {
                op.Error(ex);
                log.Error($"UserLogout.List: unexpected failure user={userName}", ex);
                throw;
            }
        }

        private static string DescribeReason(int reasonId)
        {
            return reasonId switch
            {
                1 => "User logout",
                2 => "Tokens revoked by an administrator",
                3 => "Password change",
                _ => "(unknown)"
            };
        }

        private static string DescribeOutcome(int outcomeId)
        {
            return outcomeId switch
            {
                1 => "Revoked",
                2 => "No session (cookies cleared only)",
                3 => "Revocation failed (cookies cleared)",
                _ => "(unknown)"
            };
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

            throw new ForbiddenException(strings[UserLogoutResources.PermissionDenied], permissions);
        }
    }
}